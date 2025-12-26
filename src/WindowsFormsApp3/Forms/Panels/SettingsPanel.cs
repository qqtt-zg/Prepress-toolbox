using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Forms.Controls.Settings;

namespace WindowsFormsApp3.Forms.Panels
{
    public partial class SettingsPanel : BasePanelControl
    {
        // Tab controls - 延迟初始化
        private SettingsGeneralControl settingsGeneral;
        private SettingsRegexControl settingsRegex;
        private SettingsMaterialControl settingsMaterial;
        private SettingsPathControl settingsPath;
        private SettingsImpositionControl settingsImposition;
        private SettingsEventControl settingsEvent;

        public override string PanelKey => "settings";
        public override string DisplayName => "综合设置";
        public override string IconName => "SettingOutlined";

        public SettingsPanel()
        {
            InitializeComponent();
        }

        protected override void InitializePanel()
        {
            base.InitializePanel();
            
            // 在运行时初始化子控件（不在构造函数中，避免设计器问题）
            InitializeSettingsControls();
        }
        
        /// <summary>
        /// 初始化设置页面控件（仅运行时调用）
        /// </summary>
        private void InitializeSettingsControls()
        {
            // Instantiate Controls
            this.settingsGeneral = new SettingsGeneralControl();
            this.settingsRegex = new SettingsRegexControl();
            this.settingsMaterial = new SettingsMaterialControl();
            this.settingsPath = new SettingsPathControl();
            this.settingsImposition = new SettingsImpositionControl();
            this.settingsEvent = new SettingsEventControl();

            // Dock Styles
            settingsGeneral.Dock = DockStyle.Fill;
            settingsRegex.Dock = DockStyle.Fill;
            settingsMaterial.Dock = DockStyle.Fill;
            settingsPath.Dock = DockStyle.Fill;
            settingsImposition.Dock = DockStyle.Fill;
            settingsEvent.Dock = DockStyle.Fill;

            // Wire up Events
            settingsGeneral.SettingsSaved += SettingsGeneral_SettingsSaved;
            
            // Add Pages
            var pageGeneral = new AntdUI.TabPage();
            pageGeneral.Text = "常规设置";
            pageGeneral.Controls.Add(settingsGeneral);

            var pageRegex = new AntdUI.TabPage();
            pageRegex.Text = "正则管理";
            pageRegex.Controls.Add(settingsRegex);

            var pageMaterial = new AntdUI.TabPage();
            pageMaterial.Text = "材料形状";
            pageMaterial.Controls.Add(settingsMaterial);
            
            var pagePath = new AntdUI.TabPage();
            pagePath.Text = "导出路径";
            pagePath.Controls.Add(settingsPath);

            var pageImposition = new AntdUI.TabPage();
            pageImposition.Text = "自动排版";
            pageImposition.Controls.Add(settingsImposition);

            var pageEvent = new AntdUI.TabPage();
            pageEvent.Text = "事件分组";
            pageEvent.Controls.Add(settingsEvent);

            this.tabsMain.Pages.Add(pageGeneral);
            this.tabsMain.Pages.Add(pageRegex);
            this.tabsMain.Pages.Add(pageMaterial);
            this.tabsMain.Pages.Add(pagePath);
            this.tabsMain.Pages.Add(pageImposition);
            this.tabsMain.Pages.Add(pageEvent);
        }

        private void SettingsGeneral_SettingsSaved(object sender, EventArgs e)
        {
             if (this.ParentForm is WindowsFormsApp3.Forms.Main.MainShellForm mainForm)
             {
                 mainForm.UpdateHotkeys();
             }
        }
        
        public void NavigateToSection(string sectionKey)
        {
            if (string.IsNullOrEmpty(sectionKey)) return;

            switch (sectionKey.ToLower())
            {
                case "general":
                case "basic":
                    tabsMain.SelectedIndex = 0;
                    break;
                case "regex":
                    tabsMain.SelectedIndex = 1;
                    break;
                case "material":
                    tabsMain.SelectedIndex = 2;
                    break;
                case "path":
                case "export":
                    tabsMain.SelectedIndex = 3;
                    break;
                case "imposition":
                    tabsMain.SelectedIndex = 4;
                    break;
            }
        }
    }
}
