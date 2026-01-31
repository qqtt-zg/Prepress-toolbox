using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Forms.Controls.Settings;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Services.Events;

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
            
            // 初始化主题编辑器
            InitializeThemeEditor();
            
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

        /// <summary>
        /// 初始化主题编辑器
        /// </summary>
        private void InitializeThemeEditor()
        {
            try
            {
                var themeManager = Services.ServiceLocator.Instance.GetThemeManager();
                themeEditor.Initialize(themeManager);
                
                // 绑定主题改变事件
                themeEditor.ThemeChanged += (s, e) =>
                {
                    // 通知主窗体应用新主题
                    if (this.ParentForm is WindowsFormsApp3.Forms.Main.MainShellForm mainForm)
                    {
                        mainForm.ApplyCurrentTheme();
                    }
                };
            }
            catch (Exception ex)
            {
                LogHelper.Error("初始化主题编辑器失败", ex);
            }
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
                settingsFontText?.SaveSettings();
                
                // 最后统一保存到文件
                AppSettings.Save();

                // 发布配置保存事件，通知各模块立即应用新设置
                try
                {
                    var eventBus = Services.ServiceLocator.Instance.GetEventBus();
                    eventBus?.Publish(new ConfigSavedEvent
                    {
                        ConfigKey = "AppSettings",
                        SavedItemsCount = 0,
                        ConfigFilePath = AppDataPathManager.ConfigFilePath
                    });
                }
                catch (Exception ex)
                {
                    LogHelper.Warn($"发布配置保存事件失败: {ex.Message}");
                }
                
                // 触发快捷键更新
                if (this.ParentForm is WindowsFormsApp3.Forms.Main.MainShellForm mainForm)
                {
                    mainForm.UpdateHotkeys();
                }
                
                MessageBox.Show("所有设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (System.Exception ex)
            {
                LogHelper.Error("保存设置失败", ex);
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
                case "theme":
                    tabsMain.SelectedIndex = 6;
                    break;
                case "font":
                case "text":
                    tabsMain.SelectedIndex = 7;
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
