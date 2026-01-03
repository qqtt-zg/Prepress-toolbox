using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Forms.Controls.Settings;

namespace WindowsFormsApp3.Forms.Panels
{
    public partial class SettingsPanel : BasePanelControl
    {
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
            
            // 在运行时绑定事件（设计器已创建控件）
            WireUpEvents();
        }
        
        /// <summary>
        /// 绑定设置页面控件事件（运行时调用）
        /// </summary>
        private void WireUpEvents()
        {
            // Wire up Events
            settingsGeneral.SettingsSaved += SettingsGeneral_SettingsSaved;
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
                case "event":
                    tabsMain.SelectedIndex = 5;
                    break;
            }
        }

        private void settingsMaterial_Load(object sender, EventArgs e)
        {

        }

        private void settingsGeneral_Load(object sender, EventArgs e)
        {

        }
    }
}
