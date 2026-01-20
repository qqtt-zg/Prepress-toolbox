using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Controls
{
    /// <summary>
    /// PDF页面框可视化叠加层
    /// 在PDF预览上绘制不同颜色的页面框边界
    /// </summary>
    public class PdfBoxOverlay : Control
    {
        private PageBoxInfo _pageBoxInfo;
        private bool _showMediaBox = true;
        private bool _showCropBox = true;
        private bool _showTrimBox = true;
        private bool _showBleedBox = true;
        private bool _showArtBox = false;
        private float _scale = 1.0f;
        private PointF _offset = PointF.Empty;

        // 页面框颜色定义（类似PitStop Pro）
        private readonly Color _mediaBoxColor = Color.FromArgb(180, 220, 53, 69);      // 红色
        private readonly Color _cropBoxColor = Color.FromArgb(180, 0, 123, 255);       // 蓝色
        private readonly Color _trimBoxColor = Color.FromArgb(180, 40, 167, 69);       // 绿色
        private readonly Color _bleedBoxColor = Color.FromArgb(180, 255, 193, 7);      // 黄色
        private readonly Color _artBoxColor = Color.FromArgb(180, 108, 117, 125);      // 灰色

        public PdfBoxOverlay()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.Transparent;
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.SetStyle(ControlStyles.Opaque, false);
        }

        /// <summary>
        /// 设置页面框信息
        /// </summary>
        public void SetPageBoxInfo(PageBoxInfo pageBoxInfo)
        {
            _pageBoxInfo = pageBoxInfo;
            this.Invalidate();
        }

        /// <summary>
        /// 设置缩放和偏移
        /// </summary>
        public void SetTransform(float scale, PointF offset)
        {
            _scale = scale;
            _offset = offset;
            this.Invalidate();
        }

        /// <summary>
        /// 显示/隐藏MediaBox
        /// </summary>
        public bool ShowMediaBox
        {
            get => _showMediaBox;
            set { _showMediaBox = value; this.Invalidate(); }
        }

        /// <summary>
        /// 显示/隐藏CropBox
        /// </summary>
        public bool ShowCropBox
        {
            get => _showCropBox;
            set { _showCropBox = value; this.Invalidate(); }
        }

        /// <summary>
        /// 显示/隐藏TrimBox
        /// </summary>
        public bool ShowTrimBox
        {
            get => _showTrimBox;
            set { _showTrimBox = value; this.Invalidate(); }
        }

        /// <summary>
        /// 显示/隐藏BleedBox
        /// </summary>
        public bool ShowBleedBox
        {
            get => _showBleedBox;
            set { _showBleedBox = value; this.Invalidate(); }
        }

        /// <summary>
        /// 显示/隐藏ArtBox
        /// </summary>
        public bool ShowArtBox
        {
            get => _showArtBox;
            set { _showArtBox = value; this.Invalidate(); }
        }

        /// <summary>
        /// 清除叠加层
        /// </summary>
        public void Clear()
        {
            _pageBoxInfo = null;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_pageBoxInfo == null)
                return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 绘制顺序：从大到小（MediaBox -> CropBox -> BleedBox -> TrimBox -> ArtBox）
            if (_showMediaBox && _pageBoxInfo.MediaBox.IsDefined)
            {
                DrawBox(g, _pageBoxInfo.MediaBox, _mediaBoxColor, "MediaBox", 2.5f);
            }

            if (_showCropBox && _pageBoxInfo.CropBox.IsDefined)
            {
                DrawBox(g, _pageBoxInfo.CropBox, _cropBoxColor, "CropBox", 2.0f);
            }

            if (_showBleedBox && _pageBoxInfo.BleedBox.IsDefined)
            {
                DrawBox(g, _pageBoxInfo.BleedBox, _bleedBoxColor, "BleedBox", 1.5f);
            }

            if (_showTrimBox && _pageBoxInfo.TrimBox.IsDefined)
            {
                DrawBox(g, _pageBoxInfo.TrimBox, _trimBoxColor, "TrimBox", 2.0f);
            }

            if (_showArtBox && _pageBoxInfo.ArtBox.IsDefined)
            {
                DrawBox(g, _pageBoxInfo.ArtBox, _artBoxColor, "ArtBox", 1.5f);
            }
        }

        /// <summary>
        /// 绘制单个页面框
        /// </summary>
        private void DrawBox(Graphics g, BoxDimension box, Color color, string label, float lineWidth)
        {
            // PDF坐标系转换为屏幕坐标系
            // PDF: 左下角为原点，Y轴向上
            // 屏幕: 左上角为原点，Y轴向下
            
            // 获取MediaBox作为参考（用于坐标转换）
            var mediaBox = _pageBoxInfo.MediaBox;
            if (!mediaBox.IsDefined)
                return;

            // 计算屏幕坐标
            float screenX = (float)((box.Left - mediaBox.Left) * _scale + _offset.X);
            float screenY = (float)((mediaBox.Top - box.Top) * _scale + _offset.Y);
            float screenWidth = (float)(box.Width * _scale);
            float screenHeight = (float)(box.Height * _scale);

            // 绘制矩形边框
            using (var pen = new Pen(color, lineWidth))
            {
                pen.DashStyle = DashStyle.Solid;
                g.DrawRectangle(pen, screenX, screenY, screenWidth, screenHeight);
            }

            // 绘制标签（左上角）
            using (var brush = new SolidBrush(color))
            using (var font = new Font("Consolas", 9f, FontStyle.Bold))
            {
                var labelSize = g.MeasureString(label, font);
                var labelRect = new RectangleF(
                    screenX + 4,
                    screenY + 4,
                    labelSize.Width + 8,
                    labelSize.Height + 4
                );

                // 半透明背景
                using (var bgBrush = new SolidBrush(Color.FromArgb(200, Color.White)))
                {
                    g.FillRectangle(bgBrush, labelRect);
                }

                // 文字
                g.DrawString(label, font, brush, screenX + 8, screenY + 6);
            }

            // 绘制尺寸标注（右下角）
            string sizeText = $"{box.WidthMm:F1} × {box.HeightMm:F1} mm";
            using (var brush = new SolidBrush(color))
            using (var font = new Font("Consolas", 8f))
            {
                var sizeTextSize = g.MeasureString(sizeText, font);
                var sizeTextRect = new RectangleF(
                    screenX + screenWidth - sizeTextSize.Width - 12,
                    screenY + screenHeight - sizeTextSize.Height - 8,
                    sizeTextSize.Width + 8,
                    sizeTextSize.Height + 4
                );

                // 半透明背景
                using (var bgBrush = new SolidBrush(Color.FromArgb(200, Color.White)))
                {
                    g.FillRectangle(bgBrush, sizeTextRect);
                }

                // 文字
                g.DrawString(sizeText, font, brush,
                    screenX + screenWidth - sizeTextSize.Width - 8,
                    screenY + screenHeight - sizeTextSize.Height - 6);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // 不绘制背景，保持透明
        }
    }
}
