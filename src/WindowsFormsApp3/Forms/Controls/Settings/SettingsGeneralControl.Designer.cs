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
        private AntdUI.Checkbox chkRenameNotification;
        private AntdUI.Label lblAutoSaveSeconds;
        private System.Windows.Forms.NumericUpDown numAutoSaveSeconds;
        private AntdUI.Checkbox chkEnableDailyJson;

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
            this.chkRenameNotification = new AntdUI.Checkbox();
            this.lblAutoSaveSeconds = new AntdUI.Label();
            this.numAutoSaveSeconds = new System.Windows.Forms.NumericUpDown();
            this.chkEnableDailyJson = new AntdUI.Checkbox();
            ((System.ComponentModel.ISupportInitialize)(this.numAutoSaveSeconds)).BeginInit();
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
            // chkRenameNotification
            // 
            this.chkRenameNotification.Location = new System.Drawing.Point(20, 180);
            this.chkRenameNotification.Name = "chkRenameNotification";
            this.chkRenameNotification.Size = new System.Drawing.Size(200, 32);
            this.chkRenameNotification.TabIndex = 8;
            this.chkRenameNotification.Text = "重命名完成后显示通知";
            // 
            // lblAutoSaveSeconds
            // 
            this.lblAutoSaveSeconds.Location = new System.Drawing.Point(20, 220);
            this.lblAutoSaveSeconds.Name = "lblAutoSaveSeconds";
            this.lblAutoSaveSeconds.Size = new System.Drawing.Size(90, 32);
            this.lblAutoSaveSeconds.TabIndex = 9;
            this.lblAutoSaveSeconds.Text = "自动保存(秒):";
            // 
            // numAutoSaveSeconds
            // 
            this.numAutoSaveSeconds.Location = new System.Drawing.Point(120, 224);
            this.numAutoSaveSeconds.Maximum = new decimal(new int[] {
            3600,
            0,
            0,
            0});
            this.numAutoSaveSeconds.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numAutoSaveSeconds.Name = "numAutoSaveSeconds";
            this.numAutoSaveSeconds.Size = new System.Drawing.Size(100, 23);
            this.numAutoSaveSeconds.TabIndex = 10;
            this.numAutoSaveSeconds.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // chkEnableDailyJson
            // 
            this.chkEnableDailyJson.Location = new System.Drawing.Point(20, 260);
            this.chkEnableDailyJson.Name = "chkEnableDailyJson";
            this.chkEnableDailyJson.Size = new System.Drawing.Size(260, 32);
            this.chkEnableDailyJson.TabIndex = 11;
            this.chkEnableDailyJson.Text = "启用当日JSON自动创建/加载";
            // 
            // SettingsGeneralControl
            // 
            this.Controls.Add(this.chkEnableDailyJson);
            this.Controls.Add(this.numAutoSaveSeconds);
            this.Controls.Add(this.lblAutoSaveSeconds);
            this.Controls.Add(this.chkRenameNotification);
            this.Controls.Add(this.lblSeparator);
            this.Controls.Add(this.txtSeparator);
            this.Controls.Add(this.lblUnit);
            this.Controls.Add(this.txtUnit);
            this.Controls.Add(this.lblOpacity);
            this.Controls.Add(this.sliderOpacity);
            this.Controls.Add(this.lblHotkey);
            this.Controls.Add(this.txtHotkey);
            this.Name = "SettingsGeneralControl";
            this.Size = new System.Drawing.Size(800, 600);
            ((System.ComponentModel.ISupportInitialize)(this.numAutoSaveSeconds)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
