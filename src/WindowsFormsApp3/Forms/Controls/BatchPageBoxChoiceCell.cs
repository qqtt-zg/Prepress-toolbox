using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Controls
{
    /// <summary>在既有文件表格中绘制两项尺寸基准，点击或空格键即可切换。</summary>
    public class BatchPageBoxChoiceCell : DataGridViewTextBoxCell
    {
        public Func<ThemeDefinition> GetTheme { get; set; }
        public Action<BatchFileItem, PdfPageBoxSource> SourceSelected { get; set; }

        public override object Clone()
        {
            var cell = (BatchPageBoxChoiceCell)base.Clone();
            cell.GetTheme = GetTheme;
            cell.SourceSelected = SourceSelected;
            return cell;
        }

        internal static Rectangle GetChoiceBounds(Size cellSize, PdfPageBoxSource source)
        {
            return new Rectangle(3, source == PdfPageBoxSource.MediaBox ? 3 : 24,
                Math.Max(0, cellSize.Width - 7), 19);
        }

        internal static string GetDimensionText(BatchFileItem item, PdfPageBoxSource source)
        {
            string dimensions = source == PdfPageBoxSource.MediaBox
                ? item.MediaBoxFinalDimensions : item.CropBoxFinalDimensions;
            if (!string.IsNullOrEmpty(dimensions)) return dimensions.Replace("x", "×");
            var box = source == PdfPageBoxSource.MediaBox ? item.PageBoxChoicePage.MediaBox : item.PageBoxChoicePage.CropBox;
            return $"{PdfTools.GetPageDimensionForDisplay(box.Width * 25.4 / 72):0.#}×{PdfTools.GetPageDimensionForDisplay(box.Height * 25.4 / 72):0.#}";
        }

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds,
            int rowIndex, DataGridViewElementStates cellState, object value, object formattedValue,
            string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            var item = DataGridView?.Rows[rowIndex].DataBoundItem as BatchFileItem;
            if (item?.PageBoxChoicePage == null)
            {
                base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue,
                    errorText, cellStyle, advancedBorderStyle, paintParts);
                return;
            }

            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue,
                errorText, cellStyle, advancedBorderStyle, paintParts & ~DataGridViewPaintParts.ContentForeground);
            var theme = GetTheme?.Invoke();
            Color primary = theme?.Primary ?? Color.FromArgb(22, 119, 255);
            Color surface = theme?.SurfaceLight ?? Color.White;
            Color text = theme?.TextPrimary ?? Color.FromArgb(38, 65, 102);
            using (var font = new Font("Microsoft YaHei UI", 7.5F))
            {
                foreach (var source in new[] { PdfPageBoxSource.MediaBox, PdfPageBoxSource.CropBox })
                {
                    var bounds = GetChoiceBounds(cellBounds.Size, source);
                    bounds.Offset(cellBounds.Location);
                    bool selected = item.PageBoxSource == source;
                    Color fill = selected ? Blend(surface, primary, 24) : surface;
                    Color border = selected ? primary : theme?.Border ?? Color.FromArgb(212, 219, 228);
                    using (var brush = new SolidBrush(fill)) graphics.FillRectangle(brush, bounds);
                    using (var pen = new Pen(border)) graphics.DrawRectangle(pen, bounds);
                    var radio = new Rectangle(bounds.Left + 4, bounds.Top + 5, 8, 8);
                    var smoothing = graphics.SmoothingMode;
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var pen = new Pen(selected ? primary : border)) graphics.DrawEllipse(pen, radio);
                    if (selected)
                    {
                        radio.Inflate(-2, -2);
                        using (var brush = new SolidBrush(primary)) graphics.FillEllipse(brush, radio);
                    }
                    graphics.SmoothingMode = smoothing;
                    string label = source == PdfPageBoxSource.MediaBox ? "媒体" : "裁剪";
                    string size = GetDimensionText(item, source);
                    var textBounds = new Rectangle(bounds.Left + 15, bounds.Top, bounds.Width - 17, bounds.Height);
                    TextRenderer.DrawText(graphics, $"{label} {size}", font, textBounds, selected ? primary : text,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                }
            }
        }

        protected override void OnMouseClick(DataGridViewCellMouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left || e.RowIndex < 0) return;
            var item = DataGridView?.Rows[e.RowIndex].DataBoundItem as BatchFileItem;
            if (item?.PageBoxChoicePage == null) return;
            foreach (var source in new[] { PdfPageBoxSource.MediaBox, PdfPageBoxSource.CropBox })
            {
                if (GetChoiceBounds(Size, source).Contains(e.X, e.Y))
                {
                    SourceSelected?.Invoke(item, source);
                    return;
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e, int rowIndex)
        {
            if (e.KeyCode == Keys.Space && DataGridView?.Rows[rowIndex].DataBoundItem is BatchFileItem item &&
                item.PageBoxChoicePage != null)
            {
                SourceSelected?.Invoke(item, item.PageBoxSource == PdfPageBoxSource.MediaBox
                    ? PdfPageBoxSource.CropBox : PdfPageBoxSource.MediaBox);
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            base.OnKeyDown(e, rowIndex);
        }

        private static Color Blend(Color surface, Color primary, int alpha)
        {
            return Color.FromArgb((surface.R * (255 - alpha) + primary.R * alpha) / 255,
                (surface.G * (255 - alpha) + primary.G * alpha) / 255,
                (surface.B * (255 - alpha) + primary.B * alpha) / 255);
        }
    }

    /// <summary>文件名前显示 SVG 警告图标，不改变现有行选择与 Excel 匹配底色。</summary>
    public class BatchPageBoxFileNameCell : DataGridViewTextBoxCell
    {
        private static readonly Lazy<Bitmap> WarningIcon = new Lazy<Bitmap>(() => LoadIcon(false));
        private static readonly Lazy<Bitmap> OverflowIcon = new Lazy<Bitmap>(() => LoadIcon(true));

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds,
            int rowIndex, DataGridViewElementStates cellState, object value, object formattedValue,
            string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            var item = DataGridView?.Rows[rowIndex].DataBoundItem as BatchFileItem;
            if (item?.PageBoxChoicePage == null)
            {
                base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue,
                    errorText, cellStyle, advancedBorderStyle, paintParts);
                return;
            }
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue,
                errorText, cellStyle, advancedBorderStyle, paintParts & ~DataGridViewPaintParts.ContentForeground);
            bool overflow = item.PageBoxIssueType == IssueType.OutOfBounds;
            var iconBounds = new Rectangle(cellBounds.Left + 3, cellBounds.Top + (cellBounds.Height - 16) / 2, 16, 16);
            using (var brush = new SolidBrush(overflow ? Color.FromArgb(255, 235, 238) : Color.FromArgb(255, 247, 230)))
                graphics.FillRectangle(brush, iconBounds);
            graphics.DrawImage(overflow ? OverflowIcon.Value : WarningIcon.Value, iconBounds);
            Color text = (cellState & DataGridViewElementStates.Selected) != 0
                ? cellStyle.SelectionForeColor : cellStyle.ForeColor;
            TextRenderer.DrawText(graphics, Convert.ToString(formattedValue), cellStyle.Font,
                new Rectangle(cellBounds.Left + 22, cellBounds.Top, Math.Max(0, cellBounds.Width - 25), cellBounds.Height), text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        private static Bitmap LoadIcon(bool overflow)
        {
            using (var stream = typeof(BatchPageBoxFileNameCell).Assembly.GetManifestResourceStream(
                "WindowsFormsApp3.Resources.Icons.PageBoxWarning.svg"))
            using (var reader = new System.IO.StreamReader(stream))
            {
                string svg = reader.ReadToEnd();
                if (overflow) svg = svg.Replace("#BF5D12", "#C93642");
                var document = Svg.SvgDocument.FromSvg<Svg.SvgDocument>(svg);
                return document.Draw(16, 16);
            }
        }
    }
}
