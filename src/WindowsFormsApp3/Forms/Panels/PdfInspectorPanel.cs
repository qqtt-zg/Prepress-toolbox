using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp3.Controls;
using WindowsFormsApp3.Forms.Controls;
using WindowsFormsApp3.Utils;
using WinFormsPanel = System.Windows.Forms.Panel;
using IOPath = System.IO.Path;

namespace WindowsFormsApp3.Forms.Panels
{
    /// <summary>
    /// PDF检查器面板
    /// 集成PDF预览和检查器功能，类似Enfocus PitStop Pro
    /// </summary>
    public partial class PdfInspectorPanel : BasePanelControl
    {
        public override string PanelKey => "pdf_inspector";
        public override string DisplayName => "PDF检查器";
        public override string IconName => "FileSearchOutlined";

        private string _currentFilePath;
        private PdfPreviewControl _pdfPreview;
        private PdfInspectorControl _inspector;
        private SplitContainer _mainSplitter;

        // 工具栏控件
        private WinFormsPanel _toolbarPanel;
        private AntdUI.Button _openButton;
        private AntdUI.Button _prevPageButton;
        private AntdUI.Button _nextPageButton;
        private AntdUI.Label _pageLabel;

        public PdfInspectorPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 创建工具栏
            CreateToolbar();

            // 创建分割容器
            _mainSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 600,
                FixedPanel = FixedPanel.Panel2,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 左侧：PDF预览
            CreatePreviewPanel();

            // 右侧：检查器
            CreateInspectorPanel();

            this.Controls.Add(_mainSplitter);
            this.Controls.Add(_toolbarPanel);

            this.ResumeLayout(false);
        }

        /// <summary>
        /// 创建工具栏
        /// </summary>
        private void CreateToolbar()
        {
            _toolbarPanel = new WinFormsPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(10)
            };

            // 打开按钮
            _openButton = new AntdUI.Button
            {
                Text = "打开PDF",
                Location = new Point(10, 10),
                Width = 100,
                Type = AntdUI.TTypeMini.Primary
            };
            _openButton.Click += OpenButton_Click;

            // 上一页按钮
            _prevPageButton = new AntdUI.Button
            {
                Text = "◀",
                Location = new Point(120, 10),
                Width = 40,
                Enabled = false
            };
            _prevPageButton.Click += PrevPageButton_Click;

            // 页码标签
            _pageLabel = new AntdUI.Label
            {
                Text = "0 / 0",
                Location = new Point(170, 15),
                Width = 80,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Consolas", 9F)
            };

            // 下一页按钮
            _nextPageButton = new AntdUI.Button
            {
                Text = "▶",
                Location = new Point(260, 10),
                Width = 40,
                Enabled = false
            };
            _nextPageButton.Click += NextPageButton_Click;

            _toolbarPanel.Controls.Add(_openButton);
            _toolbarPanel.Controls.Add(_prevPageButton);
            _toolbarPanel.Controls.Add(_pageLabel);
            _toolbarPanel.Controls.Add(_nextPageButton);
        }

        /// <summary>
        /// 创建预览面板
        /// </summary>
        private void CreatePreviewPanel()
        {
            var previewContainer = new WinFormsPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(10)
            };

            _pdfPreview = new PdfPreviewControl
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _pdfPreview.PageChanged += PdfPreview_PageChanged;

            previewContainer.Controls.Add(_pdfPreview);
            _mainSplitter.Panel1.Controls.Add(previewContainer);
        }

        /// <summary>
        /// 创建检查器面板
        /// </summary>
        private void CreateInspectorPanel()
        {
            _inspector = new PdfInspectorControl
            {
                Dock = DockStyle.Fill
            };
            _inspector.PageSelected += Inspector_PageSelected;

            _mainSplitter.Panel2.Controls.Add(_inspector);
        }

        /// <summary>
        /// 打开按钮点击事件
        /// </summary>
        private void OpenButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "PDF文件|*.pdf";
                dialog.Title = "选择PDF文件";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LoadPdf(dialog.FileName);
                }
            }
        }

        /// <summary>
        /// 加载PDF文件
        /// </summary>
        private async void LoadPdf(string filePath)
        {
            try
            {
                _currentFilePath = filePath;

                // 异步加载预览
                await _pdfPreview.LoadPdfAsync(filePath);

                // 加载检查器
                _inspector.LoadPdf(filePath, 1);

                // 更新UI状态
                UpdatePageNavigation();

                LogHelper.Info($"PDF检查器加载成功: {filePath}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载PDF失败: {ex.Message}");
                AntdUI.Notification.error(this.FindForm(), "加载失败", ex.Message);
            }
        }

        /// <summary>
        /// 上一页按钮点击事件
        /// </summary>
        private void PrevPageButton_Click(object sender, EventArgs e)
        {
            if (_pdfPreview.CurrentPageIndex > 0)
            {
                _pdfPreview.CurrentPageIndex--;
                UpdateInspectorPage();
                UpdatePageNavigation();
            }
        }

        /// <summary>
        /// 下一页按钮点击事件
        /// </summary>
        private void NextPageButton_Click(object sender, EventArgs e)
        {
            if (_pdfPreview.CurrentPageIndex < _pdfPreview.PageCount - 1)
            {
                _pdfPreview.CurrentPageIndex++;
                UpdateInspectorPage();
                UpdatePageNavigation();
            }
        }

        /// <summary>
        /// PDF预览页面变化事件
        /// </summary>
        private void PdfPreview_PageChanged(object sender, EventArgs e)
        {
            UpdateInspectorPage();
            UpdatePageNavigation();
        }

        /// <summary>
        /// 检查器页面选择事件
        /// </summary>
        private void Inspector_PageSelected(object sender, int pageNumber)
        {
            // 切换预览到指定页面（转换为0-based索引）
            _pdfPreview.CurrentPageIndex = pageNumber - 1;
        }

        /// <summary>
        /// 更新检查器页面
        /// </summary>
        private void UpdateInspectorPage()
        {
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                int currentPage = _pdfPreview.CurrentPageIndex + 1; // 转换为1-based
                _inspector.SwitchToPage(currentPage);
            }
        }

        /// <summary>
        /// 更新页面导航状态
        /// </summary>
        private void UpdatePageNavigation()
        {
            int currentPage = _pdfPreview.CurrentPageIndex + 1;
            int totalPages = _pdfPreview.PageCount;

            _pageLabel.Text = $"{currentPage} / {totalPages}";
            _prevPageButton.Enabled = currentPage > 1;
            _nextPageButton.Enabled = currentPage < totalPages;
        }

        /// <summary>
        /// 面板激活时
        /// </summary>
        public override void OnActivated()
        {
            base.OnActivated();
            LogHelper.Debug("PDF检查器面板已激活");
        }

        /// <summary>
        /// 面板停用时
        /// </summary>
        public override void OnDeactivated()
        {
            base.OnDeactivated();
            LogHelper.Debug("PDF检查器面板已停用");
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pdfPreview?.Dispose();
                _mainSplitter?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
