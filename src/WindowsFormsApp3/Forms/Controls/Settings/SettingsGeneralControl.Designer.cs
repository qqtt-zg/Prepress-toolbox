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
        private System.Windows.Forms.GroupBox grpTextSettings;
        private System.Windows.Forms.FlowLayoutPanel pnlTextItems; // Container for checkboxes
        private AntdUI.Button btnMoveUp;
        private AntdUI.Button btnMoveDown;
        private AntdUI.Label lblComboPreview;
        private AntdUI.Input txtComboPreview; // Multiline
        private AntdUI.Select selectPresets;
        private AntdUI.Button btnSavePreset;
        private AntdUI.Button btnDeletePreset;

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
            this.components = new System.ComponentModel.Container();

            //
            // txtSeparator
            //
            this.txtSeparator = new AntdUI.Input();
            this.txtSeparator.Location = new System.Drawing.Point(120, 20);
            this.txtSeparator.Size = new System.Drawing.Size(100, 32);
            this.txtSeparator.PlaceholderText = "例如 _";

            //
            // lblSeparator
            //
            this.lblSeparator = new AntdUI.Label();
            this.lblSeparator.Location = new System.Drawing.Point(20, 20);
            this.lblSeparator.Size = new System.Drawing.Size(90, 32);
            this.lblSeparator.Text = "分隔符:";

            //
            // txtUnit
            //
            this.txtUnit = new AntdUI.Input();
            this.txtUnit.Location = new System.Drawing.Point(120, 60);
            this.txtUnit.Size = new System.Drawing.Size(100, 32);
            this.txtUnit.PlaceholderText = "mm";

            //
            // lblUnit
            //
            this.lblUnit = new AntdUI.Label();
            this.lblUnit.Location = new System.Drawing.Point(20, 60);
            this.lblUnit.Size = new System.Drawing.Size(90, 32);
            this.lblUnit.Text = "默认单位:";



            //
            // lblOpacity
            //
            this.lblOpacity = new AntdUI.Label();
            this.lblOpacity.Location = new System.Drawing.Point(20, 140);
            this.lblOpacity.Size = new System.Drawing.Size(90, 32);
            this.lblOpacity.Text = "窗口透明度:";

            //
            // sliderOpacity
            //
            this.sliderOpacity = new AntdUI.Slider();
            this.sliderOpacity.Location = new System.Drawing.Point(120, 100);
            this.sliderOpacity.Size = new System.Drawing.Size(200, 32);
            this.sliderOpacity.Value = 100;
            this.sliderOpacity.ShowValue = true;

            //
            // lblHotkey
            //
            this.lblHotkey = new AntdUI.Label();
            this.lblHotkey.Location = new System.Drawing.Point(20, 140); // Adjusted Y-coordinate
            this.lblHotkey.Size = new System.Drawing.Size(90, 32);
            this.lblHotkey.Text = "快捷键:";

            //
            // txtHotkey
            //
            this.txtHotkey = new AntdUI.Input();
            this.txtHotkey.Location = new System.Drawing.Point(120, 140); // Adjusted Y-coordinate
            this.txtHotkey.Size = new System.Drawing.Size(100, 32);
            this.txtHotkey.PlaceholderText = "例如 Ctrl+S";

            //
            // grpTextSettings
            //
            this.grpTextSettings = new System.Windows.Forms.GroupBox();
            this.grpTextSettings.Location = new System.Drawing.Point(20, 230); // Adjusted Y-coordinate to accommodate new controls
            this.grpTextSettings.Size = new System.Drawing.Size(600, 300);
            this.grpTextSettings.Text = "文件名组合设置";

            // pnlTextItems
            this.pnlTextItems = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlTextItems.AutoScroll = true;
            this.pnlTextItems.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.pnlTextItems.WrapContents = false;
            this.pnlTextItems.Location = new System.Drawing.Point(20, 30);
            this.pnlTextItems.Size = new System.Drawing.Size(150, 250);
            this.pnlTextItems.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            // Buttons & Preview (Simplified layout for now)
            this.btnMoveUp = new AntdUI.Button();
            this.btnMoveUp.Text = "上移";
            this.btnMoveUp.Location = new System.Drawing.Point(190, 30);
            this.btnMoveUp.Size = new System.Drawing.Size(80, 32);

            this.btnMoveDown = new AntdUI.Button();
            this.btnMoveDown.Text = "下移";
            this.btnMoveDown.Location = new System.Drawing.Point(190, 70);
            this.btnMoveDown.Size = new System.Drawing.Size(80, 32);

            this.txtComboPreview = new AntdUI.Input();
            this.txtComboPreview.Location = new System.Drawing.Point(290, 30);
            this.txtComboPreview.Size = new System.Drawing.Size(280, 100);
            this.txtComboPreview.Multiline = true;
            this.txtComboPreview.ReadOnly = true;

            // Adding controls
            this.grpTextSettings.Controls.Add(this.pnlTextItems);
            this.grpTextSettings.Controls.Add(this.btnMoveUp);
            this.grpTextSettings.Controls.Add(this.btnMoveDown);
            this.grpTextSettings.Controls.Add(this.txtComboPreview);

            this.Controls.Add(this.lblSeparator);
            this.Controls.Add(this.txtSeparator);
            this.Controls.Add(this.lblUnit);
            this.Controls.Add(this.txtUnit);
            this.Controls.Add(this.lblOpacity);
            this.Controls.Add(this.sliderOpacity);
            this.Controls.Add(this.lblHotkey);
            this.Controls.Add(this.txtHotkey);
            this.Controls.Add(this.grpTextSettings);

            this.Size = new System.Drawing.Size(800, 600);
            this.BackColor = System.Drawing.Color.White;
        }
    }
}
