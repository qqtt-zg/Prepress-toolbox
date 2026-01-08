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
            
            // 添加底部保存按钮面板
            CreateSaveButtonPanel();
        }
        
        /// <summary>
        /// 创建底部保存按钮面板
        /// </summary>
        private void CreateSaveButtonPanel()
        {
            // 创建底部面板 (颜色将由 ThemeHelper 自动设置)
            var bottomPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom,
                Padding = new Padding(10),
                Tag = "bottomPanel" // Mark for identification if needed
            };
            
            // 创建保存按钮
            var btnSaveAll = new AntdUI.Button
            {
                Text = "保存所有设置",
                Type = AntdUI.TTypeMini.Primary,
                Size = new Size(140, 36),
                Anchor = AnchorStyles.Right
            };
            btnSaveAll.Location = new Point(bottomPanel.Width - btnSaveAll.Width - 20, 7);
            btnSaveAll.Click += (s, e) => SaveAllSettings();
            
            // 调整布局 - 让 tabsMain 不再填充整个区域
            tabsMain.Dock = DockStyle.None;
            tabsMain.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            
            // 添加控件
            bottomPanel.Controls.Add(btnSaveAll);
            this.Controls.Add(bottomPanel);
            
            // 调整 tabsMain 尺寸
            tabsMain.Location = new Point(0, 0);
            tabsMain.Size = new Size(this.Width, this.Height - bottomPanel.Height);
            
            // 确保控件 Z 顺序正确
            bottomPanel.BringToFront();
            
            // 处理尺寸变化
            this.Resize += (s, e) => 
            {
                tabsMain.Size = new Size(this.Width, this.Height - bottomPanel.Height);
                btnSaveAll.Location = new Point(bottomPanel.Width - btnSaveAll.Width - 20, 7);
            };
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
        
        /// <summary>
        /// 保存所有分页的设置
        /// </summary>
        public void SaveAllSettings()
        {
            try
            {
                // 保存各个设置页面
                settingsGeneral?.SaveSettings();
                settingsRegex?.SaveSettings();
                settingsMaterial?.SaveSettings();
                settingsPath?.SaveSettings();
                settingsImposition?.SaveSettings();
                settingsEvent?.SaveSettings();
                
                // 最后统一保存到文件
                Utils.AppSettings.Save();
                
                // 触发快捷键更新
                if (this.ParentForm is WindowsFormsApp3.Forms.Main.MainShellForm mainForm)
                {
                    mainForm.UpdateHotkeys();
                }
                
                MessageBox.Show("所有设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (System.Exception ex)
            {
                Utils.LogHelper.Error("保存设置失败", ex);
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
