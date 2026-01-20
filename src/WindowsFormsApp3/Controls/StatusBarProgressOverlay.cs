using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApp3.Controls
{
    /// <summary>
    /// 状态栏进度叠加层 - 在状态栏上方绘制半透明进度条
    /// </summary>
    public class StatusBarProgressOverlay : Control
    {
        private int _progress = 0;
        private Color _progressColor = Color.FromArgb(180, 0, 120, 215); // 半透明蓝色
        private Color _backgroundColor = Color.FromArgb(50, 255, 255, 255); // 非常淡的背景
        private Timer _animationTimer;
        private float _animationOffset = 0;
        private bool _isIndeterminate = false;

        public StatusBarProgressOverlay()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            
            BackColor = Color.Transparent;
            Visible = false;
            
            // 动画定时器（用于不确定进度模式）
            _animationTimer = new Timer();
            _animationTimer.Interval = 30;
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        /// <summary>
        /// 进度值 (0-100)
        /// </summary>
        public int Progress
        {
            get => _progress;
            set
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<int>(v => Progress = v), value);
                    return;
                }
                
                _progress = Math.Max(0, Math.Min(100, value));
                _isIndeterminate = false;
                _animationTimer.Stop();
                Invalidate();
            }
        }

        /// <summary>
        /// 进度条颜色
        /// </summary>
        public Color ProgressColor
        {
            get => _progressColor;
            set
            {
                _progressColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// 启动不确定进度模式（滚动动画）
        /// </summary>
        public void StartIndeterminate()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(StartIndeterminate));
                return;
            }
            
            _isIndeterminate = true;
            _animationOffset = 0;
            _animationTimer.Start();
            Visible = true;
        }

        /// <summary>
        /// 停止并隐藏进度条
        /// </summary>
        public void Stop()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(Stop));
                return;
            }
            
            _animationTimer.Stop();
            _isIndeterminate = false;
            _progress = 0;
            Visible = false;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            _animationOffset += 2;
            if (_animationOffset > Width)
                _animationOffset = 0;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!Visible)
                return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 绘制淡背景
            using (var bgBrush = new SolidBrush(_backgroundColor))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            if (_isIndeterminate)
            {
                // 不确定进度模式 - 绘制滚动条
                DrawIndeterminateProgress(g);
            }
            else if (_progress > 0)
            {
                // 确定进度模式 - 绘制进度条
                DrawDeterminateProgress(g);
            }
        }

        private void DrawDeterminateProgress(Graphics g)
        {
            int progressWidth = (int)(Width * (_progress / 100.0));
            if (progressWidth <= 0)
                return;

            var progressRect = new Rectangle(0, 0, progressWidth, Height);

            // 使用渐变色增强视觉效果
            using (var brush = new LinearGradientBrush(
                progressRect,
                Color.FromArgb(_progressColor.A, 
                    Math.Min(255, _progressColor.R + 30),
                    Math.Min(255, _progressColor.G + 30),
                    Math.Min(255, _progressColor.B + 30)),
                _progressColor,
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(brush, progressRect);
            }

            // 添加高光效果
            var highlightRect = new Rectangle(0, 0, progressWidth, Height / 2);
            using (var highlightBrush = new LinearGradientBrush(
                highlightRect,
                Color.FromArgb(60, 255, 255, 255),
                Color.FromArgb(0, 255, 255, 255),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(highlightBrush, highlightRect);
            }
        }

        private void DrawIndeterminateProgress(Graphics g)
        {
            int barWidth = 100;
            int x = (int)_animationOffset;

            // 绘制移动的进度块
            var progressRect = new Rectangle(x, 0, barWidth, Height);

            using (var brush = new LinearGradientBrush(
                new Rectangle(x - 20, 0, barWidth + 40, Height),
                Color.FromArgb(0, _progressColor),
                _progressColor,
                LinearGradientMode.Horizontal))
            {
                var blend = new ColorBlend(5);
                blend.Colors = new[]
                {
                    Color.FromArgb(0, _progressColor),
                    Color.FromArgb(_progressColor.A / 2, _progressColor),
                    _progressColor,
                    Color.FromArgb(_progressColor.A / 2, _progressColor),
                    Color.FromArgb(0, _progressColor)
                };
                blend.Positions = new[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
                brush.InterpolationColors = blend;

                g.FillRectangle(brush, progressRect);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
