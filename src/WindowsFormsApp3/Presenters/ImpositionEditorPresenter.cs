using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Presenters
{
    /// <summary>共享编辑器的草稿、校验、文件来源和提交边界。</summary>
    public class ImpositionEditorPresenter
    {
        private readonly IImpositionService _service;
        private readonly ImpositionSettingsService _settings;
        private TemporaryImpositionParameters _baseline;
        private readonly TemporaryImpositionParameters _defaults;
        public bool IsGlobal { get; }
        public bool IsFlatSheet { get; set; }
        public bool Enabled { get; set; }
        public ImpositionEditorDraft Flat { get; private set; }
        public ImpositionEditorDraft Roll { get; private set; }
        public ImpositionEditorDraft Current => IsFlatSheet ? Flat : Roll;
        public ImpositionPdfInfo PdfInfo { get; private set; }
        public string Source { get; private set; }
        public string ReferenceWidth { get; private set; }
        public string ReferenceHeight { get; private set; }
        public string MaterialOrigin
        {
            get
            {
                if (IsGlobal) return "默认草稿";
                var baseline = Draft(_defaults, IsFlatSheet);
                bool Same(string a, string b) => Number(a, false, out float x) && Number(b, false, out float y) && x == y;
                return Same(Current.Width, baseline.Width) && Same(Current.Length, baseline.Length) &&
                    Same(Current.Left, baseline.Left) && Same(Current.Right, baseline.Right) &&
                    Same(Current.Top, baseline.Top) && Same(Current.Bottom, baseline.Bottom) && Current.Rotation == baseline.Rotation
                    ? "沿用默认参数" : "本次覆盖默认";
            }
        }

        public ImpositionEditorPresenter(IImpositionService service, bool global, bool flat,
            TemporaryImpositionParameters source, ImpositionPdfInfo pdfInfo = null, ImpositionSettingsService settings = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _settings = settings ?? new ImpositionSettingsService();
            IsGlobal = global;
            IsFlatSheet = flat;
            _defaults = _settings.Load();
            _baseline = (source ?? _defaults).Clone();
            _baseline.EnsureLayoutSnapshots();
            Flat = Draft(_baseline, true);
            Roll = Draft(_baseline, false);
            Enabled = _baseline.Enabled;
            PdfInfo = pdfInfo;
            ReferenceWidth = Format(pdfInfo?.GetEffectivePageWidth() ?? 0);
            ReferenceHeight = Format(pdfInfo?.GetEffectivePageHeight() ?? 0);
            Source = pdfInfo == null ? "未取得当前文件尺寸" : string.IsNullOrEmpty(pdfInfo.FilePath)
                ? "当前任务 · 所选页面框尺寸" : Path.GetFileName(pdfInfo.FilePath) + " · 所选页面框";
            if (global && PdfInfo == null) SetReference("90", "80");
        }

        private static string Format(float value) => value.ToString("0.###", CultureInfo.CurrentCulture);

        private static ImpositionEditorDraft Draft(TemporaryImpositionParameters p, bool flat)
        {
            p = p.ForMaterial(flat);
            return new ImpositionEditorDraft
            {
                Width = Format(flat ? p.PaperWidth : p.FixedWidth), Length = Format(flat ? p.PaperHeight : p.MinLength),
                Left = Format(flat ? p.MarginLeft : p.RollMarginLeft), Right = Format(flat ? p.MarginRight : p.RollMarginRight),
                Top = Format(flat ? p.MarginTop : p.RollMarginTop), Bottom = Format(flat ? p.MarginBottom : p.RollMarginBottom),
                SpacingX = Format(p.SpacingX), SpacingY = Format(p.SpacingY),
                Rows = p.RequestedRows.ToString(), Columns = p.RequestedColumns.ToString(),
                AutoRows = p.RequestedRows == 0, AutoColumns = p.RequestedColumns == 0,
                Quantity = Math.Max(1, p.RequestedLayoutQuantity).ToString(), Multiple = Math.Max(2, p.LayoutMultiple).ToString(),
                QuantityInitialized = p.RequestedLayoutQuantity > 0,
                QuantityRows = p.RequestedLayoutQuantity > 0 ? p.RequestedRows : 0,
                QuantityColumns = p.RequestedLayoutQuantity > 0 ? p.RequestedColumns : 0,
                LastSpecifiedStrategy = p.RequestedLayoutQuantity > 0 ? ImpositionLayoutStrategy.FixedQuantity : ImpositionLayoutStrategy.FixedGrid,
                Strategy = p.Strategy ?? (p.IsLayoutMultipleEnabled ? ImpositionLayoutStrategy.Multiple
                    : p.RequestedLayoutQuantity > 0 ? ImpositionLayoutStrategy.FixedQuantity
                    : p.RequestedRows > 0 || p.RequestedColumns > 0 ? ImpositionLayoutStrategy.FixedGrid : ImpositionLayoutStrategy.Automatic),
                Rotation = (flat ? p.FlatSheetRotationMode : p.RollMaterialRotationMode) ?? RollRotationMode.Auto,
                Direction = p.LayoutConstraintDirection ?? CopyMode.AutoByColumn
            };
        }

        public void SyncCommonLayout()
        {
            if (!IsGlobal) return;
            var other = IsFlatSheet ? Roll : Flat;
            other.Rows = Current.Rows; other.Columns = Current.Columns;
            other.AutoRows = Current.AutoRows; other.AutoColumns = Current.AutoColumns;
            other.Strategy = Current.Strategy;
        }

        public void Restore(bool automaticOnly = false, string field = null)
        {
            var restored = Draft(IsGlobal ? _baseline : _defaults, IsFlatSheet);
            if (automaticOnly) Current.Strategy = ImpositionLayoutStrategy.Automatic;
            else if (field == "width") Current.Width = restored.Width;
            else if (field == "length") Current.Length = restored.Length;
            else if (field == "margins")
            {
                Current.Left = restored.Left; Current.Right = restored.Right;
                Current.Top = restored.Top; Current.Bottom = restored.Bottom;
            }
            else if (field == "spacing")
            {
                Current.SpacingX = restored.SpacingX; Current.SpacingY = restored.SpacingY;
            }
            else if (field == "rotation") Current.Rotation = restored.Rotation;
            else if (field == "rows")
            {
                Current.Rows = restored.Rows; Current.Columns = restored.Columns;
                Current.AutoRows = restored.AutoRows; Current.AutoColumns = restored.AutoColumns;
                Current.Strategy = restored.Strategy;
            }
            else if (IsFlatSheet) Flat = restored;
            else Roll = restored;
            SyncCommonLayout();
        }

        public void Cancel()
        {
            Flat = Draft(_baseline, true); Roll = Draft(_baseline, false); Enabled = _baseline.Enabled;
        }

        private static bool Number(string text, bool positive, out float value)
        {
            bool parsed = float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
                float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            return parsed && !float.IsInfinity(value) && !float.IsNaN(value) && (positive ? value > 0 : value >= 0);
        }

        public bool SetReference(string width, string height)
        {
            if (!IsGlobal) return false;
            ReferenceWidth = width; ReferenceHeight = height;
            if (!Number(width, true, out float w) || !Number(height, true, out float h))
            {
                PdfInfo = null; Source = "预览尺寸无效；默认参数可独立保存"; return false;
            }
            PdfInfo = new ImpositionPdfInfo { HasCropBox = true, CropBoxWidth = w, CropBoxHeight = h };
            Source = "手动预览尺寸 · 不保存为纸张尺寸";
            return true;
        }

        public async Task<string> ReadPdfAsync(string path, bool paper, CancellationToken token)
        {
            if (!paper && !IsGlobal) return "本次成品尺寸由当前任务提供，不能在参数窗口替换。";
            try
            {
                var info = await _service.AnalyzePdfFileAsync(path, token);
                token.ThrowIfCancellationRequested();
                if (info == null || info.GetEffectivePageWidth() <= 0 || info.GetEffectivePageHeight() <= 0)
                    return "未能读取 PDF 的页面尺寸。";
                if (paper)
                {
                    Flat.Width = Format(info.GetEffectivePageWidth()); Flat.Length = Format(info.GetEffectivePageHeight());
                }
                else
                {
                    PdfInfo = info; Source = Path.GetFileName(path) + " · 第 1 页 · 预览参考";
                    ReferenceWidth = Format(info.GetEffectivePageWidth()); ReferenceHeight = Format(info.GetEffectivePageHeight());
                }
                return null;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { LogHelper.Error("读取排版编辑器的 PDF 尺寸失败", ex); return "读取 PDF 失败，请检查文件后重试。"; }
        }

        public bool TryGetParameters(bool flat, out TemporaryImpositionParameters value, out string error)
        {
            var d = flat ? Flat : Roll;
            value = _baseline.Clone();
            error = null;
            if (!Enum.IsDefined(typeof(ImpositionLayoutStrategy), d.Strategy))
            { error = "排版方式无效，请重新选择。"; return false; }
            if (!Number(d.Width, true, out float width) || !Number(d.Length, true, out float length) ||
                !Number(d.Left, false, out float left) || !Number(d.Right, false, out float right) ||
                !Number(d.Top, false, out float top) || !Number(d.Bottom, false, out float bottom))
            { error = "尺寸请输入正数，四边距请输入非负数。"; return false; }
            Number(d.SpacingX, false, out float spX);
            Number(d.SpacingY, false, out float spY);
            if (left + right >= width || (flat && top + bottom >= length))
            { error = flat ? "纸张宽高扣除四边距后必须有可印刷区域。" : "幅宽扣除左右边距后必须有可印刷区域。"; return false; }
            value.Enabled = Enabled;
            value.SpacingX = spX;
            value.SpacingY = spY;
            value.Strategy = d.Strategy;
            value.RequestedRows = value.RequestedColumns = value.RequestedLayoutQuantity = 0;
            value.IsLayoutMultipleEnabled = d.Strategy == ImpositionLayoutStrategy.Multiple;
            value.LayoutConstraintDirection = d.Direction;
            if (IsGlobal && d.Strategy == ImpositionLayoutStrategy.Multiple)
            { error = "倍数约束仅用于本次排版。"; return false; }
            if (d.Strategy == ImpositionLayoutStrategy.Automatic)
            {
                value.RequestedRows = value.RequestedColumns = 0;
                value.Strategy = ImpositionLayoutStrategy.Automatic;
            }
            else if (d.Strategy == ImpositionLayoutStrategy.FixedGrid)
            {
                int rows = 0, columns = 0;
                if (!d.AutoRows && !string.IsNullOrWhiteSpace(d.Rows) && d.Rows != "0")
                {
                    if (!int.TryParse(d.Rows, out rows) || rows <= 0)
                    { error = "行数请输入正整数或 0 (自动)。"; return false; }
                }
                if (!d.AutoColumns && !string.IsNullOrWhiteSpace(d.Columns) && d.Columns != "0")
                {
                    if (!int.TryParse(d.Columns, out columns) || columns <= 0)
                    { error = "列数请输入正整数或 0 (自动)。"; return false; }
                }
                if ((long)Math.Max(1, rows) * Math.Max(1, columns) > int.MaxValue)
                { error = "布局总数超过整数范围，请减少行列。"; return false; }
                value.RequestedRows = rows; value.RequestedColumns = columns;
                value.Strategy = (rows > 0 || columns > 0) ? ImpositionLayoutStrategy.FixedGrid : ImpositionLayoutStrategy.Automatic;
            }
            if (d.Strategy == ImpositionLayoutStrategy.FixedQuantity)
            {
                if (!int.TryParse(d.Quantity, out int quantity) || quantity <= 0)
                { error = "每纸布局请输入正整数。"; return false; }
                value.RequestedLayoutQuantity = quantity;
            }
            if (value.IsLayoutMultipleEnabled)
            {
                if (!int.TryParse(d.Multiple, out int multiple) || multiple < 2 || multiple > 16)
                { error = "倍数 N 请输入 2–16 的整数。"; return false; }
                value.LayoutMultiple = multiple;
            }
            if (flat)
            {
                value.PaperWidth = width; value.PaperHeight = length; value.IsPaperSizeUnlocked = true;
                value.MarginLeft = left; value.MarginRight = right; value.MarginTop = top; value.MarginBottom = bottom;
                value.FlatSheetRotationMode = d.Rotation;
            }
            else
            {
                value.FixedWidth = width; value.MinLength = length; value.IsRollSizeUnlocked = true;
                value.RollMarginLeft = left; value.RollMarginRight = right; value.RollMarginTop = top; value.RollMarginBottom = bottom;
                value.RollMaterialRotationMode = d.Rotation;
            }
            value.IsForceRotationEnabled = false;
            value.StoreLayout(flat);
            return true;
        }

        public string ValidateDefaults()
        {
            if (!TryGetParameters(true, out _, out string error)) return "平张：" + error;
            if (!TryGetParameters(false, out _, out error)) return "卷装：" + error;
            return null;
        }

        public Task<List<ImpositionResult>> PreviewAsync(TemporaryImpositionParameters p, CancellationToken token)
        {
            return new TemporaryImpositionParametersPresenter(_service, IsFlatSheet, PdfInfo).PreviewAsync(p, token);
        }

        public bool TrySubmit(ImpositionResult layout, out TemporaryImpositionParameters value, out string error)
        {
            if (!TryGetParameters(IsFlatSheet, out value, out error)) return false;
            if (IsGlobal)
            {
                error = ValidateDefaults();
                if (error != null) return false;
                TryGetParameters(true, out var flat, out _);
                TryGetParameters(false, out var roll, out _);
                flat.FixedWidth = roll.FixedWidth; flat.MinLength = roll.MinLength;
                flat.RollMarginLeft = roll.RollMarginLeft; flat.RollMarginRight = roll.RollMarginRight;
                flat.RollMarginTop = roll.RollMarginTop; flat.RollMarginBottom = roll.RollMarginBottom;
                flat.RollMaterialRotationMode = roll.RollMaterialRotationMode;
                flat.RollLayout = roll.RollLayout?.Clone();
                value = flat;
                try { _settings.Save(value); _baseline = value.Clone(); return true; }
                catch (Exception ex) { LogHelper.Error("保存排版默认设置失败", ex); error = "保存默认设置失败，请重试。"; return false; }
            }
            if (Enabled && (PdfInfo == null || layout?.Success != true || !layout.IsPrintable))
            { error = "当前文件没有可用布局，请调整参数或停用本次排版。"; return false; }
            if (Enabled && value.Strategy == ImpositionLayoutStrategy.FixedQuantity)
            { value.RequestedRows = layout.Rows; value.RequestedColumns = layout.Columns; value.StoreLayout(IsFlatSheet); }
            return true;
        }

        public static ImpositionPreviewGeometry Geometry(TemporaryImpositionParameters p, bool flat, ImpositionResult r)
        {
            float width = flat ? p.PaperWidth : p.FixedWidth;
            float top = flat ? p.MarginTop : p.RollMarginTop, bottom = flat ? p.MarginBottom : p.RollMarginBottom;
            float left = flat ? p.MarginLeft : p.RollMarginLeft, right = flat ? p.MarginRight : p.RollMarginRight;
            float spX = p.SpacingX, spY = p.SpacingY;
            float gridW = (r != null && r.Columns > 0) ? r.Columns * r.CellWidth + Math.Max(0, r.Columns - 1) * spX : 0;
            float gridH = (r != null && r.Rows > 0) ? r.Rows * r.CellHeight + Math.Max(0, r.Rows - 1) * spY : 0;
            float height = flat ? p.PaperHeight : (r != null ? Math.Max(p.MinLength, top + gridH + bottom) : p.MinLength);
            float x = left + Math.Max(0, width - left - right - gridW) / 2;
            float y = top + Math.Max(0, height - top - bottom - gridH) / 2;
            return new ImpositionPreviewGeometry
            {
                Width = width, Height = height, Left = x, Right = width - x - gridW,
                Top = y, Bottom = height - y - gridH,
                PrintableLeft = left, PrintableTop = top, PrintableWidth = Math.Max(0, width - left - right),
                PrintableHeight = Math.Max(0, height - top - bottom),
                SpacingX = spX, SpacingY = spY,
                GridWidth = gridW, GridHeight = gridH
            };
        }
    }
}
