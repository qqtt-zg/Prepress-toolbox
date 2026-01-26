using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Controls.Printing
{
    /// <summary>
    /// 对齐方式选择器 (9宫格)
    /// </summary>
    public partial class AlignmentSelector : UserControl
    {
        private RadioButton[,] radios = new RadioButton[3, 3];
        private ContentAlignment _alignment = ContentAlignment.MiddleCenter;

        public event EventHandler AlignmentChanged;

        public AlignmentSelector()
        {
            InitializeComponent();
        }

        [Category("Data")]
        public ContentAlignment SelectedAlignment
        {
            get => _alignment;
            set
            {
                if (_alignment != value)
                {
                    _alignment = value;
                    UpdateCheckState();
                    AlignmentChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            int size = 20;
            int gap = 5;
            int startX = 5;
            int startY = 5;

            // 3x3 grid
            // Row 0: TopLeft, TopCenter, TopRight
            // Row 1: MiddleLeft, MiddleCenter, MiddleRight
            // Row 2: BottomLeft, BottomCenter, BottomRight
            
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    var rb = new RadioButton();
                    rb.Appearance = Appearance.Button;
                    rb.Size = new Size(size, size);
                    rb.Location = new Point(startX + c * (size + gap), startY + r * (size + gap));
                    rb.Tag = GetAlignment(r, c);
                    rb.FlatStyle = FlatStyle.Flat;
                    rb.FlatAppearance.BorderSize = 1;
                    rb.FlatAppearance.CheckedBackColor = DesignTokens.PrimaryColor;
                    rb.Click += Rb_Click;
                    
                    this.Controls.Add(rb);
                    radios[r, c] = rb;
                }
            }
            
            this.Size = new Size(startX + 3 * (size + gap), startY + 3 * (size + gap));
            this.ResumeLayout(false);
            
            UpdateCheckState();
        }

        private ContentAlignment GetAlignment(int row, int col)
        {
            // Row 0
            if (row == 0 && col == 0) return ContentAlignment.TopLeft;
            if (row == 0 && col == 1) return ContentAlignment.TopCenter;
            if (row == 0 && col == 2) return ContentAlignment.TopRight;
            
            // Row 1
            if (row == 1 && col == 0) return ContentAlignment.MiddleLeft;
            if (row == 1 && col == 1) return ContentAlignment.MiddleCenter;
            if (row == 1 && col == 2) return ContentAlignment.MiddleRight;

            // Row 2
            if (row == 2 && col == 0) return ContentAlignment.BottomLeft;
            if (row == 2 && col == 1) return ContentAlignment.BottomCenter;
            if (row == 2 && col == 2) return ContentAlignment.BottomRight;

            return ContentAlignment.MiddleCenter;
        }

        private void UpdateCheckState()
        {
            foreach (Control c in this.Controls)
            {
                if (c is RadioButton rb && rb.Tag is ContentAlignment align)
                {
                    rb.Checked = (align == _alignment);
                }
            }
        }

        private void Rb_Click(object sender, EventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is ContentAlignment align)
            {
                SelectedAlignment = align;
            }
        }
    }
}
