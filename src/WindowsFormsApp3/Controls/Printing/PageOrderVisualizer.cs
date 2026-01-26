using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Controls.Printing
{
    /// <summary>
    /// 页面顺序可视化控件
    /// </summary>
    public partial class PageOrderVisualizer : UserControl
    {
        private string _impositionType = "SaddleStitch"; // SaddleStitch, PerfectBound, CutStack
        private int _pageCount = 16;
        
        public PageOrderVisualizer()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
        }

        #region Properties

        [Category("Data")]
        public string ImpositionType
        {
            get => _impositionType;
            set
            {
                if (_impositionType != value)
                {
                    _impositionType = value;
                    this.Invalidate();
                }
            }
        }

        [Category("Data")]
        public int PageCount
        {
            get => _pageCount;
            set
            {
                if (_pageCount != value)
                {
                    _pageCount = value;
                    this.Invalidate();
                }
            }
        }

        #endregion

        private void InitializeComponent()
        {
            this.Size = new Size(300, 150);
            this.BackColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Simple visualization logic
            // Draw a schematic representation of the spreads
            
            if (_impositionType == "SaddleStitch")
            {
                DrawSaddleStitch(g);
            }
            else if (_impositionType == "NUp")
            {
                DrawNUp(g);
            }
            else
            {
                // General placeholder
                DrawPlaceholder(g);
            }
        }

        private void DrawSaddleStitch(Graphics g)
        {
            // Draw spreads: e.g. [16, 1], [2, 15], etc.
            // Just draw the first spread as an example or a stack
            
            int spreadWidth = 120;
            int spreadHeight = 80;
            int centerX = this.Width / 2;
            int centerY = this.Height / 2;
            
            Rectangle leftPage = new Rectangle(centerX - spreadWidth/2 - 2, centerY - spreadHeight/2, spreadWidth/2, spreadHeight);
            Rectangle rightPage = new Rectangle(centerX + 2, centerY - spreadHeight/2, spreadWidth/2, spreadHeight);
            
            // Fill
            g.FillRectangle(Brushes.WhiteSmoke, leftPage);
            g.FillRectangle(Brushes.WhiteSmoke, rightPage);
            
            // Stroke
            using (var pen = new Pen(DesignTokens.PrimaryColor, 2))
            {
                g.DrawRectangle(pen, leftPage);
                g.DrawRectangle(pen, rightPage);
            }
            
            // Labels (Simulate first sheet)
            int lastPage = _pageCount;
            int firstPage = 1;
            
            DrawCenteredText(g, lastPage.ToString(), leftPage);
            DrawCenteredText(g, firstPage.ToString(), rightPage);
            
            // Legend
            g.DrawString("骑马订 (首帖示意)", DesignTokens.FontBase, Brushes.Gray, 5, 5);
        }

        private void DrawNUp(Graphics g)
        {
            // Draw a 2x2 grid as example
            int cellSize = 40;
            int startX = (this.Width - cellSize * 2) / 2;
            int startY = (this.Height - cellSize * 2) / 2;
            
            for (int r = 0; r < 2; r++)
            {
                for (int c = 0; c < 2; c++)
                {
                    Rectangle rect = new Rectangle(startX + c * cellSize, startY + r * cellSize, cellSize, cellSize);
                    g.DrawRectangle(Pens.Gray, rect);
                    DrawCenteredText(g, (r * 2 + c + 1).ToString(), rect);
                }
            }
             g.DrawString("N-Up (示意)", DesignTokens.FontBase, Brushes.Gray, 5, 5);
        }
        
        private void DrawPlaceholder(Graphics g)
        {
             g.DrawString(_impositionType, DesignTokens.FontBase, Brushes.Black, 10, 10);
        }

        private void DrawCenteredText(Graphics g, string text, Rectangle rect)
        {
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(text, DesignTokens.FontLarge, Brushes.Black, rect, sf);
            }
        }
    }
}
