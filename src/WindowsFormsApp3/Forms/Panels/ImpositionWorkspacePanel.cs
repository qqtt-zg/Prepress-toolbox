using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using WindowsFormsApp3.Controls;
using WindowsFormsApp3.Controls.Printing;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Panels
{
    public partial class ImpositionWorkspacePanel : UserControl
    {
        private class WorkspacePdfItem
        {
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public int Pages { get; set; }
            public string Status { get; set; }
        }

        // 核心字段
        private TabbedPdfPreviewControl pdfPreview;
        private string currentPdfPath;
        private string selectedFunction = "saddle_stitch"; 

        // 左侧面板的容器
        private Panel fileLoadPanel;
        private Panel fileListPanel;
        private DataGridView dgvFiles;
        private BindingSource bsFiles;
        private FlowLayoutPanel functionSelectPanel;
        private Panel parameterPanel;
        private Panel actionPanel;

        private readonly System.Collections.Generic.List<WorkspacePdfItem> _workspaceItems = new System.Collections.Generic.List<WorkspacePdfItem>();
        private WorkspacePdfItem _currentItem;
        private bool _suppressSelectionChanged;

        // UI参数控件（骑马订）
        private ComboBox cmbPaperSize;
        private TextBox txtCreepCompensation;

        public ImpositionWorkspacePanel()
        {
            InitializeComponent();
            InitializeLayout();
            InitializeControls();
            
            // 订阅Load事件以初始化CEF浏览器
            this.Load += ImpositionWorkspacePanel_Load;
        }

        private void InitializeLayout()
        {
            // 设置整体背景
            this.BackColor = DesignTokens.BgSecondary;
            this.Dock = DockStyle.Fill;
            
            // 左侧面板样式
            mainContainer.Panel1.BackColor = DesignTokens.BgTertiary;
            mainContainer.Panel1.Padding = new Padding(DesignTokens.SpacingBase);
            
            // 右侧面板样式
            mainContainer.Panel2.BackColor = DesignTokens.BgPrimary;
        }

        private void InitializeControls()
        {
            // 1. 初始化右侧预览
            CreatePdfPreview();

            // 2. 初始化左侧各个区域
            // CRITICAL: Dock.Fill 控件必须最先添加，否则会被 Dock.Top/Bottom 遮挡
            
            // 参数设置区 (Fill，填充剩余空间) - 必须第一个添加！
            CreateParameterPanel();
            
            // 底部操作区 (Dock Bottom)
            CreateActionPanel();
            
            // 功能选择区 (Dock Top)
            CreateFunctionSelectPanel();
            
            // 顶部文件加载区 (Dock Top)
            CreateFileLoadPanel();

            // 文件列表区 (Dock Top)
            CreateFileListPanel();
        }

        private void CreatePdfPreview()
        {
            pdfPreview = new TabbedPdfPreviewControl();
            pdfPreview.Dock = DockStyle.Fill;
            
            // 订阅PDF事件
            pdfPreview.PdfLoaded += PdfPreview_PdfLoaded;
            pdfPreview.PageChanged += PdfPreview_PageChanged;
            pdfPreview.LoadError += PdfPreview_LoadError;
            pdfPreview.TabChanged += PdfPreview_TabChanged;
            
            mainContainer.Panel2.Controls.Add(pdfPreview);
        }

        private void CreateFileLoadPanel()
        {
            fileLoadPanel = new Panel();
            fileLoadPanel.Height = 60;
            fileLoadPanel.Dock = DockStyle.Top;
            fileLoadPanel.Padding = new Padding(0, 0, 0, DesignTokens.SpacingBase);

            var btnLoad = new AntdUI.Button();
            btnLoad.Text = "加载 PDF 文件";
            btnLoad.Type = AntdUI.TTypeMini.Primary; // 近似对应：TTypePrimary 与 Type
            btnLoad.IconSvg = "FolderOpenOutlined";
            btnLoad.Dock = DockStyle.Fill;
            btnLoad.Click += BtnLoad_Click;

            fileLoadPanel.Controls.Add(btnLoad);
            mainContainer.Panel1.Controls.Add(fileLoadPanel);
        }

        private void CreateFileListPanel()
        {
            fileListPanel = new Panel();
            fileListPanel.Dock = DockStyle.Top;
            fileListPanel.Height = 220;
            fileListPanel.Padding = new Padding(0, 0, 0, DesignTokens.SpacingBase);

            bsFiles = new BindingSource();
            bsFiles.DataSource = _workspaceItems;

            dgvFiles = new DataGridView();
            dgvFiles.Dock = DockStyle.Fill;
            dgvFiles.AllowUserToAddRows = false;
            dgvFiles.AllowUserToDeleteRows = false;
            dgvFiles.AllowUserToResizeRows = false;
            dgvFiles.ReadOnly = true;
            dgvFiles.MultiSelect = false;
            dgvFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvFiles.RowHeadersVisible = false;
            dgvFiles.AutoGenerateColumns = false;
            dgvFiles.BackgroundColor = DesignTokens.BgTertiary;
            dgvFiles.BorderStyle = BorderStyle.FixedSingle;
            dgvFiles.DataSource = bsFiles;

            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(WorkspacePdfItem.FileName),
                HeaderText = "文件",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 120
            });

            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(WorkspacePdfItem.Pages),
                HeaderText = "页数",
                Width = 60
            });

            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(WorkspacePdfItem.Status),
                HeaderText = "状态",
                Width = 80
            });

            dgvFiles.SelectionChanged += DgvFiles_SelectionChanged;
            dgvFiles.KeyDown += DgvFiles_KeyDown;

            // 拖拽导入
            dgvFiles.AllowDrop = true;
            dgvFiles.DragEnter += DgvFiles_DragEnter;
            dgvFiles.DragDrop += DgvFiles_DragDrop;

            fileListPanel.Controls.Add(dgvFiles);
            mainContainer.Panel1.Controls.Add(fileListPanel);
        }

        private void CreateFunctionSelectPanel()
        {
            functionSelectPanel = new FlowLayoutPanel();
            functionSelectPanel.Height = 100;
            functionSelectPanel.Dock = DockStyle.Top;
            functionSelectPanel.Padding = new Padding(0, 0, 0, DesignTokens.SpacingBase);
            functionSelectPanel.FlowDirection = FlowDirection.LeftToRight;
            functionSelectPanel.WrapContents = true;
            
            // 临时添加几个功能按钮
            AddFunctionButton("骑马订", "saddle_stitch", true);
            AddFunctionButton("N-Up", "nup", false);
            AddFunctionButton("裁切标记", "marks", false);
            AddFunctionButton("页面重排", "shuffle", false);
            
            mainContainer.Panel1.Controls.Add(functionSelectPanel);
        }

        private void AddFunctionButton(string text, string tag, bool isSelected)
        {
            var btn = new AntdUI.Button();
            btn.Text = text;
            btn.Tag = tag;
            btn.Width = 80;
            btn.Height = 32;
            btn.Margin = new Padding(0, 0, DesignTokens.SpacingSM, DesignTokens.SpacingSM);
            
            if (isSelected)
            {
                btn.Type = AntdUI.TTypeMini.Primary;
            }
            else
            {
                btn.Type = AntdUI.TTypeMini.Default;
            }
            
            btn.Click += (s, e) => {
                selectedFunction = tag;
                // 更新按钮样式...
                CreateParameterPanelContent(); // 刷新参数
            };
            
            functionSelectPanel.Controls.Add(btn);
        }

        private void CreateParameterPanel()
        {
            parameterPanel = new Panel();
            parameterPanel.Dock = DockStyle.Fill;
            parameterPanel.AutoScroll = true;
            parameterPanel.Padding = new Padding(0, DesignTokens.SpacingBase, 0, DesignTokens.SpacingBase);
            
            mainContainer.Panel1.Controls.Add(parameterPanel);
            
            // 初始化内容
            CreateParameterPanelContent();
        }

        private void CreateParameterPanelContent()
        {
            parameterPanel.Controls.Clear();
            
            try
            {
                // 设置背景色
                parameterPanel.BackColor = DesignTokens.BgTertiary;

                int currentY = DesignTokens.SpacingBase;

                // 标题
                var lblTitle = new Label();
                lblTitle.Text = selectedFunction == "saddle_stitch" ? "骑马订设置" : 
                               selectedFunction == "nup" ? "N-Up 拼版设置" : 
                               selectedFunction == "marks" ? "裁切标记设置" :
                               selectedFunction == "shuffle" ? "页面重排设置" :
                               "参数设置";
                lblTitle.Font = DesignTokens.FontHeading;
                lblTitle.AutoSize = true;
                lblTitle.ForeColor = DesignTokens.PrimaryColor;
                lblTitle.Location = new Point(DesignTokens.SpacingBase, currentY);
                parameterPanel.Controls.Add(lblTitle);
                currentY = lblTitle.Bottom + DesignTokens.SpacingLG;

                if (selectedFunction == "saddle_stitch")
                {
                    // 1. 纸张大小选择
                    var lblPaper = new Label();
                    lblPaper.Text = "纸张大小:";
                    lblPaper.AutoSize = true;
                    lblPaper.Location = new Point(DesignTokens.SpacingBase, currentY + 5);
                    lblPaper.Font = DesignTokens.FontBase;
                    lblPaper.ForeColor = DesignTokens.TextPrimary;
                    parameterPanel.Controls.Add(lblPaper);
                    
                    cmbPaperSize = new ComboBox();
                    cmbPaperSize.Name = "cmbPaperSize";
                    cmbPaperSize.Location = new Point(DesignTokens.SpacingBase + 80, currentY);
                    cmbPaperSize.Width = 180;
                    cmbPaperSize.DropDownStyle = ComboBoxStyle.DropDownList;
                    cmbPaperSize.Font = DesignTokens.FontBase;
                    cmbPaperSize.Items.AddRange(new object[] { 
                        "A3 (297 x 420 mm)", 
                        "A4 (210 x 297 mm)", 
                        "A5 (148 x 210 mm)",
                        "B4 (257 x 364 mm)",
                        "SRA3 (320 x 450 mm)"
                    });
                    cmbPaperSize.SelectedIndex = 1; // 默认 A4
                    parameterPanel.Controls.Add(cmbPaperSize);
                    currentY = cmbPaperSize.Bottom + DesignTokens.SpacingLG;

                    // 2. 爬移补偿
                    var lblCreep = new Label();
                    lblCreep.Text = "爬移补偿:";
                    lblCreep.AutoSize = true;
                    lblCreep.Location = new Point(DesignTokens.SpacingBase, currentY + 5);
                    lblCreep.Font = DesignTokens.FontBase;
                    lblCreep.ForeColor = DesignTokens.TextPrimary;
                    parameterPanel.Controls.Add(lblCreep);
                    
                    txtCreepCompensation = new TextBox();
                    txtCreepCompensation.Name = "txtCreepCompensation";
                    txtCreepCompensation.Location = new Point(DesignTokens.SpacingBase + 80, currentY);
                    txtCreepCompensation.Width = 60;
                    txtCreepCompensation.Text = "0.0";
                    txtCreepCompensation.Font = DesignTokens.FontBase;
                    txtCreepCompensation.TextAlign = HorizontalAlignment.Right;
                    parameterPanel.Controls.Add(txtCreepCompensation);
                    
                    var lblUnit = new Label();
                    lblUnit.Text = "mm";
                    lblUnit.AutoSize = true;
                    lblUnit.Location = new Point(txtCreepCompensation.Right + 5, currentY + 5);
                    lblUnit.Font = DesignTokens.FontBase;
                    lblUnit.ForeColor = DesignTokens.TextSecondary;
                    parameterPanel.Controls.Add(lblUnit);
                    currentY = txtCreepCompensation.Bottom + DesignTokens.SpacingXL;

                    // 3. 页面顺序可视化
                    var lblVisualizer = new Label();
                    lblVisualizer.Text = "页面顺序示意:";
                    lblVisualizer.AutoSize = true;
                    lblVisualizer.Location = new Point(DesignTokens.SpacingBase, currentY);
                    lblVisualizer.Font = DesignTokens.FontBase;
                    lblVisualizer.ForeColor = DesignTokens.TextPrimary;
                    parameterPanel.Controls.Add(lblVisualizer);
                    currentY = lblVisualizer.Bottom + DesignTokens.SpacingSM;

                    var visualizer = new PageOrderVisualizer();
                    visualizer.ImpositionType = "SaddleStitch";
                    visualizer.PageCount = 16;
                    visualizer.Location = new Point(DesignTokens.SpacingBase, currentY);
                    parameterPanel.Controls.Add(visualizer);
                }
                else if (selectedFunction == "nup")
                {
                    // N-Up 参数设置（暂时简化）
                    var lblInfo = new Label();
                    lblInfo.Text = "N-Up 拼版参数设置正在开发中...";
                    lblInfo.AutoSize = true;
                    lblInfo.Location = new Point(DesignTokens.SpacingBase, currentY);
                    lblInfo.Font = DesignTokens.FontBase;
                    lblInfo.ForeColor = DesignTokens.TextSecondary;
                    parameterPanel.Controls.Add(lblInfo);
                }
                else
                {
                    // 其他功能占位
                    var lblInfo = new Label();
                    lblInfo.Text = "此功能的参数面板正在开发中...";
                    lblInfo.AutoSize = true;
                    lblInfo.Location = new Point(DesignTokens.SpacingBase, currentY);
                    lblInfo.Font = DesignTokens.FontBase;
                    lblInfo.ForeColor = DesignTokens.TextSecondary;
                    parameterPanel.Controls.Add(lblInfo);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("CreateParameterPanelContent failed", ex);
                MessageBox.Show($"参数面板加载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateActionPanel()
        {
            actionPanel = new Panel();
            actionPanel.Height = 50;
            actionPanel.Dock = DockStyle.Bottom;
            actionPanel.Padding = new Padding(0, DesignTokens.SpacingBase, 0, 0);
            
            var btnGenerate = new AntdUI.Button();
            btnGenerate.Text = "生成 PDF";
            btnGenerate.Type = AntdUI.TTypeMini.Primary;
            btnGenerate.Dock = DockStyle.Fill;
            btnGenerate.Click += BtnGenerate_Click;
            
            actionPanel.Controls.Add(btnGenerate);
            mainContainer.Panel1.Controls.Add(actionPanel);
        }

        private void ImpositionWorkspacePanel_Load(object sender, EventArgs e)
        {
            // 标签页控件不需要预先初始化浏览器
            // 浏览器会在添加标签页时自动初始化
            UpdateStatus("就绪");
            LogHelper.Info("[ImpositionWorkspacePanel] 标签页PDF预览控件已就绪");
        }

        /// <summary>
        /// 安全地更新状态栏文本
        /// </summary>
        private void UpdateStatus(string text)
        {
            if (statusStrip?.Items["lblStatus"] != null)
            {
                statusStrip.Items["lblStatus"].Text = text;
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "PDF Files|*.pdf";
                ofd.Multiselect = true; // 支持多文件选择
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadPdfFiles(ofd.FileNames);
                }
            }
        }
        
        private async void LoadPdfFiles(string[] paths)
        {
            if (pdfPreview == null || paths == null || paths.Length == 0)
                return;

            try
            {
                if (statusStrip?.Items["lblStatus"] != null)
                {
                    statusStrip.Items["lblStatus"].Text = $"正在加载 {paths.Length} 个文件...";
                }

                foreach (var path in paths)
                {
                    // 去重：检查是否已在工作区中
                    if (_workspaceItems.Any(x => string.Equals(x.FilePath, path, StringComparison.OrdinalIgnoreCase)))
                    {
                        LogHelper.Info($"[ImpositionWorkspacePanel] 文件已存在，跳过: {Path.GetFileName(path)}");
                        continue;
                    }

                    // 添加到工作区列表（先占位，页数异步获取）
                    var item = new WorkspacePdfItem
                    {
                        FilePath = path,
                        FileName = Path.GetFileName(path),
                        Pages = -1,
                        Status = "加载中..."
                    };
                    _workspaceItems.Add(item);
                    bsFiles?.ResetBindings(false);

                    // 异步加载到预览控件并获取页数
                    var tabIndex = await pdfPreview.AddTabAsync(path);
                    if (tabIndex >= 0)
                    {
                        // 尝试获取页数（通过 TabbedPdfPreviewControl 的事件或直接读取 PDF）
                        int pageCount = await GetPdfPageCountAsync(path);
                        item.Pages = pageCount;
                        item.Status = "就绪";
                    }
                    else
                    {
                        item.Status = "加载失败";
                    }

                    bsFiles?.ResetBindings(false);

                    // 设置当前PDF路径为最后加载的文件
                    currentPdfPath = path;
                    _currentItem = item;
                }

                if (statusStrip?.Items["lblStatus"] != null)
                {
                    if (paths.Length == 1)
                    {
                        statusStrip.Items["lblStatus"].Text = $"已加载: {Path.GetFileName(paths[0])}";
                    }
                    else
                    {
                        statusStrip.Items["lblStatus"].Text = $"已加载 {paths.Length} 个文件";
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[ImpositionWorkspacePanel] 加载PDF文件失败: {ex.Message}", ex);
                MessageBox.Show($"加载 PDF 失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<int> GetPdfPageCountAsync(string pdfPath)
        {
            try
            {
                // 简单异步获取页数（可后续优化为通过 iText 或 PdfiumViewer）
                using (var reader = new iText.Kernel.Pdf.PdfReader(pdfPath))
                using (var doc = new iText.Kernel.Pdf.PdfDocument(reader))
                {
                    return doc.GetNumberOfPages();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"[ImpositionWorkspacePanel] 获取页数失败: {pdfPath}, {ex.Message}");
                return -1;
            }
        }

        private async void BtnGenerate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentPdfPath))
            {
                MessageBox.Show("请先加载PDF文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedFunction == "saddle_stitch")
            {
                await GenerateSaddleStitchAsync();
            }
            else
            {
                MessageBox.Show($"功能 {selectedFunction} 正在开发中...", "提示");
            }
        }

        /// <summary>
        /// 生成骑马订PDF
        /// </summary>
        private async Task GenerateSaddleStitchAsync()
        {
            try
            {
                // 1. 收集UI参数
                var paperSize = GetSelectedPaperSize();
                var creepCompensation = GetCreepCompensation();

                // 2. 生成输出路径
                var outputPath = GenerateOutputFilePath("saddle_stitch");

                // 3. 创建服务配置
                var service = new SaddleStitchService();
                var config = new SaddleStitchService.SaddleStitchConfig
                {
                    InputPdfPath = currentPdfPath,
                    OutputPdfPath = outputPath,
                    FinishedPageSize = paperSize,
                    CreepCompensationMm = creepCompensation
                };

                // 4. 显示进度
                ShowBottomProgress("正在生成骑马订PDF...");
                if (statusStrip?.Items["lblStatus"] != null)
                {
                    statusStrip.Items["lblStatus"].Text = "处理中...";
                }

                // 5. 调用服务生成PDF
                var progress = new Progress<int>(value =>
                {
                    UpdateBottomProgress(value);
                });

                var result = await service.GenerateSaddleStitchPdfAsync(config, progress);

                // 6. 隐藏进度
                HideBottomProgress();

                // 7. 显示成功消息
                MessageBox.Show($"骑马订PDF生成成功！\n\n输出文件：\n{result}", "成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 8. 加载预览
                if (statusStrip?.Items["lblStatus"] != null)
                {
                    statusStrip.Items["lblStatus"].Text = "加载预览...";
                }
                await pdfPreview.AddTabAsync(result);
            }
            catch (Exception ex)
            {
                HideBottomProgress();
                LogHelper.Error("[ImpositionWorkspace] 生成骑马订PDF失败", ex);
                MessageBox.Show($"生成失败：\n{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (statusStrip?.Items["lblStatus"] != null)
                {
                    statusStrip.Items["lblStatus"].Text = "生成失败";
                }
            }
        }

        /// <summary>
        /// 获取选中的纸张大小
        /// </summary>
        private iText.Kernel.Geom.PageSize GetSelectedPaperSize()
        {
            if (cmbPaperSize == null || cmbPaperSize.SelectedIndex < 0)
                return iText.Kernel.Geom.PageSize.A4;

            switch (cmbPaperSize.SelectedIndex)
            {
                case 0: return iText.Kernel.Geom.PageSize.A3;
                case 1: return iText.Kernel.Geom.PageSize.A4;
                case 2: return iText.Kernel.Geom.PageSize.A5;
                case 3: return iText.Kernel.Geom.PageSize.B4;
                case 4: // SRA3
                    return new iText.Kernel.Geom.PageSize(320 * 72f / 25.4f, 450 * 72f / 25.4f);
                default: return iText.Kernel.Geom.PageSize.A4;
            }
        }

        /// <summary>
        /// 获取爬移补偿值
        /// </summary>
        private float GetCreepCompensation()
        {
            if (txtCreepCompensation == null)
                return 0f;

            if (float.TryParse(txtCreepCompensation.Text, out float value))
                return value;

            return 0f;
        }

        /// <summary>
        /// 生成输出文件路径
        /// </summary>
        private string GenerateOutputFilePath(string suffix)
        {
            var dir = Path.GetDirectoryName(currentPdfPath);
            var fileName = Path.GetFileNameWithoutExtension(currentPdfPath);
            var ext = Path.GetExtension(currentPdfPath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(dir, $"{fileName}_{suffix}_{timestamp}{ext}");
        }

        /// <summary>
        /// 显示底部进度条
        /// </summary>
        private void ShowBottomProgress(string message)
        {
            // TODO: 实现进度条显示（与 PdfOperationsPanel 类似）
            statusStrip.Items["lblStatus"].Text = message;
        }

        /// <summary>
        /// 更新底部进度条
        /// </summary>
        private void UpdateBottomProgress(int percentage)
        {
            statusStrip.Items["lblStatus"].Text = $"处理中... {percentage}%";
        }

        /// <summary>
        /// 隐藏底部进度条
        /// </summary>
        private void HideBottomProgress()
        {
            statusStrip.Items["lblStatus"].Text = "就绪";
        }

        /// <summary>
        /// PDF加载完成事件处理
        /// </summary>
        private void PdfPreview_PdfLoaded(object sender, EventArgs e)
        {
            try
            {
                // 获取PDF信息
                var pageCount = pdfPreview.PageCount;
                var fileName = System.IO.Path.GetFileName(currentPdfPath);
                
                // 获取文件大小
                long fileSize = 0;
                if (!string.IsNullOrEmpty(currentPdfPath) && System.IO.File.Exists(currentPdfPath))
                {
                    fileSize = new System.IO.FileInfo(currentPdfPath).Length;
                }
                
                // 更新状态栏
                statusStrip.Items["lblStatus"].Text = 
                    $"已加载: {fileName} | 页数: {pageCount} | 大小: {fileSize / 1024:N0} KB";
                
                LogHelper.Info($"PDF loaded: {fileName}, Pages: {pageCount}");
            }
            catch (Exception ex)
            {
                LogHelper.Error("PdfPreview_PdfLoaded error", ex);
            }
        }

        /// <summary>
        /// PDF页面切换事件处理
        /// </summary>
        private void PdfPreview_PageChanged(object sender, EventArgs e)
        {
            try
            {
                var currentPage = pdfPreview.CurrentPage;
                var pageCount = pdfPreview.PageCount;
                var fileName = System.IO.Path.GetFileName(currentPdfPath);
                
                // 可选：更新状态栏显示当前页
                statusStrip.Items["lblStatus"].Text = 
                    $"{fileName} | 第 {currentPage}/{pageCount} 页";
            }
            catch (Exception ex)
            {
                LogHelper.Error("PdfPreview_PageChanged error", ex);
            }
        }

        /// <summary>
        /// PDF加载错误事件处理
        /// </summary>
        private void PdfPreview_LoadError(object sender, PdfLoadErrorEventArgs e)
        {
            LogHelper.Error("PDF load error", new Exception(e.Error));
            MessageBox.Show($"PDF加载失败:\n{e.Error}", "错误", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            statusStrip.Items["lblStatus"].Text = "PDF加载失败";
        }

        /// <summary>
        /// 标签页切换事件处理
        /// </summary>
        private void PdfPreview_TabChanged(object sender, TabChangedEventArgs e)
        {
            try
            {
                // 更新当前PDF路径
                currentPdfPath = e.FilePath;

                // 同步左侧文件列表选中项
                SelectItemByPath(e.FilePath);
                
                // 更新状态栏
                var fileName = Path.GetFileName(e.FilePath);
                if (statusStrip?.Items["lblStatus"] != null)
                {
                    statusStrip.Items["lblStatus"].Text = $"当前文件: {fileName}";
                }
                
                LogHelper.Info($"[ImpositionWorkspacePanel] 切换到标签页: {fileName}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[ImpositionWorkspacePanel] 标签页切换错误: {ex.Message}", ex);
            }
        }

        private void DgvFiles_SelectionChanged(object sender, EventArgs e)
        {
            if (_suppressSelectionChanged)
                return;

            try
            {
                if (dgvFiles?.SelectedRows == null || dgvFiles.SelectedRows.Count == 0)
                    return;

                var item = dgvFiles.SelectedRows[0].DataBoundItem as WorkspacePdfItem;
                if (item == null)
                    return;

                _currentItem = item;
                currentPdfPath = item.FilePath;

                // 通过标签页预览控件切换（如果已打开则只切换，不重复打开）
                _ = pdfPreview?.AddTabAsync(item.FilePath);
            }
            catch (Exception ex)
            {
                LogHelper.Error("[ImpositionWorkspacePanel] DgvFiles_SelectionChanged error", ex);
            }
        }

        private void DgvFiles_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Delete)
                {
                    RemoveSelectedItem();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("[ImpositionWorkspacePanel] DgvFiles_KeyDown error", ex);
            }
        }

        private void DgvFiles_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void DgvFiles_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop))
                    return;

                var dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (dropped == null || dropped.Length == 0)
                    return;

                var pdfs = new System.Collections.Generic.List<string>();
                foreach (var p in dropped)
                {
                    if (Directory.Exists(p))
                    {
                        try
                        {
                            pdfs.AddRange(Directory.GetFiles(p, "*.pdf", SearchOption.TopDirectoryOnly));
                        }
                        catch
                        {
                        }
                        continue;
                    }

                    if (File.Exists(p) && string.Equals(Path.GetExtension(p), ".pdf", StringComparison.OrdinalIgnoreCase))
                        pdfs.Add(p);
                }

                if (pdfs.Count > 0)
                {
                    LoadPdfFiles(pdfs.ToArray());
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("[ImpositionWorkspacePanel] DgvFiles_DragDrop error", ex);
            }
        }

        private void SelectItemByPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || dgvFiles == null)
                return;

            try
            {
                _suppressSelectionChanged = true;

                foreach (DataGridViewRow row in dgvFiles.Rows)
                {
                    var item = row.DataBoundItem as WorkspacePdfItem;
                    if (item == null)
                        continue;

                    if (string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                    {
                        row.Selected = true;
                        dgvFiles.CurrentCell = row.Cells[0];
                        _currentItem = item;
                        currentPdfPath = item.FilePath;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("[ImpositionWorkspacePanel] SelectItemByPath error", ex);
            }
            finally
            {
                _suppressSelectionChanged = false;
            }
        }

        private void RemoveSelectedItem()
        {
            if (dgvFiles?.SelectedRows == null || dgvFiles.SelectedRows.Count == 0)
                return;

            var item = dgvFiles.SelectedRows[0].DataBoundItem as WorkspacePdfItem;
            if (item == null)
                return;

            _workspaceItems.Remove(item);
            bsFiles?.ResetBindings(false);

            if (_currentItem == item)
            {
                _currentItem = null;
                currentPdfPath = null;
            }

            UpdateStatus($"已移除: {item.FileName}");
        }

        /// <summary>
        /// 应用主题
        /// </summary>
        public void ApplyTheme(bool isDark)
        {
            // 更新背景色
            this.BackColor = isDark ? Color.FromArgb(40, 40, 40) : DesignTokens.BgSecondary;
            mainContainer.Panel1.BackColor = isDark ? Color.FromArgb(50, 50, 50) : DesignTokens.BgTertiary;
            mainContainer.Panel2.BackColor = isDark ? Color.FromArgb(30, 30, 30) : DesignTokens.BgPrimary;
            
            // 同步PDF预览主题
            pdfPreview?.ApplyTheme(isDark);
            
            // 更新参数面板中的 Label 颜色
            foreach (Control c in parameterPanel.Controls)
            {
                if (c is Label lbl)
                {
                    lbl.ForeColor = isDark ? Color.FromArgb(200, 200, 200) : DesignTokens.TextPrimary;
                }
            }
        }
    }
}
