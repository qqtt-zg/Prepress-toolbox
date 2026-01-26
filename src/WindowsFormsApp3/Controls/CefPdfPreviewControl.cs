using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Controls
{
    /// <summary>
    /// 基于CefSharp的PDF预览控件
    /// 使用Chromium内核 + PDF.js实现，支持Windows 7及以上系统
    /// </summary>
    public class CefPdfPreviewControl : UserControl
    {
        #region Fields

        private ChromiumWebBrowser _browser;
        private bool _isInitialized = false;
        private string _pendingPdfPath;
        private bool? _pendingIsDark; // 待应用的主题状态
        private int _currentPage = 0;
        private int _totalPages = 0;

        #endregion

        #region Events

        /// <summary>
        /// PDF加载完成事件
        /// </summary>
        public event EventHandler<PdfLoadedEventArgs> PdfLoaded;

        /// <summary>
        /// 页面改变事件
        /// </summary>
        public event EventHandler<PageChangedEventArgs> PageChanged;

        /// <summary>
        /// 加载错误事件
        /// </summary>
        public event EventHandler<PdfLoadErrorEventArgs> LoadError;

        #endregion

        #region Properties

        /// <summary>
        /// 当前页码（1-based）
        /// </summary>
        public int CurrentPage => _currentPage;

        /// <summary>
        /// 总页数
        /// </summary>
        public int PageCount => _totalPages;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region Constructor

        public CefPdfPreviewControl()
        {
            InitializeComponent();
        }

        #endregion

        #region Initialization

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Dock = DockStyle.Fill;
            this.BackColor = System.Drawing.Color.FromArgb(45, 45, 45);
            this.ResumeLayout(false);
        }

        /// <summary>
        /// 初始化CefSharp浏览器（需在UI线程调用）
        /// </summary>
        public void InitializeBrowser()
        {
            if (_isInitialized) return;

            try
            {
                LogHelper.Debug("[CefPdfPreview] 开始初始化CefSharp浏览器");

                // 检查Cef是否已初始化
                EnsureCefInitialized();

                // 创建浏览器控件
                _browser = new ChromiumWebBrowser();
                _browser.Dock = DockStyle.Fill;

                // 注册JS回调对象
                _browser.JavascriptObjectRepository.Register("csharpBridge", new JsBridge(this), true);

                // 监听加载完成事件
                _browser.FrameLoadEnd += Browser_FrameLoadEnd;

                this.Controls.Add(_browser);

                // 加载PDF.js查看器
                string viewerPath = GetViewerPath();
                if (File.Exists(viewerPath))
                {
                    _browser.Load($"file:///{viewerPath.Replace("\\", "/")}");
                    LogHelper.Debug($"[CefPdfPreview] 加载viewer: {viewerPath}");
                }
                else
                {
                    LogHelper.Error($"[CefPdfPreview] viewer.html不存在: {viewerPath}");
                }

                _isInitialized = true;
                LogHelper.Info("[CefPdfPreview] CefSharp浏览器初始化完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[CefPdfPreview] 初始化失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 检查Cef是否已初始化（初始化在Program.cs中完成）
        /// </summary>
        private void EnsureCefInitialized()
        {
            if (!Cef.IsInitialized)
            {
                throw new InvalidOperationException("CefSharp尚未初始化，请确保在Program.cs中调用InitializeCefSharp()");
            }
        }

        private string GetViewerPath()
        {
            return Path.Combine(Application.StartupPath, "Resources", "pdfjs", "viewer.html");
        }

        #endregion

        #region PDF Loading

        /// <summary>
        /// 加载PDF文件
        /// </summary>
        public async Task LoadPdfAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                LogHelper.Error($"[CefPdfPreview] PDF文件不存在: {filePath}");
                return;
            }

            if (!_isInitialized)
            {
                // 保存待加载的路径，等初始化完成后加载
                _pendingPdfPath = filePath;
                LogHelper.Debug("[CefPdfPreview] 浏览器未初始化，保存待加载路径");
                return;
            }

            try
            {
                LogHelper.Debug($"[CefPdfPreview] 开始加载PDF: {filePath}");

                // 等待浏览器完全初始化并且可以执行JavaScript
                if (!_browser.IsBrowserInitialized || !_browser.CanExecuteJavascriptInMainFrame)
                {
                    LogHelper.Debug("[CefPdfPreview] 等待浏览器和Frame初始化...");
                    // 等待最多10秒
                    int waitCount = 0;
                    while ((!_browser.IsBrowserInitialized || !_browser.CanExecuteJavascriptInMainFrame) && waitCount < 100)
                    {
                        await Task.Delay(100);
                        waitCount++;
                    }
                    
                    if (!_browser.IsBrowserInitialized || !_browser.CanExecuteJavascriptInMainFrame)
                    {
                        LogHelper.Error("[CefPdfPreview] 浏览器或Frame初始化超时");
                        LoadError?.Invoke(this, new PdfLoadErrorEventArgs("浏览器初始化超时"));
                        return;
                    }
                }

                LogHelper.Debug("[CefPdfPreview] 浏览器和Frame已就绪，可以执行JavaScript");

                // 读取PDF并转换为Base64
                byte[] pdfBytes = File.ReadAllBytes(filePath);
                string base64 = Convert.ToBase64String(pdfBytes);
                string dataUrl = $"data:application/pdf;base64,{base64}";

                // 调用JavaScript加载PDF（使用GetMainFrame().ExecuteJavaScriptAsync）
                string script = $"loadPdf('{dataUrl}');";
                
                // ExecuteJavaScriptAsync 不返回值，直接调用
                _browser.GetMainFrame().ExecuteJavaScriptAsync(script);
                
                // 等待一小段时间让脚本执行
                await Task.Delay(100);

                LogHelper.Debug($"[CefPdfPreview] PDF Base64已发送, 大小: {pdfBytes.Length / 1024}KB");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[CefPdfPreview] 加载PDF失败: {ex.Message}", ex);
                LoadError?.Invoke(this, new PdfLoadErrorEventArgs(ex.Message));
            }
        }

        /// <summary>
        /// 导航到指定页
        /// </summary>
        public async Task GoToPageAsync(int page)
        {
            if (_browser != null && _isInitialized && _browser.CanExecuteJavascriptInMainFrame && page >= 1 && page <= _totalPages)
            {
                _browser.GetMainFrame().ExecuteJavaScriptAsync($"goToPage({page});");
                await Task.Delay(50); // 等待脚本执行
            }
        }

        /// <summary>
        /// 顺时针旋转
        /// </summary>
        public async Task RotateClockwiseAsync()
        {
            if (_browser != null && _isInitialized && _browser.CanExecuteJavascriptInMainFrame)
            {
                _browser.GetMainFrame().ExecuteJavaScriptAsync("state.rotation = (state.rotation + 90) % 360; renderPage(state.currentPage);");
                await Task.Delay(50); // 等待脚本执行
            }
        }

        /// <summary>
        /// 逆时针旋转
        /// </summary>
        public async Task RotateCounterClockwiseAsync()
        {
            if (_browser != null && _isInitialized && _browser.CanExecuteJavascriptInMainFrame)
            {
                _browser.GetMainFrame().ExecuteJavaScriptAsync("state.rotation = (state.rotation - 90 + 360) % 360; renderPage(state.currentPage);");
                await Task.Delay(50); // 等待脚本执行
            }
        }

        /// <summary>
        /// 设置深色模式（异步）
        /// </summary>
        /// <param name="isDark">是否为深色模式</param>
        private async Task SetDarkModeAsync(bool isDark)
        {
            try
            {
                if (_browser?.CanExecuteJavascriptInMainFrame == true)
                {
                    var script = $"if(typeof setDarkMode === 'function') {{ setDarkMode({isDark.ToString().ToLower()}); }}";
                    _browser.GetMainFrame().ExecuteJavaScriptAsync(script);
                    await Task.Delay(50); // 等待脚本执行
                    LogHelper.Debug($"[CefPdfPreview] PDF主题已切换: {(isDark ? "深色" : "浅色")}");
                }
                else
                {
                    LogHelper.Debug("[CefPdfPreview] Frame未就绪，主题将在就绪后应用");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[CefPdfPreview] PDF主题切换失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 应用主题（供外部调用）
        /// </summary>
        /// <param name="isDark">是否为深色模式</param>
        public void ApplyTheme(bool isDark)
        {
            // 更新待应用状态
            _pendingIsDark = isDark;
            
            // 异步调用，不等待结果
            _ = SetDarkModeAsync(isDark);
        }

        /// <summary>
        /// 切换PDF框线显示
        /// </summary>
        /// <param name="show">是否显示</param>
        public async Task ToggleBoxDisplayAsync(bool show)
        {
            try
            {
                if (_browser?.CanExecuteJavascriptInMainFrame == true)
                {
                    // 调用JS函数(如果存在)
                    var script = $"if(typeof toggleBoxDisplay === 'function') {{ if(showBoxes !== {show.ToString().ToLower()}) toggleBoxDisplay(); }}";
                    _browser.GetMainFrame().ExecuteJavaScriptAsync(script);
                    await Task.Delay(50); // 等待脚本执行
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[CefPdfPreview] 切换框线显示失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行JavaScript脚本
        /// </summary>
        /// <param name="script">要执行的JavaScript代码</param>
        public async Task ExecuteScriptAsync(string script)
        {
            try
            {
                if (_browser?.CanExecuteJavascriptInMainFrame == true)
                {
                    _browser.GetMainFrame().ExecuteJavaScriptAsync(script);
                    await Task.Delay(50); // 等待脚本执行
                }
                else
                {
                    LogHelper.Warn("[CefPdfPreview] Frame未就绪，无法执行脚本");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[CefPdfPreview] 执行脚本失败: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Event Handlers

        private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            if (e.Frame.IsMain)
            {
                LogHelper.Debug("[CefPdfPreview] 页面加载完成");

                // 如果有待加载的PDF，现在加载
                if (!string.IsNullOrEmpty(_pendingPdfPath))
                {
                    var path = _pendingPdfPath;
                    _pendingPdfPath = null;
                    this.BeginInvoke(new Action(async () =>
                    {
                        await LoadPdfAsync(path);
                    }));
                }

                // 如果有待应用的主题，现在应用
                if (_pendingIsDark.HasValue)
                {
                    var isDark = _pendingIsDark.Value;
                    // 不清除 _pendingIsDark，因为 SetDarkModeAsync 内部会检查 Frame 是否就绪
                    // 如果这里清除，SetDarkModeAsync 失败后就没机会重试了
                    // 但考虑到 FrameLoadEnd 就是就绪信号，我们可以清除或者让它保持为当前状态
                    // 这里我们保留它作为"当前期望的主题状态"
                    
                    this.BeginInvoke(new Action(async () =>
                    {
                        await SetDarkModeAsync(isDark);
                    }));
                }
            }
        }

        #endregion

        #region JS Callbacks

        /// <summary>
        /// JavaScript回调处理（从JS接收消息）
        /// </summary>
        internal void OnPdfLoaded(int totalPages, int currentPage)
        {
            _totalPages = totalPages;
            _currentPage = currentPage;
            LogHelper.Info($"[CefPdfPreview] PDF已加载，共{totalPages}页");

            this.BeginInvoke(new Action(() =>
            {
                PdfLoaded?.Invoke(this, new PdfLoadedEventArgs(totalPages, currentPage));
            }));
        }

        internal void OnPageChanged(int currentPage, int totalPages)
        {
            _currentPage = currentPage;
            _totalPages = totalPages;

            this.BeginInvoke(new Action(() =>
            {
                PageChanged?.Invoke(this, new PageChangedEventArgs(currentPage, totalPages));
            }));
        }

        internal void OnLoadError(string error)
        {
            LogHelper.Error($"[CefPdfPreview] 加载错误: {error}");

            this.BeginInvoke(new Action(() =>
            {
                LoadError?.Invoke(this, new PdfLoadErrorEventArgs(error));
            }));
        }

        internal void OnDebugMessage(string message)
        {
            LogHelper.Debug($"[PDF.js] {message}");
        }

        #endregion

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _browser?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// JavaScript桥接对象
        /// </summary>
        public class JsBridge
        {
            private readonly CefPdfPreviewControl _control;

            public JsBridge(CefPdfPreviewControl control)
            {
                _control = control;
            }

            public void PdfLoaded(int totalPages, int currentPage)
            {
                _control.OnPdfLoaded(totalPages, currentPage);
            }

            public void PageChanged(int currentPage, int totalPages)
            {
                _control.OnPageChanged(currentPage, totalPages);
            }

            public void LoadError(string error)
            {
                _control.OnLoadError(error);
            }

            public void DebugMessage(string message)
            {
                _control.OnDebugMessage(message);
            }
        }

        #endregion
    }

    #region Event Args

    public class PdfLoadedEventArgs : EventArgs
    {
        public int TotalPages { get; }
        public int CurrentPage { get; }

        public PdfLoadedEventArgs(int totalPages, int currentPage)
        {
            TotalPages = totalPages;
            CurrentPage = currentPage;
        }
    }

    public class PageChangedEventArgs : EventArgs
    {
        public int CurrentPage { get; }
        public int TotalPages { get; }

        public PageChangedEventArgs(int currentPage, int totalPages)
        {
            CurrentPage = currentPage;
            TotalPages = totalPages;
        }
    }

    public class PdfLoadErrorEventArgs : EventArgs
    {
        public string Error { get; }

        public PdfLoadErrorEventArgs(string error)
        {
            Error = error;
        }
    }

    #endregion
}
