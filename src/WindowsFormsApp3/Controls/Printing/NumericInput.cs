using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Controls.Printing
{
    /// <summary>
    /// 专业数值输入控件
    /// 支持单位切换、键盘微调、步进控制
    /// </summary>
    public partial class NumericInput : UserControl
    {
        private TextBox txtValue;
        private Label lblUnit;
        private Panel btnUp;
        private Panel btnDown;
        
        private decimal _value;
        private decimal _min = 0;
        private decimal _max = 1000;
        private decimal _increment = 1m;
        private int _decimalPlaces = 2;
        private string _unit = "mm";
        
        public event EventHandler ValueChanged;

        public NumericInput()
        {
            InitializeComponent();
            ApplyTheme();
        }

        #region Properties

        [Category("Data")]
        public decimal Value
        {
            get => _value;
            set
            {
                var clamped = Math.Max(_min, Math.Min(_max, value));
                if (_value != clamped)
                {
                    _value = clamped;
                    UpdateText();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Category("Data")]
        public decimal Minimum
        {
            get => _min;
            set => _min = value;
        }

        [Category("Data")]
        public decimal Maximum
        {
            get => _max;
            set => _max = value;
        }

        [Category("Data")]
        public decimal Increment
        {
            get => _increment;
            set => _increment = value;
        }

        [Category("Appearance")]
        public int DecimalPlaces
        {
            get => _decimalPlaces;
            set
            {
                _decimalPlaces = value;
                UpdateText();
            }
        }

        [Category("Appearance")]
        public string Unit
        {
            get => _unit;
            set
            {
                _unit = value;
                if (lblUnit != null) lblUnit.Text = value;
            }
        }

        #endregion

        private void InitializeComponent()
        {
            this.txtValue = new TextBox();
            this.lblUnit = new Label();
            this.btnUp = new Panel();
            this.btnDown = new Panel();
            
            this.SuspendLayout();
            
            // 
            // txtValue
            // 
            this.txtValue.BorderStyle = BorderStyle.None;
            this.txtValue.Location = new Point(5, 7);
            this.txtValue.Width = 60;
            this.txtValue.Font = new Font("Microsoft YaHei UI", 9F);
            this.txtValue.TextAlign = HorizontalAlignment.Right;
            this.txtValue.KeyPress += TxtValue_KeyPress;
            this.txtValue.Validating += TxtValue_Validating;
            this.txtValue.KeyDown += TxtValue_KeyDown;
            
            // 
            // lblUnit
            // 
            this.lblUnit.AutoSize = true;
            this.lblUnit.Text = "mm";
            this.lblUnit.Location = new Point(70, 7);
            this.lblUnit.ForeColor = Color.Gray;
            this.lblUnit.Font = new Font("Microsoft YaHei UI", 8F);
            
            // 
            // Container
            // 
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(txtValue);
            this.Controls.Add(lblUnit);
            this.Size = new Size(120, 28);  // Explicit size
            this.MinimumSize = new Size(100, 26);
            
            this.ResumeLayout(false);
            this.PerformLayout();
            
            UpdateText();
        }
        
        private void TxtValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                Value += Increment;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                Value -= Increment;
                e.Handled = true;
            }
        }

        private void TxtValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 允许数字、控制键、小数点和负号
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && 
                (e.KeyChar != '.') && (e.KeyChar != '-'))
            {
                e.Handled = true;
            }

            // 仅允许一个小数点
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
            
            // 仅允许一个负号且在开头
            if ((e.KeyChar == '-') && ((sender as TextBox).Text.IndexOf('-') > -1))
            {
                e.Handled = true;
            }
        }

        private void TxtValue_Validating(object sender, CancelEventArgs e)
        {
            if (decimal.TryParse(txtValue.Text, out decimal val))
            {
                Value = val;
            }
            else
            {
                UpdateText(); // Revert to last valid value
            }
        }

        private void UpdateText()
        {
            if (txtValue != null)
                txtValue.Text = _value.ToString($"F{_decimalPlaces}");
        }
        
        public void ApplyTheme()
        {
             // 使用 DesignTokens (确保 DesignTokens 在同一命名空间或引用)
             // 简单处理
             this.BackColor = DesignTokens.BgPrimary;
             this.txtValue.BackColor = DesignTokens.BgPrimary;
             this.txtValue.ForeColor = DesignTokens.TextPrimary;
             this.lblUnit.BackColor = DesignTokens.BgPrimary;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // 可以绘制边框或装饰
            ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, 
                Color.LightGray, ButtonBorderStyle.Solid);
        }
        
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (txtValue != null)
            {
                txtValue.Width = this.Width - 40;
                lblUnit.Left = this.Width - 35;
                
                // Vertical center
                int y = (this.Height - txtValue.Height) / 2;
                txtValue.Top = y;
                lblUnit.Top = y + 2; 
            }
        }
    }
}
