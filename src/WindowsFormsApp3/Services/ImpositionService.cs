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
                    result.Success = true;
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
                    result.Success = true;
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
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>布局计算结果（确保列数为最优偶数）</returns>
        public async Task<ImpositionResult> CalculateOptimalEvenColumnsLayoutAsync(
            object config,
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
                    LogHelper.Debug("[ImpositionService] 开始一式两联最优偶数列布局计算");

                    // 根据配置类型调用相应的布局计算方法，但修改列数计算逻辑
                    ImpositionResult result = null;

                    if (config is FlatSheetConfiguration flatSheetConfig)
                    {
                        result = CalculateFlatSheetLayoutWithEvenColumns(flatSheetConfig, pdfInfo);
                    }
                    else if (config is RollMaterialConfiguration rollMaterialConfig)
                    {
                        result = CalculateRollMaterialLayoutWithEvenColumns(rollMaterialConfig, pdfInfo);
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
            FlatSheetConfiguration config, ImpositionPdfInfo pdfInfo)
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

            // 调用标准布局计算逻辑，但增加偶数列约束
            var result = CalculateOptimalLayoutWithEvenColumns(
                pageWidth, pageHeight, usableWidth, usableHeight, config, pdfInfo.PageRotation);

            result.MaterialType = MaterialType.FlatSheet;
            result.Success = true;

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
            int pdfPageRotation = 0)
        {
            // 计算不旋转时的布局
            int columnsNormal = (int)Math.Floor(usableWidth / pageWidth);
            int rowsNormal = (int)Math.Floor(usableHeight / pageHeight);
            int layoutNormal = columnsNormal * rowsNormal;

            // 计算旋转90度时的布局
            int columnsRotated = (int)Math.Floor(usableWidth / pageHeight);
            int rowsRotated = (int)Math.Floor(usableHeight / pageWidth);
            int layoutRotated = columnsRotated * rowsRotated;

            LogHelper.Debug($"[ImpositionService] 一式两联布局计算: 不旋转={columnsNormal}列×{rowsNormal}行={layoutNormal}页, 旋转90度={columnsRotated}列×{rowsRotated}行={layoutRotated}页");

            // 应用偶数列约束到两种布局方案
            var evenNormalResult = CalculateEvenColumnsLayout(columnsNormal, rowsNormal, false);
            var evenRotatedResult = CalculateEvenColumnsLayout(columnsRotated, rowsRotated, true);

            // 应用用户指定的行数限制（偶数列约束后）
            if (config.Rows > 0)
            {
                evenNormalResult.Rows = Math.Min(config.Rows, evenNormalResult.Rows);
                evenRotatedResult.Rows = Math.Min(config.Rows, evenRotatedResult.Rows);
            }
            if (config.Columns > 0)
            {
                evenNormalResult.Columns = Math.Min(config.Columns, evenNormalResult.Columns);
                evenRotatedResult.Columns = Math.Min(config.Columns, evenRotatedResult.Columns);
                // 确保用户指定的列数也是偶数
                if (evenNormalResult.Columns % 2 != 0) evenNormalResult.Columns--;
                if (evenRotatedResult.Columns % 2 != 0) evenRotatedResult.Columns--;
            }

            // 确保至少为1行2列（满足一式两联的基本要求）
            evenNormalResult.Rows = Math.Max(1, evenNormalResult.Rows);
            evenNormalResult.Columns = Math.Max(2, evenNormalResult.Columns);
            evenRotatedResult.Rows = Math.Max(1, evenRotatedResult.Rows);
            evenRotatedResult.Columns = Math.Max(2, evenRotatedResult.Columns);

            // 计算调整后的页数
            int evenLayoutNormal = evenNormalResult.Rows * evenNormalResult.Columns;
            int evenLayoutRotated = evenRotatedResult.Rows * evenRotatedResult.Columns;

            LogHelper.Debug($"[ImpositionService] 一式两联调整后: 不旋转={evenNormalResult.Columns}列×{evenNormalResult.Rows}行={evenLayoutNormal}页, 旋转90度={evenRotatedResult.Columns}列×{evenRotatedResult.Rows}行={evenLayoutRotated}页");

            // 选择页数更多的方案（保持与标准布局相同的选择逻辑）
            bool useRotation = evenLayoutRotated > evenLayoutNormal;
            var selectedResult = useRotation ? evenRotatedResult : evenNormalResult;

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

            LogHelper.Debug($"[ImpositionService] 一式两联最终选择: {(useRotation ? "旋转90度" : "不旋转")}，{selectedResult.Rows}行×{selectedResult.Columns}列={layoutQuantity}页，最终旋转{finalRotationAngle}°，利用率{spaceUtilization:F1}%");

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
                Description = $"一式两联平张{selectedResult.Rows}行×{selectedResult.Columns}列 = {layoutQuantity}页/纸，{(useRotation ? "旋转90度" : "不旋转")}，最终旋转{finalRotationAngle}°，利用率{spaceUtilization:F1}%（偶数列约束）"
            };
        }

        /// <summary>
        /// 对给定的行列配置应用偶数列约束
        /// </summary>
        private (int Columns, int Rows) CalculateEvenColumnsLayout(int columns, int rows, bool isRotated)
        {
            // 确保列数为偶数
            int evenColumns = columns;
            if (evenColumns % 2 != 0)
            {
                evenColumns = Math.Max(2, evenColumns - 1); // 减1变为偶数，至少保持2列
            }

            // 如果无法达到偶数列要求，返回最小可用配置
            if (evenColumns < 2)
            {
                evenColumns = 2;
                // 可能需要调整行数来适应2列的约束
                // 但这里我们保持行数不变，让上层逻辑处理
            }

            return (evenColumns, rows);
        }

        /// <summary>
        /// 计算卷装模式的最优偶数列布局（在标准布局计算基础上增加偶数列约束）
        /// </summary>
        private ImpositionResult CalculateRollMaterialLayoutWithEvenColumns(
            RollMaterialConfiguration config, ImpositionPdfInfo pdfInfo)
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

            // 调用标准布局计算逻辑，但增加偶数列约束
            var result = CalculateRollMaterialLayoutWithEvenColumnsOptimized(
                pageWidth, pageHeight, usableWidth, config, pdfInfo.PageRotation);

            result.MaterialType = MaterialType.RollMaterial;
            result.Success = true;

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
            int pdfPageRotation = 0)
        {
            // 计算固定1行时的最大可能列数（不旋转和旋转两种情况）
            int maxColumnsWithoutRotation = (int)Math.Floor(usableWidth / pageWidth);
            int maxColumnsWithRotation = (int)Math.Floor(usableWidth / pageHeight);

            LogHelper.Debug($"[ImpositionService] 卷装一式两联布局计算: 最大可能列数-不旋转={maxColumnsWithoutRotation}列, 旋转90度={maxColumnsWithRotation}列");

            // 应用偶数列约束到两种布局方案
            var evenNormalResult = CalculateEvenColumnsLayout(maxColumnsWithoutRotation, 1, false);
            var evenRotatedResult = CalculateEvenColumnsLayout(maxColumnsWithRotation, 1, true);

            // 应用用户指定的列数限制（偶数列约束后）
            if (config.Columns > 0)
            {
                evenNormalResult.Columns = Math.Min(config.Columns, evenNormalResult.Columns);
                evenRotatedResult.Columns = Math.Min(config.Columns, evenRotatedResult.Columns);
                // 确保用户指定的列数也是偶数
                if (evenNormalResult.Columns % 2 != 0) evenNormalResult.Columns--;
                if (evenRotatedResult.Columns % 2 != 0) evenRotatedResult.Columns--;
            }

            // 确保至少为2列（满足一式两联的基本要求）
            evenNormalResult.Columns = Math.Max(2, evenNormalResult.Columns);
            evenRotatedResult.Columns = Math.Max(2, evenRotatedResult.Columns);

            // 直接基于偶数列约束计算两种方案的利用率
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

            LogHelper.Debug($"[ImpositionService] 卷装一式两联调整后: 不旋转={columnsNormal}列(利用率{utilizationNormal:F1}%), 旋转90度={columnsRotated}列(利用率{utilizationRotated:F1}%)");

            // 计算两种方案的实际使用宽度
            float usedWidthNormal = columnsNormal * pageWidth;   // 不旋转时的实际使用宽度
            float usedWidthRotated = columnsRotated * pageHeight; // 旋转90度时的实际使用宽度
            
            LogHelper.Debug($"[ImpositionService] 卷装一式两联宽度使用: 不旋转使用宽度={usedWidthNormal:F1}mm, 旋转90度使用宽度={usedWidthRotated:F1}mm");

            // 选择最优旋转方向（优先级：1.列数最大 2.宽度利用率 3.空间利用率）
            // 在卷装材料中，优先选择能放置更多列的方案，提高生产效率
            bool useRotation = columnsRotated > columnsNormal;
            if (columnsRotated == columnsNormal) // 列数相同时
            {
                // 优先选择宽度利用率更大的方案
                useRotation = usedWidthRotated > usedWidthNormal;
                if (Math.Abs(usedWidthRotated - usedWidthNormal) < 0.1f) // 宽度差异小于0.1mm时认为相等
                {
                    // 最后选择空间利用率更高的方案
                    useRotation = utilizationRotated > utilizationNormal;
                }
            }
            
            int optimalColumns = useRotation ? evenRotatedResult.Columns : evenNormalResult.Columns;
            float actualUtilization = useRotation ? utilizationRotated : utilizationNormal;

            LogHelper.Debug($"[ImpositionService] 卷装一式两联选择: 不旋转={evenNormalResult.Columns}列(利用率{utilizationNormal:F1}%), 旋转90度={evenRotatedResult.Columns}列(利用率{utilizationRotated:F1}%), 选择{(useRotation ? "旋转" : "不旋转")}");

            // 计算单个页面的实际尺寸（考虑旋转）
            float actualPageWidth = useRotation ? pageHeight : pageWidth;
            float actualPageHeight = useRotation ? pageWidth : pageHeight;

            LogHelper.Debug($"[ImpositionService] 卷装一式两联页面尺寸: 实际页面宽度={actualPageWidth:F1}mm, 实际页面高度={actualPageHeight:F1}mm");

            // 计算需要的行数以满足最小长度要求（最终长度需要超过最小长度）
            float singleRowLength = config.MarginTop + actualPageHeight + config.MarginBottom;
            int minRowsRequired = (int)Math.Ceiling(config.MinLength / singleRowLength);

            // 确保至少有1行
            minRowsRequired = Math.Max(1, minRowsRequired);

            LogHelper.Debug($"[ImpositionService] 卷装一式两联多行计算: 最小长度要求={config.MinLength}mm, 单行长度={singleRowLength:F1}mm, 需要行数={minRowsRequired}行");

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

            LogHelper.Debug($"[ImpositionService] 卷装一式两联最终选择: {(useRotation ? "旋转90度" : "不旋转")}，{minRowsRequired}行×{optimalColumns}列={layoutQuantity}页，最终旋转{finalRotationAngle}°，利用率{finalUtilization:F1}%");

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
                Description = $"一式两联卷装{minRowsRequired}行×{optimalColumns}列 = {layoutQuantity}页/纸，{(useRotation ? "旋转90度" : "不旋转")}，最终旋转{finalRotationAngle}°，利用率{finalUtilization:F1}%（偶数列约束）"
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
        public async Task<ImpositionPdfInfo> AnalyzePdfFileAsync(
            string filePath,
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
                                var firstPage = document.GetFirstPage();
                                var mediaBox = firstPage.GetPageSize();
                                var cropBox = firstPage.GetCropBox();

                                // 获取页面旋转角度
                                pageRotation = firstPage.GetRotation();
                                LogHelper.Debug($"[排版分析] 检测到PDF页面旋转角度: {pageRotation}°");

                                // 获取原始宽高（未旋转前的尺寸）
                                float rawWidth = cropBox != null ? cropBox.GetWidth() : mediaBox.GetWidth();
                                float rawHeight = cropBox != null ? cropBox.GetHeight() : mediaBox.GetHeight();

                                // 如果有有效的裁剪框且不同于媒体框
                                if (cropBox != null &&
                                    Math.Abs(cropBox.GetWidth() - mediaBox.GetWidth()) > 0.1 &&
                                    Math.Abs(cropBox.GetHeight() - mediaBox.GetHeight()) > 0.1)
                                {
                                    hasCropBox = true;
                                }

                                // 注意：这里保存的是原始未旋转的宽高，具体旋转处理由GetEffectivePageWidth/Height方法负责
                                cropBoxWidth = rawWidth * 0.3528f; // 转换为毫米
                                cropBoxHeight = rawHeight * 0.3528f;

                                LogHelper.Debug($"[排版分析] 原始裁剪框尺寸（未旋转）: {cropBoxWidth:F1}x{cropBoxHeight:F1}mm");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
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
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>空白页补充结果</returns>
        public async Task<BlankPageResult> CalculateBlankPageInsertionAsync(
            int currentPageCount,
            int layoutQuantity,
            LayoutMode layoutMode,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var result = new BlankPageResult
                {
                    OriginalPageCount = currentPageCount,
                    LayoutQuantity = layoutQuantity,
                    LayoutMode = layoutMode,
                    TotalPageCount = currentPageCount,
                    RequiredSheets = (int)Math.Ceiling((double)currentPageCount / layoutQuantity)
                };

                if (layoutQuantity <= 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "布局数量必须大于0";
                    return result;
                }

                int remainder = currentPageCount % layoutQuantity;

                if (remainder == 0)
                {
                    result.BlankPagesNeeded = 0;
                    result.CalculationDetails = $"{layoutMode}模式，当前页数{currentPageCount}是布局数量{layoutQuantity}的倍数，无需补充空白页";
                    result.Success = true;
                    return result;
                }

                if (layoutMode == LayoutMode.Folding)
                {
                    // 折手模式：需要补足到布局数量的倍数
                    result.BlankPagesNeeded = layoutQuantity - remainder;
                    result.TotalPageCount = currentPageCount + result.BlankPagesNeeded;

                    // 计算插入位置（均匀分布策略）
                    int interval = currentPageCount / (result.BlankPagesNeeded + 1);
                    for (int i = 1; i <= result.BlankPagesNeeded; i++)
                    {
                        int position = i * interval;
                        result.BlankPagePositions.Add(position);
                    }

                    result.RequiredSheets = (int)Math.Ceiling((double)result.TotalPageCount / layoutQuantity);
                    result.CalculationDetails = $"折手模式：{currentPageCount}页 ÷ {layoutQuantity}页/纸 = 余{remainder}页，需补充{result.BlankPagesNeeded}个空白页，共{result.TotalPageCount}页，需要{result.RequiredSheets}张纸";
                }
                else
                {
                    // 连拼模式：不需要添加空白页
                    result.BlankPagesNeeded = 0;
                    result.CalculationDetails = $"连拼模式：{currentPageCount}页，按{layoutQuantity}页/纸布局，剩余{remainder}页直接拼接，无需补充空白页";
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
            bool useRotation = layoutRotated > layoutNormal;
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

            int optimalColumns = useRotation ? columnsWithRotation : columnsWithoutRotation;
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

        #endregion
    }
}