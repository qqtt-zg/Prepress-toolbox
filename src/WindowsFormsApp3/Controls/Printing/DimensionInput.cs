using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Controls.Printing
{
    /// <summary>
    /// 尺寸输入组合控件 (宽 x 高 + 单位)
    /// </summary>
    public partial class DimensionInput : UserControl
    {
        private NumericInput numWidth;
        private NumericInput numHeight;
        private Label lblX;
        private ComboBox cmbUnit;
        private Label lblWidthTitle;
        private Label lblHeightTitle;

        public event EventHandler ValueChanged;

        public DimensionInput()
        {
            InitializeComponent();
        }

        #region Properties

        [Category("Data")]
        public decimal WidthValue
        {
            get => numWidth.Value;
            set => numWidth.Value = value;
        }

        [Category("Data")]
        public decimal HeightValue
        {
            get => numHeight.Value;
            set => numHeight.Value = value;
        }
        
        [Category("Data")]
        public string Unit
        {
            get => cmbUnit.Text;
            set => cmbUnit.Text = value;
        }

        #endregion

        private void InitializeComponent()
        {
            this.numWidth = new NumericInput();
            this.numHeight = new NumericInput();
            this.lblX = new Label();
            this.cmbUnit = new ComboBox();
            this.lblWidthTitle = new Label();
            this.lblHeightTitle = new Label();

            this.SuspendLayout();

            // 
            // lblWidthTitle
            // 
            this.lblWidthTitle.AutoSize = true;
            this.lblWidthTitle.Text = "宽";
            this.lblWidthTitle.Location = new Point(0, 5);
            this.lblWidthTitle.ForeColor = SystemColors.GrayText;

            // 
            // numWidth
            // 
            this.numWidth.Location = new Point(25, 0);
            this.numWidth.Size = new Size(80, 26);
            this.numWidth.Unit = "";
            this.numWidth.ValueChanged += (s, e) => ValueChanged?.Invoke(this, EventArgs.Empty);

            // 
            // lblHeightTitle
            // 
            this.lblHeightTitle.AutoSize = true;
            this.lblHeightTitle.Text = "高";
            this.lblHeightTitle.Location = new Point(120, 5);
            this.lblHeightTitle.ForeColor = SystemColors.GrayText;

            // 
            // numHeight
            // 
            this.numHeight.Location = new Point(145, 0);
            this.numHeight.Size = new Size(80, 26);
            this.numHeight.Unit = "";
            this.numHeight.ValueChanged += (s, e) => ValueChanged?.Invoke(this, EventArgs.Empty);
            
            // 
            // cmbUnit
            // 
            this.cmbUnit.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbUnit.Items.AddRange(new object[] { "mm", "cm", "inch", "pt" });
            this.cmbUnit.Location = new Point(235, 1);
            this.cmbUnit.Size = new Size(60, 25);
            this.cmbUnit.Text = "mm";
            this.cmbUnit.SelectedIndexChanged += CmbUnit_SelectedIndexChanged;

            // 
            // DimensionInput
            // 
            this.Controls.Add(lblWidthTitle);
            this.Controls.Add(numWidth);
            this.Controls.Add(lblHeightTitle);
            this.Controls.Add(numHeight);
            this.Controls.Add(cmbUnit);
            this.Size = new Size(300, 30);
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void CmbUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update internal units if needed, or just expose the unit string
            // For now just triggering event
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
