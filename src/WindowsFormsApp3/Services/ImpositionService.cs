using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using iText.IO.Font.Constants;
using iText.Kernel.Pdf;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 印刷排版服务实现
    /// 提供平张和卷装材料的布局计算、空白页补充等核心排版功能
    /// </summary>
    public class ImpositionService : IImpositionService
    {
        #region 私有字段

        private readonly IPdfInfoProvider _pdfInfoProvider;

        /// <summary>
        /// 判定页面尺寸是否为正方形（容差0.09mm）
        /// </summary>
        private static bool IsSquareDimension(float width, float height, float tolerance = 0.09f)
        {
            return Math.Abs(width - height) <= tolerance;
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pdfInfoProvider">PDF信息提供者</param>
        public ImpositionService(IPdfInfoProvider pdfInfoProvider = null)
        {
            _pdfInfoProvider = pdfInfoProvider ?? new PdfInfoProvider();
        }

        #endregion

        #region 核心布局计算

        public async Task<List<ImpositionResult>> GetLayoutChoicesAsync(
            object config, ImpositionPdfInfo pdfInfo, int layoutQuantity = 0,
            CancellationToken cancellationToken = default)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (pdfInfo == null) throw new ArgumentNullException(nameof(pdfInfo));
            var results = new List<ImpositionResult>();
            if (layoutQuantity < 0)
                return new List<ImpositionResult> { CreateFailedResult("每纸布局数量请输入正整数。", config is FlatSheetConfiguration ? MaterialType.FlatSheet : MaterialType.RollMaterial) };

            var rowChoices = new SortedSet<int>();
            if (layoutQuantity == 0) rowChoices.Add(1);
            // 仅枚举因子，长卷的大数量不会触发逐页循环。
            for (int divisor = 1; layoutQuantity > 0 && divisor <= layoutQuantity / divisor; divisor++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (layoutQuantity % divisor != 0) continue;
                rowChoices.Add(divisor);
                rowChoices.Add(layoutQuantity / divisor);
            }
            foreach (int rows in rowChoices)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (layoutQuantity > 0 && layoutQuantity % rows != 0) continue;
                // 每个候选均走生产排版服务，预览不另建一套尺寸和约束算法。
                if (config is FlatSheetConfiguration flat)
                {
                    var candidate = new FlatSheetConfiguration
                    {
                        PaperWidth = flat.PaperWidth, PaperHeight = flat.PaperHeight,
                        MarginTop = flat.MarginTop, MarginBottom = flat.MarginBottom,
                        MarginLeft = flat.MarginLeft, MarginRight = flat.MarginRight,
                        RotationMode = flat.RotationMode, LayoutMultiple = flat.LayoutMultiple,
                        LayoutMultipleDirection = flat.LayoutMultipleDirection,
                        Rows = layoutQuantity > 0 ? rows : flat.Rows,
                        Columns = layoutQuantity > 0 ? layoutQuantity / rows : flat.Columns
                    };
                    results.Add(await CalculateFlatSheetLayoutAsync(candidate, pdfInfo, cancellationToken));
                }
                else if (config is RollMaterialConfiguration roll)
                {
                    var candidate = new RollMaterialConfiguration
                    {
                        FixedWidth = roll.FixedWidth, MinLength = roll.MinLength,
                        MarginTop = roll.MarginTop, MarginBottom = roll.MarginBottom,
                        MarginLeft = roll.MarginLeft, MarginRight = roll.MarginRight,
                        RotationMode = roll.RotationMode, LayoutMultiple = roll.LayoutMultiple,
                        LayoutMultipleDirection = roll.LayoutMultipleDirection,
                        UseExactRows = layoutQuantity > 0 || roll.UseExactRows,
                        Rows = layoutQuantity > 0 ? rows : roll.Rows,
                        Columns = layoutQuantity > 0 ? layoutQuantity / rows : roll.Columns
                    };
                    results.Add(await CalculateRollMaterialLayoutAsync(candidate, pdfInfo, cancellationToken));
                }
                else throw new ArgumentException("不支持的排版配置。", nameof(config));
            }
            var feasible = results.Where(r => r.Success && r.IsPrintable &&
                (layoutQuantity == 0 || r.OptimalLayoutQuantity == layoutQuantity)).ToList();
            if (feasible.Count > 0) return feasible;
            return new List<ImpositionResult>
            {
                layoutQuantity > 0
                    ? CreateFailedResult($"布局数 {layoutQuantity} 没有适合当前纸张或幅宽及页面方向的行列组合。", config is FlatSheetConfiguration ? MaterialType.FlatSheet : MaterialType.RollMaterial)
                    : results.First()
            };
        }

        private static string ValidateLayoutMultiple(int rows, int columns, int multiple, CopyMode direction)
        {
            if (multiple < 1 || multiple > 16) return "布局倍数请输入 2–16 的整数，关闭约束时使用 1。";
            if (multiple == 1) return null;
            bool byRow = direction == CopyMode.AutoByRow || direction == CopyMode.FixedNoRotationByRow;
            int value = byRow ? rows : columns;
            return value > 0 && value % multiple != 0
                ? $"手动{(byRow ? "行" : "列")}数 {value} 不满足 {multiple} 的倍数约束。请调整布局、倍数或关闭约束。"
                : null;
        }

        private static void ApplyLayoutMultiple(ref int rows, ref int columns, int requestedRows,
            int requestedColumns, int multiple, CopyMode direction, bool isRoll = false)
        {
            if (multiple <= 1) return;
            bool byRow = direction == CopyMode.AutoByRow || direction == CopyMode.FixedNoRotationByRow;
            if (byRow && requestedRows == 0)
                rows = isRoll ? ((rows + multiple - 1) / multiple) * multiple : rows - rows % multiple;
            else if (!byRow && requestedColumns == 0)
                columns -= columns % multiple;
        }

        /// <summary>
        /// 计算平张模式的布局
        /// </summary>
        /// <param name="config">平张模式配置</param>
        /// <param name="pdfInfo">PDF文件信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>布局计算结果</returns>
        public async Task<ImpositionResult> CalculateFlatSheetLayoutAsync(
            FlatSheetConfiguration config,
            ImpositionPdfInfo pdfInfo,
            CancellationToken cancellationToken = default)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (pdfInfo == null)
                throw new ArgumentNullException(nameof(pdfInfo));

            return await Task.Run(() =>
            {
                var startTime = DateTime.Now;

                try
                {
                    // 记录平张布局计算开始
                    LogHelper.Debug($"[ImpositionService] 开始平张布局计算: 纸张尺寸={config.PaperWidth}×{config.PaperHeight}mm, PDF尺寸={pdfInfo.GetEffectivePageWidth():F1}×{pdfInfo.GetEffectivePageHeight():F1}mm");

                    // 验证配置
                    var validationResult = ValidateFlatSheetConfiguration(config);
                    if (!validationResult.IsValid)
                    {
                        return new ImpositionResult
                        {
                            Success = false,
                            ErrorMessage = validationResult.ErrorMessage,
                            MaterialType = MaterialType.FlatSheet
                        };
                    }

                    string constraintError = ValidateLayoutMultiple(config.Rows, config.Columns, config.LayoutMultiple, config.LayoutMultipleDirection);
                    if (constraintError != null) return CreateFailedResult(constraintError, MaterialType.FlatSheet);

                    // 计算可用空间
                    float usableWidth = config.PrintableWidth;
                    float usableHeight = config.PrintableHeight;

                    // 获取页面尺寸
                    float pageWidth = pdfInfo.GetEffectivePageWidth();
                    float pageHeight = pdfInfo.GetEffectivePageHeight();

                    if (pageWidth <= 0 || pageHeight <= 0)
                    {
                        return new ImpositionResult
                        {
                            Success = false,
                            ErrorMessage = "PDF页面尺寸无效",
                            MaterialType = MaterialType.FlatSheet
                        };
                    }

                    // 计算布局（支持自动旋转）
                    var result = CalculateOptimalLayout(
                        pageWidth, pageHeight, usableWidth, usableHeight, config, pdfInfo.PageRotation);

                    result.MaterialType = MaterialType.FlatSheet;
                    result.CalculationTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;

                    return result;
                }
                catch (Exception ex)
                {
                    return new ImpositionResult
                    {
                        Success = false,
                        ErrorMessage = $"平张布局计算失败: {ex.Message}",
                        MaterialType = MaterialType.FlatSheet,
                        CalculationTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds
                    };
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 计算卷装模式的布局
        /// </summary>
        /// <param name="config">卷装模式配置</param>
        /// <param name="pdfInfo">PDF文件信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="rotationMode">卷装旋转模式（可选，默认null表示自动计算）</param>
        /// <returns>布局计算结果</returns>
        public async Task<ImpositionResult> CalculateRollMaterialLayoutAsync(
            RollMaterialConfiguration config,
            ImpositionPdfInfo pdfInfo,
            CancellationToken cancellationToken = default,
            RollRotationMode? rotationMode = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (pdfInfo == null)
                throw new ArgumentNullException(nameof(pdfInfo));

            rotationMode = config.RotationMode ?? rotationMode;

            return await Task.Run(() =>
            {
                var startTime = DateTime.Now;

                try
                {
                    // 记录卷装布局计算开始
                    LogHelper.Debug($"[ImpositionService] 开始卷装布局计算: 固定宽度={config.FixedWidth}mm, 最小长度={config.MinLength}mm, PDF尺寸={pdfInfo.GetEffectivePageWidth():F1}×{pdfInfo.GetEffectivePageHeight():F1}mm");

                    // 验证配置
                    var validationResult = ValidateRollMaterialConfiguration(config);
                    if (!validationResult.IsValid)
                    {
                        return new ImpositionResult
                        {
                            Success = false,
                            ErrorMessage = validationResult.ErrorMessage,
                            MaterialType = MaterialType.RollMaterial
                        };
                    }

                    string constraintError = ValidateLayoutMultiple(config.Rows, config.Columns, config.LayoutMultiple, config.LayoutMultipleDirection);
                    if (constraintError != null) return CreateFailedResult(constraintError, MaterialType.RollMaterial);

                    // 获取页面尺寸
                    float pageWidth = pdfInfo.GetEffectivePageWidth();
                    float pageHeight = pdfInfo.GetEffectivePageHeight();

                    if (pageWidth <= 0 || pageHeight <= 0)
                    {
                        return new ImpositionResult
                        {
                            Success = false,
                            ErrorMessage = "PDF页面尺寸无效",
                            MaterialType = MaterialType.RollMaterial
                        };
                    }

                    // 计算可用宽度
                    float usableWidth = config.UsableWidth;

                    // 计算卷装布局（基于固定1行利用率最大化）
                    var result = CalculateRollMaterialLayoutOptimized(
                        pageWidth, pageHeight, usableWidth, config, pdfInfo.PageRotation, rotationMode);

                    result.MaterialType = MaterialType.RollMaterial;
                    result.CalculationTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;

                    return result;
                }
                catch (Exception ex)
                {
                    return new ImpositionResult
                    {
                        Success = false,
                        ErrorMessage = $"卷装布局计算失败: {ex.Message}",
                        MaterialType = MaterialType.RollMaterial,
                        CalculationTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds
                    };
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 计算最优偶数列布局（一式两联）
        /// </summary>
        /// <param name="config">排版配置（平张或卷装）</param>
        /// <param name="pdfInfo">PDF文件信息</param>
        /// <param name="copyCount">联数（一式几联，0=不使用）</param>
        /// <param name="copyMode">联数倍数方向</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>布局计算结果（确保列数或行数为联数的倍数）</returns>
        public async Task<ImpositionResult> CalculateOptimalEvenColumnsLayoutAsync(
            object config,
            ImpositionPdfInfo pdfInfo,
            int copyCount = 0,
            CopyMode copyMode = CopyMode.AutoByColumn,
            CancellationToken cancellationToken = default)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (pdfInfo == null)
                throw new ArgumentNullException(nameof(pdfInfo));

            return await Task.Run(() =>
            {
                var startTime = DateTime.Now;

                try
                {
                    LogHelper.Debug("[ImpositionService] 开始一式两联最优偶数列布局计算");

                    // 根据配置类型调用相应的布局计算方法，但修改列数计算逻辑
                    ImpositionResult result = null;

                    if (config is FlatSheetConfiguration flatSheetConfig)
                    {
                        result = CalculateFlatSheetLayoutWithEvenColumns(flatSheetConfig, pdfInfo, copyCount, copyMode);
                    }
                    else if (config is RollMaterialConfiguration rollMaterialConfig)
                    {
                        result = CalculateRollMaterialLayoutWithEvenColumns(rollMaterialConfig, pdfInfo, copyCount, copyMode);
                    }
                    else
                    {
                        return new ImpositionResult
                        {
                            Success = false,
                            ErrorMessage = "不支持的配置类型，期望平张或卷装配置"
                        };
                    }

                    if (result != null)
                    {
                        result.CalculationTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
                        LogHelper.Info($"[ImpositionService] 一式两联布局计算完成: {result.Rows}x{result.Columns}, 利用率: {result.SpaceUtilization:F1}%");
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"[ImpositionService] 一式两联布局计算失败: {ex.Message}", ex);
                    return new ImpositionResult
                    {
                        Success = false,
                        ErrorMessage = $"一式两联布局计算失败: {ex.Message}",
                        CalculationTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds
                    };
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 计算平张模式的最优偶数列布局（在标准布局计算基础上增加偶数列约束）
        /// </summary>
        private ImpositionResult CalculateFlatSheetLayoutWithEvenColumns(
            FlatSheetConfiguration config, ImpositionPdfInfo pdfInfo, int copyCount, CopyMode copyMode)
        {
            // 验证配置
            var validationResult = ValidateFlatSheetConfiguration(config);
            if (!validationResult.IsValid)
            {
                return new ImpositionResult
                {
                    Success = false,
                    ErrorMessage = validationResult.ErrorMessage,
                    MaterialType = MaterialType.FlatSheet
                };
            }

            // 计算可用空间
            float usableWidth = config.PrintableWidth;
            float usableHeight = config.PrintableHeight;

            // 获取页面尺寸
            float pageWidth = pdfInfo.GetEffectivePageWidth();
            float pageHeight = pdfInfo.GetEffectivePageHeight();

            if (pageWidth <= 0 || pageHeight <= 0)
            {
                return new ImpositionResult
                {
                    Success = false,
                    ErrorMessage = "PDF页面尺寸无效",
                    MaterialType = MaterialType.FlatSheet
                };
            }

            // 手动行列优先于联数优化，不能在后续计算中被静默缩小。
            if (config.Rows > 0 || config.Columns > 0)
            {
                return CalculateForcedFlatSheetLayout(
                    pageWidth, pageHeight, usableWidth, usableHeight, config, pdfInfo.PageRotation,
                    config.RotationMode ?? (copyMode == CopyMode.FixedNoRotationByColumn || copyMode == CopyMode.FixedNoRotationByRow
                        ? RollRotationMode.Force0Degree : (RollRotationMode?)null));
            }

            // 调用标准布局计算逻辑，但增加偶数列约束
            var result = CalculateOptimalLayoutWithEvenColumns(
                pageWidth, pageHeight, usableWidth, usableHeight, config, pdfInfo.PageRotation, copyCount, copyMode);

            result.MaterialType = MaterialType.FlatSheet;

            return result;
        }

        /// <summary>
        /// 在标准布局计算逻辑基础上增加偶数列约束的最优布局计算
        /// </summary>
        private ImpositionResult CalculateOptimalLayoutWithEvenColumns(
            float pageWidth,
            float pageHeight,
            float usableWidth,
            float usableHeight,
            FlatSheetConfiguration config,
            int pdfPageRotation = 0,
            int copyCount = 0,
            CopyMode copyMode = CopyMode.AutoByColumn)
        {
            if (config.RotationMode == RollRotationMode.Auto)
                copyMode = copyMode == CopyMode.FixedNoRotationByRow ? CopyMode.AutoByRow :
                    copyMode == CopyMode.FixedNoRotationByColumn ? CopyMode.AutoByColumn : copyMode;
            // 计算不旋转时的布局
            int columnsNormal = (int)Math.Floor(usableWidth / pageWidth);
            int rowsNormal = (int)Math.Floor(usableHeight / pageHeight);
            int layoutNormal = columnsNormal * rowsNormal;

            // 计算旋转90度时的布局
            int columnsRotated = (int)Math.Floor(usableWidth / pageHeight);
            int rowsRotated = (int)Math.Floor(usableHeight / pageWidth);
            int layoutRotated = columnsRotated * rowsRotated;

            LogHelper.Debug($"[ImpositionService] 一式N联布局计算: 不旋转={columnsNormal}列×{rowsNormal}行={layoutNormal}页, 旋转90度={columnsRotated}列×{rowsRotated}行={layoutRotated}页");

            // 应用联数倍数约束到两种布局方案
            var evenNormalResult = CalculateNCopiesLayout(columnsNormal, rowsNormal, copyCount, copyMode);
            var evenRotatedResult = CalculateNCopiesLayout(columnsRotated, rowsRotated, copyCount, copyMode);

            // 应用用户指定的行数限制（联数约束后）
            if (config.Rows > 0)
            {
                evenNormalResult.Rows = Math.Min(config.Rows, evenNormalResult.Rows);
                evenRotatedResult.Rows = Math.Min(config.Rows, evenRotatedResult.Rows);
            }
            if (config.Columns > 0)
            {
                evenNormalResult.Columns = Math.Min(config.Columns, evenNormalResult.Columns);
                evenRotatedResult.Columns = Math.Min(config.Columns, evenRotatedResult.Columns);
                // 确保用户指定的列数也是联数的倍数（仅在按列模式下）
                if ((copyMode == CopyMode.AutoByColumn || copyMode == CopyMode.FixedNoRotationByColumn) && copyCount > 0)
                {
                    if (evenNormalResult.Columns % copyCount != 0)
                        evenNormalResult.Columns = evenNormalResult.Columns - (evenNormalResult.Columns % copyCount);
                    if (evenRotatedResult.Columns % copyCount != 0)
                        evenRotatedResult.Columns = evenRotatedResult.Columns - (evenRotatedResult.Columns % copyCount);
                }
            }

            // 确定最小值（基于联数或默认值2）
            int minValue = copyCount > 0 ? copyCount : 2;

            // 根据倍数方向应用不同的最小值约束
            if ((copyMode == CopyMode.AutoByColumn || copyMode == CopyMode.FixedNoRotationByColumn))
            {
                // 按列模式：列数至少为联数值，行数至少为1
                evenNormalResult.Rows = Math.Max(1, evenNormalResult.Rows);
                evenNormalResult.Columns = Math.Max(minValue, evenNormalResult.Columns);
                evenRotatedResult.Rows = Math.Max(1, evenRotatedResult.Rows);
                evenRotatedResult.Columns = Math.Max(minValue, evenRotatedResult.Columns);
            }
            else
            {
                // 按行模式：行数至少为联数值，列数至少为1
                evenNormalResult.Rows = Math.Max(minValue, evenNormalResult.Rows);
                evenNormalResult.Columns = Math.Max(1, evenNormalResult.Columns);
                evenRotatedResult.Rows = Math.Max(minValue, evenRotatedResult.Rows);
                evenRotatedResult.Columns = Math.Max(1, evenRotatedResult.Columns);
            }

            // 计算调整后的页数
            int evenLayoutNormal = evenNormalResult.Rows * evenNormalResult.Columns;
            int evenLayoutRotated = evenRotatedResult.Rows * evenRotatedResult.Columns;

            LogHelper.Debug($"[ImpositionService] 一式N联调整后: 不旋转={evenNormalResult.Columns}列×{evenNormalResult.Rows}行={evenLayoutNormal}页, 旋转90度={evenRotatedResult.Columns}列×{evenRotatedResult.Rows}行={evenLayoutRotated}页");

            // 选择页数更多的方案（保持与标准布局相同的选择逻辑）
            // 但如果不旋转模式，则强制不旋转
            bool useRotation = false;
            if (copyMode != CopyMode.FixedNoRotationByColumn && copyMode != CopyMode.FixedNoRotationByRow)
            {
                if (IsSquareDimension(pageWidth, pageHeight, 0.09f))
                {
                    useRotation = false;
                    LogHelper.Debug($"[ImpositionService] 一式N联平张: 尺寸 {pageWidth:F2}x{pageHeight:F2}mm 判定为正方形，默认锁定不旋转");
                }
                else
                {
                    useRotation = evenLayoutRotated > evenLayoutNormal;
                }
            }
            if (config.RotationMode.HasValue && config.RotationMode != RollRotationMode.Auto)
                useRotation = config.RotationMode == RollRotationMode.Force270Degree;
            else if (config.RotationMode == RollRotationMode.Auto)
                useRotation = !IsSquareDimension(pageWidth, pageHeight, 0.09f) && evenLayoutRotated > evenLayoutNormal;
            var selectedResult = useRotation ? evenRotatedResult : evenNormalResult;
            if (config.RotationMode.HasValue &&
                (selectedResult.Columns * (useRotation ? pageHeight : pageWidth) > usableWidth + 0.01f ||
                 selectedResult.Rows * (useRotation ? pageWidth : pageHeight) > usableHeight + 0.01f))
                return CreateFailedResult("指定旋转方向与布局约束无法容纳在纸张可印刷区域内。", MaterialType.FlatSheet);

            // 计算实际页面尺寸（考虑旋转）
            float actualPageWidth = useRotation ? pageHeight : pageWidth;
            float actualPageHeight = useRotation ? pageWidth : pageHeight;

            int layoutQuantity = selectedResult.Rows * selectedResult.Columns;

            // 计算空间利用率
            float totalPageArea = layoutQuantity * pageWidth * pageHeight;
            float usableArea = usableWidth * usableHeight;
            float spaceUtilization = (totalPageArea / usableArea) * 100;

            // 最终旋转角度 = 布局旋转角度（PDF页面旋转已在GetEffectivePageWidth/Height中考虑，不需要叠加）
            int layoutRotationAngle = useRotation ? 270 : 0;
            int finalRotationAngle = layoutRotationAngle;

            string modeDesc = copyMode switch
            {
                CopyMode.AutoByColumn => "自适应列",
                CopyMode.AutoByRow => "自适应行",
                CopyMode.FixedNoRotationByColumn => "不旋转列",
                CopyMode.FixedNoRotationByRow => "不旋转行",
                _ => "列"
            };
            LogHelper.Debug($"[ImpositionService] 一式N联最终选择: {(useRotation ? "旋转90度" : "不旋转")}，{selectedResult.Rows}行×{selectedResult.Columns}列={layoutQuantity}页，最终旋转{finalRotationAngle}°，利用率{spaceUtilization:F1}%");

            return new ImpositionResult
            {
                OptimalLayoutQuantity = layoutQuantity,
                Columns = selectedResult.Columns,
                Rows = selectedResult.Rows,
                CellWidth = actualPageWidth,
                CellHeight = actualPageHeight,
                SpaceUtilization = spaceUtilization,
                IsPrintable = layoutQuantity > 0,
                UseRotation = useRotation,
                RotationAngle = finalRotationAngle,
                Description = $"一式{copyCount}联平张{selectedResult.Rows}行×{selectedResult.Columns}列 = {layoutQuantity}页/纸，{(useRotation ? "旋转90度" : "不旋转")}，最终旋转{finalRotationAngle}°，利用率{spaceUtilization:F1}%（按{modeDesc}倍数约束）"
            };
        }

        /// <summary>
        /// 对给定的行列配置应用联数倍数约束
        /// </summary>
        /// <param name="columns">原始列数</param>
        /// <param name="rows">原始行数</param>
        /// <param name="copyCount">联数（0=不使用约束，使用默认值2）</param>
        /// <param name="copyMode">倍数方向</param>
        /// <returns>调整后的行列数</returns>
        private (int Columns, int Rows) CalculateNCopiesLayout(int columns, int rows, int copyCount, CopyMode copyMode)
        {
            int effectiveCopyCount = copyCount > 0 ? copyCount : 2;

            if ((copyMode == CopyMode.AutoByColumn || copyMode == CopyMode.FixedNoRotationByColumn))
            {
                // 列数必须是联数的倍数
                int remainder = columns % effectiveCopyCount;
                int adjustedColumns = remainder == 0 ? columns : columns - remainder;
                // 如果调整后小于物理限制或为0，保留物理限制，不强制增加
                if (adjustedColumns <= 0 || adjustedColumns > columns)
                {
                    adjustedColumns = columns;
                }
                return (adjustedColumns, rows);
            }
            else
            {
                // 行数必须是联数的倍数
                int remainder = rows % effectiveCopyCount;
                int adjustedRows = remainder == 0 ? rows : rows - remainder;
                // 如果调整后小于物理限制或为0，保留物理限制，不强制增加
                if (adjustedRows <= 0 || adjustedRows > rows)
                {
                    adjustedRows = rows;
                }
                return (columns, adjustedRows);
            }
        }

        /// <summary>
        /// 计算卷装模式的最优偶数列布局（在标准布局计算基础上增加偶数列约束）
        /// </summary>
        private ImpositionResult CalculateRollMaterialLayoutWithEvenColumns(
            RollMaterialConfiguration config, ImpositionPdfInfo pdfInfo, int copyCount, CopyMode copyMode)
        {
            // 验证配置
            var validationResult = ValidateRollMaterialConfiguration(config);
            if (!validationResult.IsValid)
            {
                return new ImpositionResult
                {
                    Success = false,
                    ErrorMessage = validationResult.ErrorMessage,
                    MaterialType = MaterialType.RollMaterial
                };
            }

            // 获取页面尺寸
            float pageWidth = pdfInfo.GetEffectivePageWidth();
            float pageHeight = pdfInfo.GetEffectivePageHeight();
            float usableWidth = config.UsableWidth;

            if (pageWidth <= 0 || pageHeight <= 0)
            {
                return new ImpositionResult
                {
                    Success = false,
                    ErrorMessage = "PDF页面尺寸无效",
                    MaterialType = MaterialType.RollMaterial
                };
            }

            // 手动行列优先于联数优化，不能在后续计算中被静默缩小。
            if (config.Rows > 0 || config.Columns > 0)
            {
                RollRotationMode? rotationMode =
                    config.RotationMode ?? (copyMode == CopyMode.FixedNoRotationByColumn || copyMode == CopyMode.FixedNoRotationByRow
                        ? RollRotationMode.Force0Degree
                        : (RollRotationMode?)null);
                return CalculateForcedRollMaterialLayout(
                    pageWidth, pageHeight, usableWidth, config, rotationMode);
            }

            // 调用标准布局计算逻辑，但增加偶数列约束
            var result = CalculateRollMaterialLayoutWithEvenColumnsOptimized(
                pageWidth, pageHeight, usableWidth, config, pdfInfo.PageRotation, copyCount, copyMode);

            result.MaterialType = MaterialType.RollMaterial;

            return result;
        }

        /// <summary>
        /// 在标准卷装布局计算基础上增加偶数列约束的最优布局计算
        /// </summary>
        private ImpositionResult CalculateRollMaterialLayoutWithEvenColumnsOptimized(
            float pageWidth,
            float pageHeight,
            float usableWidth,
            RollMaterialConfiguration config,
            int pdfPageRotation = 0,
            int copyCount = 0,
            CopyMode copyMode = CopyMode.AutoByColumn)
        {
            if (config.RotationMode == RollRotationMode.Auto)
                copyMode = copyMode == CopyMode.FixedNoRotationByRow ? CopyMode.AutoByRow :
                    copyMode == CopyMode.FixedNoRotationByColumn ? CopyMode.AutoByColumn : copyMode;
            // 计算固定1行时的最大可能列数（不旋转和旋转两种情况）
            int maxColumnsWithoutRotation = (int)Math.Floor(usableWidth / pageWidth);
            int maxColumnsWithRotation = (int)Math.Floor(usableWidth / pageHeight);

            LogHelper.Debug($"[ImpositionService] 卷装一式N联布局计算: 最大可能列数-不旋转={maxColumnsWithoutRotation}列, 旋转90度={maxColumnsWithRotation}列");

            // 应用联数倍数约束到两种布局方案
            var evenNormalResult = CalculateNCopiesLayout(maxColumnsWithoutRotation, 1, copyCount, copyMode);
            var evenRotatedResult = CalculateNCopiesLayout(maxColumnsWithRotation, 1, copyCount, copyMode);

            // 确定最小值（基于联数或默认值2）
            int minValue = copyCount > 0 ? copyCount : 2;

            // 应用用户指定的列数限制（联数约束后）
            if (config.Columns > 0)
            {
                evenNormalResult.Columns = Math.Min(config.Columns, evenNormalResult.Columns);
                evenRotatedResult.Columns = Math.Min(config.Columns, evenRotatedResult.Columns);
                // 确保用户指定的列数也是联数的倍数（仅在按列模式下）
                if ((copyMode == CopyMode.AutoByColumn || copyMode == CopyMode.FixedNoRotationByColumn) && copyCount > 0)
                {
                    if (evenNormalResult.Columns % copyCount != 0)
                        evenNormalResult.Columns = evenNormalResult.Columns - (evenNormalResult.Columns % copyCount);
                    if (evenRotatedResult.Columns % copyCount != 0)
                        evenRotatedResult.Columns = evenRotatedResult.Columns - (evenRotatedResult.Columns % copyCount);
                }
            }

            // 直接基于联数倍数约束计算两种方案的利用率
            // 注意：不要在这里用 Math.Max 约束，因为这会影响旋转判断的准确性
            // 不旋转时的布局计算
            int columnsNormal = evenNormalResult.Columns;
            float cellWidthNormal = usableWidth / columnsNormal;
            float cellHeightNormal = pageHeight;
            float totalPageAreaNormal = columnsNormal * pageWidth * pageHeight;
            float usedAreaNormal = columnsNormal * cellWidthNormal * cellHeightNormal;
            float utilizationNormal = (usedAreaNormal / totalPageAreaNormal) * 100;

            // 旋转90度时的布局计算
            int columnsRotated = evenRotatedResult.Columns;
            float cellWidthRotated = usableWidth / columnsRotated;
            float cellHeightRotated = pageWidth;
            float totalPageAreaRotated = columnsRotated * pageWidth * pageHeight;
            float usedAreaRotated = columnsRotated * cellWidthRotated * cellHeightRotated;
            float utilizationRotated = (usedAreaRotated / totalPageAreaRotated) * 100;

            LogHelper.Debug($"[ImpositionService] 卷装一式N联调整后: 不旋转={columnsNormal}列(利用率{utilizationNormal:F1}%), 旋转90度={columnsRotated}列(利用率{utilizationRotated:F1}%)");

            // 计算两种方案的实际使用宽度
            float usedWidthNormal = columnsNormal * pageWidth;   // 不旋转时的实际使用宽度
            float usedWidthRotated = columnsRotated * pageHeight; // 旋转90度时的实际使用宽度

            LogHelper.Debug($"[ImpositionService] 卷装一式N联宽度使用: 不旋转使用宽度={usedWidthNormal:F1}mm, 旋转90度使用宽度={usedWidthRotated:F1}mm");

            // 选择最优旋转方向（优先级：1.宽度利用率 2.列数 3.空间利用率）
            // 但如果不旋转模式，则强制不旋转
            bool useRotation = false;
            if (copyMode != CopyMode.FixedNoRotationByColumn && copyMode != CopyMode.FixedNoRotationByRow)
            {
                if (IsSquareDimension(pageWidth, pageHeight, 0.09f))
                {
                    useRotation = false;
                    LogHelper.Debug($"[ImpositionService] 卷装一式N联: 尺寸 {pageWidth:F2}x{pageHeight:F2}mm 判定为正方形，默认锁定不旋转");
                }
                else
                {
                // 优先比较宽度利用率
                if (Math.Abs(usedWidthRotated - usedWidthNormal) > 0.1f)
                {
                    useRotation = usedWidthRotated > usedWidthNormal;
                }
                else
                {
                    // 宽度利用率相同时比较列数
                    if (columnsRotated != columnsNormal)
                    {
                        useRotation = columnsRotated > columnsNormal;
                    }
                    else
                    {
                        // 列数也相同时比较空间利用率
                        useRotation = utilizationRotated > utilizationNormal;
                    }
                }
                }
            }
            
            if (config.RotationMode.HasValue && config.RotationMode != RollRotationMode.Auto)
                useRotation = config.RotationMode == RollRotationMode.Force270Degree;
            else if (config.RotationMode == RollRotationMode.Auto &&
                (copyMode == CopyMode.FixedNoRotationByColumn || copyMode == CopyMode.FixedNoRotationByRow))
                useRotation = !IsSquareDimension(pageWidth, pageHeight, 0.09f) && usedWidthRotated > usedWidthNormal;
            int optimalColumns = useRotation ? evenRotatedResult.Columns : evenNormalResult.Columns;
            if (config.RotationMode.HasValue &&
                (optimalColumns < 1 || optimalColumns * (useRotation ? pageHeight : pageWidth) > usableWidth + 0.01f))
                return CreateFailedResult("指定旋转方向与布局约束无法容纳在卷装幅宽内。", MaterialType.RollMaterial);
            float actualUtilization = useRotation ? utilizationRotated : utilizationNormal;

            LogHelper.Debug($"[ImpositionService] 卷装一式N联选择: 不旋转={evenNormalResult.Columns}列(利用率{utilizationNormal:F1}%), 旋转90度={evenRotatedResult.Columns}列(利用率{utilizationRotated:F1}%), 选择{(useRotation ? "旋转" : "不旋转")}");

            // 计算单个页面的实际尺寸（考虑旋转）
            float actualPageWidth = useRotation ? pageHeight : pageWidth;
            float actualPageHeight = useRotation ? pageWidth : pageHeight;

            LogHelper.Debug($"[ImpositionService] 卷装一式N联页面尺寸: 实际页面宽度={actualPageWidth:F1}mm, 实际页面高度={actualPageHeight:F1}mm");

            // 计算需要的行数以满足最小长度要求（最终长度需要超过最小长度）
            float singleRowLength = config.MarginTop + actualPageHeight + config.MarginBottom;
            int minRowsRequired = (int)Math.Ceiling(config.MinLength / singleRowLength);

            // 确保至少有1行
            minRowsRequired = Math.Max(1, minRowsRequired);

            // 如果是按行模式，需要将行数调整为联数的倍数
            if ((copyMode == CopyMode.AutoByRow || copyMode == CopyMode.FixedNoRotationByRow) && copyCount > 0)
            {
                int remainder = minRowsRequired % copyCount;
                if (remainder != 0)
                {
                    minRowsRequired = minRowsRequired + (copyCount - remainder);
                }
                minRowsRequired = Math.Max(copyCount, minRowsRequired);
            }

            LogHelper.Debug($"[ImpositionService] 卷装一式N联多行计算: 最小长度要求={config.MinLength}mm, 单行长度={singleRowLength:F1}mm, 需要行数={minRowsRequired}行");

            // 确保最终列数满足最小联数值约束（仅在按列模式下）
            // 注意：这里的约束是在旋转判断之后应用的，不会影响旋转选择的准确性
            if ((copyMode == CopyMode.AutoByColumn || copyMode == CopyMode.FixedNoRotationByColumn))
            {
                optimalColumns = Math.Max(minValue, optimalColumns);
            }

            int layoutQuantity = optimalColumns * minRowsRequired;

            // 计算最终的空间利用率（考虑实际行数）
            float totalMaterialArea = config.FixedWidth * (config.MarginTop + (minRowsRequired * actualPageHeight) + config.MarginBottom);
            float totalPageArea = layoutQuantity * pageWidth * pageHeight;
            float finalUtilization = (totalPageArea / totalMaterialArea) * 100;

            // 计算实际使用的材料长度
            float usedMaterialLength = config.MarginTop + (minRowsRequired * actualPageHeight) + config.MarginBottom;

            // 最终旋转角度
            int layoutRotationAngle = useRotation ? 270 : 0;
            int finalRotationAngle = layoutRotationAngle;

            string modeDesc = copyMode switch
            {
                CopyMode.AutoByColumn => "自适应列",
                CopyMode.AutoByRow => "自适应行",
                CopyMode.FixedNoRotationByColumn => "不旋转列",
                CopyMode.FixedNoRotationByRow => "不旋转行",
                _ => "列"
            };
            LogHelper.Debug($"[ImpositionService] 卷装一式N联最终选择: {(useRotation ? "旋转90度" : "不旋转")}，{minRowsRequired}行×{optimalColumns}列={layoutQuantity}页，最终旋转{finalRotationAngle}°，利用率{finalUtilization:F1}%");

            return new ImpositionResult
            {
                OptimalLayoutQuantity = layoutQuantity,
                Columns = optimalColumns,
                Rows = minRowsRequired,
                CellWidth = actualPageWidth,
                CellHeight = actualPageHeight,
                SpaceUtilization = finalUtilization,
                IsPrintable = layoutQuantity > 0,
                UseRotation = useRotation,
                RotationAngle = finalRotationAngle,
                Description = $"一式{copyCount}联卷装{minRowsRequired}行×{optimalColumns}列 = {layoutQuantity}页/纸，{(useRotation ? "旋转90度" : "不旋转")}，最终旋转{finalRotationAngle}°，利用率{finalUtilization:F1}%（按{modeDesc}倍数约束）"
            };
        }

        #endregion

        #region PDF文件分析

        /// <summary>
        /// 分析PDF文件信息（返回排版专用的PDF信息）
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>排版用PDF文件信息</returns>
        public Task<ImpositionPdfInfo> AnalyzePdfFileAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            return AnalyzePdfFileAsync(filePath, PdfPageBoxSource.Automatic, 1, cancellationToken);
        }

        public async Task<ImpositionPdfInfo> AnalyzePdfFileAsync(
            string filePath,
            PdfPageBoxSource pageBoxSource,
            int referencePageNumber,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"PDF文件不存在: {filePath}");

            return await Task.Run(() =>
            {
                try
                {
                    // 使用现有的PDF信息提供者获取基础信息
                    var basePdfInfo = _pdfInfoProvider.AnalyzePdf(filePath);

                    // 获取裁剪框信息
                    float cropBoxWidth = 0;
                    float cropBoxHeight = 0;
                    bool hasCropBox = false;
                    int pageRotation = 0;

                    // 尝试获取第一页的裁剪框和旋转信息
                    try
                    {
                        using (var reader = new PdfReader(filePath))
                        using (var document = new PdfDocument(reader))
                        {
                            if (document.GetNumberOfPages() > 0)
                            {
                                int framePageNumber = pageBoxSource == PdfPageBoxSource.Automatic ? 1 : referencePageNumber;
                                if (framePageNumber < 1 || framePageNumber > document.GetNumberOfPages())
                                    throw new ArgumentOutOfRangeException(nameof(referencePageNumber), "页面框基准页码无效");
                                var framePage = document.GetPage(framePageNumber);
                                var mediaBox = framePage.GetMediaBox();
                                var cropBox = framePage.GetCropBox();

                                // 与页面框统一处理共用所选原始框；毫米换算不再使用近似常量。
                                basePdfInfo.FirstPageSize = new WindowsFormsApp3.Utils.PageSize
                                {
                                    Width = mediaBox.GetWidth() * 25.4 / 72,
                                    Height = mediaBox.GetHeight() * 25.4 / 72
                                };

                                // 获取页面旋转角度
                                pageRotation = framePage.GetRotation();
                                LogHelper.Debug($"[排版分析] 检测到PDF页面旋转角度: {pageRotation}°");

                                bool validCrop = cropBox != null && cropBox.GetWidth() > 0 && cropBox.GetHeight() > 0;
                                bool cropInsideMedia = validCrop && cropBox.GetLeft() >= mediaBox.GetLeft() - 0.0001 &&
                                    cropBox.GetBottom() >= mediaBox.GetBottom() - 0.0001 &&
                                    cropBox.GetRight() <= mediaBox.GetRight() + 0.0001 &&
                                    cropBox.GetTop() <= mediaBox.GetTop() + 0.0001;
                                if (pageBoxSource == PdfPageBoxSource.CropBox && !validCrop)
                                    throw new InvalidOperationException("所选裁剪框尺寸无效");
                                hasCropBox = pageBoxSource == PdfPageBoxSource.CropBox ||
                                    (pageBoxSource == PdfPageBoxSource.Automatic && cropInsideMedia);
                                var selectedBox = hasCropBox ? cropBox : mediaBox;
                                float rawWidth = selectedBox.GetWidth();
                                float rawHeight = selectedBox.GetHeight();

                                // 注意：这里保存的是原始未旋转的宽高，具体旋转处理由GetEffectivePageWidth/Height方法负责
                                cropBoxWidth = (float)((validCrop ? cropBox.GetWidth() : mediaBox.GetWidth()) * 25.4 / 72);
                                cropBoxHeight = (float)((validCrop ? cropBox.GetHeight() : mediaBox.GetHeight()) * 25.4 / 72);

                                LogHelper.Debug($"[排版分析] 第{framePageNumber}页排版基准={(hasCropBox ? "CropBox" : "MediaBox")}, 原始尺寸={rawWidth * 25.4 / 72:0.####}x{rawHeight * 25.4 / 72:0.####}mm, 用户选择={pageBoxSource}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (pageBoxSource != PdfPageBoxSource.Automatic) throw;
                        // 裁剪框获取失败时使用媒体框
                        LogHelper.Debug($"[排版分析] 获取裁剪框信息失败: {ex.Message}");
                    }

                    // 创建排版专用的PDF信息（传递旋转角度）
                    return ImpositionPdfInfo.FromPdfFileInfo(
                        basePdfInfo,
                        cropBoxWidth,
                        cropBoxHeight,
                        hasCropBox,
                        pageRotation);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"分析PDF文件失败: {ex.Message}", ex);
                }
            }, cancellationToken);
        }

        #endregion

        #region 空白页计算

        /// <summary>
        /// 计算需要补充的空白页数量
        /// </summary>
        /// <param name="currentPageCount">当前页数</param>
        /// <param name="layoutQuantity">布局数量</param>
        /// <returns>空白页数量</returns>
        public int CalculateBlankPagesNeeded(int currentPageCount, int layoutQuantity)
        {
            if (currentPageCount < 0 || layoutQuantity <= 0)
                return 0;

            int remainder = currentPageCount % layoutQuantity;
            return remainder == 0 ? 0 : layoutQuantity - remainder;
        }

        /// <summary>
        /// 异步计算需要补充的空白页数量
        /// </summary>
        /// <param name="currentPageCount">当前页数</param>
        /// <param name="layoutQuantity">布局数量</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>空白页数量</returns>
        public Task<int> CalculateBlankPagesNeededAsync(
            int currentPageCount,
            int layoutQuantity,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CalculateBlankPagesNeeded(currentPageCount, layoutQuantity));
        }

        /// <summary>
        /// 计算空白页插入方案
        /// </summary>
        /// <param name="currentPageCount">当前页数</param>
        /// <param name="layoutQuantity">布局数量</param>
        /// <param name="layoutMode">排版模式</param>
        /// <param name="copyCount">联数（一式几联，0=不使用）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>空白页补充结果</returns>
        public async Task<BlankPageResult> CalculateBlankPageInsertionAsync(
            int currentPageCount,
            int layoutQuantity,
            LayoutMode layoutMode,
            int copyCount = 0,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                // 如果启用了一式N联，实际页数需要乘以联数
                int effectivePageCount = copyCount > 0 ? currentPageCount * copyCount : currentPageCount;

                var result = new BlankPageResult
                {
                    OriginalPageCount = currentPageCount,
                    LayoutQuantity = layoutQuantity,
                    LayoutMode = layoutMode,
                    TotalPageCount = effectivePageCount,
                    RequiredSheets = (int)Math.Ceiling((double)effectivePageCount / layoutQuantity)
                };

                if (layoutQuantity <= 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "布局数量必须大于0";
                    return result;
                }

                int remainder = effectivePageCount % layoutQuantity;

                if (remainder == 0)
                {
                    result.BlankPagesNeeded = 0;
                    string copyDesc = copyCount > 0 ? $"一式{copyCount}联，" : "";
                    result.CalculationDetails = $"{copyDesc}{layoutMode}模式，当前页数{effectivePageCount}是布局数量{layoutQuantity}的倍数，无需补充空白页";
                    result.Success = true;
                    return result;
                }

                if (layoutMode == LayoutMode.Folding)
                {
                    // 折手模式：空白页基于复制后总页数计算，但分配到每个副本
                    int totalBlankPages = layoutQuantity - remainder;
                    // 每个副本需要的空白页数 = 总空白页 ÷ 联数
                    result.BlankPagesNeeded = copyCount > 0 ? totalBlankPages / copyCount : totalBlankPages;
                    result.TotalPageCount = currentPageCount + result.BlankPagesNeeded;

                    // 计算插入位置（均匀分布策略）
                    int interval = currentPageCount / (result.BlankPagesNeeded + 1);
                    for (int i = 1; i <= result.BlankPagesNeeded; i++)
                    {
                        int position = i * interval;
                        result.BlankPagePositions.Add(position);
                    }

                    result.RequiredSheets = (int)Math.Ceiling((double)result.TotalPageCount / layoutQuantity);
                    string copyDesc = copyCount > 0 ? $"一式{copyCount}联，" : "";
                    int finalTotalPages = (currentPageCount + result.BlankPagesNeeded) * copyCount;
                    result.CalculationDetails = $"{copyDesc}折手模式：{currentPageCount}页×{copyCount}联={currentPageCount * copyCount}页 ÷ {layoutQuantity}页/纸 = 余{remainder}页，需补充{result.BlankPagesNeeded}个空白页/副本，凑成{currentPageCount + result.BlankPagesNeeded}页/副本（一式{copyCount}联后共{finalTotalPages}页）";
                }
                else
                {
                    // 连拼模式：不需要添加空白页
                    result.BlankPagesNeeded = 0;
                    string copyDesc = copyCount > 0 ? $"一式{copyCount}联，" : "";
                    result.CalculationDetails = $"连拼模式：{currentPageCount}页 × {copyCount}(联数) = {effectivePageCount}页，按{layoutQuantity}页/纸布局，剩余{remainder}页直接拼接，无需补充空白页";
                }

                result.Success = true;
                return result;
            }, cancellationToken);
        }

        #endregion

        #region 标准配置获取

        /// <summary>
        /// 获取标准平张配置
        /// </summary>
        /// <returns>标准平张配置</returns>
        public FlatSheetConfiguration GetStandardFlatSheetConfiguration()
        {
            return new FlatSheetConfiguration
            {
                Name = "A4标准配置",
                PaperWidth = 210f,
                PaperHeight = 297f,
                MarginTop = 10f,
                MarginBottom = 10f,
                MarginLeft = 10f,
                MarginRight = 10f,
                Rows = 0,
                Columns = 0
            };
        }

        /// <summary>
        /// 获取标准卷装配置
        /// </summary>
        /// <returns>标准卷装配置</returns>
        public RollMaterialConfiguration GetStandardRollMaterialConfiguration()
        {
            return new RollMaterialConfiguration
            {
                Name = "标准卷装配置",
                FixedWidth = 210f,
                MinLength = 297f,
                MarginTop = 10f,
                MarginBottom = 10f,
                MarginLeft = 10f,
                MarginRight = 10f
            };
        }

        /// <summary>
        /// 获取所有预设的平张配置
        /// </summary>
        /// <returns>平张配置列表</returns>
        public List<FlatSheetConfiguration> GetPresetFlatSheetConfigurations()
        {
            return new List<FlatSheetConfiguration>
            {
                new FlatSheetConfiguration
                {
                    Name = "A4标准配置",
                    PaperWidth = 210f,
                    PaperHeight = 297f,
                    MarginTop = 10f,
                    MarginBottom = 10f,
                    MarginLeft = 10f,
                    MarginRight = 10f,
                    Rows = 0,
                    Columns = 0
                },
                new FlatSheetConfiguration
                {
                    Name = "A3配置",
                    PaperWidth = 297f,
                    PaperHeight = 420f,
                    MarginTop = 15f,
                    MarginBottom = 15f,
                    MarginLeft = 15f,
                    MarginRight = 15f,
                    Rows = 0,
                    Columns = 0
                },
                new FlatSheetConfiguration
                {
                    Name = "Letter配置",
                    PaperWidth = 215.9f,
                    PaperHeight = 279.4f,
                    MarginTop = 12.7f,
                    MarginBottom = 12.7f,
                    MarginLeft = 12.7f,
                    MarginRight = 12.7f,
                    Rows = 0,
                    Columns = 0
                }
            };
        }

        /// <summary>
        /// 获取所有预设的卷装配置
        /// </summary>
        /// <returns>卷装配置列表</returns>
        public List<RollMaterialConfiguration> GetPresetRollMaterialConfigurations()
        {
            return new List<RollMaterialConfiguration>
            {
                new RollMaterialConfiguration
                {
                    Name = "标准卷装配置",
                    FixedWidth = 210f,
                    MinLength = 297f,
                    MarginTop = 10f,
                    MarginBottom = 10f,
                    MarginLeft = 10f,
                    MarginRight = 10f
                },
                new RollMaterialConfiguration
                {
                    Name = "宽幅卷装配置",
                    FixedWidth = 420f,
                    MinLength = 297f,
                    MarginTop = 15f,
                    MarginBottom = 15f,
                    MarginLeft = 15f,
                    MarginRight = 15f
                },
                new RollMaterialConfiguration
                {
                    Name = "窄幅卷装配置",
                    FixedWidth = 150f,
                    MinLength = 210f,
                    MarginTop = 8f,
                    MarginBottom = 8f,
                    MarginLeft = 8f,
                    MarginRight = 8f
                }
            };
        }

        #endregion

        #region 配置验证

        /// <summary>
        /// 验证平张配置
        /// </summary>
        /// <param name="config">平张配置</param>
        /// <returns>验证结果</returns>
        public ImpositionValidationResult ValidateFlatSheetConfiguration(FlatSheetConfiguration config)
        {
            if (config == null)
                return new ImpositionValidationResult { IsValid = false, ErrorMessage = "配置不能为null" };

            return config.Validate();
        }

        /// <summary>
        /// 验证卷装配置
        /// </summary>
        /// <param name="config">卷装配置</param>
        /// <returns>验证结果</returns>
        public ImpositionValidationResult ValidateRollMaterialConfiguration(RollMaterialConfiguration config)
        {
            if (config == null)
                return new ImpositionValidationResult { IsValid = false, ErrorMessage = "配置不能为null" };

            return config.Validate();
        }

        /// <summary>
        /// 验证PDF文件是否适合排版
        /// </summary>
        /// <param name="pdfInfo">PDF文件信息</param>
        /// <returns>验证结果</returns>
        public ImpositionValidationResult ValidatePdfForImposition(ImpositionPdfInfo pdfInfo)
        {
            if (pdfInfo == null)
                return new ImpositionValidationResult { IsValid = false, ErrorMessage = "PDF信息不能为null" };

            if (pdfInfo.PageCount <= 0)
                return new ImpositionValidationResult { IsValid = false, ErrorMessage = "PDF文件没有页面" };

            float pageWidth = pdfInfo.GetEffectivePageWidth();
            float pageHeight = pdfInfo.GetEffectivePageHeight();

            if (pageWidth <= 0 || pageHeight <= 0)
                return new ImpositionValidationResult { IsValid = false, ErrorMessage = "PDF页面尺寸无效" };

            return new ImpositionValidationResult { IsValid = true };
        }

        #endregion

        #region 批量处理支持

        /// <summary>
        /// 批量计算多个PDF文件的布局
        /// </summary>
        /// <param name="files">PDF文件路径列表</param>
        /// <param name="config">排版配置（平张或卷装）</param>
        /// <param name="materialType">材料类型</param>
        /// <param name="progressCallback">进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>批量计算结果</returns>
        public async Task<List<ImpositionResult>> BatchCalculateLayoutAsync(
            List<string> files,
            object config,
            MaterialType materialType,
            IProgress<ImpositionProgress> progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (files == null)
                throw new ArgumentNullException(nameof(files));
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var results = new List<ImpositionResult>();
            var totalFiles = files.Count;

            for (int i = 0; i < totalFiles; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var filePath = files[i];
                var progress = new ImpositionProgress
                {
                    CurrentFileIndex = i,
                    TotalFiles = totalFiles,
                    CurrentFileName = Path.GetFileName(filePath),
                    CurrentOperation = "分析PDF文件"
                };

                try
                {
                    // 分析PDF文件
                    progress.CurrentOperation = "分析PDF文件";
                    progressCallback?.Report(progress);

                    var pdfInfo = await AnalyzePdfFileAsync(filePath, cancellationToken);

                    // 验证PDF
                    var validationResult = ValidatePdfForImposition(pdfInfo);
                    if (!validationResult.IsValid)
                    {
                        results.Add(new ImpositionResult
                        {
                            Success = false,
                            ErrorMessage = validationResult.ErrorMessage,
                            MaterialType = materialType
                        });
                        continue;
                    }

                    // 计算布局
                    progress.CurrentOperation = "计算布局";
                    progressCallback?.Report(progress);

                    ImpositionResult result;
                    if (materialType == MaterialType.FlatSheet)
                    {
                        result = await CalculateFlatSheetLayoutAsync(
                            (FlatSheetConfiguration)config, pdfInfo, cancellationToken);
                    }
                    else
                    {
                        result = await CalculateRollMaterialLayoutAsync(
                            (RollMaterialConfiguration)config, pdfInfo, cancellationToken);
                    }

                    results.Add(result);
                }
                catch (Exception ex)
                {
                    results.Add(new ImpositionResult
                    {
                        Success = false,
                        ErrorMessage = $"处理文件失败: {ex.Message}",
                        MaterialType = materialType
                    });
                }

                progress.CurrentFileIndex = i + 1;
                progress.CurrentOperation = "处理完成";
                progressCallback?.Report(progress);
            }

            return results;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 计算最优布局（支持自动旋转）
        /// </summary>
        /// <param name="pageWidth">PDF页面宽度（已考虑页面本身旋转）</param>
        /// <param name="pageHeight">PDF页面高度（已考虑页面本身旋转）</param>
        /// <param name="usableWidth">可用宽度</param>
        /// <param name="usableHeight">可用高度</param>
        /// <param name="config">配置</param>
        /// <param name="pdfPageRotation">PDF页面本身的旋转角度</param>
        /// <returns>布局计算结果</returns>
        private ImpositionResult CalculateOptimalLayout(
            float pageWidth,
            float pageHeight,
            float usableWidth,
            float usableHeight,
            FlatSheetConfiguration config,
            int pdfPageRotation = 0)
        {
            if (config.Rows > 0 || config.Columns > 0 ||
                config.LayoutMultiple > 1 ||
                (config.RotationMode.HasValue && config.RotationMode != RollRotationMode.Auto))
            {
                return CalculateForcedFlatSheetLayout(pageWidth, pageHeight, usableWidth, usableHeight, config, pdfPageRotation);
            }

            // 计算不旋转时的布局
            int columnsNormal = (int)Math.Floor(usableWidth / pageWidth);
            int rowsNormal = (int)Math.Floor(usableHeight / pageHeight);
            int layoutNormal = columnsNormal * rowsNormal;

            // 计算旋转90度时的布局
            int columnsRotated = (int)Math.Floor(usableWidth / pageHeight);
            int rowsRotated = (int)Math.Floor(usableHeight / pageWidth);
            int layoutRotated = columnsRotated * rowsRotated;

            LogHelper.Debug($"[ImpositionService] 布局计算: 不旋转={columnsNormal}列×{rowsNormal}行={layoutNormal}页, 旋转90度={columnsRotated}列×{rowsRotated}行={layoutRotated}页");

            // 应用用户指定的行数或列数限制
            bool isSquare = IsSquareDimension(pageWidth, pageHeight, 0.09f);
            bool useRotation = !isSquare && (layoutRotated > layoutNormal);
            if (isSquare)
            {
                LogHelper.Debug($"[ImpositionService] 平张布局: 尺寸 {pageWidth:F2}x{pageHeight:F2}mm 判定为正方形，默认锁定不旋转");
            }
            int actualColumns = useRotation ? columnsRotated : columnsNormal;
            int actualRows = useRotation ? rowsRotated : rowsNormal;

            // 根据配置调整布局
            if (config.Rows > 0)
            {
                actualRows = Math.Min(config.Rows, actualRows);
            }
            if (config.Columns > 0)
            {
                actualColumns = Math.Min(config.Columns, actualColumns);
            }

            if (actualRows <= 0 || actualColumns <= 0)
                return CreateFailedResult("页面无法容纳在纸张可印刷区域内，请调整大版尺寸或页面方向。", MaterialType.FlatSheet);

            // 确保至少为1
            actualRows = Math.Max(1, actualRows);
            actualColumns = Math.Max(1, actualColumns);

            // 计算实际页面尺寸（考虑旋转）
            float actualPageWidth = useRotation ? pageHeight : pageWidth;
            float actualPageHeight = useRotation ? pageWidth : pageHeight;

            int layoutQuantity = actualRows * actualColumns;

            // 计算空间利用率
            float totalPageArea = layoutQuantity * pageWidth * pageHeight;
            float usableArea = usableWidth * usableHeight;
            float spaceUtilization = (usableArea / totalPageArea) * 100;

            // 最终旋转角度 = 布局旋转角度（PDF页面旋转已在GetEffectivePageWidth/Height中考虑，不需要叠加）
            int layoutRotationAngle = useRotation ? 270 : 0;
            int finalRotationAngle = layoutRotationAngle;

            LogHelper.Debug($"[ImpositionService] 旋转角度计算: PDF页面旋转={pdfPageRotation}°(已在尺寸计算中考虑), 布局旋转={layoutRotationAngle}°, 最终输出旋转={finalRotationAngle}°");

            return new ImpositionResult
            {
                OptimalLayoutQuantity = layoutQuantity,
                Columns = actualColumns,
                Rows = actualRows,
                CellWidth = actualPageWidth,
                CellHeight = actualPageHeight,
                SpaceUtilization = spaceUtilization,
                IsPrintable = layoutQuantity > 0,
                UseRotation = useRotation,
                RotationAngle = finalRotationAngle,
                Description = $"平张{actualRows}行×{actualColumns}列 = {layoutQuantity}页/纸，{(useRotation ? "旋转90度" : "不旋转")}，最终旋转{finalRotationAngle}°，利用率{spaceUtilization:F1}%"
            };
        }

        private ImpositionResult CalculateForcedFlatSheetLayout(
            float pageWidth,
            float pageHeight,
            float usableWidth,
            float usableHeight,
            FlatSheetConfiguration config,
            int pdfPageRotation,
            RollRotationMode? rotationMode = null)
        {
            var normal = CreateFlatSheetCandidate(pageWidth, pageHeight, false);
            var rotated = CreateFlatSheetCandidate(pageHeight, pageWidth, true);

            var candidates = new[] { normal, rotated }.Where(candidate => candidate.IsFeasible).ToList();
            rotationMode = config.RotationMode ?? rotationMode;
            if (rotationMode == RollRotationMode.Force0Degree)
                candidates = candidates.Where(candidate => !candidate.UseRotation).ToList();
            else if (rotationMode == RollRotationMode.Force270Degree)
                candidates = candidates.Where(candidate => candidate.UseRotation).ToList();
            else if (IsSquareDimension(pageWidth, pageHeight, 0.09f))
            {
                candidates = candidates.Where(candidate => !candidate.UseRotation).ToList();
            }
            if (candidates.Count == 0)
            {
                return CreateFailedResult(
                    "指定的行数或列数无法容纳在纸张可印刷区域内。",
                    MaterialType.FlatSheet);
            }

            var selected = candidates
                .OrderByDescending(candidate => candidate.Rows * candidate.Columns)
                .ThenBy(candidate => candidate.UseRotation)
                .First();
            int layoutQuantity = selected.Rows * selected.Columns;
            float spaceUtilization = usableWidth * usableHeight <= 0
                ? 0
                : layoutQuantity * pageWidth * pageHeight / (usableWidth * usableHeight) * 100;

            return new ImpositionResult
            {
                OptimalLayoutQuantity = layoutQuantity,
                Columns = selected.Columns,
                Rows = selected.Rows,
                CellWidth = selected.PageWidth,
                CellHeight = selected.PageHeight,
                SpaceUtilization = spaceUtilization,
                IsPrintable = true,
                UseRotation = selected.UseRotation,
                RotationAngle = selected.UseRotation ? 270 : 0,
                Description = $"平张{selected.Rows}行×{selected.Columns}列 = {layoutQuantity}页，{(selected.UseRotation ? "旋转90度" : "不旋转")}，利用率{spaceUtilization:F1}%"
            };

            (int Columns, int Rows, float PageWidth, float PageHeight, bool UseRotation, bool IsFeasible) CreateFlatSheetCandidate(
                float candidatePageWidth, float candidatePageHeight, bool useRotation)
            {
                int maximumColumns = (int)Math.Floor(usableWidth / candidatePageWidth);
                int maximumRows = (int)Math.Floor(usableHeight / candidatePageHeight);
                int columns = config.Columns > 0 ? config.Columns : maximumColumns;
                int rows = config.Rows > 0 ? config.Rows : maximumRows;
                ApplyLayoutMultiple(ref rows, ref columns, config.Rows, config.Columns,
                    config.LayoutMultiple, config.LayoutMultipleDirection);
                bool isFeasible = columns > 0 && rows > 0 && columns <= maximumColumns && rows <= maximumRows;
                return (columns, rows, candidatePageWidth, candidatePageHeight, useRotation, isFeasible);
            }
        }

        private static ImpositionResult CreateFailedResult(string errorMessage, MaterialType materialType)
        {
            return new ImpositionResult
            {
                Success = false,
                IsPrintable = false,
                ErrorMessage = errorMessage,
                MaterialType = materialType
            };
        }

        /// <summary>
        /// 计算卷装布局优化（基于固定1行利用率最大化）
        /// </summary>
        /// <param name="pageWidth">PDF页面宽度（已考虑页面本身旋转）</param>
        /// <param name="pageHeight">PDF页面高度（已考虑页面本身旋转）</param>
        /// <param name="usableWidth">可用宽度</param>
        /// <param name="config">配置</param>
        /// <param name="pdfPageRotation">PDF页面本身的旋转角度</param>
        /// <param name="rotationMode">卷装旋转模式（可选，默认null表示自动计算）</param>
        /// <returns>布局计算结果</returns>
        private ImpositionResult CalculateRollMaterialLayoutOptimized(
            float pageWidth,
            float pageHeight,
            float usableWidth,
            RollMaterialConfiguration config,
            int pdfPageRotation = 0,
            RollRotationMode? rotationMode = null)
        {
            if (config.Rows > 0 || config.Columns > 0 ||
                config.LayoutMultiple > 1 ||
                rotationMode.HasValue)
            {
                return CalculateForcedRollMaterialLayout(pageWidth, pageHeight, usableWidth, config, rotationMode);
            }

            // 计算固定1行时的布局利用率（不旋转和旋转两种情况）

            // 不旋转时的固定1行布局
            int columnsWithoutRotation = (int)Math.Floor(usableWidth / pageWidth);
            float cellWidthNormal = usableWidth / columnsWithoutRotation;
            float cellHeightNormal = pageHeight;
            float totalPageAreaNormal = columnsWithoutRotation * pageWidth * pageHeight;
            float usedAreaNormal = columnsWithoutRotation * cellWidthNormal * cellHeightNormal;
            float utilizationNormal = (usedAreaNormal / totalPageAreaNormal) * 100;

            // 旋转90度时的固定1行布局
            int columnsWithRotation = (int)Math.Floor(usableWidth / pageHeight);
            float cellWidthRotated = usableWidth / columnsWithRotation;
            float cellHeightRotated = pageWidth;
            float totalPageAreaRotated = columnsWithRotation * pageWidth * pageHeight;
            float usedAreaRotated = columnsWithRotation * cellWidthRotated * cellHeightRotated;
            float utilizationRotated = (usedAreaRotated / totalPageAreaRotated) * 100;

            // 计算两种方案的实际使用宽度
            float usedWidthNormal = columnsWithoutRotation * pageWidth;   // 不旋转时的实际使用宽度
            float usedWidthRotated = columnsWithRotation * pageHeight;  // 旋转90度时的实际使用宽度

            LogHelper.Debug($"[ImpositionService] 卷装布局宽度使用: 不旋转使用宽度={usedWidthNormal:F1}mm, 旋转90度使用宽度={usedWidthRotated:F1}mm");

            // 计算宽度利用率（使用宽度 / 可用宽度 * 100%）
            float widthUtilizationNormal = (usedWidthNormal / usableWidth) * 100;
            float widthUtilizationRotated = (usedWidthRotated / usableWidth) * 100;

            // 选择最优旋转方向（优先级：1.手动强制 > 2.宽度利用率 > 3.列数 > 4.空间利用率）
            bool useRotation = false;

            // 如果指定了手动旋转模式，强制使用指定模式
            if (rotationMode.HasValue && rotationMode.Value != RollRotationMode.Auto)
            {
                // 强制模式
                useRotation = (rotationMode.Value == RollRotationMode.Force270Degree);
                LogHelper.Debug($"[ImpositionService] 卷装布局: 手动强制{(useRotation ? "270度" : "0度")}");
            }
            else
            {
                // 默认自动模式：如果是正方形尺寸，直接锁定不旋转（0度）
                if (IsSquareDimension(pageWidth, pageHeight, 0.09f))
                {
                    useRotation = false;
                    LogHelper.Debug($"[ImpositionService] 卷装布局: 尺寸 {pageWidth:F2}x{pageHeight:F2}mm 判定为正方形，默认锁定不旋转");
                }
                else
                {
                // 自动选择逻辑：宽度利用率是最关键的，决定了能否充分利用卷材宽度
                if (Math.Abs(widthUtilizationRotated - widthUtilizationNormal) > 0.1f) // 宽度利用率差异大于0.1%
                {
                    // 优先选择宽度利用率更高的方案
                    useRotation = widthUtilizationRotated > widthUtilizationNormal;
                }
                else // 宽度利用率基本相同时
                {
                    if (columnsWithRotation != columnsWithoutRotation)
                    {
                        // 选择列数更多的方案
                        useRotation = columnsWithRotation > columnsWithoutRotation;
                    }
                    else
                    {
                        // 列数也相同，选择空间利用率更高的方案
                        useRotation = utilizationRotated > utilizationNormal;
                    }
                }
                }
            }

            int optimalColumns = useRotation ? columnsWithRotation : columnsWithoutRotation;
            if (optimalColumns <= 0)
                return CreateFailedResult("页面无法容纳在卷装材料固定宽度内，请调整幅宽或页面方向。", MaterialType.RollMaterial);
            float actualUtilization = useRotation ? utilizationRotated : utilizationNormal;

            LogHelper.Debug($"[ImpositionService] 卷装布局计算: 固定1行-不旋转={columnsWithoutRotation}列(宽度利用率{widthUtilizationNormal:F1}%, 空间利用率{utilizationNormal:F1}%), 旋转90度={columnsWithRotation}列(宽度利用率{widthUtilizationRotated:F1}%, 空间利用率{utilizationRotated:F1}%), 选择{(useRotation ? "旋转" : "不旋转")}");

            // 计算单个页面的实际尺寸（考虑旋转）
            float actualPageWidth = useRotation ? pageHeight : pageWidth;
            float actualPageHeight = useRotation ? pageWidth : pageHeight;

            LogHelper.Debug($"[ImpositionService] 卷装页面尺寸: 实际页面宽度={actualPageWidth:F1}mm, 实际页面高度={actualPageHeight:F1}mm");

            // 计算需要的行数以满足最小长度要求（最终长度需要超过最小长度）
            float singleRowLength = config.MarginTop + actualPageHeight + config.MarginBottom;
            int minRowsRequired = (int)Math.Ceiling(config.MinLength / singleRowLength);

            // 确保至少有1行
            minRowsRequired = Math.Max(1, minRowsRequired);

            LogHelper.Debug($"[ImpositionService] 卷装多行计算: 最小长度要求={config.MinLength}mm, 单行长度={singleRowLength:F1}mm, 需要行数={minRowsRequired}行");

            int layoutQuantity = optimalColumns * minRowsRequired;

            // 计算最终的空间利用率（考虑实际行数）
            float totalMaterialArea = config.FixedWidth * (config.MarginTop + (minRowsRequired * actualPageHeight) + config.MarginBottom);
            float totalPageArea = layoutQuantity * pageWidth * pageHeight;
            float finalUtilization = (totalPageArea / totalMaterialArea) * 100;

            // 计算实际使用的材料长度
            float usedMaterialLength = config.MarginTop + (minRowsRequired * actualPageHeight) + config.MarginBottom;

            LogHelper.Debug($"[ImpositionService] 卷装最终布局: {minRowsRequired}行×{optimalColumns}列={layoutQuantity}个, 使用长度={usedMaterialLength:F1}mm, 材料面积={totalMaterialArea:F1}mm², 页面面积={totalPageArea:F1}mm², 总利用率={finalUtilization:F1}%");

            // 最终旋转角度 = 布局旋转角度（PDF页面旋转已在GetEffectivePageWidth/Height中考虑，不需要叠加）
            int layoutRotationAngle = useRotation ? 270 : 0;
            int finalRotationAngle = layoutRotationAngle;

            LogHelper.Debug($"[ImpositionService] 旋转角度计算: PDF页面旋转={pdfPageRotation}°(已在尺寸计算中考虑), 布局旋转={layoutRotationAngle}°, 最终输出旋转={finalRotationAngle}°");

            return new ImpositionResult
            {
                OptimalLayoutQuantity = layoutQuantity,
                Columns = optimalColumns,
                Rows = minRowsRequired,
                CellWidth = actualPageWidth,
                CellHeight = actualPageHeight,
                SpaceUtilization = finalUtilization,
                RowOneUtilization = actualUtilization, // 固定1行的利用率
                IsPrintable = layoutQuantity > 0,
                UseRotation = useRotation,
                RotationAngle = finalRotationAngle,
                Description = $"卷装{minRowsRequired}行×{optimalColumns}列 = {layoutQuantity}个，{(useRotation ? "旋转90度" : "不旋转")}，最终旋转{finalRotationAngle}°，单行利用率{actualUtilization:F1}%，总利用率{finalUtilization:F1}%"
            };
        }

        private ImpositionResult CalculateForcedRollMaterialLayout(
            float pageWidth,
            float pageHeight,
            float usableWidth,
            RollMaterialConfiguration config,
            RollRotationMode? rotationMode)
        {
            var normal = CreateRollCandidate(pageWidth, pageHeight, false);
            var rotated = CreateRollCandidate(pageHeight, pageWidth, true);
            var candidates = new[] { normal, rotated }.Where(candidate => candidate.IsFeasible);

            if (rotationMode == RollRotationMode.Force0Degree)
            {
                candidates = candidates.Where(candidate => !candidate.UseRotation);
            }
            else if (rotationMode == RollRotationMode.Force270Degree)
            {
                candidates = candidates.Where(candidate => candidate.UseRotation);
            }
            else if (IsSquareDimension(pageWidth, pageHeight, 0.09f))
            {
                // 默认模式下正方形优先不旋转
                candidates = candidates.Where(candidate => !candidate.UseRotation);
            }

            var selectedCandidates = candidates.ToList();
            if (selectedCandidates.Count == 0)
            {
                return CreateFailedResult(
                    "指定的列数无法容纳在卷装材料固定宽度内，或指定的旋转方向不可行。",
                    MaterialType.RollMaterial);
            }

            var selected = selectedCandidates
                .OrderByDescending(candidate => candidate.UsedWidth)
                .ThenByDescending(candidate => candidate.Columns)
                .ThenBy(candidate => candidate.UseRotation)
                .First();

            int rows = selected.Rows;
            float actualMaterialLength = Math.Max(config.MinLength, config.MarginTop + rows * selected.PageHeight + config.MarginBottom);
            if (float.IsNaN(actualMaterialLength) || float.IsInfinity(actualMaterialLength))
                return CreateFailedResult("数值无法计算，请检查页面尺寸和行数。", MaterialType.RollMaterial);
            int layoutQuantity = selected.Columns * rows;
            float materialArea = config.FixedWidth * actualMaterialLength;
            float spaceUtilization = materialArea <= 0
                ? 0
                : layoutQuantity * pageWidth * pageHeight / materialArea * 100;

            return new ImpositionResult
            {
                OptimalLayoutQuantity = layoutQuantity,
                Columns = selected.Columns,
                Rows = rows,
                CellWidth = selected.PageWidth,
                CellHeight = selected.PageHeight,
                SpaceUtilization = spaceUtilization,
                RowOneUtilization = selected.UsedWidth / usableWidth * 100,
                ActualMaterialLength = actualMaterialLength,
                IsPrintable = true,
                UseRotation = selected.UseRotation,
                RotationAngle = selected.UseRotation ? 270 : 0,
                Description = $"卷装{rows}行×{selected.Columns}列 = {layoutQuantity}页，实际材料长度{actualMaterialLength:F1}mm，利用率{spaceUtilization:F1}%"
            };

            (int Columns, int Rows, float PageWidth, float PageHeight, bool UseRotation, float UsedWidth, bool IsFeasible) CreateRollCandidate(
                float candidatePageWidth, float candidatePageHeight, bool useRotation)
            {
                int maximumColumns = (int)Math.Floor(usableWidth / candidatePageWidth);
                int columns = config.Columns > 0 ? config.Columns : maximumColumns;
                int minimumRows = Math.Max(1, (int)Math.Ceiling(
                    Math.Max(0, config.MinLength - config.MarginTop - config.MarginBottom) / candidatePageHeight));
                int rows = config.UseExactRows && config.Rows > 0
                    ? config.Rows : Math.Max(config.Rows, minimumRows);
                ApplyLayoutMultiple(ref rows, ref columns, config.Rows, config.Columns,
                    config.LayoutMultiple, config.LayoutMultipleDirection, true);
                float usedWidth = columns * candidatePageWidth;
                bool isFeasible = columns > 0 && columns <= maximumColumns && rows > 0 &&
                    (long)rows * columns <= int.MaxValue;
                return (columns, rows, candidatePageWidth, candidatePageHeight, useRotation, usedWidth, isFeasible);
            }
        }

        #endregion
    }
}
