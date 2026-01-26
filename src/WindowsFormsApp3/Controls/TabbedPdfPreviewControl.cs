using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntdUI;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Controls
{
    /// <summary>
    /// 支持标签页的PDF预览控件
    /// 允许同时打开和显示多个PDF文件
    /// </summary>
    public class TabbedPdfPreviewControl : UserControl
    {
        #region Fields

        private AntdUI.Tabs _tabs;
        private System.Windows.Forms.ContextMenuStrip _tabContextMenu;
        private CefPdfPreviewControl _emptyStateControl; // 空状态下的全功能PDF控件
        private System.Windows.Forms.Panel _tabHeaderPanel; // 标签页顶部面板
        private AntdUI.Button _closeTabButton; // 关闭当前标签页按钮
        private List<TabInfo> _tabInfos;
        private int _currentTabIndex = -1;
        private const int MAX_TABS = 10;
        private bool _isClosingTab = false; // 标志位，防止重复处理关闭事件

        #endregion

        #region Properties

        /// <summary>
        /// 当前活动标签页的PDF控件
        /// </summary>
        public CefPdfPreviewControl CurrentPdfControl
        {
            get
            {
                if (_currentTabIndex >= 0 && _currentTabIndex < _tabInfos.Count)
                {
                    return _tabInfos[_currentTabIndex].PdfControl;
                }
                return null;
            }
        }

        /// <summary>
        /// 所有已打开的PDF文件路径
        /// </summary>
        public List<string> OpenedFiles => _tabInfos.Select(t => t.FilePath).ToList();

        /// <summary>
        /// 当前标签页数量
        /// </summary>
        public int TabCount => _tabInfos.Count;

        /// <summary>
        /// 当前活动标签页索引
        /// </summary>
        public int CurrentTabIndex => _currentTabIndex;

        /// <summary>
        /// 当前页码（当前活动PDF的页码）
        /// </summary>
        public int CurrentPage => CurrentPdfControl?.CurrentPage ?? 0;

        /// <summary>
        /// 总页数（当前活动PDF的总页数）
        /// </summary>
        public int PageCount => CurrentPdfControl?.PageCount ?? 0;

        #endregion

        #region Events

        /// <summary>
        /// 当前活动标签页改变
        /// </summary>
        public event EventHandler<TabChangedEventArgs> TabChanged;

        /// <summary>
        /// 标签页关闭
        /// </summary>
        public event EventHandler<TabClosedEventArgs> TabClosed;

        /// <summary>
        /// 所有标签页关闭
        /// </summary>
        public event EventHandler AllTabsClosed;

        /// <summary>
        /// PDF加载完成（转发自当前活动标签页）
        /// </summary>
        public event EventHandler<PdfLoadedEventArgs> PdfLoaded;

        /// <summary>
        /// 页面改变（转发自当前活动标签页）
        /// </summary>
        public event EventHandler<PageChangedEventArgs> PageChanged;

        /// <summary>
        /// 加载错误（转发自当前活动标签页）
        /// </summary>
        public event EventHandler<PdfLoadErrorEventArgs> LoadError;

        #endregion

        #region Constructor

        public TabbedPdfPreviewControl()
        {
            _tabInfos = new List<TabInfo>();
            InitializeComponent();
        }

        #endregion

        #region Initialization

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 设置控件属性
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(240, 240, 240);

            // 创建标签页顶部面板（包含标签页和关闭按钮）
            _tabHeaderPanel = new System.Windows.Forms.Panel();
            _tabHeaderPanel.Dock = DockStyle.Fill;
            _tabHeaderPanel.Visible = false; // 初始隐藏，有标签页时显示

            // 创建关闭按钮
            _closeTabButton = new AntdUI.Button();
            _closeTabButton.Text = "✕";
            _closeTabButton.Size = new System.Drawing.Size(24, 24); // 小正方形
            _closeTabButton.Location = new System.Drawing.Point(0, 0); // 初始位置，稍后会调整
            _closeTabButton.Anchor = AnchorStyles.Top | AnchorStyles.Right; // 锚定到右上角
            _closeTabButton.Type = AntdUI.TTypeMini.Default; // 使用默认样式而非红色
            _closeTabButton.BorderWidth = 0; // 无边框
            _closeTabButton.BackColor = Color.Transparent;
            _closeTabButton.ForeColor = Color.Gray;
            _closeTabButton.Font = new Font("Segoe UI", 8F);
            _closeTabButton.Margin = new Padding(0);
            _closeTabButton.Padding = new Padding(0);
            
            // 在面板调整大小时重新定位按钮到右上角
            _tabHeaderPanel.SizeChanged += (s, e) =>
            {
                _closeTabButton.Location = new System.Drawing.Point(
                    _tabHeaderPanel.Width - _closeTabButton.Width - 2, 
                    2);
            };
            
            _closeTabButton.Click += (s, e) =>
            {
                if (_currentTabIndex >= 0)
                {
                    LogHelper.Info($"[TabbedPdfPreview] 工具栏按钮关闭当前标签页: {_currentTabIndex}");
                    CloseTab(_currentTabIndex);
                }
            };

            // 创建标签页控件
            _tabs = new AntdUI.Tabs();
            _tabs.Dock = DockStyle.Fill;
            _tabs.Gap = 10;
            // 尝试使用StyleCard以支持关闭按钮
            // 假定AntdUI有StyleCard类
            _tabs.Style = new AntdUI.Tabs.StyleCard(); 
            
            // 订阅鼠标事件用于中键关闭
            _tabs.MouseDown += OnTabsMouseDown;
            // 尝试启用关闭功能
            // _tabs.ReadOnly = false; // Probe

            // 将标签页和按钮添加到顶部面板
            _tabHeaderPanel.Controls.Add(_tabs);
            _tabHeaderPanel.Controls.Add(_closeTabButton);
            _closeTabButton.BringToFront(); // 确保按钮在最上层

            // 创建空状态控件（常驻显示全功能PDF界面，但不加载文件）
            _emptyStateControl = new CefPdfPreviewControl();
            _emptyStateControl.Dock = DockStyle.Fill;
            // 初始化浏览器，让它显示viewer.html
            _emptyStateControl.InitializeBrowser(); 

            // 创建右键菜单
            _tabContextMenu = new System.Windows.Forms.ContextMenuStrip();
            var closeItem = new System.Windows.Forms.ToolStripMenuItem("关闭当前标签页");
            closeItem.Click += (s, e) => { if (_currentTabIndex >= 0) CloseTab(_currentTabIndex); };
            var closeAllItem = new System.Windows.Forms.ToolStripMenuItem("关闭所有标签页");
            closeAllItem.Click += (s, e) => CloseAllTabs();
            
            _tabContextMenu.Items.Add(closeItem);
            _tabContextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            _tabContextMenu.Items.Add(closeAllItem);
            
            _tabs.ContextMenuStrip = _tabContextMenu;

            // 添加控件
            // 先添加emptyStateControl，再添加tabHeaderPanel
            // 这样如果不隐藏，tabHeaderPanel会覆盖emptyStateControl（因为最后添加的在最上层，除非调整Z-Order）
            this.Controls.Add(_emptyStateControl);
            this.Controls.Add(_tabHeaderPanel);

            this.ResumeLayout(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 添加新标签页并加载PDF
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <returns>新标签页的索引，失败返回-1</returns>
        public async Task<int> AddTabAsync(string pdfPath)
        {
            if (string.IsNullOrEmpty(pdfPath) || !File.Exists(pdfPath))
            {
                LogHelper.Error($"[TabbedPdfPreview] PDF文件不存在: {pdfPath}");
                return -1;
            }

            // 检查是否已经打开该文件
            var existingIndex = _tabInfos.FindIndex(t => t.FilePath.Equals(pdfPath, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                // 切换到已存在的标签页
                SwitchToTab(existingIndex);
                LogHelper.Info($"[TabbedPdfPreview] 切换到已打开的文件: {Path.GetFileName(pdfPath)}");
                return existingIndex;
            }

            // 检查标签页数量限制
            if (_tabInfos.Count >= MAX_TABS)
            {
                MessageBox.Show($"已达到最大标签页数量限制({MAX_TABS}个)，请关闭部分标签页后再试。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return -1;
            }

            try
            {
                // 创建标签页信息
                var tabInfo = new TabInfo
                {
                    FilePath = pdfPath,
                    LoadTime = DateTime.Now
                };

                // 创建容器面板
                tabInfo.ContainerPanel = new System.Windows.Forms.Panel();
                tabInfo.ContainerPanel.Dock = DockStyle.Fill;
                tabInfo.ContainerPanel.BackColor = Color.FromArgb(45, 45, 45);

                // 创建PDF预览控件
                tabInfo.PdfControl = new CefPdfPreviewControl();
                tabInfo.PdfControl.Dock = DockStyle.Fill;

                // 订阅事件
                tabInfo.PdfControl.PdfLoaded += OnPdfControlLoaded;
                tabInfo.PdfControl.PageChanged += OnPdfControlPageChanged;
                tabInfo.PdfControl.LoadError += OnPdfControlLoadError;

                // 添加到容器
                tabInfo.ContainerPanel.Controls.Add(tabInfo.PdfControl);

                // 初始化浏览器
                tabInfo.PdfControl.InitializeBrowser();

                // 添加到列表
                _tabInfos.Add(tabInfo);
                int newIndex = _tabInfos.Count - 1;

                // 创建标签页标题
                string tabTitle = GenerateTabTitle(pdfPath);

                // 添加到标签页控件
                var tabPage = new AntdUI.TabPage
                {
                    Text = tabTitle,
                    Tag = newIndex
                };
                tabPage.Controls.Add(tabInfo.ContainerPanel);

                _tabs.Pages.Add(tabPage);

                // 显示标签页控件，隐藏空状态控件
                _emptyStateControl.Visible = false;
                _tabHeaderPanel.Visible = true;

                // 切换到新标签页
                SwitchToTab(newIndex);

                // 加载PDF
                await tabInfo.PdfControl.LoadPdfAsync(pdfPath);

                LogHelper.Info($"[TabbedPdfPreview] 添加标签页成功: {tabTitle}");

                return newIndex;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[TabbedPdfPreview] 添加标签页失败: {ex.Message}", ex);
                return -1;
            }
        }

        /// <summary>
        /// 关闭指定标签页
        /// </summary>
        /// <param name="tabIndex">标签页索引</param>
        public void CloseTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= _tabInfos.Count)
            {
                LogHelper.Warn($"[TabbedPdfPreview] CloseTab: 索引超出范围 tabIndex={tabIndex}, count={_tabInfos.Count}");
                return;
            }

            if (_isClosingTab) // 防止重入
            {
                LogHelper.Warn($"[TabbedPdfPreview] CloseTab: 已在关闭中，忽略重复请求");
                return;
            }

            try
            {
                _isClosingTab = true; // 设置标志位
                var tabInfo = _tabInfos[tabIndex];
                var filePath = tabInfo.FilePath;
                var pdfControl = tabInfo.PdfControl; // 保存引用
                
                LogHelper.Info($"[TabbedPdfPreview] 开始关闭标签页 {tabIndex}: {Path.GetFileName(filePath)}");

                // 取消订阅事件
                try
                {
                    if (pdfControl != null)
                    {
                        LogHelper.Debug($"[TabbedPdfPreview] 取消订阅事件...");
                        pdfControl.PdfLoaded -= OnPdfControlLoaded;
                        pdfControl.PageChanged -= OnPdfControlPageChanged;
                        pdfControl.LoadError -= OnPdfControlLoadError;
                        
                        // ⚠️ 关键修复：延迟Dispose CefSharp控件，避免立即释放导致崩溃
                        LogHelper.Debug($"[TabbedPdfPreview] 延迟释放PdfControl...");
                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            try
                            {
                                // 等待50ms让UI线程完成当前操作
                                await System.Threading.Tasks.Task.Delay(50);
                                
                                // 在后台线程延迟Dispose
                                if (pdfControl.InvokeRequired)
                                {
                                    pdfControl.Invoke(new Action(() =>
                                    {
                                        try
                                        {
                                            pdfControl.Dispose();
                                            LogHelper.Debug($"[TabbedPdfPreview] PdfControl已异步释放: {Path.GetFileName(filePath)}");
                                        }
                                        catch (Exception disposeEx)
                                        {
                                            LogHelper.Error($"[TabbedPdfPreview] 异步Dispose失败: {disposeEx.Message}", disposeEx);
                                        }
                                    }));
                                }
                                else
                                {
                                    pdfControl.Dispose();
                                    LogHelper.Debug($"[TabbedPdfPreview] PdfControl已异步释放: {Path.GetFileName(filePath)}");
                                }
                            }
                            catch (Exception asyncEx)
                            {
                                LogHelper.Error($"[TabbedPdfPreview] 异步Dispose任务失败: {asyncEx.Message}", asyncEx);
                            }
                        });
                        
                        LogHelper.Debug($"[TabbedPdfPreview] PdfControl已安排延迟释放");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"[TabbedPdfPreview] 处理PdfControl事件取消失败: {ex.Message}", ex);
                    // 继续执行，不要因为事件取消失败而中断
                }

                // 从列表中移除
                LogHelper.Debug($"[TabbedPdfPreview] 从_tabInfos中移除...");
                _tabInfos.RemoveAt(tabIndex);

                // 从标签页控件中移除（这会触发OnTabsControlRemoved，但由于标志位已设置，不会重复处理）
                try
                {
                    if (_tabs.Pages != null && tabIndex < _tabs.Pages.Count)
                    {
                        LogHelper.Debug($"[TabbedPdfPreview] 从_tabs.Pages中移除...");
                        _tabs.Pages.RemoveAt(tabIndex);
                        LogHelper.Debug($"[TabbedPdfPreview] 已从标签页移除");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"[TabbedPdfPreview] 移除标签页UI失败: {ex.Message}", ex);
                }

                // 触发关闭事件
                try
                {
                    TabClosed?.Invoke(this, new TabClosedEventArgs(filePath, tabIndex));
                    LogHelper.Debug($"[TabbedPdfPreview] TabClosed事件已触发");
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"[TabbedPdfPreview] 触发TabClosed事件失败: {ex.Message}", ex);
                }

                LogHelper.Info($"[TabbedPdfPreview] 标签页已关闭: {Path.GetFileName(filePath)}");

                // 如果没有标签页了，显示空状态控件
                if (_tabInfos.Count == 0)
                {
                    LogHelper.Info($"[TabbedPdfPreview] 所有标签页已关闭，显示空状态控件");
                    _tabHeaderPanel.Visible = false; // 隐藏标签页面板
                    _emptyStateControl.Visible = true;
                    _currentTabIndex = -1;
                    
                    try
                    {
                        AllTabsClosed?.Invoke(this, EventArgs.Empty);
                        LogHelper.Debug($"[TabbedPdfPreview] AllTabsClosed事件已触发");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"[TabbedPdfPreview] 触发AllTabsClosed事件失败: {ex.Message}", ex);
                    }
                }
                else
                {
                    // 切换到相邻标签页
                    int newIndex = Math.Min(tabIndex, _tabInfos.Count - 1);
                    LogHelper.Debug($"[TabbedPdfPreview] 切换到标签页 {newIndex}");
                    SwitchToTab(newIndex);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[TabbedPdfPreview] 关闭标签页失败: {ex.Message}", ex);
                MessageBox.Show($"关闭标签页失败: {ex.Message}\n\n{ex.StackTrace}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isClosingTab = false; // 重置标志位
                LogHelper.Debug($"[TabbedPdfPreview] CloseTab完成，重置标志位");
            }
        }

        /// <summary>
        /// 切换到指定标签页
        /// </summary>
        /// <param name="tabIndex">标签页索引</param>
        public void SwitchToTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= _tabInfos.Count)
                return;

            if (_currentTabIndex == tabIndex)
                return;

            var oldIndex = _currentTabIndex;
            _currentTabIndex = tabIndex;

            // 更新AntdUI.Tabs的选中索引
            if (_tabs.Pages != null && tabIndex < _tabs.Pages.Count)
            {
                _tabs.SelectedIndex = tabIndex;
            }

            // 触发标签页切换事件
            TabChanged?.Invoke(this, new TabChangedEventArgs(oldIndex, tabIndex, _tabInfos[tabIndex].FilePath));

            LogHelper.Debug($"[TabbedPdfPreview] 切换标签页: {oldIndex} -> {tabIndex}");
        }

        /// <summary>
        /// 关闭所有标签页
        /// </summary>
        public void CloseAllTabs()
        {
            try
            {
                LogHelper.Info($"[TabbedPdfPreview] 开始关闭所有标签页，当前共 {_tabInfos.Count} 个标签页");
                
                int closeCount = 0;
                while (_tabInfos.Count > 0)
                {
                    closeCount++;
                    LogHelper.Debug($"[TabbedPdfPreview] CloseAllTabs: 关闭第 {closeCount} 个标签页，剩余 {_tabInfos.Count}");
                    CloseTab(0);
                    
                    // 添加短暂延迟，避免过快关闭导致的问题
                    System.Threading.Thread.Sleep(10);
                }
                
                LogHelper.Info($"[TabbedPdfPreview] 所有标签页已关闭，共关闭 {closeCount} 个");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[TabbedPdfPreview] CloseAllTabs失败: {ex.Message}", ex);
                MessageBox.Show($"关闭所有标签页失败: {ex.Message}\n\n{ex.StackTrace}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 应用主题到所有PDF控件
        /// </summary>
        /// <param name="isDark">是否为深色模式</param>
        public void ApplyTheme(bool isDark)
        {
            // 更新背景色
            this.BackColor = isDark ? Color.FromArgb(40, 40, 40) : Color.FromArgb(240, 240, 240);
            
            // 应用到空状态控件
            _emptyStateControl?.ApplyTheme(isDark);

            // 应用到所有PDF控件
            foreach (var tabInfo in _tabInfos)
            {
                tabInfo.PdfControl?.ApplyTheme(isDark);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 生成标签页标题
        /// </summary>
        private string GenerateTabTitle(string filePath)
        {
            string fileName = Path.GetFileName(filePath);

            // 检查是否有重名文件
            var sameNameCount = _tabInfos.Count(t => Path.GetFileName(t.FilePath) == fileName);
            if (sameNameCount > 0)
            {
                // 有重名，显示部分路径
                string dirName = Path.GetFileName(Path.GetDirectoryName(filePath));
                fileName = $"{fileName} ({dirName})";
            }

            // 限制长度
            if (fileName.Length > 30)
            {
                fileName = fileName.Substring(0, 27) + "...";
            }

            return fileName;
        }

        #endregion

        #region Event Handlers

        private void OnTabsMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                // 鼠标中键关闭标签页
                // AntdUI 会在 MouseDown 时自动切换 SelectedIndex
                // 我们在 MouseDown 后获取新的 SelectedIndex 即为点击的标签页
                this.BeginInvoke(new Action(() =>
                {
                    int clickedIndex = _tabs.SelectedIndex;
                    if (clickedIndex >= 0 && clickedIndex < _tabInfos.Count)
                    {
                        LogHelper.Info($"[TabbedPdfPreview] 中键点击关闭标签页: {clickedIndex}");
                        CloseTab(clickedIndex);
                    }
                }));
            }
        }

        private void OnTabsControlRemoved(object sender, ControlEventArgs e)
        {
            // 如果是通过CloseTab方法触发的移除，则跳过处理（避免重复）
            if (_isClosingTab)
                return;

            if (e.Control is AntdUI.TabPage tabPage)
            {
                // 查找对应的TabInfo并移除
                // 通过Tag或对象引用查找
                var info = _tabInfos.FirstOrDefault(t => t.ContainerPanel.Parent == tabPage);
                
                if (info != null)
                {
                    int index = _tabInfos.IndexOf(info);
                    if (index >= 0)
                    {
                        try
                        {
                            _isClosingTab = true; // 设置标志位
                            
                            // 这是一个内部关闭（由UI触发），我们需要清理资源但不触发CloseTab的UI操作（因为它已经关闭了）
                            // 但我们需要触发外部事件
                            
                            // 释放控件
                            info.PdfControl?.Dispose();
                            
                            _tabInfos.RemoveAt(index);
                            
                            // 触发事件
                            TabClosed?.Invoke(this, new TabClosedEventArgs(info.FilePath, index));
                            LogHelper.Info($"[TabbedPdfPreview] 标签页被移除: {Path.GetFileName(info.FilePath)}");

                             // 如果没有标签页了，显示空状态控件
                            if (_tabInfos.Count == 0)
                            {
                                _tabHeaderPanel.Visible = false;
                                _emptyStateControl.Visible = true;
                                _currentTabIndex = -1;
                                AllTabsClosed?.Invoke(this, EventArgs.Empty);
                            }
                            else
                            {
                                // 更新选中索引（如果需要）
                                // AntdUI应该会自动处理选中项，我们只需要同步 _currentTabIndex
                                if (_tabs.SelectedIndex >= 0 && _tabs.SelectedIndex < _tabs.Pages.Count)
                                {
                                    int newIndex = _tabs.SelectedIndex;
                                    if (newIndex < _tabInfos.Count)
                                    {
                                        SwitchToTab(newIndex);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            _isClosingTab = false; // 重置标志位
                        }
                    }
                }
            }
        }

        private void OnPdfControlLoaded(object sender, PdfLoadedEventArgs e)
        {
            // 只转发当前活动标签页的事件
            if (sender == CurrentPdfControl)
            {
                PdfLoaded?.Invoke(this, e);
            }
        }

        private void OnPdfControlPageChanged(object sender, PageChangedEventArgs e)
        {
            // 只转发当前活动标签页的事件
            if (sender == CurrentPdfControl)
            {
                PageChanged?.Invoke(this, e);
            }
        }

        private void OnPdfControlLoadError(object sender, PdfLoadErrorEventArgs e)
        {
            // 只转发当前活动标签页的事件
            if (sender == CurrentPdfControl)
            {
                LoadError?.Invoke(this, e);
            }
        }

        #endregion

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CloseAllTabs();
                _emptyStateControl?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// 标签页信息
        /// </summary>
        private class TabInfo
        {
            public string FilePath { get; set; }
            public CefPdfPreviewControl PdfControl { get; set; }
            public System.Windows.Forms.Panel ContainerPanel { get; set; }
            public DateTime LoadTime { get; set; }
        }

        #endregion
    }

    #region Event Args

    public class TabChangedEventArgs : EventArgs
    {
        public int OldIndex { get; }
        public int NewIndex { get; }
        public string FilePath { get; }

        public TabChangedEventArgs(int oldIndex, int newIndex, string filePath)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
            FilePath = filePath;
        }
    }

    public class TabClosedEventArgs : EventArgs
    {
        public string FilePath { get; }
        public int TabIndex { get; }

        public TabClosedEventArgs(string filePath, int tabIndex)
        {
            FilePath = filePath;
            TabIndex = tabIndex;
        }
    }

    #endregion
}
