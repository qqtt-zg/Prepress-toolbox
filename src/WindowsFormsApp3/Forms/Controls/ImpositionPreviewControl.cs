using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Forms.Controls
{
    /// <summary>仅渲染 Presenter 给出的几何结果，不重新计算生产布局。</summary>
    public class ImpositionPreviewControl : Control
    {
        public ImpositionResult LayoutResult { get; set; }
        public ImpositionPreviewGeometry Geometry { get; set; }
        public string EmptyText { get; set; } = "等待排版结果";
        public bool Detail { get; set; }
        public float Zoom { get; set; } = 1;
        public int Position { get; set; }
        public int HorizontalPosition { get; set; } = 50;

        public ImpositionPreviewControl()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(247, 249, 252);
            Name = "impositionPreviewCanvas";
            AccessibleName = "居中的排版预览";
            AccessibleRole = AccessibleRole.Graphic;
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var dots = new SolidBrush(Color.FromArgb(226, 233, 242)))
                for (int y = 8; y < Height; y += 16)
                    for (int x = 8; x < Width; x += 16) g.FillEllipse(dots, x, y, 1, 1);
            var r = LayoutResult;
            var p = Geometry;
            if (p == null || p.Width <= 0 || p.Height <= 0)
            {
                TextRenderer.DrawText(g, EmptyText, Font, ClientRectangle, Color.FromArgb(86, 107, 130),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                return;
            }
            float availableWidth = Math.Max(1, Width - 70), availableHeight = Math.Max(1, Height - 64);
            float scale = (Detail ? availableWidth / p.Width : Math.Min(availableWidth / p.Width, availableHeight / p.Height)) * Zoom;
            float x0 = Zoom > 1 ? 35 - Math.Max(0, p.Width * scale - availableWidth) * HorizontalPosition / 100F
                : (Width - p.Width * scale) / 2;
            float y0 = Detail || Zoom > 1 ? 32 - Math.Max(0, p.Height * scale - availableHeight) * Position / 100f
                : (Height - p.Height * scale) / 2;
            var paper = new RectangleF(x0, y0, p.Width * scale, p.Height * scale);
            var state = g.Save();
            g.SetClip(new Rectangle(0, 22, Width, Math.Max(1, Height - 44)));
            var clip = new RectangleF(0, 22, Width, Math.Max(1, Height - 44));
            void Outline(Pen pen, RectangleF rect)
            {
                float left = Math.Max(clip.Left, rect.Left), right = Math.Min(clip.Right, rect.Right);
                float top = Math.Max(clip.Top, rect.Top), bottom = Math.Min(clip.Bottom, rect.Bottom);
                if (left >= right || top >= bottom) return;
                if (rect.Left >= clip.Left) g.DrawLine(pen, rect.Left, top, rect.Left, bottom);
                if (rect.Right <= clip.Right) g.DrawLine(pen, rect.Right, top, rect.Right, bottom);
                if (rect.Top >= clip.Top) g.DrawLine(pen, left, rect.Top, right, rect.Top);
                if (rect.Bottom <= clip.Bottom) g.DrawLine(pen, left, rect.Bottom, right, rect.Bottom);
            }
            using (var border = new Pen(Color.FromArgb(71, 85, 105), 1.5f))
            using (var printable = new Pen(Color.FromArgb(18, 100, 214), 1.5f) { DashStyle = DashStyle.Dash })
            using (var printableFill = new SolidBrush(Color.FromArgb(237, 245, 255)))
            using (var cell = new SolidBrush(Color.FromArgb(230, 241, 255)))
            using (var grid = new Pen(Color.FromArgb(22, 119, 255), 1.2f))
            {
                g.FillRectangle(Brushes.White, RectangleF.Intersect(paper, clip));
                Outline(border, paper);

                var printableRect = new RectangleF(x0 + p.PrintableLeft * scale, y0 + p.PrintableTop * scale,
                    p.PrintableWidth * scale, p.PrintableHeight * scale);

                if (r?.Success != true || r.CellWidth <= 0 || r.CellHeight <= 0)
                {
                    // 【方案 B 纯净大版图示】：没有排版网格时，安全区填充柔和淡蓝，居中绘制清晰规格标注
                    g.FillRectangle(printableFill, RectangleF.Intersect(printableRect, clip));
                    Outline(printable, printableRect);

                    if (printableRect.Width > 70 && printableRect.Height > 50)
                    {
                        var textRect = Rectangle.Round(printableRect);
                        using (var titleFont = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold))
                        using (var numFont = new Font("Consolas", 15F, FontStyle.Bold))
                        using (var subFont = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular))
                        {
                            int cy = textRect.Top + textRect.Height / 2;
                            TextRenderer.DrawText(g, "净有效印刷区", titleFont,
                                new Rectangle(textRect.Left, cy - 26, textRect.Width, 20),
                                Color.FromArgb(18, 100, 214), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                            TextRenderer.DrawText(g, $"{p.PrintableWidth:0.##} × {p.PrintableHeight:0.##} mm", numFont,
                                new Rectangle(textRect.Left, cy - 5, textRect.Width, 24),
                                Color.FromArgb(30, 58, 138), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                            TextRenderer.DrawText(g, "四周边距留白已扣除", subFont,
                                new Rectangle(textRect.Left, cy + 19, textRect.Width, 18),
                                Color.FromArgb(100, 116, 139), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                }
                else
                {
                    // 【任务模式】：带真实物理间距与真实比例成品的满血生产网格
                    Outline(printable, printableRect);
                    float gx = x0 + p.Left * scale, gy = y0 + p.Top * scale;
                    float cw = r.CellWidth * scale, ch = r.CellHeight * scale;
                    float spX = p.SpacingX * scale, spY = p.SpacingY * scale;

                    if (cw > 0 && ch > 0)
                    {
                        if ((long)r.Rows * r.Columns <= 120 && cw > 12 && ch > 12)
                        {
                            using (var numFont = new Font("Consolas", Math.Max(8F, Math.Min(11F, ch * 0.35F)), FontStyle.Bold))
                            {
                                int idx = 1;
                                for (int row = 0; row < r.Rows; row++)
                                {
                                    for (int col = 0; col < r.Columns; col++)
                                    {
                                        var cellRect = new RectangleF(gx + col * (cw + spX), gy + row * (ch + spY), cw, ch);
                                        if (clip.IntersectsWith(cellRect))
                                        {
                                            g.FillRectangle(cell, cellRect);
                                            Outline(grid, cellRect);
                                            TextRenderer.DrawText(g, (idx).ToString(), numFont, Rectangle.Round(cellRect),
                                                Color.FromArgb(29, 78, 216), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                                        }
                                        idx++;
                                    }
                                }
                            }
                        }
                        else
                        {
                            float totalGridW = r.Columns * cw + Math.Max(0, r.Columns - 1) * spX;
                            float totalGridH = r.Rows * ch + Math.Max(0, r.Rows - 1) * spY;
                            var gridBounds = new RectangleF(gx, gy, totalGridW, totalGridH);
                            g.FillRectangle(cell, RectangleF.Intersect(gridBounds, clip));
                            Outline(grid, gridBounds);
                        }
                    }
                }
            }
            g.Restore(state);

            // 绘制贴近纸张外框的真实物理尺寸标尺（与 HTML 完全一致）
            using (var rulerPen = new Pen(Color.FromArgb(148, 163, 184), 1))
            using (var rulerFont = new Font("Consolas", 8.5F, FontStyle.Regular))
            using (var marginFont = new Font("Microsoft YaHei UI", 8F, FontStyle.Regular))
            {
                // 顶部幅宽标尺
                int rYTop = (int)Math.Max(2, y0 - 15);
                g.DrawLine(rulerPen, x0, rYTop + 13, x0 + p.Width * scale, rYTop + 13);
                TextRenderer.DrawText(g, $"大版幅宽: {p.Width:0.##} mm", rulerFont,
                    new Rectangle((int)x0, rYTop - 2, (int)(p.Width * scale), 14),
                    Color.FromArgb(71, 85, 105), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // 底部高度标尺
                int rYBottom = (int)Math.Min(Height - 16, y0 + p.Height * scale + 3);
                g.DrawLine(rulerPen, x0, rYBottom, x0 + p.Width * scale, rYBottom);
                TextRenderer.DrawText(g, $"大版高度: {p.Height:0.##} mm", rulerFont,
                    new Rectangle((int)x0, rYBottom + 1, (int)(p.Width * scale), 14),
                    Color.FromArgb(71, 85, 105), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // 左右留白标注 (严格对应四周边距设定值)
                float rightMargin = Math.Max(0, p.Width - p.PrintableLeft - p.PrintableWidth);
                TextRenderer.DrawText(g, $"左 {p.PrintableLeft:0.##}mm", marginFont,
                    new Rectangle(0, (int)(y0 + (p.Height * scale) / 2 - 10), (int)x0 - 3, 20),
                    Color.FromArgb(100, 116, 139), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, $"右 {rightMargin:0.##}mm", marginFont,
                    new Rectangle((int)(x0 + p.Width * scale + 3), (int)(y0 + (p.Height * scale) / 2 - 10), Math.Max(1, Width - (int)(x0 + p.Width * scale + 3)), 20),
                    Color.FromArgb(100, 116, 139), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }

            // 画布外框淡边框
            using (var canvasBorder = new Pen(Color.FromArgb(226, 232, 240)))
                g.DrawRectangle(canvasBorder, 0, 0, Width - 1, Height - 1);
        }
    }
}
