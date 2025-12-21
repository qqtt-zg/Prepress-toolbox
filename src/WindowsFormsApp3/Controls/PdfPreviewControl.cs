using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Controls
{
    /// <summary>
    /// PDF预览控件包装器
    /// 内部使用PdfiumPdfPreviewControl实现PDF预览功能
    /// 保持向后兼容的公共API
    /// </summary>
    public class PdfPreviewControl : Panel
    {
        #region 私有字段

        private PdfiumPdfPreviewControl _pdfiumControl;
        private bool _isLoading = false;
        private readonly bool _isDesignMode;

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前显示的页面索引（从0开始）
        /// </summary>
        public int CurrentPageIndex
        {
            get => _pdfiumControl?.CurrentPage - 1 ?? 0;
            set
            {
                if (_pdfiumControl != null && value >= 0)
                {
                    _pdfiumControl.CurrentPage = value + 1;
                }
            }
        }

        /// <summary>
        /// PDF 文档总页数
        /// </summary>
        public int PageCount => _pdfiumControl?.PageCount ?? 0;

        /// <summary>
        /// 当前缩放百分比
        /// </summary>
        public float CurrentZoom
        {
            get => (float)(_pdfiumControl?.Zoom * 100 ?? 100f);
            set
            {
                if (_pdfiumControl != null)
                {
                    _pdfiumControl.Zoom = value / 100.0;
                }
            }
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading => _isLoading;

        #endregion

        #region 事件

        /// <summary>
        /// 页面加载完成事件
        /// </summary>
        public event EventHandler<PageLoadedEventArgs> PageLoaded;

        /// <summary>
        /// 加载出错事件
        /// </summary>
        public event EventHandler<ErrorEventArgs> LoadError;

        #endregion

        #region 构造函数

        public PdfPreviewControl()
        {
            _isDesignMode = DesignMode;
            InitializeUI();
        }

        #endregion

        #region UI 初始化

        private void InitializeUI()
        {
            this.BackColor = Color.FromArgb(248, 248, 248);
            this.Dock = DockStyle.Fill;
            this.BorderStyle = BorderStyle.None;
            this.Padding = new Padding(0);
            this.DoubleBuffered = true;

            // 在设计器模式下，显示占位符
            if (_isDesignMode)
            {
                CreateDesignTimePlaceholder();
                return;
            }

            try
            {
                // 创建Pdfium PDF预览控件
                _pdfiumControl = new PdfiumPdfPreviewControl
                {
                    Dock = DockStyle.Fill
                };

                // 绑定事件
                _pdfiumControl.PageChanged += PdfiumControl_PageChanged;
                _pdfiumControl.PageLoaded += PdfiumControl_PageLoaded;
                _pdfiumControl.LoadError += PdfiumControl_LoadError;

                this.Controls.Add(_pdfiumControl);
                LogHelper.Debug("[PdfPreviewControl] PdfiumViewer PDF预览控件初始化完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PdfPreviewControl] 初始化失败: {ex.Message}");
                // 创建一个错误提示标签
                var errorLabel = new Label
                {
                    Text = $"PDF预览组件初始化失败\n{ex.Message}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.Red,
                    BackColor = Color.LightGray
                };
                this.Controls.Add(errorLabel);
            }
        }

        /// <summary>
        /// 创建设计时占位符
        /// </summary>
        private void CreateDesignTimePlaceholder()
        {
            var placeholder = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.FixedSingle
            };

            var label = new Label
            {
                Text = "PDF预览控件\n(PdfiumViewer - 设计时模式)",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(100, 100, 100),
                Font = new System.Drawing.Font("Microsoft YaHei", 9f, FontStyle.Regular),
                AutoSize = false
            };

            placeholder.Controls.Add(label);
            this.Controls.Add(placeholder);
        }

        #endregion

        #region 事件处理

        private void PdfiumControl_PageChanged(object sender, EventArgs e)
        {
            // 页面变化时可以触发相关事件
        }

        private void PdfiumControl_PageLoaded(object sender, EventArgs e)
        {
            _isLoading = false;
            OnPageLoaded(CurrentPageIndex, PageCount);
        }

        private void PdfiumControl_LoadError(object sender, string error)
        {
            _isLoading = false;
            OnLoadError(new Exception(error));
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 异步加载 PDF 文件
        /// </summary>
        public async Task<bool> LoadPdfAsync(string filePath)
        {
            // 设计时模式下不加载PDF
            if (_isDesignMode)
            {
                return true;
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                LogHelper.Error($"[PdfPreviewControl] PDF 文件不存在: {filePath}");
                return false;
            }

            try
            {
                _isLoading = true;
                LogHelper.Debug($"[PdfPreviewControl] 开始加载PDF: {filePath}");

                if (_pdfiumControl != null)
                {
                    bool success = await _pdfiumControl.LoadPdfAsync(filePath);
                    if (success)
                    {
                        LogHelper.Debug("[PdfPreviewControl] PDF加载成功");
                        return true;
                    }
                    else
                    {
                        LogHelper.Error("[PdfPreviewControl] PDF加载失败");
                        OnLoadError(new Exception("PDF加载失败"));
                        return false;
                    }
                }
                else
                {
                    LogHelper.Error("[PdfPreviewControl] Pdfium控件未初始化");
                    OnLoadError(new Exception("PDF预览组件未初始化"));
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PdfPreviewControl] 加载PDF异常: {ex.Message}");
                OnLoadError(ex);
                return false;
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// 跳转到指定页面
        /// </summary>
        public void GoToPage(int pageIndex)
        {
            if (_pdfiumControl != null && pageIndex >= 0)
            {
                _pdfiumControl.CurrentPage = pageIndex + 1;
            }
        }

        /// <summary>
        /// 下一页
        /// </summary>
        public void NextPage()
        {
            _pdfiumControl?.NextPage();
        }

        /// <summary>
        /// 上一页
        /// </summary>
        public void PreviousPage()
        {
            _pdfiumControl?.PreviousPage();
        }

        /// <summary>
        /// 第一页
        /// </summary>
        public void FirstPage()
        {
            if (_pdfiumControl != null)
            {
                _pdfiumControl.CurrentPage = 1;
            }
        }

        /// <summary>
        /// 最后一页
        /// </summary>
        public void LastPage()
        {
            if (_pdfiumControl != null)
            {
                _pdfiumControl.CurrentPage = PageCount;
            }
        }

        /// <summary>
        /// 放大
        /// </summary>
        public void ZoomIn()
        {
            _pdfiumControl?.ZoomIn();
        }

        /// <summary>
        /// 缩小
        /// </summary>
        public void ZoomOut()
        {
            _pdfiumControl?.ZoomOut();
        }

        /// <summary>
        /// 设置缩放百分比
        /// </summary>
        public void SetZoom(float zoomPercent)
        {
            if (_pdfiumControl != null)
            {
                _pdfiumControl.Zoom = zoomPercent / 100.0;
            }
        }

        /// <summary>
        /// 适应宽度
        /// </summary>
        public void FitWidth()
        {
            _pdfiumControl?.FitWidth();
        }

        /// <summary>
        /// 适应页面
        /// </summary>
        public void FitPage()
        {
            _pdfiumControl?.FitPage();
        }
        
        /// <summary>
        /// 公共刷新方法（供外部调用以刷新PDF显示）
        /// </summary>
        public void ApplyBestFitZoomPublic()
        {
            _pdfiumControl?.ApplyBestFitZoomPublic();
        }

        /// <summary>
        /// 显示/隐藏工具栏
        /// </summary>
        public void ToggleToolbar()
        {
            _pdfiumControl?.ToggleToolbar();
        }

        /// <summary>
        /// 显示/隐藏边栏
        /// </summary>
        public void ToggleSidebar()
        {
            _pdfiumControl?.ToggleBookmarks();
        }

        /// <summary>
        /// 关闭当前PDF
        /// </summary>
        public void ClosePdf()
        {
            _pdfiumControl?.ClosePdf();
        }

        /// <summary>
        /// 应用最佳适应缩放
        /// </summary>
        public void ApplyBestFit()
        {
            _pdfiumControl?.FitWidth();
        }

        /// <summary>
        /// 刷新显示
        /// </summary>
        public new void Refresh()
        {
            _pdfiumControl?.Refresh();
            base.Refresh();
        }

        #endregion

        #region 受保护方法

        protected virtual void OnPageLoaded(int pageIndex, int pageCount)
        {
            PageLoaded?.Invoke(this, new PageLoadedEventArgs(pageIndex, pageCount));
        }

        protected virtual void OnLoadError(Exception ex)
        {
            LoadError?.Invoke(this, new ErrorEventArgs(ex));
        }

        #endregion

        #region 清理资源

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pdfiumControl?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }

    #region 事件参数类

    /// <summary>
    /// 页面加载事件参数
    /// </summary>
    public class PageLoadedEventArgs : EventArgs
    {
        public int PageIndex { get; }
        public int PageCount { get; }

        public PageLoadedEventArgs(int pageIndex, int pageCount)
        {
            PageIndex = pageIndex;
            PageCount = pageCount;
        }
    }

    /// <summary>
    /// 错误事件参数
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }

        public ErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }

    #endregion
}