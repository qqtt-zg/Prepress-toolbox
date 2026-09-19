using WindowsFormsApp3.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp3.Forms.Controls;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3
{
    /// <summary>
    /// 方案五：智能工艺分组工作台控制器（部分类）
    /// 负责实现多卡片流式渲染、独立尺寸解析、真实排版计算、全选/选区与自由切分分组
    /// </summary>
    public partial class MaterialSelectFormModern
    {
        private readonly Dictionary<string, bool> _batchSortDirections = new Dictionary<string, bool>(StringComparer.Ordinal);
        private bool _isBatchLayoutUpdating = false;
        private bool _batchLayoutRefreshRequested = false;
        private bool _isRenderingGroupCards;

        private sealed class GroupDragPayload
        {
            public string SourceGroupId { get; set; }
            public List<string> FilePaths { get; set; } = new List<string>();
        }

        private List<BatchProcessGroup> _processGroups = new List<BatchProcessGroup>();
        public List<BatchProcessGroup> ProcessGroups => _processGroups;

        /// <summary>
        /// 将监控目录新增文件增量加入联动组，绝不重建或修改锁定组。
        /// </summary>
        public void AddPendingItemsToLinkedGroup(IEnumerable<BatchFileItem> pendingItems)
        {
            var items = pendingItems?.Where(item => item != null).ToList() ?? new List<BatchFileItem>();
            if (items.Count == 0) return;

            var targetGroup = _processGroups.FirstOrDefault(group => !group.IsLocked && !group.IsPreserveGroup);
            if (targetGroup == null)
            {
                targetGroup = CreateLinkedProcessGroup();
                _processGroups.Add(targetGroup);
            }

            foreach (var item in items)
            {
                ApplyGroupValues(item, targetGroup);
                item.IsLocked = false;
                item.IsPreserveJob = false;
                targetGroup.Items.Add(item);
            }
        }

        private BatchProcessGroup CreateLinkedProcessGroup()
        {
            return new BatchProcessGroup
            {
                GroupId = Guid.NewGuid().ToString("N"),
                GroupName = $"【{ToChineseNumber(_processGroups.Count + 1)}组】",
                IsPreserveGroup = false,
                IsLocked = false,
                Material = SelectedMaterial ?? "未指派材料",
                Process = FixedField ?? "",
                ColorMode = string.IsNullOrEmpty(ColorMode) ? "彩色" : ColorMode,
                FilmType = FilmType ?? "",
                MaterialType = (rollMaterialRadioButton != null && rollMaterialRadioButton.Checked) ? "卷装" : "平张",
                LayoutPattern = (foldingLayoutRadioButton != null && foldingLayoutRadioButton.Checked) ? "折手" : "连拼",
                Shape = SelectedShape.ToString(),
                RoundRadius = RoundRadius.ToString(),
                ImpositionMode = "",
                ExportPath = SelectedExportPath ?? ""
            };
        }

        private static void ApplyGroupValues(BatchFileItem item, BatchProcessGroup group)
        {
            item.GroupId = group.GroupId;
            item.GroupName = group.GroupName;
            item.Material = group.Material;
            item.Process = group.Process;
            item.ColorMode = group.ColorMode;
            item.FilmType = group.FilmType;
            item.MaterialType = group.MaterialType;
            item.LayoutPattern = group.LayoutPattern;
            item.Shape = group.Shape;
            item.RoundRadius = group.RoundRadius;
            item.ExportPath = group.ExportPath;
        }

        /// <summary>
        /// 将阿拉伯数字转为中文数字，用于组别名：一组、二组……
        /// </summary>
        private static string ToChineseNumber(int number)
        {
            string[] digits = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
            if (number <= 10) return digits[Math.Max(0, number)];
            if (number < 20) return "十" + digits[number % 10];
            if (number < 100)
            {
                string result = digits[number / 10] + "十";
                if (number % 10 != 0) result += digits[number % 10];
                return result;
            }
            return number.ToString();
        }

        /// <summary>
        /// 独立解析每个文件的真实成品尺寸（优先保留前缀 -> 真实PDF尺寸提取 -> 文件名规格正则 -> 弹窗尺寸兜底）
        /// </summary>
        public string ResolveFileDimensions(string filePath, string fileName, out double rawW, out double rawH)
        {
            rawW = 0;
            rawH = 0;
            try
            {
                string fileNameWithoutExt = !string.IsNullOrEmpty(fileName) ? Path.GetFileNameWithoutExtension(fileName) : "";

                // 1. 优先从返单保留前缀中提取（&MK-尺寸R圆角 或 &CU-尺寸）
                if (!string.IsNullOrEmpty(fileNameWithoutExt))
                {
                    var mkMatch = Regex.Match(fileNameWithoutExt, @"&MK-([0-9]+(?:\.[0-9]+)?)[xX*×]([0-9]+(?:\.[0-9]+)?)(?:R([0-9]+(?:\.[0-9]+)?))?");
                    if (mkMatch.Success)
                    {
                        double.TryParse(mkMatch.Groups[1].Value, out double w);
                        double.TryParse(mkMatch.Groups[2].Value, out double h);
                        if (w > 0 && h > 0)
                        {
                            if (AppSettings.SwapWidthHeightForDisplay && w < h)
                            {
                                double tmp = w; w = h; h = tmp;
                            }
                            rawW = w;
                            rawH = h;
                            string r = mkMatch.Groups[3].Success ? $"R{mkMatch.Groups[3].Value}" : "";
                            return $"{w:0.#}×{h:0.#}{r}";
                        }
                    }

                    var cuMatch = Regex.Match(fileNameWithoutExt, @"&CU-([0-9]+(?:\.[0-9]+)?)[xX*×]([0-9]+(?:\.[0-9]+)?)");
                    if (cuMatch.Success)
                    {
                        double.TryParse(cuMatch.Groups[1].Value, out double w);
                        double.TryParse(cuMatch.Groups[2].Value, out double h);
                        if (w > 0 && h > 0)
                        {
                            if (AppSettings.SwapWidthHeightForDisplay && w < h)
                            {
                                double tmp = w; w = h; h = tmp;
                            }
                            rawW = w;
                            rawH = h;
                            return $"{w:0.#}×{h:0.#}";
                        }
                    }
                }

                // 2. 真实物理 PDF 文件存在时，调用 IText7PdfTools 读取真实第一页尺寸
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    try
                    {
                        if (IText7PdfTools.GetFirstPageSize(filePath, out double rw, out double rh) && rw > 0 && rh > 0)
                        {
                            if (AppSettings.SwapWidthHeightForDisplay && rw < rh)
                            {
                                double tmp = rw; rw = rh; rh = tmp;
                            }
                            rawW = rw;
                            rawH = rh;
                            double bleed = SelectedTetBleed;
                            var dimensionService = ServiceLocator.Instance.GetDimensionCalculationService();
                            string cornerRadius = AppSettings.GetValue<string>("LastCornerRadius") ?? "0";
                            bool enableShapeProcessing = GetIsShapeSelected();
                            if (dimensionService != null)
                            {
                                return dimensionService.CalculateFinalDimensions(rw, rh, bleed, cornerRadius, enableShapeProcessing);
                            }
                            return $"{rw:0.#}×{rh:0.#}";
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Warn($"[ResolveFileDimensions] 读取PDF实际尺寸失败: {filePath}, {ex.Message}");
                    }
                }

                // 3. 文件名通用正则提取（如 45x45、84x54、PO1001_54x84-100pcs 等）
                if (!string.IsNullOrEmpty(fileNameWithoutExt))
                {
                    var dimMatch = Regex.Match(fileNameWithoutExt, @"(?:^|[^0-9.])([0-9]+(?:\.[0-9]+)?)\s*[xX*×]\s*([0-9]+(?:\.[0-9]+)?)(?:R([0-9]+(?:\.[0-9]+)?))?");
                    if (dimMatch.Success)
                    {
                        double.TryParse(dimMatch.Groups[1].Value, out double w);
                        double.TryParse(dimMatch.Groups[2].Value, out double h);
                        if (w > 0 && h > 0)
                        {
                            if (AppSettings.SwapWidthHeightForDisplay && w < h)
                            {
                                double tmp = w; w = h; h = tmp;
                            }
                            rawW = w;
                            rawH = h;
                            string r = dimMatch.Groups[3].Success ? $"R{dimMatch.Groups[3].Value}" : "";
                            return $"{w:0.#}×{h:0.#}{r}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"[ResolveFileDimensions] 解析尺寸异常: {ex.Message}");
            }

            rawW = _initialWidth;
            rawH = _initialHeight;
            return AdjustedDimensions ?? "";
        }

        public string ResolveFileDimensions(string filePath, string fileName)
        {
            return ResolveFileDimensions(filePath, fileName, out _, out _);
        }

        /// <summary>
        /// 根据右侧当前真实的排版模式、材料幅宽和各文件自身尺寸，独立计算每个文件的排版行列与版数
        /// </summary>
        public string CalculateBatchItemLayout(string dimensionsStr, string materialType = null, string layoutPattern = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dimensionsStr)) return "-";

                double w = 0, h = 0;
                var match = Regex.Match(dimensionsStr, @"([0-9]+(?:\.[0-9]+)?)\s*[xX*×]\s*([0-9]+(?:\.[0-9]+)?)");
                if (match.Success)
                {
                    double.TryParse(match.Groups[1].Value, out w);
                    double.TryParse(match.Groups[2].Value, out h);
                }

                if (w <= 0 || h <= 0) return "-";

                if (AppSettings.SwapWidthHeightForDisplay && w < h)
                {
                    double tmp = w; w = h; h = tmp;
                }

                double bleed = SelectedTetBleed;
                double itemW = w + bleed * 2;
                double itemH = h + bleed * 2;

                bool isRoll = !string.IsNullOrEmpty(materialType)
                    ? materialType == "卷装"
                    : rollMaterialRadioButton?.Checked == true;
                bool isFolding = !string.IsNullOrEmpty(layoutPattern)
                    ? layoutPattern == "折手"
                    : foldingLayoutRadioButton?.Checked == true;

                if (isRoll)
                {
                    var rollConfig = GetRollMaterialConfiguration();
                    double rollWidth = rollConfig != null ? (double)rollConfig.UsableWidth : 310.0;
                    if (rollWidth <= 0 && rollConfig != null) rollWidth = (double)rollConfig.FixedWidth;
                    if (rollWidth <= 0) rollWidth = 310.0;

                    double spacing = 3.0;

                    int cols1 = Math.Max(1, (int)Math.Floor((rollWidth + spacing) / (itemW + spacing)));
                    int cols2 = Math.Max(1, (int)Math.Floor((rollWidth + spacing) / (itemH + spacing)));

                    int bestCols = Math.Max(cols1, cols2);
                    int rows = isFolding ? 6 : 4;

                    if (_isDuplicateLayoutEnabled && bestCols % 2 != 0 && bestCols > 1)
                    {
                        bestCols -= 1;
                    }

                    int total = rows * bestCols;
                    return $"{rows}×{bestCols} · {total}版";
                }
                else
                {
                    var flatConfig = GetFlatSheetConfiguration();
                    double paperW = flatConfig != null ? (double)flatConfig.PrintableWidth : 460.0;
                    double paperH = flatConfig != null ? (double)flatConfig.PrintableHeight : 300.0;
                    if (paperW <= 0 && flatConfig != null) paperW = (double)flatConfig.PaperWidth;
                    if (paperH <= 0 && flatConfig != null) paperH = (double)flatConfig.PaperHeight;
                    if (paperW <= 0) paperW = 460.0;
                    if (paperH <= 0) paperH = 300.0;

                    double spacing = 3.0;

                    int c1 = Math.Max(1, (int)Math.Floor((paperW + spacing) / (itemW + spacing)));
                    int r1 = Math.Max(1, (int)Math.Floor((paperH + spacing) / (itemH + spacing)));
                    int total1 = c1 * r1;

                    int c2 = Math.Max(1, (int)Math.Floor((paperW + spacing) / (itemH + spacing)));
                    int r2 = Math.Max(1, (int)Math.Floor((paperH + spacing) / (itemW + spacing)));
                    int total2 = c2 * r2;

                    int bestRows, bestCols, bestTotal;
                    if (total2 > total1)
                    {
                        bestRows = r2; bestCols = c2; bestTotal = total2;
                    }
                    else
                    {
                        bestRows = r1; bestCols = c1; bestTotal = total1;
                    }

                    if (_isDuplicateLayoutEnabled && bestCols % 2 != 0 && bestCols > 1)
                    {
                        bestCols -= 1;
                        bestTotal = bestRows * bestCols;
                    }

                    return $"{bestRows}×{bestCols} · {bestTotal}版";
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"[CalculateBatchItemLayout] 计算单项版数异常: {ex.Message}");
                return "-";
            }
        }

        /// <summary>
        /// 当右侧出血值变化时，依据各文件自身物理尺寸独立刷新成品尺寸与排版版数
        /// </summary>
        public void UpdateAllFileDimensionsWithBleed()
        {
            try
            {
                if (_batchItems == null || _batchItems.Count == 0) return;

                double bleed = SelectedTetBleed;
                var dimensionService = ServiceLocator.Instance.GetDimensionCalculationService();
                string cornerRadius = AppSettings.GetValue<string>("LastCornerRadius") ?? "0";
                bool enableShapeProcessing = GetIsShapeSelected();

                foreach (var item in _batchItems)
                {
                    if (item.IsLocked) continue;

                    if (item.RawPdfWidth <= 0 || item.RawPdfHeight <= 0)
                    {
                        string resolved = ResolveFileDimensions(item.FilePath, item.FileName, out double rw, out double rh);
                        item.RawPdfWidth = rw;
                        item.RawPdfHeight = rh;
                        item.Dimensions = resolved;
                    }

                    if (item.RawPdfWidth > 0 && item.RawPdfHeight > 0)
                    {
                        if (dimensionService != null)
                        {
                            item.Dimensions = dimensionService.CalculateFinalDimensions(
                                item.RawPdfWidth, item.RawPdfHeight, bleed, cornerRadius, enableShapeProcessing);
                        }
                        else
                        {
                            double finW = Math.Max(1, item.RawPdfWidth - bleed * 2);
                            double finH = Math.Max(1, item.RawPdfHeight - bleed * 2);
                            item.Dimensions = $"{finW:0.#}×{finH:0.#}";
                        }
                    }
                }

                UpdateBatchLayoutColumns();
                RenderGroupCards();
                dgvBatchFiles?.Refresh();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[UpdateAllFileDimensionsWithBleed] 依据出血值刷新尺寸异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从文件名或保留前缀中解析工艺数据并组织智能工艺分组
        /// </summary>
        public void RebuildProcessGroups()
        {
            try
            {
                if (_batchItems == null || _batchItems.Count == 0)
                {
                    _processGroups.Clear();
                    return;
                }

                var tempComponents = new FileNameComponents();
                tempComponents.Prefixes = new Dictionary<string, string>
                {
                    { "订单号", "&ID-" },
                    { "材料", "&MT-" },
                    { "数量", "&DN-" },
                    { "工艺", "&DP-" },
                    { "尺寸", "&CU-" }
                };
                tempComponents.PreserveGroupConfig = new Dictionary<string, bool>
                {
                    { "&MT-", true },
                    { "&DP-", true },
                    { "&MK-", true },
                    { "&CU-", true },
                    { "&ID-", true },
                    { "&DN-", true },
                    { "material", true },
                    { "process", true },
                    { "材料", true },
                    { "工艺", true },
                    { "尺寸", true }
                };

                var groupsDict = new Dictionary<string, BatchProcessGroup>();
                var unassignedItems = new List<BatchFileItem>();

                foreach (var item in _batchItems)
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(item.FilePath ?? item.FileName ?? "");
                    
                    var preserveData = tempComponents.ExtractPreserveGroupData(fileNameWithoutExt);
                    string extractedMat = preserveData != null && preserveData.ContainsKey("材料") ? preserveData["材料"] : "";
                    string extractedProc = preserveData != null && preserveData.ContainsKey("工艺") ? preserveData["工艺"] : "";

                    if (string.IsNullOrEmpty(extractedMat))
                    {
                        var matM = Regex.Match(fileNameWithoutExt, @"&MT-([^&]+)");
                        if (matM.Success) extractedMat = matM.Groups[1].Value.Trim();
                    }
                    if (string.IsNullOrEmpty(extractedProc))
                    {
                        var procM = Regex.Match(fileNameWithoutExt, @"&DP-([^&]+)");
                        if (procM.Success) extractedProc = procM.Groups[1].Value.Trim();
                    }

                    bool isPreserve = !string.IsNullOrEmpty(extractedMat) || !string.IsNullOrEmpty(extractedProc) || fileNameWithoutExt.Contains("&MT-");

                    if (isPreserve)
                    {
                        item.IsPreserveJob = true;
                        item.IsLocked = true;
                        item.Material = extractedMat;
                        item.Process = extractedProc;
                        if (preserveData != null && preserveData.ContainsKey("尺寸"))
                        {
                            item.Dimensions = preserveData["尺寸"];
                        }

                        string groupKey = $"PRESERVE_{item.Material}_{item.Process}";
                        if (!groupsDict.TryGetValue(groupKey, out var grp))
                        {
                            grp = new BatchProcessGroup
                            {
                                GroupId = Guid.NewGuid().ToString("N"),
                                GroupName = $"【{ToChineseNumber(groupsDict.Count + 1)}组】",
                                IsPreserveGroup = true,
                                IsLocked = true,
                                Material = item.Material,
                                Process = item.Process,
                                ColorMode = (item.Process ?? "").Contains("黑白") ? "黑白" : ((item.Process ?? "").Contains("彩色") ? "彩色" : ColorMode),
                                FilmType = (item.Process ?? "").Replace("黑白", "").Replace("彩色", "").Trim(),
                                MaterialType = !string.IsNullOrEmpty(item.MaterialType)
                                    ? item.MaterialType
                                    : ((rollMaterialRadioButton != null && rollMaterialRadioButton.Checked) ? "卷装" : "平张"),
                                LayoutPattern = !string.IsNullOrEmpty(item.LayoutPattern)
                                    ? item.LayoutPattern
                                    : ((foldingLayoutRadioButton != null && foldingLayoutRadioButton.Checked) ? "折手" : "连拼"),
                                Shape = item.Shape ?? "RightAngle",
                                RoundRadius = item.RoundRadius ?? "0",
                                ImpositionMode = "历史锁定",
                                ExportPath = SelectedExportPath ?? item.ExportPath ?? ""
                            };
                            groupsDict[groupKey] = grp;
                        }
                        item.GroupId = grp.GroupId;
                        item.GroupName = grp.GroupName;
                        item.ColorMode = grp.ColorMode;
                        item.FilmType = grp.FilmType;
                        item.MaterialType = grp.MaterialType;
                        item.LayoutPattern = grp.LayoutPattern;
                        item.ExportPath = grp.ExportPath;
                        grp.Items.Add(item);
                    }
                    else
                    {
                        item.IsPreserveJob = false;
                        unassignedItems.Add(item);
                    }
                }

                if (unassignedItems.Count > 0)
                {
                    string newGroupKey = "NEW_JOB_GROUP";
                    var currentMat = SelectedMaterial ?? "未指派材料";
                    var currentProc = FixedField ?? "";
                    var currentShape = SelectedShape.ToString();
                    var currentRadius = RoundRadius.ToString();
                    var currentExportPath = SelectedExportPath ?? "";
                    var currentColorMode = string.IsNullOrEmpty(ColorMode) ? "彩色" : ColorMode;
                    var currentFilmType = FilmType ?? "";
                    var currentMaterialType = (rollMaterialRadioButton != null && rollMaterialRadioButton.Checked) ? "卷装" : "平张";
                    var currentLayoutPattern = (foldingLayoutRadioButton != null && foldingLayoutRadioButton.Checked) ? "折手" : "连拼";

                    var newGroup = new BatchProcessGroup
                    {
                        GroupId = Guid.NewGuid().ToString("N"),
                        GroupName = $"【{ToChineseNumber(groupsDict.Count + 1)}组】",
                        IsPreserveGroup = false,
                        IsLocked = false,
                        Material = currentMat,
                        Process = currentProc,
                        ColorMode = currentColorMode,
                        FilmType = currentFilmType,
                        MaterialType = currentMaterialType,
                        LayoutPattern = currentLayoutPattern,
                        Shape = currentShape,
                        RoundRadius = currentRadius,
                        ImpositionMode = "",
                        ExportPath = currentExportPath
                    };

                    foreach (var item in unassignedItems)
                    {
                        if (!item.IsLocked)
                        {
                            item.Material = currentMat;
                            item.Process = currentProc;
                            item.ColorMode = currentColorMode;
                            item.FilmType = currentFilmType;
                            item.MaterialType = currentMaterialType;
                            item.LayoutPattern = currentLayoutPattern;
                            item.Shape = currentShape;
                            item.RoundRadius = currentRadius;
                        }
                        item.GroupId = newGroup.GroupId;
                        item.GroupName = newGroup.GroupName;
                        item.ExportPath = currentExportPath;
                        newGroup.Items.Add(item);
                    }
                    groupsDict[newGroupKey] = newGroup;
                }

                _processGroups = groupsDict.Values.ToList();
                UpdateBatchLayoutColumns();
                RefreshGroupSummaryHeader();
                RenderGroupCards();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[RebuildProcessGroups] 重建工艺分组失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 当右侧面板参数变化时，同步刷新所有未锁定的新单组
        /// </summary>
        public void SyncCurrentSelectionsToActiveGroup()
        {
            try
            {
                if (_processGroups == null || _processGroups.Count == 0) return;

                var currentMat = SelectedMaterial ?? "";
                var currentProc = FixedField ?? "";
                var currentShape = SelectedShape.ToString();
                var currentRadius = RoundRadius.ToString();
                var currentExportPath = SelectedExportPath ?? "";
                var currentColorMode = string.IsNullOrEmpty(ColorMode) ? "彩色" : ColorMode;
                var currentFilmType = FilmType ?? "";
                var currentMaterialType = (rollMaterialRadioButton != null && rollMaterialRadioButton.Checked) ? "卷装" : "平张";
                var currentLayoutPattern = (foldingLayoutRadioButton != null && foldingLayoutRadioButton.Checked) ? "折手" : "连拼";

                foreach (var grp in _processGroups)
                {
                    if (!grp.IsPreserveGroup && !grp.IsLocked)
                    {
                        grp.Material = currentMat;
                        grp.Process = currentProc;
                        grp.ColorMode = currentColorMode;
                        grp.FilmType = currentFilmType;
                        grp.MaterialType = currentMaterialType;
                        grp.LayoutPattern = currentLayoutPattern;
                        grp.Shape = currentShape;
                        grp.RoundRadius = currentRadius;
                        grp.ExportPath = currentExportPath;

                        foreach (var item in grp.Items)
                        {
                            if (!item.IsLocked)
                            {
                                item.Material = currentMat;
                                item.Process = currentProc;
                                item.ColorMode = currentColorMode;
                                item.FilmType = currentFilmType;
                                item.MaterialType = currentMaterialType;
                                item.LayoutPattern = currentLayoutPattern;
                                item.Shape = currentShape;
                                item.RoundRadius = currentRadius;
                                item.ExportPath = currentExportPath;
                            }
                        }
                    }
                }
                UpdateBatchLayoutColumns();
                RenderGroupCards();
                dgvBatchFiles?.Refresh();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[SyncCurrentSelectionsToActiveGroup] 同步工艺参数失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 先以简化公式占位，再逐文件调用真实排版服务修正“行×列·版数”
        /// </summary>
        public async void UpdateBatchLayoutColumns()
        {
            try
            {
                if (_batchItems == null || _batchItems.Count == 0) return;
                if (_isBatchLayoutUpdating)
                {
                    _batchLayoutRefreshRequested = true;
                    return;
                }

                if (enableImpositionCheckbox?.Checked != true)
                {
                    foreach (var item in _batchItems)
                    {
                        item.LayoutInfo = "-";
                    }
                    RenderGroupCards();
                    dgvBatchFiles?.Refresh();
                    return;
                }

                _isBatchLayoutUpdating = true;
                _batchLayoutRefreshRequested = false;

                try
                {
                    foreach (var item in _batchItems)
                    {
                        item.LayoutInfo = "-";
                    }

                    var syncContext = SynchronizationContext.Current;
                    var snapshot = _batchItems.ToList();
                    var realLayouts = new List<string>();

                    foreach (var item in snapshot)
                    {
                        string realLayout = await TryCalculateRealLayoutAsync(item);
                        realLayouts.Add(realLayout ?? item.LayoutInfo);
                    }

                    void ApplyRealLayouts()
                    {
                        if (enableImpositionCheckbox?.Checked != true)
                        {
                            foreach (var item in snapshot)
                            {
                                item.LayoutInfo = "-";
                            }
                            RenderGroupCards();
                            dgvBatchFiles?.Refresh();
                            return;
                        }

                        for (int i = 0; i < snapshot.Count && i < realLayouts.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(realLayouts[i]))
                            {
                                snapshot[i].LayoutInfo = realLayouts[i];
                            }
                        }
                        RenderGroupCards();
                        dgvBatchFiles?.Refresh();
                    }

                    if (syncContext != null)
                    {
                        syncContext.Post(_ => ApplyRealLayouts(), null);
                    }
                    else
                    {
                        ApplyRealLayouts();
                    }
                }
                finally
                {
                    _isBatchLayoutUpdating = false;
                    if (_batchLayoutRefreshRequested && !this.IsDisposed)
                    {
                        _batchLayoutRefreshRequested = false;
                        UpdateBatchLayoutColumns();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[UpdateBatchLayoutColumns] 刷新排版版数失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 使用与右侧面板一致的真实排版服务计算单个文件的行列版数
        /// </summary>
        private async Task<string> TryCalculateRealLayoutAsync(BatchFileItem item)
        {
            try
            {
                if (item == null) return null;
                if (enableImpositionCheckbox?.Checked != true) return null;

                string materialType = !string.IsNullOrEmpty(item.MaterialType)
                    ? item.MaterialType
                    : ((rollMaterialRadioButton != null && rollMaterialRadioButton.Checked) ? "卷装" : "平张");

                object config = materialType == "卷装"
                    ? (object)GetRollMaterialConfiguration()
                    : GetFlatSheetConfiguration();
                if (config == null) return null;

                ImpositionPdfInfo pdfInfo = null;
                if (!string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                {
                    try
                    {
                        pdfInfo = await _impositionService.AnalyzePdfFileAsync(item.FilePath);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Warn($"[UpdateBatchLayoutColumns] 读取文件排版信息失败: {item.FilePath}, {ex.Message}");
                    }
                }
                if (pdfInfo == null)
                {
                    // 与右侧一致：文件不存在或分析失败时，用该文件自身尺寸构造模拟 PDF 信息继续计算
                    double pw = 0, ph = 0;
                    var dimMatch = Regex.Match(item.Dimensions ?? "", @"([0-9]+(?:\.[0-9]+)?)\s*[xX*×]\s*([0-9]+(?:\.[0-9]+)?)");
                    if (dimMatch.Success)
                    {
                        double.TryParse(dimMatch.Groups[1].Value, out pw);
                        double.TryParse(dimMatch.Groups[2].Value, out ph);
                    }
                    if (pw <= 0 || ph <= 0) return null;

                    pdfInfo = new ImpositionPdfInfo
                    {
                        FilePath = item.FilePath ?? "Demo.pdf",
                        FileName = item.FileName ?? "Demo.pdf",
                        PageCount = 1,
                        FirstPageSize = new WindowsFormsApp3.Utils.PageSize { Width = (float)pw, Height = (float)ph },
                        CropBoxWidth = (float)pw,
                        CropBoxHeight = (float)ph,
                        HasCropBox = true,
                        PageRotation = 0,
                        Errors = new List<WindowsFormsApp3.Utils.PageBoxError>()
                    };
                }

                ImpositionResult result = null;
                if (_isDuplicateLayoutEnabled)
                {
                    result = await _impositionService.CalculateOptimalEvenColumnsLayoutAsync(config, pdfInfo);
                }
                else if (config is RollMaterialConfiguration rollConfig)
                {
                    result = await _impositionService.CalculateRollMaterialLayoutAsync(
                        rollConfig, pdfInfo, default, GetCurrentRollRotationMode());
                }
                else if (config is FlatSheetConfiguration flatConfig)
                {
                    result = await _impositionService.CalculateFlatSheetLayoutAsync(flatConfig, pdfInfo);
                }

                if (result == null || !result.Success || result.Rows <= 0 || result.Columns <= 0)
                {
                    return null;
                }

                int quantity = result.OptimalLayoutQuantity > 0
                    ? result.OptimalLayoutQuantity
                    : result.Rows * result.Columns;
                return $"{result.Rows}×{result.Columns} · {quantity}版";
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"[UpdateBatchLayoutColumns] 单项真实排版计算异常: {item?.FileName}, {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 方案五：按工艺分组卡片动态渲染容器（呈现公共参数胶囊头与组内必异清单）
        /// </summary>
        public void RenderGroupCards()
        {
            if (pnlCardsContainer == null || this.Disposing || this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke((Action)RenderGroupCards);
                return;
            }

            try
            {
                _isRenderingGroupCards = true;
                pnlCardsContainer.SuspendLayout();
                pnlCardsContainer.Controls.Clear();

                if (_processGroups == null || _processGroups.Count == 0)
                {
                    _isRenderingGroupCards = false;
                    pnlCardsContainer.ResumeLayout(true);
                    return;
                }

                int currentTop = 6;
                int cardWidth = Math.Max(480, pnlCardsContainer.ClientSize.Width - 18);
                ThemeDefinition theme = _activeTheme;

                foreach (var grp in _processGroups)
                {
                    var cardPanel = new System.Windows.Forms.Panel
                    {
                        Location = new Point(6, currentTop),
                        Width = cardWidth,
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                        BackColor = theme?.Surface ?? Color.White,
                        Padding = new Padding(1)
                    };

                    var headerCard = new BatchGroupHeaderCard(grp);
                    if (theme != null)
                    {
                        headerCard.ApplyTheme(theme);
                    }

                    var dgvGroup = CreateGroupDataGridView(grp);
                    if (theme != null)
                    {
                        ApplyThemeToBatchDataGridView(dgvGroup, theme);
                    }
                    dgvGroup.Location = new Point(0, headerCard.Height);
                    dgvGroup.Dock = DockStyle.Top;
                    dgvGroup.Visible = !grp.IsCollapsed;

                    void ToggleCollapse()
                    {
                        grp.IsCollapsed = !grp.IsCollapsed;
                        dgvGroup.Visible = !grp.IsCollapsed;
                        headerCard.Invalidate();
                        cardPanel.Height = grp.IsCollapsed ? headerCard.Height : (headerCard.Height + dgvGroup.Height + 2);
                        RelayoutCards();
                    }

                    headerCard.HeaderClicked += (s, e) => ToggleCollapse();
                    headerCard.LockBadgeClicked += (s, e) => ToggleGroupLock(grp);

                    cardPanel.Controls.Add(dgvGroup);
                    cardPanel.Controls.Add(headerCard);
                    cardPanel.Height = grp.IsCollapsed ? headerCard.Height : (headerCard.Height + dgvGroup.Height + 2);

                    cardPanel.Paint += (s, e) =>
                    {
                        ThemeDefinition activeTheme = _activeTheme;
                        Color bc = activeTheme == null
                            ? (grp.IsPreserveGroup ? Color.FromArgb(211, 173, 247) : Color.FromArgb(203, 213, 225))
                            : (grp.IsPreserveGroup ? activeTheme.AccentColor4 : (grp.IsLocked ? activeTheme.Border : activeTheme.Primary));
                        ControlPaint.DrawBorder(e.Graphics, cardPanel.ClientRectangle, bc, ButtonBorderStyle.Solid);
                    };

                    pnlCardsContainer.Controls.Add(cardPanel);
                    currentTop += cardPanel.Height + 10;
                }

                _isRenderingGroupCards = false;
                pnlCardsContainer.ResumeLayout(true);
            }
            catch (Exception ex)
            {
                _isRenderingGroupCards = false;
                pnlCardsContainer.ResumeLayout(true);
                LogHelper.Error($"[RenderGroupCards] 渲染工艺分组卡片失败: {ex.Message}", ex);
            }
        }

        private void RelayoutCards()
        {
            if (pnlCardsContainer == null) return;
            pnlCardsContainer.SuspendLayout();
            int currentTop = 6;
            foreach (Control c in pnlCardsContainer.Controls)
            {
                if (c is System.Windows.Forms.Panel card)
                {
                    card.Top = currentTop;
                    currentTop += card.Height + 10;
                }
            }
            pnlCardsContainer.ResumeLayout(true);
        }

        private DataGridView CreateGroupDataGridView(BatchProcessGroup grp)
        {
            var dgv = new DataGridView
            {
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AllowDrop = true,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = true,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                RowTemplate = { Height = 26 },
                ColumnHeadersHeight = 26,
                ScrollBars = ScrollBars.None
            };

            var colIndex = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(BatchFileItem.SerialNumber),
                HeaderText = "序号",
                Width = 32,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            var colFileName = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "FileName",
                HeaderText = "文件名",
                Width = 150,
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };
            var colOrder = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "OrderNumber",
                HeaderText = "订单",
                Width = 50,
                ReadOnly = false
            };
            var colQty = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Quantity",
                HeaderText = "数量",
                Width = 55,
                ReadOnly = false
            };
            var colPageCount = CreatePageCountColumn(45);
            var colDim = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Dimensions",
                HeaderText = "尺寸",
                Width = 65,
                ReadOnly = true
            };
            var colLayout = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LayoutInfo",
                HeaderText = "排版/版数",
                Width = 85,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };

            dgv.Columns.Add(colIndex);
            dgv.Columns.Add(colFileName);
            dgv.Columns.Add(colOrder);
            dgv.Columns.Add(colQty);
            dgv.Columns.Add(colPageCount);
            dgv.Columns.Add(colDim);
            dgv.Columns.Add(colLayout);

            var bindingList = new BindingList<BatchFileItem>(grp.Items);
            dgv.DataSource = bindingList;
            RestoreBatchGridSelection(dgv, bindingList);
            dgv.SelectionChanged += (s, e) =>
            {
                if (!_isRenderingGroupCards)
                {
                    SyncBatchGridSelectionState(dgv, bindingList);
                }
            };

            Rectangle dragBox = Rectangle.Empty;
            dgv.MouseDown += (s, e) =>
            {
                dragBox = Rectangle.Empty;
                if (e.Button != MouseButtons.Left) return;

                var hit = dgv.HitTest(e.X, e.Y);
                if (hit.RowIndex < 0 || hit.RowIndex >= bindingList.Count ||
                    hit.ColumnIndex < 0 || hit.ColumnIndex >= dgv.Columns.Count) return;

                bool keepMultiSelection = (ModifierKeys & Keys.Control) == Keys.Control ||
                                           (ModifierKeys & Keys.Shift) == Keys.Shift;
                if (!keepMultiSelection)
                {
                    ClearSelectionsOutsideGrid(dgv);
                }

                DataGridViewCell clickedCell = dgv.Rows[hit.RowIndex].Cells[hit.ColumnIndex];
                if (!keepMultiSelection && !clickedCell.Selected)
                {
                    dgv.ClearSelection();
                    dgv.CurrentCell = clickedCell;
                    clickedCell.Selected = true;
                }

                // 普通列只负责选择/编辑，只有序号列允许启动拖拽。
                if (grp.IsLocked ||
                    dgv.Columns[hit.ColumnIndex].DataPropertyName != nameof(BatchFileItem.SerialNumber)) return;

                Size dragSize = SystemInformation.DragSize;
                dragBox = new Rectangle(
                    e.X - dragSize.Width / 2,
                    e.Y - dragSize.Height / 2,
                    dragSize.Width,
                    dragSize.Height);
            };

            dgv.MouseMove += (s, e) =>
            {
                if ((e.Button & MouseButtons.Left) != MouseButtons.Left ||
                    dragBox == Rectangle.Empty || dragBox.Contains(e.Location) || grp.IsLocked)
                {
                    return;
                }

                var selectedPaths = GetSelectedItemsFromGroupGrid(dgv, bindingList)
                    .Select(item => item.FilePath)
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                dragBox = Rectangle.Empty;
                if (selectedPaths.Count == 0) return;

                dgv.DoDragDrop(new GroupDragPayload
                {
                    SourceGroupId = grp.GroupId,
                    FilePaths = selectedPaths
                }, DragDropEffects.Move);
            };

            dgv.DragEnter += (s, e) =>
            {
                e.Effect = CanAcceptGroupDrop(e.Data, grp) ? DragDropEffects.Move : DragDropEffects.None;
            };
            dgv.DragOver += (s, e) =>
            {
                e.Effect = CanAcceptGroupDrop(e.Data, grp) ? DragDropEffects.Move : DragDropEffects.None;
            };
            dgv.DragDrop += (s, e) =>
            {
                if (!(e.Data?.GetData(typeof(GroupDragPayload)) is GroupDragPayload payload)) return;
                if (payload.SourceGroupId == grp.GroupId)
                {
                    Point clientPoint = dgv.PointToClient(new Point(e.X, e.Y));
                    int targetRowIndex = dgv.HitTest(clientPoint.X, clientPoint.Y).RowIndex;
                    if (targetRowIndex < 0)
                    {
                        targetRowIndex = bindingList.Count - 1;
                    }
                    if (targetRowIndex < 0 || targetRowIndex >= bindingList.Count) return;

                    string sourcePath = payload.FilePaths.FirstOrDefault();
                    string targetPath = bindingList[targetRowIndex]?.FilePath;
                    int sourceIndex = _batchItems.ToList().FindIndex(item =>
                        string.Equals(item.FilePath, sourcePath, StringComparison.OrdinalIgnoreCase));
                    int targetIndex = _batchItems.ToList().FindIndex(item =>
                        string.Equals(item.FilePath, targetPath, StringComparison.OrdinalIgnoreCase));
                    MoveBatchItem(sourceIndex, targetIndex);
                    return;
                }

                TryMoveFilesBetweenGroups(payload.SourceGroupId, grp.GroupId, payload.FilePaths);
            };

            int totalHeight = dgv.ColumnHeadersHeight + grp.Items.Count * dgv.RowTemplate.Height + 2;
            dgv.Height = Math.Max(26, totalHeight);

            dgv.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgv.IsCurrentCellDirty)
                {
                    dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            dgv.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < bindingList.Count)
                {
                    QueueBatchFilePreview(bindingList[e.RowIndex]?.FilePath);
                }

                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 &&
                    dgv.Columns[e.ColumnIndex].DataPropertyName == nameof(BatchFileItem.SerialNumber))
                {
                    bool keepMultiSelection = (ModifierKeys & Keys.Control) == Keys.Control ||
                                               (ModifierKeys & Keys.Shift) == Keys.Shift;
                    if (!keepMultiSelection)
                    {
                        ClearSelectionsOutsideGrid(dgv);
                    }
                    SelectBatchGridRow(dgv, e.RowIndex, keepMultiSelection);
                }
            };

            dgv.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete)
                {
                    _ = CancelSelectedBatchItemsAsync();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (e.Control && e.KeyCode == Keys.V)
                {
                    PasteQuantitiesFromClipboard();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            dgv.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0 &&
                    dgv.Columns[e.ColumnIndex].DataPropertyName == "Quantity")
                {
                    var clickedCell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex];

                    // 若右键点击的单元格尚不在选中集合内，则替换为唯一点选；
                    // 若已在多选集合中，则保留现有数量列多选状态，支持批量右键操作。
                    if (!clickedCell.Selected)
                    {
                        dgv.ClearSelection();
                        clickedCell.Selected = true;
                        dgv.CurrentCell = clickedCell;
                    }

                    ShowBatchContextMenu(dgv, new Point(e.X, e.Y));
                }
            };

            dgv.CellEndEdit += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < grp.Items.Count)
                {
                    var item = grp.Items[e.RowIndex];
                    var globalItem = _batchItems.FirstOrDefault(b => b.FilePath == item.FilePath);
                    if (globalItem != null)
                    {
                        globalItem.Quantity = item.Quantity;
                        globalItem.OrderNumber = item.OrderNumber;
                    }
                }
            };

            dgv.ColumnHeaderMouseClick += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && e.ColumnIndex < dgv.Columns.Count)
                {
                    string prop = dgv.Columns[e.ColumnIndex].DataPropertyName;
                    SortBatchFilesByProperty(prop, GetNextBatchSortDirection(prop));
                }
            };

            dgv.CellFormatting += (s, e) =>
            {
                try
                {
                    if (e.RowIndex < 0 || e.RowIndex >= grp.Items.Count) return;
                    var item = grp.Items[e.RowIndex];
                    if (item == null) return;
                    ThemeDefinition theme = _activeTheme;

                    if (item.IsPreserveJob)
                    {
                        e.CellStyle.BackColor = theme == null
                            ? Color.FromArgb(249, 240, 255)
                            : BlendThemeColor(theme.SurfaceLight, theme.AccentColor4, 36);
                        if (dgv.Columns[e.ColumnIndex].DataPropertyName == "FileName")
                        {
                            e.CellStyle.ForeColor = theme == null
                                ? Color.FromArgb(114, 46, 209)
                                : GetReadableThemeTextColor(e.CellStyle.BackColor, theme.AccentColor4);
                        }
                    }
                    if (dgv.Columns[e.ColumnIndex].DataPropertyName == "LayoutInfo")
                    {
                        Color background = e.CellStyle.BackColor.IsEmpty
                            ? (theme?.SurfaceLight ?? Color.White)
                            : e.CellStyle.BackColor;
                        e.CellStyle.ForeColor = theme == null
                            ? Color.FromArgb(19, 194, 194)
                            : GetReadableThemeTextColor(background, theme.AccentColor2);
                        e.CellStyle.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
                    }

                    ApplyBatchMatchStatusStyle(dgv, e, item);
                }
                catch { }
            };

            return dgv;
        }

        private static List<BatchFileItem> GetSelectedItemsFromGroupGrid(
            DataGridView dgv,
            BindingList<BatchFileItem> items)
        {
            var rowIndices = new HashSet<int>();
            foreach (DataGridViewCell cell in dgv.SelectedCells)
            {
                if (cell.RowIndex >= 0 && cell.RowIndex < items.Count)
                {
                    rowIndices.Add(cell.RowIndex);
                }
            }
            foreach (DataGridViewRow row in dgv.SelectedRows)
            {
                if (row.Index >= 0 && row.Index < items.Count)
                {
                    rowIndices.Add(row.Index);
                }
            }

            return rowIndices.OrderBy(index => index).Select(index => items[index]).ToList();
        }

        internal static void SelectBatchGridRow(DataGridView dgv, int rowIndex, bool keepExistingSelection)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
            if (!keepExistingSelection)
            {
                dgv.ClearSelection();
            }

            if (dgv.Rows[rowIndex].Cells.Count > 0)
            {
                dgv.CurrentCell = dgv.Rows[rowIndex].Cells[0];
            }
            foreach (DataGridViewCell cell in dgv.Rows[rowIndex].Cells)
            {
                cell.Selected = true;
            }
        }

        internal static void SyncBatchGridSelectionState(
            DataGridView dgv,
            BindingList<BatchFileItem> items)
        {
            if (dgv == null || items == null) return;
            for (int rowIndex = 0; rowIndex < items.Count && rowIndex < dgv.Rows.Count; rowIndex++)
            {
                DataGridViewRow row = dgv.Rows[rowIndex];
                items[rowIndex].IsSelected = row.Selected ||
                    row.Cells.Cast<DataGridViewCell>().Any(cell => cell.Selected);
            }
        }

        internal static void RestoreBatchGridSelection(
            DataGridView dgv,
            BindingList<BatchFileItem> items)
        {
            if (dgv == null || items == null) return;
            dgv.ClearSelection();
            for (int rowIndex = 0; rowIndex < items.Count && rowIndex < dgv.Rows.Count; rowIndex++)
            {
                if (items[rowIndex].IsSelected)
                {
                    SelectBatchGridRow(dgv, rowIndex, true);
                }
            }
        }

        private void ClearSelectionsOutsideGrid(DataGridView activeGrid)
        {
            if (pnlCardsContainer == null) return;
            foreach (DataGridView grid in pnlCardsContainer.Controls
                .Cast<Control>()
                .OfType<Panel>()
                .SelectMany(panel => panel.Controls.Cast<Control>().OfType<DataGridView>()))
            {
                if (!ReferenceEquals(grid, activeGrid))
                {
                    grid.ClearSelection();
                }
            }
        }

        private bool CanAcceptGroupDrop(IDataObject data, BatchProcessGroup targetGroup)
        {
            if (targetGroup == null || targetGroup.IsLocked || targetGroup.IsPreserveGroup || data == null) return false;
            if (!(data.GetData(typeof(GroupDragPayload)) is GroupDragPayload payload)) return false;

            var sourceGroup = _processGroups.FirstOrDefault(group => group.GroupId == payload.SourceGroupId);
            return sourceGroup != null && !sourceGroup.IsLocked && !sourceGroup.IsPreserveGroup;
        }

        public List<BatchFileItem> GetSelectedBatchItems()
        {
            var selectedList = new List<BatchFileItem>();
            try
            {
                if (pnlCardsContainer != null && pnlCardsContainer.Controls.Count > 0)
                {
                    foreach (Control c in pnlCardsContainer.Controls)
                    {
                        if (c is System.Windows.Forms.Panel card)
                        {
                            foreach (Control sub in card.Controls)
                            {
                                if (sub is DataGridView dgv && dgv.Visible)
                                {
                                    var itemsInDgv = dgv.DataSource as BindingList<BatchFileItem>;
                                    if (itemsInDgv == null) continue;

                                    var rowIndices = new HashSet<int>();
                                    foreach (DataGridViewCell cell in dgv.SelectedCells)
                                    {
                                        if (cell.RowIndex >= 0 && cell.RowIndex < itemsInDgv.Count)
                                        {
                                            rowIndices.Add(cell.RowIndex);
                                        }
                                    }
                                    foreach (DataGridViewRow row in dgv.SelectedRows)
                                    {
                                        if (row.Index >= 0 && row.Index < itemsInDgv.Count)
                                        {
                                            rowIndices.Add(row.Index);
                                        }
                                    }

                                    foreach (int r in rowIndices)
                                    {
                                        if (!selectedList.Contains(itemsInDgv[r]))
                                        {
                                            selectedList.Add(itemsInDgv[r]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (selectedList.Count == 0)
                {
                    selectedList.AddRange(_batchItems.Where(item => item.IsSelected));
                }

                if (selectedList.Count == 0)
                {
                    var rows = GetSelectedBatchRows();
                    foreach (int r in rows)
                    {
                        if (r >= 0 && r < _batchItems.Count)
                        {
                            selectedList.Add(_batchItems[r]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[GetSelectedBatchItems] 获取选中项异常: {ex.Message}", ex);
            }
            return selectedList;
        }

        public void ToggleGroupLock(BatchProcessGroup grp)
        {
            if (grp == null) return;
            try
            {
                grp.IsLocked = !grp.IsLocked;
                if (string.IsNullOrEmpty(grp.ExportPath))
                {
                    grp.ExportPath = SelectedExportPath ?? "";
                }
                foreach (var item in grp.Items)
                {
                    item.IsLocked = grp.IsLocked;
                    if (string.IsNullOrEmpty(item.ExportPath))
                    {
                        item.ExportPath = grp.ExportPath;
                    }
                    var globalItem = _batchItems.FirstOrDefault(b => b.FilePath == item.FilePath);
                    if (globalItem != null)
                    {
                        globalItem.IsLocked = grp.IsLocked;
                        globalItem.ExportPath = item.ExportPath;
                    }
                }
                RefreshGroupSummaryHeader();
                RenderGroupCards();
                dgvBatchFiles?.Refresh();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[ToggleGroupLock] 切换组锁定状态异常: {ex.Message}", ex);
            }
        }

        public void MoveSelectedFilesToGroup(BatchProcessGroup targetGroup)
        {
            if (targetGroup == null) return;
            try
            {
                var selectedItems = GetSelectedBatchItems();
                if (selectedItems.Count == 0) return;
                string sourceGroupId = selectedItems.Select(item => item.GroupId).Distinct().SingleOrDefault();
                if (string.IsNullOrEmpty(sourceGroupId)) return;
                TryMoveFilesBetweenGroups(sourceGroupId, targetGroup.GroupId, selectedItems.Select(item => item.FilePath));
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MoveSelectedFilesToGroup] 移动文件到分组失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 在两个未锁定联动组之间原子移动文件；任何校验失败均不修改集合。
        /// </summary>
        public bool TryMoveFilesBetweenGroups(
            string sourceGroupId,
            string targetGroupId,
            IEnumerable<string> filePaths)
        {
            var paths = filePaths?
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
            if (paths.Count == 0 || string.IsNullOrEmpty(sourceGroupId) || string.IsNullOrEmpty(targetGroupId))
            {
                return false;
            }

            var sourceGroup = _processGroups.FirstOrDefault(group => group.GroupId == sourceGroupId);
            var targetGroup = _processGroups.FirstOrDefault(group => group.GroupId == targetGroupId);
            if (sourceGroup == null || targetGroup == null || sourceGroup == targetGroup ||
                sourceGroup.IsLocked || targetGroup.IsLocked ||
                sourceGroup.IsPreserveGroup || targetGroup.IsPreserveGroup)
            {
                return false;
            }

            var movingItems = new List<Tuple<BatchFileItem, BatchFileItem>>();
            foreach (string path in paths)
            {
                var matches = sourceGroup.Items
                    .Where(item => string.Equals(item.FilePath, path, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var globalMatches = _batchItems
                    .Where(item => string.Equals(item.FilePath, path, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var groupMatches = _processGroups
                    .SelectMany(group => group.Items.Select(item => new { Group = group, Item = item }))
                    .Where(entry => string.Equals(entry.Item.FilePath, path, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (matches.Count != 1 || globalMatches.Count != 1 ||
                    groupMatches.Count != 1 || !ReferenceEquals(groupMatches[0].Group, sourceGroup) ||
                    !string.Equals(matches[0].GroupId, sourceGroupId, StringComparison.Ordinal) ||
                    !string.Equals(globalMatches[0].GroupId, sourceGroupId, StringComparison.Ordinal) ||
                    targetGroup.Items.Any(item => string.Equals(item.FilePath, path, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
                movingItems.Add(Tuple.Create(matches[0], globalMatches[0]));
            }

            foreach (var pair in movingItems)
            {
                var item = pair.Item1;
                var globalItem = pair.Item2;
                sourceGroup.Items.Remove(item);
                ApplyGroupValues(item, targetGroup);
                item.IsLocked = false;
                item.IsSelected = false;
                if (!ReferenceEquals(item, globalItem))
                {
                    ApplyGroupValues(globalItem, targetGroup);
                    globalItem.IsLocked = false;
                    globalItem.IsSelected = false;
                }
                targetGroup.Items.Add(item);
            }

            if (_processGroups.Count > 1 && sourceGroup.Items.Count == 0)
            {
                _processGroups.Remove(sourceGroup);
            }

            RefreshGroupSummaryHeader();
            RenderGroupCards();
            dgvBatchFiles?.Refresh();
            return true;
        }

        public void CreateGroupFromSelectedFiles()
        {
            try
            {
                var selectedItems = GetSelectedBatchItems();
                if (selectedItems.Count == 0) return;

                var currentMat = SelectedMaterial ?? "未指派材料";
                var currentProc = FixedField ?? "";
                var currentShape = SelectedShape.ToString();
                var currentRadius = RoundRadius.ToString();
                var currentExportPath = SelectedExportPath ?? "";
                var currentColorMode = string.IsNullOrEmpty(ColorMode) ? "彩色" : ColorMode;
                var currentFilmType = FilmType ?? "";
                var currentMaterialType = (rollMaterialRadioButton != null && rollMaterialRadioButton.Checked) ? "卷装" : "平张";
                var currentLayoutPattern = (foldingLayoutRadioButton != null && foldingLayoutRadioButton.Checked) ? "折手" : "连拼";

                var newGroup = new BatchProcessGroup
                {
                    GroupId = Guid.NewGuid().ToString("N"),
                    GroupName = $"【{ToChineseNumber(_processGroups.Count + 1)}组】",
                    IsPreserveGroup = false,
                    IsLocked = true,
                    Material = currentMat,
                    Process = currentProc,
                    ColorMode = currentColorMode,
                    FilmType = currentFilmType,
                    MaterialType = currentMaterialType,
                    LayoutPattern = currentLayoutPattern,
                    Shape = currentShape,
                    RoundRadius = currentRadius,
                    ImpositionMode = "",
                    ExportPath = currentExportPath
                };

                foreach (var sel in selectedItems)
                {
                    foreach (var g in _processGroups)
                    {
                        g.Items.RemoveAll(i => i.FilePath == sel.FilePath);
                    }

                    sel.GroupId = newGroup.GroupId;
                    sel.GroupName = newGroup.GroupName;
                    sel.Material = currentMat;
                    sel.Process = currentProc;
                    sel.ColorMode = currentColorMode;
                    sel.FilmType = currentFilmType;
                    sel.MaterialType = currentMaterialType;
                    sel.LayoutPattern = currentLayoutPattern;
                    sel.Shape = currentShape;
                    sel.RoundRadius = currentRadius;
                    sel.ExportPath = currentExportPath;
                    sel.IsLocked = true;
                    sel.IsSelected = false; // 分组后取消选中
                    newGroup.Items.Add(sel);

                    var globalItem = _batchItems.FirstOrDefault(b => b.FilePath == sel.FilePath);
                    if (globalItem != null)
                    {
                        globalItem.GroupId = sel.GroupId;
                        globalItem.GroupName = sel.GroupName;
                        globalItem.Material = sel.Material;
                        globalItem.Process = sel.Process;
                        globalItem.ColorMode = sel.ColorMode;
                        globalItem.FilmType = sel.FilmType;
                        globalItem.MaterialType = sel.MaterialType;
                        globalItem.LayoutPattern = sel.LayoutPattern;
                        globalItem.Shape = sel.Shape;
                        globalItem.RoundRadius = sel.RoundRadius;
                        globalItem.ExportPath = sel.ExportPath;
                        globalItem.IsLocked = true;
                        globalItem.IsSelected = false;
                    }
                }

                _processGroups.RemoveAll(g => g.Items.Count == 0);
                _processGroups.Add(newGroup);

                RefreshGroupSummaryHeader();
                RenderGroupCards();
                dgvBatchFiles?.Refresh();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[CreateGroupFromSelectedFiles] 新建分组失败: {ex.Message}", ex);
            }
        }

        public void ToggleSelectedFilesLock()
        {
            try
            {
                var selectedItems = GetSelectedBatchItems();
                if (selectedItems.Count == 0) return;

                foreach (var item in selectedItems)
                {
                    item.IsLocked = !item.IsLocked;
                    var globalItem = _batchItems.FirstOrDefault(b => b.FilePath == item.FilePath);
                    if (globalItem != null)
                    {
                        globalItem.IsLocked = item.IsLocked;
                    }
                }
                RefreshGroupSummaryHeader();
                RenderGroupCards();
                dgvBatchFiles?.Refresh();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[ToggleSelectedFilesLock] 切换锁定状态失败: {ex.Message}", ex);
            }
        }

        public void SetAllBatchFilesSelected(bool isSelected)
        {
            if (_batchItems == null) return;
            foreach (var item in _batchItems)
            {
                item.IsSelected = isSelected;
            }
            RenderGroupCards();
            dgvBatchFiles?.Refresh();
        }

        /// <summary>
        /// 按文件名点击表头排序（A-Z / Z-A 交替）
        /// </summary>
        public void SortBatchFilesByFileName(bool ascending = true)
        {
            SortBatchFilesByProperty(nameof(BatchFileItem.FileName), ascending);
        }

        /// <summary>
        /// 按数量列点击表头排序（数值升/降序交替）
        /// </summary>
        public void SortBatchFilesByQuantity(bool ascending = true)
        {
            SortBatchFilesByProperty(nameof(BatchFileItem.Quantity), ascending);
        }

        public void SortBatchFilesByProperty(string propertyName, bool ascending = true)
        {
            if (_batchItems == null || _batchItems.Count == 0) return;
            ReapplyBatchSortOrder(BatchFileItemSortService.Sort(_batchItems, propertyName, ascending));
        }

        private bool GetNextBatchSortDirection(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) propertyName = nameof(BatchFileItem.Index);
            bool ascending = !_batchSortDirections.TryGetValue(propertyName, out bool nextDirection) || nextDirection;
            _batchSortDirections[propertyName] = !ascending;
            return ascending;
        }

        private void ReapplyBatchSortOrder(List<BatchFileItem> sorted)
        {
            _batchItems.Clear();
            foreach (var item in sorted)
            {
                _batchItems.Add(item);
            }
            UpdateBatchOrderNumbers();
            ReorderExistingGroupItems();
            RefreshGroupSummaryHeader();
            RenderGroupCards();
            dgvBatchFiles?.Refresh();
        }

        private void ReorderExistingGroupItems()
        {
            var orderByPath = _batchItems
                .Select((item, index) => new { item.FilePath, Index = index })
                .Where(entry => !string.IsNullOrWhiteSpace(entry.FilePath))
                .GroupBy(entry => entry.FilePath, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);

            foreach (var group in _processGroups)
            {
                group.Items = group.Items
                    .OrderBy(item => orderByPath.TryGetValue(item.FilePath ?? string.Empty, out int index)
                        ? index
                        : int.MaxValue)
                    .ToList();
            }
        }

        public void SortBatchFilesByDimension(bool ascending = true)
        {
            SortBatchFilesByProperty(nameof(BatchFileItem.Dimensions), ascending);
        }

        private void ApplyBatchMatchStatusStyle(
            DataGridView dgv,
            DataGridViewCellFormattingEventArgs e,
            BatchFileItem item)
        {
            if (!IsExcelMatchingActive || item == null || item.IsExcelMatched) return;

            ThemeDefinition theme = _activeTheme;
            Color background = theme == null
                ? Color.FromArgb(255, 235, 238)
                : BlendThemeColor(theme.SurfaceLight, theme.Error, 24);
            e.CellStyle.BackColor = background;
            e.CellStyle.SelectionBackColor = theme == null
                ? Color.FromArgb(255, 205, 210)
                : BlendThemeColor(theme.SurfaceLight, theme.Error, 45);
            e.CellStyle.ForeColor = GetReadableThemeTextColor(background, theme?.Error ?? Color.FromArgb(198, 40, 40));
            e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
            dgv.Rows[e.RowIndex].HeaderCell.ToolTipText = "未匹配到 Excel 数据";
        }
    }
}
