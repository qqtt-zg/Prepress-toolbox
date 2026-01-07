namespace WindowsFormsApp3.Forms.Panels
{
    partial class SettingsPanel
    {
        private System.ComponentModel.IContainer components = null;
        
        // Tab controls - 在设计器中声明
        private AntdUI.Tabs tabsMain;
        private AntdUI.TabPage pageGeneral;
        private AntdUI.TabPage pageRegex;
        private AntdUI.TabPage pageMaterial;
        private AntdUI.TabPage pagePath;
        private AntdUI.TabPage pageImposition;
        private AntdUI.TabPage pageEvent;
        private Controls.Settings.SettingsGeneralControl settingsGeneral;
        private Controls.Settings.SettingsRegexControl settingsRegex;
        private Controls.Settings.SettingsMaterialControl settingsMaterial;
        private Controls.Settings.SettingsPathControl settingsPath;
        private Controls.Settings.SettingsImpositionControl settingsImposition;
        private Controls.Settings.SettingsEventControl settingsEvent;

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
            AntdUI.Tabs.StyleLine styleLine1 = new AntdUI.Tabs.StyleLine();
            this.tabsMain = new AntdUI.Tabs();
            this.pageGeneral = new AntdUI.TabPage();
            this.settingsGeneral = new WindowsFormsApp3.Forms.Controls.Settings.SettingsGeneralControl();
            this.pageRegex = new AntdUI.TabPage();
            this.settingsRegex = new WindowsFormsApp3.Forms.Controls.Settings.SettingsRegexControl();
            this.pageMaterial = new AntdUI.TabPage();
            this.settingsMaterial = new WindowsFormsApp3.Forms.Controls.Settings.SettingsMaterialControl();
            this.pagePath = new AntdUI.TabPage();
            this.settingsPath = new WindowsFormsApp3.Forms.Controls.Settings.SettingsPathControl();
            this.pageImposition = new AntdUI.TabPage();
            this.settingsImposition = new WindowsFormsApp3.Forms.Controls.Settings.SettingsImpositionControl();
            this.pageEvent = new AntdUI.TabPage();
            this.settingsEvent = new WindowsFormsApp3.Forms.Controls.Settings.SettingsEventControl();
            this.tabsMain.SuspendLayout();
            this.pageGeneral.SuspendLayout();
            this.pageRegex.SuspendLayout();
            this.pageMaterial.SuspendLayout();
            this.pagePath.SuspendLayout();
            this.pageImposition.SuspendLayout();
            this.pageEvent.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabsMain
            // 
            this.tabsMain.Controls.Add(this.pageGeneral);
            this.tabsMain.Controls.Add(this.pageRegex);
            this.tabsMain.Controls.Add(this.pageMaterial);
            this.tabsMain.Controls.Add(this.pagePath);
            this.tabsMain.Controls.Add(this.pageImposition);
            this.tabsMain.Controls.Add(this.pageEvent);
            this.tabsMain.Cursor = System.Windows.Forms.Cursors.Default;
            this.tabsMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabsMain.Location = new System.Drawing.Point(0, 0);
            this.tabsMain.Name = "tabsMain";
            this.tabsMain.Pages.Add(this.pageGeneral);
            this.tabsMain.Pages.Add(this.pageRegex);
            this.tabsMain.Pages.Add(this.pageMaterial);
            this.tabsMain.Pages.Add(this.pagePath);
            this.tabsMain.Pages.Add(this.pageImposition);
            this.tabsMain.Pages.Add(this.pageEvent);
            this.tabsMain.Size = new System.Drawing.Size(935, 503);
            this.tabsMain.Style = styleLine1;
            this.tabsMain.TabIndex = 0;
            // 
            // pageGeneral
            // 
            this.pageGeneral.Controls.Add(this.settingsGeneral);
            this.pageGeneral.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pageGeneral.Location = new System.Drawing.Point(0, 28);
            this.pageGeneral.Name = "pageGeneral";
            this.pageGeneral.Size = new System.Drawing.Size(935, 475);
            this.pageGeneral.TabIndex = 0;
            this.pageGeneral.Text = "常规设置";
            // 
            // settingsGeneral
            // 
            this.settingsGeneral.BackColor = System.Drawing.Color.White;
            this.settingsGeneral.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsGeneral.Location = new System.Drawing.Point(0, 0);
            this.settingsGeneral.Name = "settingsGeneral";
            this.settingsGeneral.Size = new System.Drawing.Size(935, 475);
            this.settingsGeneral.TabIndex = 0;
            this.settingsGeneral.Load += new System.EventHandler(this.settingsGeneral_Load);
            // 
            // pageRegex
            // 
            this.pageRegex.Controls.Add(this.settingsRegex);
            this.pageRegex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pageRegex.Location = new System.Drawing.Point(0, 28);
            this.pageRegex.Name = "pageRegex";
            this.pageRegex.Size = new System.Drawing.Size(935, 475);
            this.pageRegex.TabIndex = 1;
            this.pageRegex.Text = "正则管理";
            // 
            // settingsRegex
            // 
            this.settingsRegex.BackColor = System.Drawing.Color.White;
            this.settingsRegex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsRegex.Location = new System.Drawing.Point(0, 0);
            this.settingsRegex.Name = "settingsRegex";
            this.settingsRegex.Size = new System.Drawing.Size(935, 475);
            this.settingsRegex.TabIndex = 0;
            // 
            // pageMaterial
            // 
            this.pageMaterial.Controls.Add(this.settingsMaterial);
            this.pageMaterial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pageMaterial.Location = new System.Drawing.Point(0, 28);
            this.pageMaterial.Name = "pageMaterial";
            this.pageMaterial.Size = new System.Drawing.Size(935, 475);
            this.pageMaterial.TabIndex = 2;
            this.pageMaterial.Text = "材料形状";
            // 
            // settingsMaterial
            // 
            this.settingsMaterial.BackColor = System.Drawing.Color.White;
            this.settingsMaterial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsMaterial.Location = new System.Drawing.Point(0, 0);
            this.settingsMaterial.Name = "settingsMaterial";
            this.settingsMaterial.Size = new System.Drawing.Size(935, 475);
            this.settingsMaterial.TabIndex = 0;
            this.settingsMaterial.Load += new System.EventHandler(this.settingsMaterial_Load);
            // 
            // pagePath
            // 
            this.pagePath.Controls.Add(this.settingsPath);
            this.pagePath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pagePath.Location = new System.Drawing.Point(0, 28);
            this.pagePath.Name = "pagePath";
            this.pagePath.Size = new System.Drawing.Size(935, 475);
            this.pagePath.TabIndex = 3;
            this.pagePath.Text = "导出路径";
            // 
            // settingsPath
            // 
            this.settingsPath.BackColor = System.Drawing.Color.White;
            this.settingsPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsPath.Location = new System.Drawing.Point(0, 0);
            this.settingsPath.Name = "settingsPath";
            this.settingsPath.Size = new System.Drawing.Size(935, 475);
            this.settingsPath.TabIndex = 0;
            // 
            // pageImposition
            // 
            this.pageImposition.Controls.Add(this.settingsImposition);
            this.pageImposition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pageImposition.Location = new System.Drawing.Point(0, 28);
            this.pageImposition.Name = "pageImposition";
            this.pageImposition.Size = new System.Drawing.Size(935, 475);
            this.pageImposition.TabIndex = 4;
            this.pageImposition.Text = "自动排版";
            // 
            // settingsImposition
            // 
            this.settingsImposition.BackColor = System.Drawing.Color.White;
            this.settingsImposition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsImposition.Location = new System.Drawing.Point(0, 0);
            this.settingsImposition.Name = "settingsImposition";
            this.settingsImposition.Size = new System.Drawing.Size(935, 475);
            this.settingsImposition.TabIndex = 0;
            // 
            // pageEvent
            // 
            this.pageEvent.Controls.Add(this.settingsEvent);
            this.pageEvent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pageEvent.Location = new System.Drawing.Point(0, 28);
            this.pageEvent.Name = "pageEvent";
            this.pageEvent.Size = new System.Drawing.Size(935, 475);
            this.pageEvent.TabIndex = 5;
            this.pageEvent.Text = "事件分组";
            // 
            // settingsEvent
            // 
            this.settingsEvent.BackColor = System.Drawing.Color.White;
            this.settingsEvent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsEvent.Location = new System.Drawing.Point(0, 0);
            this.settingsEvent.Name = "settingsEvent";
            this.settingsEvent.Size = new System.Drawing.Size(935, 475);
            this.settingsEvent.TabIndex = 0;
            // 
            // SettingsPanel
            // 
            this.Controls.Add(this.tabsMain);
            this.Name = "SettingsPanel";
            this.Size = new System.Drawing.Size(935, 503);
            this.tabsMain.ResumeLayout(false);
            this.pageGeneral.ResumeLayout(false);
            this.pageRegex.ResumeLayout(false);
            this.pageMaterial.ResumeLayout(false);
            this.pagePath.ResumeLayout(false);
            this.pageImposition.ResumeLayout(false);
            this.pageEvent.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
