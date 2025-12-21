using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PdfiumViewer;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Controls
{
    /// <summary>
    /// 基于 PdfiumViewer 的 PDF 预览控件
    /// 提供高性能的 PDF 渲染和完整的用户交互功能
    /// </summary>
    public class PdfiumPdfPreviewControl : UserControl
    {
        #region 私有字段

        private PdfViewer _pdfViewer;
        private PdfDocument _pdfDocument;
        private string _currentFilePath;
        private bool _isInitialized = false;

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前页码（从1开始）
        /// </summary>
        public int CurrentPage
        {
            get => _pdfViewer?.Renderer?.Page + 1 ?? 1;
            set
            {
                if (_pdfViewer?.Renderer != null && value >= 1 && value <= PageCount)
                {
                    _pdfViewer.Renderer.Page = value - 1;
                }
            }
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int PageCount => _pdfDocument?.PageCount ?? 0;

        /// <summary>
        /// 当前缩放级别
        /// </summary>
        public double Zoom
        {
            get => _pdfViewer?.Renderer?.Zoom ?? 1.0;
            set
            {
                if (_pdfViewer?.Renderer != null)
                {
                    _pdfViewer.Renderer.Zoom = value;
                }
            }
        }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region 事件

        /// <summary>
        /// 页面加载完成事件
        /// </summary>
        public event EventHandler PageLoaded;

        /// <summary>
        /// 页面改变事件
        /// </summary>
        public event EventHandler PageChanged;

        /// <summary>
        /// 加载错误事件
        /// </summary>
        public event EventHandler<string> LoadError;

        #endregion

        #region 构造函数

        public PdfiumPdfPreviewControl()
        {
            InitializeComponent();
        }

        #endregion

        #region 初始化

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 创建 PdfViewer 控件
            _pdfViewer = new PdfViewer
            {
                Dock = DockStyle.Fill,
                ShowToolbar = false,  // 禁用默认工具栏（功能有限）
                ShowBookmarks = false, // 默认不显示书签
                ZoomMode = PdfViewerZoomMode.FitBest // 自动选择最佳适应（宽度或高度）
            };

            // 绑定事件
            _pdfViewer.Renderer.DisplayRectangleChanged += Renderer_DisplayRectangleChanged;
            
            // 鼠标进入时自动获得焦点，以便键盘快捷键可以工作
            _pdfViewer.MouseEnter += (s, e) => _pdfViewer.Focus();
            _pdfViewer.Renderer.MouseEnter += (s, e) => _pdfViewer.Focus();
            
            // 拦截键盘事件实现精确翻页（而非滚动）
            _pdfViewer.Renderer.PreviewKeyDown += Renderer_PreviewKeyDown;
            _pdfViewer.Renderer.KeyDown += Renderer_KeyDown;
            
            // 控件大小变化时重新应用最佳适应模式（延迟调用确保尺寸更新完成）
            this.SizeChanged += (s, e) =>
            {
                if (_pdfDocument != null && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() => ApplyBestFitZoom()));
                }
            };
            
            // 控件变为可见时刷新（与折叠展开使用相同机制）
            this.VisibleChanged += (s, e) =>
            {
                if (this.Visible && _pdfDocument != null && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        ApplyBestFitZoom();
                        _pdfViewer.Refresh();
                    }));
                }
            };
            
            // 创建右键菜单
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("最佳适应", null, (s, e) => 
            {
                // 先重置Zoom值和ZoomMode，触发重新计算
                _pdfViewer.Renderer.Zoom = 1.0;
                _pdfViewer.ZoomMode = PdfViewerZoomMode.FitBest;
                _pdfViewer.Renderer.Padding = new Padding(0);
                ApplyBestFitZoomPublic();
            });
            contextMenu.Items.Add("适应宽度", null, (s, e) => 
            {
                // 先重置Zoom值，强制触发ZoomMode变更
                _pdfViewer.Renderer.Zoom = 1.0;
                _pdfViewer.ZoomMode = PdfViewerZoomMode.FitBest; // 先设置为其他模式
                // 宽度适应需要底部Padding让最后一页可以滚动到顶部
                int bottomPadding = (int)_pdfViewer.Renderer.Height - 50;
                if (bottomPadding < 100) bottomPadding = 100;
                _pdfViewer.Renderer.Padding = new Padding(0, 0, 0, bottomPadding);
                _pdfViewer.ZoomMode = PdfViewerZoomMode.FitWidth;
                _pdfViewer.Refresh();
            });
            contextMenu.Items.Add("适应高度", null, (s, e) => 
            {
                // 先重置Zoom值，强制触发ZoomMode变更
                _pdfViewer.Renderer.Zoom = 1.0;
                _pdfViewer.ZoomMode = PdfViewerZoomMode.FitBest; // 先设置为其他模式
                // 高度适应不需要额外Padding
                _pdfViewer.Renderer.Padding = new Padding(0);
                _pdfViewer.ZoomMode = PdfViewerZoomMode.FitHeight;
                _pdfViewer.Refresh();
            });
            _pdfViewer.Renderer.ContextMenuStrip = contextMenu;

            this.Controls.Add(_pdfViewer);
            this.BackColor = Color.White;

            _isInitialized = true;

            this.ResumeLayout(false);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 加载 PDF 文件
        /// </summary>
        /// <param name="filePath">PDF 文件路径</param>
        /// <returns>是否加载成功</returns>
        public bool LoadPdf(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                LoadError?.Invoke(this, "文件不存在");
                return false;
            }

            try
            {
                // 关闭之前打开的文档
                ClosePdf();

                _currentFilePath = filePath;

                // 打开 PDF 文档
                _pdfDocument = PdfDocument.Load(filePath);
                _pdfViewer.Document = _pdfDocument;
                
                // 手动应用最佳适应（根据页面宽高比选择）
                ApplyBestFitZoom();
                
                // 使用两次延迟刷新确保可靠显示（第一次100ms，第二次300ms）
                var timer1 = new System.Windows.Forms.Timer { Interval = 100 };
                timer1.Tick += (s, args) =>
                {
                    timer1.Stop();
                    timer1.Dispose();
                    ApplyBestFitZoom();
                    _pdfViewer.Refresh();
                };
                timer1.Start();
                
                var timer2 = new System.Windows.Forms.Timer { Interval = 300 };
                timer2.Tick += (s, args) =>
                {
                    timer2.Stop();
                    timer2.Dispose();
                    ApplyBestFitZoom();
                    _pdfViewer.Refresh();
                };
                timer2.Start();

                // 触发页面加载事件
                PageLoaded?.Invoke(this, EventArgs.Empty);

                return true;
            }
            catch (Exception ex)
            {
                LoadError?.Invoke(this, ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// 应用最佳适应缩放（根据页面和控件比例自动选择宽度或高度适应）
        /// </summary>
        private void ApplyBestFitZoom()
        {
            try
            {
                if (_pdfDocument == null || _pdfDocument.PageCount == 0)
                    return;
                    
                // 获取第一页尺寸
                var pageSize = _pdfDocument.PageSizes[0];
                float pageWidth = pageSize.Width;
                float pageHeight = pageSize.Height;
                
                // 获取控件可用尺寸（考虑滚动条占用的空间）
                float scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
                float viewerWidth = _pdfViewer.Renderer.Width - scrollBarWidth; // 减去垂直滚动条宽度
                float viewerHeight = _pdfViewer.Renderer.Height;
                
                // 调试输出（写入应用日志）
                LogHelper.Debug($"[ApplyBestFitZoom] 页面尺寸: {pageWidth}x{pageHeight}, 控件尺寸: {viewerWidth}x{viewerHeight}, 页数: {_pdfDocument.PageCount}");
                
                if (viewerWidth <= 0 || viewerHeight <= 0)
                    return;
                
                // 计算页面宽高比和控件宽高比
                float pageAspect = pageWidth / pageHeight;
                float viewerAspect = viewerWidth / viewerHeight;
                
                // 如果页面更宽（相对于控件），使用适应宽度；否则使用适应高度
                if (pageAspect > viewerAspect)
                {
                    _pdfViewer.ZoomMode = PdfViewerZoomMode.FitWidth;
                    // 宽度适应模式下，页面高度会超出视口，需要添加底部Padding
                    // 这样最后一页才能滚动到视口顶部
                    int bottomPadding = (int)viewerHeight - 50; // 留出足够空间让末页可以滚动到顶部
                    if (bottomPadding < 100) bottomPadding = 100;
                    _pdfViewer.Renderer.Padding = new Padding(0, 0, 0, bottomPadding);
                }
                else
                {
                    _pdfViewer.ZoomMode = PdfViewerZoomMode.FitHeight;
                    // 高度适应模式下，页面完全显示，不需要额外Padding
                    _pdfViewer.Renderer.Padding = new Padding(0);
                }
            }
            catch
            {
                // 出错时使用默认的FitBest
                _pdfViewer.ZoomMode = PdfViewerZoomMode.FitBest;
            }
        }

        /// <summary>
        /// 异步加载 PDF 文件
        /// </summary>
        public async System.Threading.Tasks.Task<bool> LoadPdfAsync(string filePath)
        {
            return await System.Threading.Tasks.Task.Run(() => LoadPdf(filePath));
        }

        /// <summary>
        /// 关闭当前 PDF 文档
        /// </summary>
        public void ClosePdf()
        {
            if (_pdfDocument != null)
            {
                _pdfViewer.Document = null;
                _pdfDocument.Dispose();
                _pdfDocument = null;
                _currentFilePath = null;
            }
        }

        /// <summary>
        /// 适应页面宽度
        /// </summary>
        public void FitWidth()
        {
            if (_pdfViewer != null)
            {
                _pdfViewer.ZoomMode = PdfViewerZoomMode.FitWidth;
            }
        }

        /// <summary>
        /// 适应整页
        /// </summary>
        public void FitPage()
        {
            if (_pdfViewer != null)
            {
                _pdfViewer.ZoomMode = PdfViewerZoomMode.FitHeight;
            }
        }
        
        /// <summary>
        /// 公共刷新方法（供外部调用以刷新PDF显示）
        /// </summary>
        public void ApplyBestFitZoomPublic()
        {
            if (_pdfDocument != null && _pdfViewer != null)
            {
                ApplyBestFitZoom();
                _pdfViewer.Refresh();
            }
        }

        /// <summary>
        /// 放大
        /// </summary>
        public void ZoomIn()
        {
            if (_pdfViewer?.Renderer != null)
            {
                _pdfViewer.Renderer.Zoom *= 1.2;
            }
        }

        /// <summary>
        /// 缩小
        /// </summary>
        public void ZoomOut()
        {
            if (_pdfViewer?.Renderer != null)
            {
                _pdfViewer.Renderer.Zoom /= 1.2;
            }
        }

        /// <summary>
        /// 下一页
        /// </summary>
        public void NextPage()
        {
            if (_pdfViewer?.Renderer != null && CurrentPage < PageCount)
            {
                CurrentPage++;
            }
        }

        /// <summary>
        /// 上一页
        /// </summary>
        public void PreviousPage()
        {
            if (_pdfViewer?.Renderer != null && CurrentPage > 1)
            {
                CurrentPage--;
            }
        }

        /// <summary>
        /// 显示/隐藏工具栏
        /// </summary>
        public void ToggleToolbar()
        {
            if (_pdfViewer != null)
            {
                _pdfViewer.ShowToolbar = !_pdfViewer.ShowToolbar;
            }
        }

        /// <summary>
        /// 显示/隐藏书签
        /// </summary>
        public void ToggleBookmarks()
        {
            if (_pdfViewer != null)
            {
                _pdfViewer.ShowBookmarks = !_pdfViewer.ShowBookmarks;
            }
        }

        /// <summary>
        /// 刷新显示
        /// </summary>
        public new void Refresh()
        {
            _pdfViewer?.Refresh();
            base.Refresh();
        }

        #endregion

        #region 事件处理

        private void Renderer_DisplayRectangleChanged(object sender, EventArgs e)
        {
            PageChanged?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// 预处理键盘事件，标记需要特殊处理的按键
        /// </summary>
        private void Renderer_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            // 标记这些按键不使用默认行为，由我们自己处理
            if (e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown ||
                e.KeyCode == Keys.Up || e.KeyCode == Keys.Down ||
                e.KeyCode == Keys.Left || e.KeyCode == Keys.Right ||
                e.KeyCode == Keys.Home || e.KeyCode == Keys.End)
            {
                e.IsInputKey = true;
            }
        }
        
        /// <summary>
        /// 处理键盘事件实现精确翻页
        /// </summary>
        private void Renderer_KeyDown(object sender, KeyEventArgs e)
        {
            bool handled = false;
            int targetPage = -1;
            
            switch (e.KeyCode)
            {
                case Keys.PageDown:
                case Keys.Down:
                case Keys.Right:
                    if (CurrentPage < PageCount)
                    {
                        targetPage = CurrentPage; // 下一页的索引（0-based）
                        handled = true;
                    }
                    break;
                    
                case Keys.PageUp:
                case Keys.Up:
                case Keys.Left:
                    if (CurrentPage > 1)
                    {
                        targetPage = CurrentPage - 2; // 上一页的索引（0-based）
                        handled = true;
                    }
                    break;
                    
                case Keys.Home:
                    targetPage = 0;
                    handled = true;
                    break;
                    
                case Keys.End:
                    targetPage = PageCount - 1;
                    handled = true;
                    break;
            }
            
            if (handled && targetPage >= 0)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                
                // 设置页面
                _pdfViewer.Renderer.Page = targetPage;
                
                // 使用BeginInvoke确保在UI更新后再进行调整
                int page = targetPage;
                this.BeginInvoke(new Action(() =>
                {
                    ApplyBestFitZoom();
                    // 再次延迟滚动确保ZoomMode生效
                    this.BeginInvoke(new Action(() =>
                    {
                        _pdfViewer.Renderer.Page = page;
                    }));
                }));
            }
        }
        
        /// <summary>
        /// 滚动到指定页面的顶部
        /// </summary>
        private void ScrollToPageTop(int pageIndex)
        {
            try
            {
                if (_pdfDocument != null && pageIndex >= 0 && pageIndex < PageCount)
                {
                    // 获取页面边界并滚动到页面顶部
                    var pageBounds = _pdfViewer.Renderer.BoundsFromPdf(new PdfiumViewer.PdfRectangle(pageIndex, 
                        new System.Drawing.RectangleF(0, 0, 1, 1)));
                    _pdfViewer.Renderer.ScrollIntoView(pageBounds);
                }
            }
            catch
            {
                // 忽略滚动错误
            }
        }

        #endregion

        #region 清理资源

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClosePdf();
                _pdfViewer?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
