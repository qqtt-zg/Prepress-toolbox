using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Controls
{
    /// <summary>
    /// 带检查器功能的PDF预览控件
    /// 集成页面框可视化叠加层
    /// </summary>
    public class PdfPreviewWithInspector : UserControl
    {
        private PdfPreviewControl _pdfPreview;
        private PdfBoxOverlay _boxOverlay;
        private PdfInspectorService _inspectorService;
        private PdfInspectorInfo _currentInspectorInfo;
        private string _currentFilePath;
        private bool _overlayEnabled = false;

        public event EventHandler<int> PageChanged;

        public PdfPreviewWithInspector()
        {
            _inspectorService = new PdfInspectorService();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // PDF预览控件（底层）
            _pdfPreview = new PdfPreviewControl
            {
                Dock = DockStyle.Fill
            };
            _pdfPreview.PageChanged += PdfPreview_PageChanged;

            // 页面框叠加层（顶层）
            _boxOverlay = new PdfBoxOverlay
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            this.Controls.Add(_boxOverlay);
            this.Controls.Add(_pdfPreview);

            // 确保叠加层在最上层
            _boxOverlay.BringToFront();

            this.ResumeLayout(false);
        }

        /// <summary>
        /// 异步加载PDF文件
        /// </summary>
        public async Task<bool> LoadPdfAsync(string filePath)
        {
            try
            {
                _currentFilePath = filePath;

                // 加载PDF预览
                bool success = await _pdfPreview.LoadPdfAsync(filePath);
                if (!success)
                    return false;

                // 检查PDF
                await Task.Run(() =>
                {
                    _currentInspectorInfo = _inspectorService.InspectPdf(filePath, 1);
                });

                // 如果叠加层已启用，更新显示
                if (_overlayEnabled)
                {
                    UpdateOverlay();
                }

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载PDF失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 启用/禁用页面框叠加层
        /// </summary>
        public bool OverlayEnabled
        {
            get => _overlayEnabled;
            set
            {
                _overlayEnabled = value;
                _boxOverlay.Visible = value;
                
                if (value && _currentInspectorInfo != null)
                {
                    UpdateOverlay();
                }
            }
        }

        /// <summary>
        /// 获取检查器信息
        /// </summary>
        public PdfInspectorInfo InspectorInfo => _currentInspectorInfo;

        /// <summary>
        /// 当前页码（0-based）
        /// </summary>
        public int CurrentPageIndex
        {
            get => _pdfPreview.CurrentPageIndex;
            set
            {
                _pdfPreview.CurrentPageIndex = value;
                UpdateOverlay();
            }
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int PageCount => _pdfPreview.PageCount;

        /// <summary>
        /// 显示/隐藏MediaBox
        /// </summary>
        public bool ShowMediaBox
        {
            get => _boxOverlay.ShowMediaBox;
            set => _boxOverlay.ShowMediaBox = value;
        }

        /// <summary>
        /// 显示/隐藏CropBox
        /// </summary>
        public bool ShowCropBox
        {
            get => _boxOverlay.ShowCropBox;
            set => _boxOverlay.ShowCropBox = value;
        }

        /// <summary>
        /// 显示/隐藏TrimBox
        /// </summary>
        public bool ShowTrimBox
        {
            get => _boxOverlay.ShowTrimBox;
            set => _boxOverlay.ShowTrimBox = value;
        }

        /// <summary>
        /// 显示/隐藏BleedBox
        /// </summary>
        public bool ShowBleedBox
        {
            get => _boxOverlay.ShowBleedBox;
            set => _boxOverlay.ShowBleedBox = value;
        }

        /// <summary>
        /// 显示/隐藏ArtBox
        /// </summary>
        public bool ShowArtBox
        {
            get => _boxOverlay.ShowArtBox;
            set => _boxOverlay.ShowArtBox = value;
        }

        /// <summary>
        /// 更新叠加层显示
        /// </summary>
        private void UpdateOverlay()
        {
            if (_currentInspectorInfo == null || !_overlayEnabled)
                return;

            int currentPage = _pdfPreview.CurrentPageIndex + 1; // 转换为1-based
            var pageBoxInfo = _currentInspectorInfo.AllPageBoxes.Find(p => p.PageNumber == currentPage);

            if (pageBoxInfo != null)
            {
                _boxOverlay.SetPageBoxInfo(pageBoxInfo);
                
                // TODO: 根据PDF预览的实际缩放和偏移设置变换
                // 这需要从PdfPreviewControl获取当前的缩放比例
                _boxOverlay.SetTransform(1.0f, new PointF(0, 0));
            }
        }

        /// <summary>
        /// PDF预览页面变化事件
        /// </summary>
        private void PdfPreview_PageChanged(object sender, EventArgs e)
        {
            UpdateOverlay();
            PageChanged?.Invoke(this, _pdfPreview.CurrentPageIndex);
        }

        /// <summary>
        /// 刷新检查器信息
        /// </summary>
        public async Task RefreshInspectorAsync()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
                return;

            await Task.Run(() =>
            {
                _currentInspectorInfo = _inspectorService.InspectPdf(_currentFilePath, CurrentPageIndex + 1);
            });

            if (_overlayEnabled)
            {
                UpdateOverlay();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pdfPreview?.Dispose();
                _boxOverlay?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
