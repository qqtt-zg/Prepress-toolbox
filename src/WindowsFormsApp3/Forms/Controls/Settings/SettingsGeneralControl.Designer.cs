namespace WindowsFormsApp3.Forms.Controls.Settings
{
    partial class SettingsGeneralControl
    {
        private System.ComponentModel.IContainer components = null;

        // UI Controls
        private AntdUI.Input txtSeparator;
        private AntdUI.Label lblSeparator;
        private AntdUI.Input txtUnit;
        private AntdUI.Label lblUnit;
        private AntdUI.Label lblOpacity;
        private AntdUI.Slider sliderOpacity;
        private AntdUI.Label lblHotkey;
        private AntdUI.Input txtHotkey;


        // Text Items Section
        private System.Windows.Forms.Panel grpTextSettings;
        private System.Windows.Forms.FlowLayoutPanel pnlTextItems; // Container for checkboxes
        private AntdUI.Button btnMoveUp;
        private AntdUI.Button btnMoveDown;
        private AntdUI.Input txtComboPreview; // Multiline

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtSeparator = new AntdUI.Input();
            this.lblSeparator = new AntdUI.Label();
            this.txtUnit = new AntdUI.Input();
            this.lblUnit = new AntdUI.Label();
            this.lblOpacity = new AntdUI.Label();
            this.sliderOpacity = new AntdUI.Slider();
            this.lblHotkey = new AntdUI.Label();
            this.txtHotkey = new AntdUI.Input();
            this.grpTextSettings = new System.Windows.Forms.Panel();
            this.pnlTextItems = new System.Windows.Forms.FlowLayoutPanel();
            this.btnMoveUp = new AntdUI.Button();
            this.btnMoveDown = new AntdUI.Button();
            this.txtComboPreview = new AntdUI.Input();
            this.grpTextSettings.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtSeparator
            // 
            this.txtSeparator.Location = new System.Drawing.Point(120, 20);
            this.txtSeparator.Name = "txtSeparator";
            this.txtSeparator.PlaceholderText = "例如 _";
            this.txtSeparator.Size = new System.Drawing.Size(100, 32);
            this.txtSeparator.TabIndex = 1;
            // 
            // lblSeparator
            // 
            this.lblSeparator.Location = new System.Drawing.Point(20, 20);
            this.lblSeparator.Name = "lblSeparator";
            this.lblSeparator.Size = new System.Drawing.Size(90, 32);
            this.lblSeparator.TabIndex = 0;
            this.lblSeparator.Text = "分隔符:";
            // 
            // txtUnit
            // 
            this.txtUnit.Location = new System.Drawing.Point(120, 60);
            this.txtUnit.Name = "txtUnit";
            this.txtUnit.PlaceholderText = "mm";
            this.txtUnit.Size = new System.Drawing.Size(100, 32);
            this.txtUnit.TabIndex = 3;
            // 
            // lblUnit
            // 
            this.lblUnit.Location = new System.Drawing.Point(20, 60);
            this.lblUnit.Name = "lblUnit";
            this.lblUnit.Size = new System.Drawing.Size(90, 32);
            this.lblUnit.TabIndex = 2;
            this.lblUnit.Text = "默认单位:";
            // 
            // lblOpacity
            // 
            this.lblOpacity.Location = new System.Drawing.Point(20, 102);
            this.lblOpacity.Name = "lblOpacity";
            this.lblOpacity.Size = new System.Drawing.Size(90, 32);
            this.lblOpacity.TabIndex = 4;
            this.lblOpacity.Text = "窗口透明度:";
            // 
            // sliderOpacity
            // 
            this.sliderOpacity.Location = new System.Drawing.Point(120, 100);
            this.sliderOpacity.Name = "sliderOpacity";
            this.sliderOpacity.ShowValue = true;
            this.sliderOpacity.Size = new System.Drawing.Size(200, 32);
            this.sliderOpacity.TabIndex = 5;
            this.sliderOpacity.Value = 100;
            // 
            // lblHotkey
            // 
            this.lblHotkey.Location = new System.Drawing.Point(20, 140);
            this.lblHotkey.Name = "lblHotkey";
            this.lblHotkey.Size = new System.Drawing.Size(90, 32);
            this.lblHotkey.TabIndex = 6;
            this.lblHotkey.Text = "最小化快捷键:";
            // 
            // txtHotkey
            // 
            this.txtHotkey.Location = new System.Drawing.Point(120, 140);
            this.txtHotkey.Name = "txtHotkey";
            this.txtHotkey.PlaceholderText = "例如 Ctrl+S";
            this.txtHotkey.Size = new System.Drawing.Size(100, 32);
            this.txtHotkey.TabIndex = 7;
            // 
            // grpTextSettings
            // 
            this.grpTextSettings.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.grpTextSettings.Controls.Add(this.pnlTextItems);
            this.grpTextSettings.Controls.Add(this.btnMoveUp);
            this.grpTextSettings.Controls.Add(this.btnMoveDown);
            this.grpTextSettings.Controls.Add(this.txtComboPreview);
            this.grpTextSettings.Location = new System.Drawing.Point(20, 230);
            this.grpTextSettings.Name = "grpTextSettings";
            this.grpTextSettings.Size = new System.Drawing.Size(600, 300);
            this.grpTextSettings.TabIndex = 8;
            // 
            // pnlTextItems
            // 
            this.pnlTextItems.AutoScroll = true;
            this.pnlTextItems.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlTextItems.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.pnlTextItems.Location = new System.Drawing.Point(20, 30);
            this.pnlTextItems.Name = "pnlTextItems";
            this.pnlTextItems.Size = new System.Drawing.Size(150, 250);
            this.pnlTextItems.TabIndex = 0;
            this.pnlTextItems.WrapContents = false;
            // 
            // btnMoveUp
            // 
            this.btnMoveUp.Location = new System.Drawing.Point(190, 30);
            this.btnMoveUp.Name = "btnMoveUp";
            this.btnMoveUp.Size = new System.Drawing.Size(80, 32);
            this.btnMoveUp.TabIndex = 1;
            this.btnMoveUp.Text = "上移";
            // 
            // btnMoveDown
            // 
            this.btnMoveDown.Location = new System.Drawing.Point(190, 70);
            this.btnMoveDown.Name = "btnMoveDown";
            this.btnMoveDown.Size = new System.Drawing.Size(80, 32);
            this.btnMoveDown.TabIndex = 2;
            this.btnMoveDown.Text = "下移";
            // 
            // txtComboPreview
            // 
            this.txtComboPreview.Location = new System.Drawing.Point(290, 30);
            this.txtComboPreview.Multiline = true;
            this.txtComboPreview.Name = "txtComboPreview";
            this.txtComboPreview.ReadOnly = true;
            this.txtComboPreview.Size = new System.Drawing.Size(280, 100);
            this.txtComboPreview.TabIndex = 3;
            // 
            // SettingsGeneralControl
            // 
            this.Controls.Add(this.lblSeparator);
            this.Controls.Add(this.txtSeparator);
            this.Controls.Add(this.lblUnit);
            this.Controls.Add(this.txtUnit);
            this.Controls.Add(this.lblOpacity);
            this.Controls.Add(this.sliderOpacity);
            this.Controls.Add(this.lblHotkey);
            this.Controls.Add(this.txtHotkey);
            this.Controls.Add(this.grpTextSettings);
            this.Name = "SettingsGeneralControl";
            this.Size = new System.Drawing.Size(800, 600);
            this.grpTextSettings.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
