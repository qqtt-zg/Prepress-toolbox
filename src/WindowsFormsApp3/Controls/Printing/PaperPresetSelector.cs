using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Controls.Printing
{
    /// <summary>
    /// 纸张预设选择器
    /// </summary>
    public partial class PaperPresetSelector : UserControl
    {
        private ComboBox cmbPresets;
        private Label lblTitle;
        
        public event EventHandler<PaperSizeEventArgs> PaperSizeChanged;

        public PaperPresetSelector()
        {
            InitializeComponent();
            InitializePresets();
        }

        private void InitializeComponent()
        {
            this.cmbPresets = new ComboBox();
            this.lblTitle = new Label();
            this.SuspendLayout();
            
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Text = "纸张大小";
            this.lblTitle.Location = new Point(0, 7);
            this.lblTitle.Font = new Font("Microsoft YaHei UI", 9F);
            this.lblTitle.ForeColor = SystemColors.GrayText;
            
            // 
            // cmbPresets
            // 
            this.cmbPresets.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbPresets.Location = new Point(70, 3);
            this.cmbPresets.Width = 180;
            this.cmbPresets.Height = 25;
            this.cmbPresets.Font = new Font("Microsoft YaHei UI", 9F);
            this.cmbPresets.SelectedIndexChanged += CmbPresets_SelectedIndexChanged;
            
            // 
            // PaperPresetSelector
            // 
            this.Controls.Add(lblTitle);
            this.Controls.Add(cmbPresets);
            this.Size = new Size(260, 30);
            this.MinimumSize = new Size(230, 26);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void InitializePresets()
        {
            cmbPresets.Items.Add(new PaperSizeItem("A3", 297, 420));
            cmbPresets.Items.Add(new PaperSizeItem("A4", 210, 297));
            cmbPresets.Items.Add(new PaperSizeItem("A5", 148, 210));
            cmbPresets.Items.Add(new PaperSizeItem("B3 (JIS)", 364, 515));
            cmbPresets.Items.Add(new PaperSizeItem("B4 (JIS)", 257, 364));
            cmbPresets.Items.Add(new PaperSizeItem("B5 (JIS)", 182, 257));
            cmbPresets.Items.Add(new PaperSizeItem("SRA3", 320, 450));
            cmbPresets.Items.Add(new PaperSizeItem("自定义", 0, 0));
            
            cmbPresets.SelectedIndex = 0; // Default A3
        }

        private void CmbPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbPresets.SelectedItem is PaperSizeItem item)
            {
                PaperSizeChanged?.Invoke(this, new PaperSizeEventArgs(item.Name, item.Width, item.Height));
            }
        }
    }

    public class PaperSizeItem
    {
        public string Name { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }

        public PaperSizeItem(string name, decimal width, decimal height)
        {
            Name = name;
            Width = width;
            Height = height;
        }

        public override string ToString()
        {
            if (Width == 0 && Height == 0) return Name;
            return $"{Name} ({Width} x {Height} mm)";
        }
    }

    public class PaperSizeEventArgs : EventArgs
    {
        public string Name { get; }
        public decimal Width { get; }
        public decimal Height { get; }

        public PaperSizeEventArgs(string name, decimal width, decimal height)
        {
            Name = name;
            Width = width;
            Height = height;
        }
    }
}
