using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp3.Helpers;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Panels
{
    /// <summary>
    /// PDF拆分面板
    /// 根据Excel数据按页数范围拆分PDF文件
    /// </summary>
    public partial class PdfSplitPanel : BasePanelControl
    {
        public override string PanelKey => "pdf_split";
        public override string DisplayName => "PDF拆分";
        public override string IconName => "FileSplitOutlined";

        private PdfSplitService _pdfSplitService;
        private PdfSplitExcelHelper _excelHelper;
        private string _currentPdfPath;
        private string _currentExcelPath;
        private List<PageRangeInfo> _currentPageRanges;
        private CancellationTokenSource _cancellationTokenSource;

        public PdfSplitPanel()
        {
            InitializeComponent();
            InitializeDragDrop();
            LoadIcons();
        }

        private void LoadIcons()
        {
            // 加载PDF拆分图标
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Icons", "PdfSplit.ico");
            if (File.Exists(iconPath))
            {
                using (var stream = File.OpenRead(iconPath))
                {
                    var icon = new Icon(stream);
                    _lblPdfIcon.BackgroundImage = icon.ToBitmap();
                    _lblPdfIcon.BackgroundImageLayout = ImageLayout.Zoom;
                }
                _lblPdfIcon.Text = null; // 清除emoji
            }

            // 加载Excel图标（使用内置的绿色方块图标代表Excel）
            _lblExcelIcon.BackgroundImage = CreateExcelIcon();
            _lblExcelIcon.BackgroundImageLayout = ImageLayout.Zoom;
            _lblExcelIcon.Text = null;
        }

        private Bitmap CreateExcelIcon()
        {
            var bmp = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(bmp))
            {
                // 绿色背景的Excel图标风格
                var brush = new SolidBrush(Color.FromArgb(34, 139, 34)); // 森林绿
                g.FillRectangle(brush, 0, 0, 24, 24);
                // X 字母
                using (var pen = new Pen(Color.White, 2))
                {
                    g.DrawLine(pen, 6, 6, 18, 18);
                    g.DrawLine(pen, 18, 6, 6, 18);
                }
            }
            return bmp;
        }

        protected override void InitializePanel()
        {
            base.InitializePanel();

            _pdfSplitService = new PdfSplitService();
            _excelHelper = new PdfSplitExcelHelper();
            _currentPageRanges = new List<PageRangeInfo>();
            _cancellationTokenSource = null;
        }

        private void InitializeDragDrop()
        {
            // 设置拖拽属性
            _txtPdfPath.AllowDrop = true;
            _txtExcelPath.AllowDrop = true;

            // PDF拖拽事件
            _txtPdfPath.DragEnter += TxtPdfPath_DragEnter;
            _txtPdfPath.DragDrop += TxtPdfPath_DragDrop;

            // Excel拖拽事件
            _txtExcelPath.DragEnter += TxtExcelPath_DragEnter;
            _txtExcelPath.DragDrop += TxtExcelPath_DragDrop;
        }

        private void TxtPdfPath_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && files[0].ToLower().EndsWith(".pdf"))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void TxtPdfPath_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && files[0].ToLower().EndsWith(".pdf"))
                {
                    LoadPdfFile(files[0]);
                }
            }
        }

        private void TxtExcelPath_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string ext = Path.GetExtension(files[0]).ToLower();
                    if (ext == ".xlsx" || ext == ".xls" || ext == ".csv")
                    {
                        e.Effect = DragDropEffects.Copy;
                        return;
                    }
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void TxtExcelPath_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string ext = Path.GetExtension(files[0]).ToLower();
                    if (ext == ".xlsx" || ext == ".xls" || ext == ".csv")
                    {
                        LoadExcelFile(files[0]);
                    }
                }
            }
        }

        private void LoadPdfFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            _currentPdfPath = filePath;
            _txtPdfPath.Text = _currentPdfPath;

            // 获取源PDF页数
            var pageCount = _pdfSplitService.GetSourcePageCount(_currentPdfPath);
            _lblSourcePageCount.Text = pageCount.HasValue
                ? $"源PDF页数: {pageCount.Value}"
                : "无法读取PDF页数";

            UpdatePreview();
        }

        private void LoadExcelFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            _currentExcelPath = filePath;
            _txtExcelPath.Text = _currentExcelPath;
            UpdatePreview();
        }

        private void BtnBrowsePdf_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "PDF文件|*.pdf";
                dialog.Title = "选择源PDF文件";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LoadPdfFile(dialog.FileName);
                }
            }
        }

        private void BtnBrowseExcel_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Excel文件|*.xlsx;*.xls|CSV文件|*.csv|All文件|*.*";
                dialog.Title = "选择Excel/CSV文件";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LoadExcelFile(dialog.FileName);
                }
            }
        }

        private void TxtExcelPath_TextChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void TxtExcelPath_DoubleClick(object sender, EventArgs e)
        {
            // 双击Excel路径尝试打开文件
            if (!string.IsNullOrEmpty(_currentExcelPath) && File.Exists(_currentExcelPath))
            {
                try
                {
                    System.Diagnostics.Process.Start(_currentExcelPath);
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"打开Excel文件失败: {ex.Message}");
                }
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _lblStatus.Text = "正在取消...";
                _lblStatus.ForeColor = Color.Orange;
            }
        }

        private void UpdatePreview()
        {
            _dgvPreview.Rows.Clear();
            _currentPageRanges.Clear();

            if (string.IsNullOrEmpty(_currentExcelPath) || !File.Exists(_currentExcelPath))
                return;

            if (string.IsNullOrEmpty(_currentPdfPath) || !File.Exists(_currentPdfPath))
                return;

            try
            {
                // 读取Excel数据（包含文件名和页数）
                var excelRows = _excelHelper.ReadSplitInfoWithOrderFromFile(_currentExcelPath);
                if (excelRows.Count == 0)
                {
                    _lblStatus.Text = "Excel文件中没有有效数据";
                    _lblStatus.ForeColor = Color.Orange;
                    return;
                }

                // 转换为SplitFileInfo列表用于计算页数范围
                var fileInfos = new List<SplitFileInfo>();
                foreach (var row in excelRows)
                {
                    fileInfos.Add(new SplitFileInfo { FileName = row.FileName, PageCount = row.PageCount });
                }

                // 计算页数范围
                _currentPageRanges = _pdfSplitService.CalculatePageRanges(fileInfos);

                // 更新预览表格
                foreach (var range in _currentPageRanges)
                {
                    _dgvPreview.Rows.Add(
                        range.Index.ToString(),
                        range.OutputFileName,
                        range.PageCount.ToString(),
                        range.StartPage.ToString(),
                        range.EndPage.ToString()
                    );
                }

                // 检查是否超出范围
                int? totalPages = _pdfSplitService.GetSourcePageCount(_currentPdfPath);
                if (totalPages.HasValue)
                {
                    int lastEndPage = _currentPageRanges.Count > 0
                        ? _currentPageRanges[_currentPageRanges.Count - 1].EndPage
                        : 0;

                    if (lastEndPage > totalPages.Value)
                    {
                        _lblStatus.Text = $"警告：总页数({lastEndPage})超出源PDF页数({totalPages.Value})";
                        _lblStatus.ForeColor = Color.Red;
                    }
                    else
                    {
                        _lblStatus.Text = $"就绪 - 将拆分为 {fileInfos.Count} 个文件";
                        _lblStatus.ForeColor = Color.Gray;
                    }
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"读取Excel失败: {ex.Message}";
                _lblStatus.ForeColor = Color.Red;
                LogHelper.Error($"PDF拆分预览更新失败: {ex.Message}");
            }
        }

        private async void BtnExecuteSplit_Click(object sender, EventArgs e)
        {
            if (_currentPageRanges.Count == 0)
            {
                ShowWarning("请先选择PDF文件和Excel文件");
                return;
            }

            // 选择输出目录
            string outputDir;
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择输出目录";
                dialog.SelectedPath = Path.GetDirectoryName(_currentPdfPath);

                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                outputDir = dialog.SelectedPath;
            }

            // 确认操作
            if (!ShowConfirm($"确定要拆分为 {_currentPageRanges.Count} 个文件吗？\n\n输出目录: {outputDir}"))
                return;

            // 禁用按钮，启用取消按钮
            _btnExecuteSplit.Enabled = false;
            _btnCancel.Enabled = true;
            _progressBar.Value = 0;
            _lblStatus.Text = "正在拆分...";
            _lblStatus.ForeColor = Color.Blue;

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Run(() =>
                {
                    _pdfSplitService.SplitPdfByPageRanges(
                        _currentPdfPath,
                        outputDir,
                        _currentPageRanges,
                        _cancellationTokenSource.Token,
                        (current, total, message) =>
                        {
                            this.Invoke(new Action(() =>
                            {
                                _progressBar.Value = (int)((double)current / total * 100);
                                _lblStatus.Text = message;
                            }));
                        });
                }, _cancellationTokenSource.Token);

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    _lblStatus.Text = "操作已取消";
                    _lblStatus.ForeColor = Color.Orange;
                }
                else
                {
                    _lblStatus.Text = "拆分完成！";
                    _lblStatus.ForeColor = Color.Green;
                    ShowSuccess($"PDF拆分完成！\n\n已生成 {_currentPageRanges.Count} 个文件到:\n{outputDir}");
                }
            }
            catch (OperationCanceledException)
            {
                _lblStatus.Text = "操作已取消";
                _lblStatus.ForeColor = Color.Orange;
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"拆分失败: {ex.Message}";
                _lblStatus.ForeColor = Color.Red;
                ShowError($"拆分失败: {ex.Message}");
                LogHelper.Error($"PDF拆分失败: {ex.Message}");
            }
            finally
            {
                _btnExecuteSplit.Enabled = true;
                _btnCancel.Enabled = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
        }
    }
}
