using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WindowsFormsApp3.Utils; // For AppSettings
using WindowsFormsApp3.Forms.Panels;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Forms.Main
{
    /// <summary>
    /// 主窗体壳 - 包含导航菜单和内容区域
    /// </summary>
    public partial class MainShellForm : Form
    {
        private Dictionary<string, UserControl> panelCache;
        private Dictionary<AntdUI.Button, string> navButtons = new Dictionary<AntdUI.Button, string>();
        
        // 系统托盘
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayMenu;

        #region Native Window Dragging & Hotkeys
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;
        public const int HTCAPTION = 0x0002;

        // Hotkey P/Invoke
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAddAtom(string lpString);
        [DllImport("kernel32.dll")]
        private static extern ushort GlobalDeleteAtom(IntPtr nAtom);

        private const int WM_HOTKEY = 0x0312;
        private int toggleHotkeyId = 1;
        private IntPtr toggleHotkeyAtom = IntPtr.Zero;
        private bool _allowClose = false;

        private void TitlePanel_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }



        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == toggleHotkeyId)
                {
                    ToggleWindowVisibility();
                }
            }
        }

        private void ToggleWindowVisibility()
        {
            if (this.WindowState == FormWindowState.Minimized || !this.Visible)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
                trayIcon.Visible = false;
            }
            else
            {
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
                trayIcon.Visible = true;
            }
        }
        #endregion

        #region Window Control Events
        private void HeaderPanel_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }

        private void BtnMin_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void BtnMax_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Normal;
            else
                this.WindowState = FormWindowState.Maximized;
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnCollapse_Click(object sender, EventArgs e)
        {
            // If current distance is wide (170), we collapse (to 92).
            // If current distance is narrow (92), we expand (to 170).
            
             if (mainContainer.SplitterDistance > 100)
            {
                // Collapse
                mainContainer.SplitterDistance = 92;
                titleLabel.Visible = false;
                versionLabel.Visible = false;
                btnCollapse.IconSvg = "MenuUnfoldOutlined";
            }
            else
            {
                // Expand
                mainContainer.SplitterDistance = 170;
                titleLabel.Visible = true;
                versionLabel.Visible = true;
                btnCollapse.IconSvg = "MenuFoldOutlined";
            }
        }


        #endregion
        
        // 底部导航菜单 - 已合并到主菜单


        public MainShellForm()
        {
            InitializeComponent();
            
            // 显示版本号
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                versionLabel.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
            }



            InitializeMenuItems(); 
            
            InitializeTrayIcon();
            RegisterHotkeys();
            
            // 设置窗口图标
            string iconPath = GetIconPath();
            if (!string.IsNullOrEmpty(iconPath) && System.IO.File.Exists(iconPath))
            {
                this.Icon = new Icon(iconPath);
            }
            
            // 初始化面板缓存
            panelCache = new Dictionary<string, UserControl>();
            
            // 应用保存的主题
            ApplyCurrentTheme();

            // 默认显示文件重命名面板
            SwitchToPanel("rename");
            
            // 默认激活"首页"按钮
            var homeButton = navButtons.FirstOrDefault(kvp => kvp.Value == "rename").Key;
            if (homeButton != null)
            {
                var theme = Services.ServiceLocator.Instance.GetThemeManager().GetCurrentTheme();
                homeButton.Type = AntdUI.TTypeMini.Primary; // 使用Primary
                homeButton.Ghost = false;
                homeButton.BackColor = theme.BackActive;
                homeButton.ForeColor = theme.TextPrimary;
            }
            
            // InitializeBottomMenu(); // 已合并到InitializeMenuItems

        }

        /// <summary>
        /// 设置主题 (混合模式: AntdUI + Krypton + Recursive WinForms)
        /// </summary>
        public void SetTheme(bool isDark)
        {
            // 1. 设置 AntdUI 主题 (Global Config if available, otherwise rely on individual control updates or internal detection)
            // AntdUI 2.x often uses TMode.Dark but we need to be sure about the API.
            // If SetMode is unavailable, we might rely on the window background color for some AntdUI controls?
            // Let's at least try to set the dark mode explicitly if we can find the API later.
            // For now, AntdUI controls often check their parent's background color or need explicit property updates.

            // 2. Krypton 适配
            if (kryptonManager != null)
            {
                kryptonManager.GlobalPaletteMode = isDark 
                    ? Krypton.Toolkit.PaletteMode.Office2010Black 
                    : Krypton.Toolkit.PaletteMode.Office2010Silver;
            }

            // 3. 递归更新 WinForms 控件 (Panel, Label, etc.)
            ThemeHelper.ApplyTheme(this, isDark);

            // 4. 特殊处理主界面结构 (Override recursive defaults if needed)
            if (isDark)
            {
                mainContainer.Panel1.BackColor = Color.FromArgb(40, 42, 46); // 侧边栏 - 使用深色主背景色
                navPanel.BackColor = Color.FromArgb(40, 42, 46);             // 导航菜单 - 使用深色主背景色
                logoPanel.BackColor = Color.FromArgb(40, 42, 46);            // Logo区域 - 使用深色主背景色
                panelCollapseWrapper.BackColor = Color.FromArgb(40, 42, 46); // 折叠按钮区域 - 使用深色主背景色

                contentPanel.BackColor = Color.FromArgb(30, 30, 30);         // Content Area
                headerPanel.BackColor = Color.FromArgb(45, 45, 45);          // Header
            }
            else
            {
                mainContainer.Panel1.BackColor = Color.FromArgb(248, 249, 250); // 侧边栏 - 使用浅色主背景色
                navPanel.BackColor = Color.FromArgb(248, 249, 250);             // 导航菜单 - 使用浅色主背景色
                logoPanel.BackColor = Color.FromArgb(248, 249, 250);            // Logo区域 - 使用浅色主背景色
                panelCollapseWrapper.BackColor = Color.FromArgb(248, 249, 250); // 折叠按钮区域 - 使用浅色主背景色

                contentPanel.BackColor = Color.FromArgb(248, 249, 250);         // 浅色模式：内容区浅灰
                headerPanel.BackColor = Color.White;

            }

            // 5. 更新所有已缓存的面板
            foreach (var panel in panelCache.Values)
            {
                ThemeHelper.ApplyTheme(panel, isDark);
                
                // 如果是 PDF 操作面板，应用 PDF 主题
                if (panel is Panels.PdfOperationsPanel pdfPanel)
                {
                    pdfPanel.ApplyTheme(isDark);
                }
            }

            // 保存设置
            AppSettings.CurrentThemeName = isDark ? "深色" : "浅色";
            AppSettings.Save();
        }
        
        /// <summary>
        /// 应用当前保存的主题（使用 ThemeManager）
        /// </summary>
        public void ApplyCurrentTheme()
        {
            try
            {
                var themeManager = Services.ServiceLocator.Instance.GetThemeManager();
                var themeName = AppSettings.CurrentThemeName;
                var theme = themeManager.GetThemeByName(themeName);
                
                if (theme != null)
                {
                    // 设置当前主题
                    themeManager.SetCurrentTheme(themeName);
                    
                    // Determine isDark based on theme name
                    bool isDark = themeName.Contains("深色") || themeName.Contains("Dark");

                    // Set dark mode state first
                    ThemeHelper.ApplyTheme(this, isDark);
                    
                    // 应用主题到整个界面
                    ThemeHelper.ApplyTheme(this, theme);
                    
                    // 明确设置导航菜单使用主背景色
                    navPanel.BackColor = theme.Background;
                    mainContainer.Panel1.BackColor = theme.Background;
                    logoPanel.BackColor = theme.Background;
                    panelCollapseWrapper.BackColor = theme.Background;
                    
                    // 更新所有已缓存的面板
                    foreach (var panel in panelCache.Values)
                    {
                        ThemeHelper.ApplyTheme(panel, isDark); // Ensure isDark is updated for each panel context if needed
                        ThemeHelper.ApplyTheme(panel, theme);
                        
                        // 如果是 PDF 操作面板，应用 PDF 主题
                        if (panel is Panels.PdfOperationsPanel pdfPanel)
                        {
                            pdfPanel.ApplyTheme(isDark);
                        }
                    }
                    
                    // Krypton 适配
                    if (kryptonManager != null)
                    {
                        kryptonManager.GlobalPaletteMode = isDark 
                            ? Krypton.Toolkit.PaletteMode.Office2010Black 
                            : Krypton.Toolkit.PaletteMode.Office2010Silver;
                    }

                    // 🔧 更新所有打开的 MaterialSelectFormModern 窗口
                    foreach (Form form in Application.OpenForms)
                    {
                        if (form is MaterialSelectFormModern materialForm)
                        {
                            materialForm.ApplyTheme(theme);
                        }
                    }
                    
                    // 刷新导航按钮的颜色
                    RefreshNavigationButtonColors(theme);
                }
                else
                {
                    // 如果主题不存在，回退到默认主题
                    SetTheme(AppSettings.CurrentThemeName == "深色");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("应用主题失败", ex);
                // 回退到默认主题
                SetTheme(false);
            }
        }
        
        /// <summary>
        /// 初始化底部菜单按钮 - 使用独立的Menu控件以保持样式完全一致
        /// </summary>

        /// <summary>
        /// 刷新导航按钮的颜色（主题切换时调用）
        /// </summary>
        private void RefreshNavigationButtonColors(ThemeDefinition theme)
        {
            if (theme == null || navButtons == null || navButtons.Count == 0)
                return;
                
            foreach (var btn in navButtons.Keys)
            {
                // 检查当前激活的按钮（Ghost为false表示激活状态）
                if (!btn.Ghost)
                {
                    // 已激活的按钮，更新为新主题的激活颜色
                    btn.BackColor = theme.BackActive;
                    btn.ForeColor = theme.TextPrimary;
                }
                else
                {
                    // 未激活按钮，保持透明背景和淡色文字
                    btn.BackColor = Color.Transparent;
                    btn.ForeColor = theme.TextSecondary;
                }
            }
        }
        
        private void ShowAboutDialog()
        {
            // 获取程序集版本
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string versionStr = version != null ? $"V{version.Major}.{version.Minor}.{version.Build}" : "V2.3.9";
            
            MessageBox.Show($"大诚工具箱 (Prepress Toolbox)\n版本: {versionStr}\n\n一个专业的印前处理辅助工具。", 
                "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        /// <summary>
        /// 初始化导航菜单项 (Button Implementation)
        /// </summary>
        private void InitializeMenuItems()
        {
            navPanel.Controls.Clear();
            navButtons.Clear();

            // 1. 首页 (重命名)
            AddNavButton("首页", "FolderOpenOutlined", "rename");
            
            // 2. 数据库
            AddNavButton("数据库", "DatabaseOutlined", "database");
            
            // 3. PDF操作
            AddNavButton("PDF操作", "FilePdfOutlined", "pdf_operations");
            
            // 4. 拼版工具
            AddNavButton("拼版工具", "AppstoreAddOutlined", "imposition_workspace");
             
             // 5. 设置
            AddNavButton("设置", "SettingOutlined", "settings");
            
            // 6. 更多菜单 (Special handling)
            // AddNavButton("菜单", "MoreOutlined", "menu_root"); // Can treat as normal button but override click
            AddNavButton("菜单", "MoreOutlined", "menu_root", true);
        }

        private void AddNavButton(string text, string iconSvg, string tag, bool isMenu = false)
        {
            // 侧边栏固定宽度170，减去PaddingLeft(5)，按钮宽90
            // 居中计算：(170 - 5 - 90) / 2 ≈ 37
            int marginLeft = 37;

            // 获取当前主题以设置正确的初始颜色
            var theme = Services.ServiceLocator.Instance.GetThemeManager().GetCurrentTheme();
            Color initialColor = theme != null ? theme.TextSecondary : Color.Gray;

            var btn = new AntdUI.Button
            {
                Size = new Size(100, 100),
                Radius = 6, // Slightly rounded square
                Type = AntdUI.TTypeMini.Default, 
                Ghost = true, // Start transparent
                IconSvg = iconSvg,
                IconRatio = 3f, // 适中的图标比例
                IconSize = new Size(40, 40), // 图标大小32×32像素
                IconPosition = AntdUI.TAlignMini.Top, // Icon on top
                Text = text, // Show text
                Tag = tag,
                Margin = new Padding(marginLeft, 0, marginLeft, 8), // 水平居中
                ForeColor = initialColor, // 使用主题定义的次要文字颜色
                Font = new Font("Microsoft YaHei UI", 9F), // Ensure readable font size
                // Tooltip logic could be added here if needed
            };
            
            // For menu button, maybe align differently or just add to end
            
            btn.Click += NavButton_Click;
            navPanel.Controls.Add(btn);
            navButtons.Add(btn, tag);
        }

        /// <summary>
        /// 导航按钮点击事件
        /// </summary>
        private void NavButton_Click(object sender, EventArgs e)
        {
            if (sender is AntdUI.Button btn && btn.Tag != null)
            {
                string key = btn.Tag.ToString();
                
                if (key == "menu_root")
                {
                    // Show Context Menu
                    ShowMoreMenu(btn);
                    return;
                }
                
                // 获取当前主题
                var themeManager = Services.ServiceLocator.Instance.GetThemeManager();
                var theme = themeManager.GetCurrentTheme();
                
                // Highlight logic
                foreach(var b in navButtons.Keys)
                {
                    // Reset all to normal state
                    b.Type = AntdUI.TTypeMini.Default;
                    b.Ghost = true; // 恢复透明背景
                    b.BackColor = Color.Transparent; // 确保背景透明
                    b.ForeColor = theme.TextSecondary; // 恢复未选中时的文字颜色(主题次要颜色)
                }
                
                // 使用交互色组的激活状态颜色高亮选中的按钮
                btn.Type = AntdUI.TTypeMini.Primary; // 使用Primary类型（如果有定义样式）或保持Default配合BackColor
                btn.Ghost = false; // 取消透明，显示背景色
                btn.BackColor = theme.BackActive; // 设置激活背景色
                btn.ForeColor = theme.TextPrimary; // 设置高亮文字颜色
                
                // Switch Panel
                SwitchToPanel(key);
            }
        }
        
        private void ShowMoreMenu(Control anchor)
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("打开日志", null, (s, e) => OpenLogFolder());
            contextMenu.Items.Add("关于", null, (s, e) => ShowAboutDialog());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("退出", null, (s, e) => ExitApplication());
            
            contextMenu.Show(anchor, new Point(anchor.Width, 0)); // Show to the right
        }
        
        /// <summary>
        /// 注册全局热键
        /// </summary>
        public void RegisterHotkeys()
        {
            try
            {
                // 先注销旧的热键
                if (toggleHotkeyAtom != IntPtr.Zero)
                {
                    UnregisterHotKey(this.Handle, toggleHotkeyId);
                    GlobalDeleteAtom(toggleHotkeyAtom);
                    toggleHotkeyAtom = IntPtr.Zero;
                }

                string toggleHotkey = AppSettings.ToggleMinimizeHotkey;
                if (!string.IsNullOrEmpty(toggleHotkey))
                {
                    toggleHotkeyAtom = GlobalAddAtom(Guid.NewGuid().ToString());
                    if (toggleHotkeyAtom != IntPtr.Zero)
                    {
                        int hotkeyId = (int)(toggleHotkeyAtom.ToInt64() & 0xFFFF);
                        if (ParseHotkey(toggleHotkey, out uint modifiers, out uint key))
                        {
                            if (RegisterHotKey(this.Handle, hotkeyId, modifiers, key))
                            {
                                toggleHotkeyId = hotkeyId;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"热键注册失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新热键设置 (供外部调用)
        /// </summary>
        public void UpdateHotkeys()
        {
            RegisterHotkeys();
        }

        private bool ParseHotkey(string hotkey, out uint modifiers, out uint key)
        {
            modifiers = 0;
            key = 0;

            if (string.IsNullOrEmpty(hotkey)) return false;

            string[] parts = hotkey.Split('+');
            foreach (string part in parts)
            {
                string lowerPart = part.Trim().ToLower();
                switch (lowerPart)
                {
                    case "ctrl": modifiers |= 2; break;
                    case "alt": modifiers |= 1; break;
                    case "shift": modifiers |= 4; break;
                    default:
                        if (lowerPart.Length > 0)
                        {
                            // 处理特殊键或普通字符
                            try
                            {
                                Keys k = (Keys)Enum.Parse(typeof(Keys), part, true);
                                key = (uint)k;
                            }
                            catch
                            {
                                if (char.IsLetterOrDigit(lowerPart[0]))
                                    key = (uint)char.ToUpper(lowerPart[0]);
                            }
                        }
                        break;
                }
            }
            return key != 0;
        }

        /// <summary>
        /// 菜单选择改变事件
        /// </summary>

        
        /// <summary>
        /// 初始化系统托盘图标
        /// </summary>
        private void InitializeTrayIcon()
        {
            trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Text = "大诚工具箱",
                Visible = false
            };
            
            string iconPath = GetIconPath();
            if (!string.IsNullOrEmpty(iconPath) && System.IO.File.Exists(iconPath))
            {
                trayIcon.Icon = new Icon(iconPath);
            }
            else
            {
                // Fallback to form icon or system icon
                trayIcon.Icon = this.Icon ?? SystemIcons.Application;
            }
            
            // 托盘菜单
            trayMenu = new System.Windows.Forms.ContextMenuStrip();
            trayMenu.Items.Add("显示主窗口", null, (s, e) => ShowMainWindow());
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("退出", null, (s, e) => ExitApplication());
            trayIcon.ContextMenuStrip = trayMenu;
            
            // 双击托盘图标显示窗口
            trayIcon.DoubleClick += (s, e) => ShowMainWindow();
        }

        /// <summary>
        /// 切换面板
        /// </summary>
        private void SwitchToPanel(string panelKey)
        {
            try
            {
                // 解析主面板Key (处理子菜单情况)
                string primaryKey = panelKey;
                if (panelKey.StartsWith("settings_"))
                {
                    primaryKey = "settings";
                }
                
                // 1. 检查缓存
                if (!panelCache.ContainsKey(primaryKey))
                {
                    var newPanel = CreatePanel(primaryKey);
                    if (newPanel == null) return;
                    
                    newPanel.Dock = DockStyle.Fill;
                    panelCache[primaryKey] = newPanel;
                    
                    // 应用当前临渧题到新创建的面板
                    var isDark = AppSettings.CurrentThemeName == "深色";
                    ThemeHelper.ApplyTheme(newPanel, isDark);

                    // 如果是 PDF 操作面板，需额外调用 ApplyTheme 以触发 JS 主题切换
                    if (newPanel is WindowsFormsApp3.Forms.Panels.PdfOperationsPanel pdfPanel)
                    {
                        pdfPanel.ApplyTheme(isDark);
                    }
                }

                var panel = panelCache[primaryKey];

                // 2. 切换显示
                contentPanel.Controls.Clear();
                contentPanel.Controls.Add(panel);
                panel.BringToFront();
                panel.Show();

                // 3. 处理深度链接/子菜单动作
                if (primaryKey == "settings" && panel is WindowsFormsApp3.Forms.Panels.SettingsPanel settingsPanel)
                {
                    // 如果是设置相关的具体子项，调用面板的导航方法
                    if (panelKey != "settings")
                    {
                        settingsPanel.NavigateToSection(panelKey);
                    }
                }

                // 4. 触发面板刷新（如果支持）
                if (panel is WindowsFormsApp3.Forms.Panels.BasePanelControl basePanel)
                {
                     // 可以调用公开的 RefreshData 或其他生命周期方法
                     // basePanel.OnActivated(); 
                }

                // Update status bar

            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换面板失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 创建功能面板工厂方法
        /// </summary>
        private UserControl CreatePanel(string panelKey)
        {
            // 根据 Key 创建对应面板
            switch (panelKey)
            {
                case "rename":
                    return new WindowsFormsApp3.Forms.Panels.FileRenamePanel();
                    
                case "database":
                    return new WindowsFormsApp3.Forms.Panels.DatabasePanel();
                    
                case "excel": 
                    var excelPanel = new WindowsFormsApp3.Forms.Panels.ExcelImportPanel();
                    excelPanel.DataImported += OnExcelDataImported;
                    return excelPanel;
                    
                case "settings":
                case "settings_general":
                case "settings_regex":
                case "settings_material":
                case "settings_path":
                    // 所有设置相关菜单都返回设置面板
                    return new WindowsFormsApp3.Forms.Panels.SettingsPanel();
                    
                case "pdf_operations":
                    return new WindowsFormsApp3.Forms.Panels.PdfOperationsPanel();

                case "imposition_workspace":
                    return new WindowsFormsApp3.Forms.Panels.ImpositionWorkspacePanel();
                    
                default:
                    return CreatePlaceholderPanel(panelKey);
            }
        }

        /// <summary>
        /// Excel数据导入完成回调
        /// </summary>
        private void OnExcelDataImported(object sender, EventArgs e)
        {
            // 切换回重命名面板
            SwitchToPanel("rename");
            
            // 刷新重命名面板的数据
            if (panelCache.TryGetValue("rename", out var control) 
                && control is WindowsFormsApp3.Forms.Panels.FileRenamePanel renamePanel)
            {
                renamePanel.UpdateExcelData();
            }
        }

        /// <summary>
        /// 创建占位面板
        /// </summary>
        private UserControl CreatePlaceholderPanel(string panelKey)
        {
            var panel = new UserControl
            {
                BackColor = Color.White
            };
            
            var label = new Label
            {
                Text = $"功能面板: {GetPanelDisplayName(panelKey)}\n\n(阶段三将实现)",
                Font = new Font("Microsoft YaHei UI", 16),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(50, 50)
            };
            panel.Controls.Add(label);
            
            return panel;
        }
        
        /// <summary>
        /// 获取面板显示名称
        /// </summary>
        private string GetPanelDisplayName(string panelKey)
        {
            return panelKey switch
            {
                "rename" => "文件重命名",
                "excel" => "Excel导入",
                "settings" => "设置",
                "pdf_operations" => "PDF操作",
                _ => panelKey
            };
        }
        

        
        /// <summary>
        /// 显示主窗口
        /// </summary>
        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            trayIcon.Visible = false;
        }
        
        /// <summary>
        /// 打开日志文件夹
        /// </summary>
        private void OpenLogFolder()
        {
            try
            {
                // 获取日志文件夹路径
                string logFolderPath = AppDataPathManager.LogsDirectory;
                
                // 检查日志文件夹是否存在
                if (System.IO.Directory.Exists(logFolderPath))
                {
                    // 打开日志文件夹
                    System.Diagnostics.Process.Start("explorer.exe", logFolderPath);
                }
                else
                {
                    // 如果文件夹不存在，尝试创建
                    System.IO.Directory.CreateDirectory(logFolderPath);
                    
                    // 再次检查是否创建成功
                    if (System.IO.Directory.Exists(logFolderPath))
                    {
                        // 打开创建的文件夹
                        System.Diagnostics.Process.Start("explorer.exe", logFolderPath);
                        AntdUI.Message.success(this, "日志文件夹已创建并打开", autoClose: 3);
                    }
                    else
                    {
                        // 显示错误消息
                        string errorMessage = "无法访问日志文件夹: " + logFolderPath;
                        AntdUI.Message.error(this, errorMessage, autoClose: 3);
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "打开日志文件夹时发生错误: " + ex.Message;
                AntdUI.Message.error(this, errorMessage, autoClose: 3);
            }
        }
        
        /// <summary>
        /// 退出应用程序
        /// </summary>
        private void ExitApplication()
        {
            _allowClose = true;
            trayIcon.Visible = false;
            trayIcon.Dispose();
            
            // 注销热键
            if (toggleHotkeyAtom != IntPtr.Zero)
            {
                UnregisterHotKey(this.Handle, toggleHotkeyId);
                GlobalDeleteAtom(toggleHotkeyAtom);
            }

            Application.Exit();
        }
        
        /// <summary>
        /// 窗口大小改变时处理托盘
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                trayIcon.Visible = true;
            }
        }
        
        /// <summary>
        /// 窗口关闭时最小化到托盘
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !_allowClose)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
                trayIcon.Visible = true;
            }
            else
            {
                base.OnFormClosing(e);
            }
        }
        
        /// <summary>
        /// 获取图标路径 (支持根目录和Resources子目录)
        /// </summary>
        private string GetIconPath()
        {
            string[] paths = new[]
            {
                System.IO.Path.Combine(Application.StartupPath, "dc.ico"),
                System.IO.Path.Combine(Application.StartupPath, "Resources", "dc.ico")
            };
            
            foreach (var path in paths)
            {
                if (System.IO.File.Exists(path))
                {
                    return path;
                }
            }
            
            return null;
        }
        
        // Dispose method is removed (handled in Designer.cs)
    }
}
