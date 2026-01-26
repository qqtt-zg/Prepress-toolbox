using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntdUI;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Forms;
using WinFormsPanel = System.Windows.Forms.Panel;
using System.ComponentModel;

namespace WindowsFormsApp3.Forms.Controls
{
    /// <summary>
    /// PDF检查器控件
    /// 类似Enfocus PitStop Pro的Inspector面板
    /// </summary>
    public partial class PdfInspectorControl : UserControl
    {
        private PdfInspectorService _inspectorService;
        private PdfFontInspectorService_Poppler _fontInspectorService;
        private PdfFontOutlineService _fontOutlineService;
        private PdfInspectorInfo _currentInfo;
        private DocumentFontInfo _fontInfo;
        private MeasurementUnit _currentUnit = MeasurementUnit.Millimeter;

        // UI控件
        private WinFormsPanel _headerPanel;
        private AntdUI.Select _unitSelector;
        private AntdUI.Button _refreshButton;

        private WinFormsPanel _contentPanel;
        private AntdUI.Tabs _mainTabs;

        // 当前页面标签页
        private AntdUI.TabPage _currentPageTabPage;
        private WinFormsPanel _currentPagePanel;
        private TableLayoutPanel _boxesTable;

        // 所有页面标签页
        private AntdUI.TabPage _allPagesTabPage;
        private WinFormsPanel _allPagesPanel;
        private AntdUI.Table _pagesTable;

        // 问题标签页
        private AntdUI.TabPage _issuesTabPage;
        private WinFormsPanel _issuesPanel;
        private AntdUI.Table _issuesTable;

        // 字体标签页
        private AntdUI.TabPage _fontsTabPage;
        private WinFormsPanel _fontsPanel;
        private AntdUI.Table _fontsTable;
        private AntdUI.Button _outlineButton;
        private string _loadedFilePath; // 跟踪已加载的文件路径

        public event EventHandler<int> PageSelected;

        public PdfInspectorControl()
        {
            InitializeComponent();
            
            // 初始化标签页（在Designer初始化组件后调用）
            InitializeTabs();

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            LogHelper.Debug("[PdfInspectorControl] Initializing Inspector Services...");
            _inspectorService = new PdfInspectorService();
            
            LogHelper.Debug("[PdfInspectorControl] Instantiating PdfFontInspectorService_Poppler...");
            _fontInspectorService = new PdfFontInspectorService_Poppler();
            
            _fontOutlineService = new PdfFontOutlineService();
        }

        private void InitializeTabs()
        {
            // 创建所有标签页
            CreateCurrentPageTab();
            CreateAllPagesTab();
            CreateIssuesTab();
            CreateFontsTab();

            if (_mainTabs != null)
            {
                _mainTabs.Controls.Add(_currentPageTabPage);
                _mainTabs.Controls.Add(_allPagesTabPage);
                _mainTabs.Controls.Add(_issuesTabPage);
                _mainTabs.Controls.Add(_fontsTabPage);
                
                _mainTabs.Pages.Add(_currentPageTabPage);
                _mainTabs.Pages.Add(_allPagesTabPage);
                _mainTabs.Pages.Add(_issuesTabPage);
                _mainTabs.Pages.Add(_fontsTabPage);
            }
        }



        /// <summary>
        /// 创建当前页面标签页
        /// </summary>
        private void CreateCurrentPageTab()
        {
            _currentPagePanel = new WinFormsPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            // 创建页面框信息表格
            _boxesTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                BackColor = Color.White
            };
            _boxesTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            _boxesTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _currentPagePanel.Controls.Add(_boxesTable);
            
            // Edit Button Panel
            var editPanel = new WinFormsPanel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(0, 5, 0, 5) };
            var editButton = new AntdUI.Button
            {
                Text = "编辑页面几何框",
                IconSvg = "EditOutlined",
                Type = TTypeMini.Primary,
                Dock = DockStyle.Right,
                Width = 140
            };
            editButton.Click += EditPageBoxButton_Click;
            editPanel.Controls.Add(editButton);
            
            _currentPagePanel.Controls.Add(editPanel);
            // Reverse order because Dock=Top. Added last appears at top? No, Dock=Top stacks. 
            // _boxesTable is added first (Top). editPanel added second (Top). editPanel will be below _boxesTable.
            // Wait, Dock=Top: First added is at the top. Subsequent Dock=Top goes *under* previous Dock=Top.
            // So if I want Edit Button at Top, I must add it *before* _boxesTable.
            
            // Correction: WinForms Dock=Top logic:
            // Controls[0] is at the bottom of the Dock stack? No.
            // The last control added with Dock.Top appears at the very top of the Z-order, and thus physically at the top.
            // Let's verify: Control A (Top), Control B (Top). Visual: B, A.
            // So if I want Edit Panel at the very top, I should add it LAST.
            // Currently _boxesTable is added. If I add editPanel now, editPanel will be ABOVE _boxesTable.
            // That is what I want.
            
            // Wait, standard practice:
            // panel.Controls.Add(A); A.Dock = Top; -> A is Top.
            // panel.Controls.Add(B); B.Dock = Top; -> B is above A.
            // So YES, adding editPanel NOW (after _boxesTable) will put it at the very top.


            _currentPageTabPage = new AntdUI.TabPage
            {
                Text = "当前页面",
                Name = "currentPageTab",
                Dock = DockStyle.Fill
            };
            _currentPageTabPage.Controls.Add(_currentPagePanel);
        }

        /// <summary>
        /// 编辑页面几何框
        /// </summary>
        private void EditPageBoxButton_Click(object sender, EventArgs e)
        {
            if (_currentInfo?.CurrentPageBoxes == null) return;

            var form = new PageBoxEditForm(_currentInfo.CurrentPageBoxes, _currentUnit);
            if (form.ShowDialog() == DialogResult.OK)
            {
                if (form.ResultInfo != null)
                {
                    // Save changes
                    bool success = _inspectorService.SavePageBox(_currentInfo.FilePath, form.ResultInfo, form.ApplyToAllPages);
                    if (success)
                    {
                        AntdUI.Notification.success(this.FindForm(), "保存成功", "页面几何框已更新");
                        // Reload
                        LoadPdf(_currentInfo.FilePath, _currentInfo.CurrentPage);
                        // Trigger external update event if needed
                        // OnPdfModified(new PdfModifiedEventArgs(_currentInfo.FilePath)); 
                    }
                    else
                    {
                        AntdUI.Notification.error(this.FindForm(), "保存失败", "无法保存页面几何框修改");
                    }
                }
            }
        }

        /// <summary>
        /// 创建所有页面标签页
        /// </summary>
        private void CreateAllPagesTab()
        {
            _allPagesPanel = new WinFormsPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            _pagesTable = new AntdUI.Table
            {
                Dock = DockStyle.Fill,
                Bordered = true,
                VisibleHeader = true,
                FixedHeader = true
            };
            _pagesTable.CellClick += PagesTable_CellClick;

            _allPagesPanel.Controls.Add(_pagesTable);

            _allPagesTabPage = new AntdUI.TabPage
            {
                Text = "所有页面",
                Name = "allPagesTab",
                Dock = DockStyle.Fill
            };
            _allPagesTabPage.Controls.Add(_allPagesPanel);
        }

        /// <summary>
        /// 创建问题标签页
        /// </summary>
        private void CreateIssuesTab()
        {
            _issuesPanel = new WinFormsPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            _issuesTable = new AntdUI.Table
            {
                Dock = DockStyle.Fill,
                Bordered = true,
                VisibleHeader = true,
                FixedHeader = true
            };
            _issuesTable.CellClick += IssuesTable_CellClick;

            _issuesPanel.Controls.Add(_issuesTable);

            _issuesTabPage = new AntdUI.TabPage
            {
                Text = "问题",
                Name = "issuesTab",
                Dock = DockStyle.Fill
            };
            _issuesTabPage.Controls.Add(_issuesPanel);
        }

        /// <summary>
        /// 创建字体标签页
        /// </summary>
        private void CreateFontsTab()
        {
            try
            {
                LogHelper.Debug("[PdfInspectorControl] 开始创建字体标签页");

                _fontsPanel = new WinFormsPanel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Padding = new Padding(10)
                };

                // 创建工具栏
                var toolbarPanel = new WinFormsPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    MinimumSize = new Size(0, 50),
                    BackColor = Color.White,
                    Padding = new Padding(0, 5, 0, 10) // 增加底部内边距
                };

                // 转曲按钮
                _outlineButton = new AntdUI.Button
                {
                    Text = "转曲",
                    Location = new Point(0, 10),
                    Width = 80,
                    Height = 30,
                    Type = AntdUI.TTypeMini.Primary,
                    IconSvg = "ThunderboltOutlined",
                    Enabled = false,  // 初始禁用，加载PDF后启用
                    Visible = true    // 确保可见
                };
                _outlineButton.Click += OutlineButton_Click;
                
                LogHelper.Debug($"[PdfInspectorControl] 转曲按钮已创建: Enabled={_outlineButton.Enabled}, Visible={_outlineButton.Visible}");

                // 说明文字
                var infoLabel = new AntdUI.Label
                {
                    Text = "将PDF中的文字转换为路径（曲线），转曲后文字将无法编辑",
                    Location = new Point(90, 15),
                    AutoSize = true,
                    ForeColor = Color.Gray,
                    Font = new Font("Microsoft YaHei UI", 8F),
                    Visible = true
                };

                toolbarPanel.Controls.Add(_outlineButton);
                toolbarPanel.Controls.Add(infoLabel);
                
                LogHelper.Debug($"[PdfInspectorControl] 工具栏控件数量: {toolbarPanel.Controls.Count}");

                // 字体表格
                _fontsTable = new AntdUI.Table
                {
                    Dock = DockStyle.Fill,
                    Bordered = true,
                    VisibleHeader = true,  // 确保列头可见
                    FixedHeader = true     // 固定列头
                };

                // 先添加工具栏（Dock.Top），再添加表格（Dock.Fill）
                // 注意：在WinForms中，为了让Dock=Fill的控件不遮挡Dock=Top的控件，
                // Dock=Top的控件需要在Z-order的底部（先被Dock处理），
                // 而Dock=Fill的控件需要在Z-order的顶部（最后被Dock处理）。
                _fontsPanel.Controls.Add(toolbarPanel);
                _fontsPanel.Controls.Add(_fontsTable);
                
                // 明确设置Z-order确保布局正确
                toolbarPanel.SendToBack(); // 底部 Z-order -> 优先 Dock
                _fontsTable.BringToFront(); // 顶部 Z-order -> 最后 Dock (Fill剩余空间)

                _fontsTabPage = new AntdUI.TabPage
                {
                    Text = "字体",
                    Name = "fontsTab",
                    Dock = DockStyle.Fill
                };
                _fontsTabPage.Controls.Add(_fontsPanel);

                LogHelper.Debug($"[PdfInspectorControl] 字体标签页创建完成: Text={_fontsTabPage.Text}, Name={_fontsTabPage.Name}, HasControls={_fontsTabPage.Controls.Count > 0}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PdfInspectorControl] 创建字体标签页失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载PDF文件
        /// </summary>
        public void LoadPdf(string filePath, int currentPage = 1)
        {
            try
            {
                LogHelper.Debug($"[PdfInspectorControl] LoadPdf 开始: {filePath}, 页码: {currentPage}");
                LogHelper.Debug($"[PdfInspectorControl] _outlineButton 是否为 null: {_outlineButton == null}");
                
                _currentInfo = _inspectorService.InspectPdf(filePath, currentPage);
                _fontInfo = _fontInspectorService.InspectFonts(filePath);

                if (_currentInfo != null)
                {
                    LogHelper.Debug("[PdfInspectorControl] PDF信息加载成功，开始更新显示");
                    
                    UpdateCurrentPageDisplay();
                    UpdateAllPagesDisplay();
                    UpdateIssuesDisplay();
                    UpdateFontsDisplay();

                    // 更新问题标签页的徽章
                    UpdateIssuesBadge();
                    
                    // 保存文件路径
                    _loadedFilePath = filePath;
                    
                    // 启用转曲按钮
                    if (_outlineButton != null)
                    {
                        LogHelper.Debug($"[PdfInspectorControl] 启用转曲按钮，当前状态: Enabled={_outlineButton.Enabled}, Visible={_outlineButton.Visible}");
                        
                        // 使用 Invoke 确保在 UI 线程上执行
                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(() =>
                            {
                                _outlineButton.Enabled = true;
                                LogHelper.Debug($"[PdfInspectorControl] 转曲按钮已启用(Invoke): Enabled={_outlineButton.Enabled}");
                            }));
                        }
                        else
                        {
                            _outlineButton.Enabled = true;
                            LogHelper.Debug($"[PdfInspectorControl] 转曲按钮已启用(直接): Enabled={_outlineButton.Enabled}");
                        }
                    }
                    else
                    {
                        LogHelper.Warn("[PdfInspectorControl] _outlineButton 为 null，无法启用");
                    }
                }
                else
                {
                    LogHelper.Warn("[PdfInspectorControl] _currentInfo 为 null");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PdfInspectorControl] 加载PDF检查器失败: {ex.Message}", ex);
                AntdUI.Notification.error(this.FindForm(), "加载失败", ex.Message);
                
                // 禁用转曲按钮
                if (_outlineButton != null)
                {
                    _outlineButton.Enabled = false;
                }
            }
        }

        /// <summary>
        /// 更新当前页面显示
        /// </summary>
        private void UpdateCurrentPageDisplay()
        {
            if (_currentInfo?.CurrentPageBoxes == null)
                return;

            _boxesTable.Controls.Clear();
            _boxesTable.RowStyles.Clear();
            _boxesTable.RowCount = 0;

            var pageInfo = _currentInfo.CurrentPageBoxes;

            // 添加页面信息
            AddInfoRow("页码", $"{_currentInfo.CurrentPage} / {_currentInfo.TotalPages}");
            AddInfoRow("旋转", $"{pageInfo.Rotation}°");
            AddSeparator();

            // MediaBox
            AddBoxInfoRows("MediaBox", pageInfo.MediaBox, Color.FromArgb(220, 53, 69));

            // CropBox
            AddBoxInfoRows("CropBox", pageInfo.CropBox, Color.FromArgb(0, 123, 255));

            // TrimBox
            AddBoxInfoRows("TrimBox", pageInfo.TrimBox, Color.FromArgb(40, 167, 69));

            // BleedBox
            AddBoxInfoRows("BleedBox", pageInfo.BleedBox, Color.FromArgb(255, 193, 7));

            // ArtBox
            AddBoxInfoRows("ArtBox", pageInfo.ArtBox, Color.FromArgb(108, 117, 125));

            // 出血信息
            AddSeparator();
            var bleedInfo = _inspectorService.GetBleedInfo(pageInfo);
            AddInfoRow("出血", bleedInfo.ToString());

            // 问题提示
            if (pageInfo.HasIssues)
            {
                AddSeparator();
                foreach (var issue in pageInfo.IssueDescriptions)
                {
                    AddWarningRow(issue);
                }
            }
        }

        /// <summary>
        /// 添加页面框信息行
        /// </summary>
        private void AddBoxInfoRows(string boxName, BoxDimension box, Color color)
        {
            // 标题行
            var titleLabel = new AntdUI.Label
            {
                Text = boxName,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = color,
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 2)
            };

            var statusLabel = new AntdUI.Label
            {
                Text = box.IsDefined ? "✓ 已定义" : "✗ 未定义",
                Font = new Font("Microsoft YaHei UI", 8F),
                ForeColor = box.IsDefined ? Color.Green : Color.Gray,
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 2)
            };

            _boxesTable.RowCount++;
            _boxesTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _boxesTable.Controls.Add(titleLabel, 0, _boxesTable.RowCount - 1);
            _boxesTable.Controls.Add(statusLabel, 1, _boxesTable.RowCount - 1);

            if (box.IsDefined)
            {
                // 尺寸
                AddInfoRow("  尺寸", box.GetFormattedSize(_currentUnit), 8);

                // 位置
                AddInfoRow("  位置", box.GetFormattedPosition(_currentUnit), 8);
            }
        }

        /// <summary>
        /// 添加信息行
        /// </summary>
        private void AddInfoRow(string label, string value, int fontSize = 9)
        {
            var labelControl = new AntdUI.Label
            {
                Text = label,
                Font = new Font("Microsoft YaHei UI", fontSize),
                ForeColor = Color.FromArgb(100, 100, 100),
                AutoSize = true,
                Padding = new Padding(0, 4, 0, 4)
            };

            var valueControl = new AntdUI.Label
            {
                Text = value,
                Font = new Font("Consolas", fontSize),
                AutoSize = true,
                Padding = new Padding(0, 4, 0, 4)
            };

            _boxesTable.RowCount++;
            _boxesTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _boxesTable.Controls.Add(labelControl, 0, _boxesTable.RowCount - 1);
            _boxesTable.Controls.Add(valueControl, 1, _boxesTable.RowCount - 1);
        }

        /// <summary>
        /// 添加警告行
        /// </summary>
        private void AddWarningRow(string message)
        {
            var warningLabel = new AntdUI.Label
            {
                Text = "⚠ " + message,
                Font = new Font("Microsoft YaHei UI", 8F),
                ForeColor = Color.FromArgb(255, 193, 7),
                AutoSize = true,
                Padding = new Padding(0, 4, 0, 4)
            };

            _boxesTable.RowCount++;
            _boxesTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _boxesTable.SetColumnSpan(warningLabel, 2);
            _boxesTable.Controls.Add(warningLabel, 0, _boxesTable.RowCount - 1);
        }

        /// <summary>
        /// 添加分隔线
        /// </summary>
        private void AddSeparator()
        {
            var separator = new WinFormsPanel
            {
                Height = 1,
                BackColor = Color.FromArgb(230, 230, 230),
                Dock = DockStyle.Top,
                Margin = new Padding(0, 8, 0, 8)
            };

            _boxesTable.RowCount++;
            _boxesTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            _boxesTable.SetColumnSpan(separator, 2);
            _boxesTable.Controls.Add(separator, 0, _boxesTable.RowCount - 1);
        }

        /// <summary>
        /// 更新所有页面显示
        /// </summary>
        private void UpdateAllPagesDisplay()
        {
            if (_currentInfo?.AllPageBoxes == null)
                return;

            // 配置表格列
            _pagesTable.Columns = new AntdUI.ColumnCollection
            {
                new AntdUI.ColumnCheck("selected") { Width = "50" },
                new AntdUI.Column("page", "页码", AntdUI.ColumnAlign.Center) { Width = "60" },
                new AntdUI.Column("size", "尺寸 (CropBox)", AntdUI.ColumnAlign.Left) { Width = "150" },
                new AntdUI.Column("rotation", "旋转", AntdUI.ColumnAlign.Center) { Width = "60" },
                new AntdUI.Column("status", "状态", AntdUI.ColumnAlign.Center) { Width = "80" }
            };

            // 填充数据
            var dataSource = _currentInfo.AllPageBoxes.Select(p => new
            {
                selected = false,
                page = p.PageNumber,
                size = p.CropBox.GetFormattedSize(_currentUnit),
                rotation = $"{p.Rotation}°",
                status = p.HasIssues ? "⚠ 有问题" : "✓ 正常"
            }).ToArray();

            _pagesTable.DataSource = dataSource;
        }

        /// <summary>
        /// 更新问题显示
        /// </summary>
        private void UpdateIssuesDisplay()
        {
            if (_currentInfo?.Issues == null)
                return;

            // 配置表格列
            _issuesTable.Columns = new AntdUI.ColumnCollection
            {
                new AntdUI.Column("severity", "级别", AntdUI.ColumnAlign.Center) { Width = "60" },
                new AntdUI.Column("page", "页码", AntdUI.ColumnAlign.Center) { Width = "60" },
                new AntdUI.Column("boxType", "页面框", AntdUI.ColumnAlign.Center) { Width = "100" },
                new AntdUI.Column("description", "描述", AntdUI.ColumnAlign.Left)
            };

            // 填充数据
            var dataSource = _currentInfo.Issues.Select(i => new
            {
                severity = GetSeverityIcon(i.Severity),
                page = i.PageNumber == 0 ? "全部" : i.PageNumber.ToString(),
                boxType = i.BoxType,
                description = i.Description
            }).ToArray();

            _issuesTable.DataSource = dataSource;
        }

        /// <summary>
        /// 更新字体显示
        /// </summary>
        private void UpdateFontsDisplay()
        {
            if (_fontInfo?.Fonts == null)
                return;

            // 配置表格列
            _fontsTable.Columns = new AntdUI.ColumnCollection
            {
                new AntdUI.Column("status", "状态", AntdUI.ColumnAlign.Center) { Width = "50" },
                new AntdUI.Column("fontName", "字体名称", AntdUI.ColumnAlign.Left) { Width = "200" },
                new AntdUI.Column("fontType", "类型", AntdUI.ColumnAlign.Center) { Width = "80" },
                new AntdUI.Column("embedding", "嵌入状态", AntdUI.ColumnAlign.Center) { Width = "100" },
                new AntdUI.Column("pages", "使用页面", AntdUI.ColumnAlign.Left) { Width = "120" },
                new AntdUI.Column("issues", "问题", AntdUI.ColumnAlign.Left)
            };

            // 填充数据
            var dataSource = _fontInfo.Fonts.Select(f => new
            {
                status = f.StatusIcon,
                fontName = f.FontName,
                fontType = f.FontSubtype,
                embedding = f.EmbeddingStatusText,
                pages = f.UsedPagesText,
                issues = f.HasIssues ? string.Join("; ", f.Issues) : "-"
            }).ToArray();

            _fontsTable.DataSource = dataSource;

            // 更新字体标签页标题
            UpdateFontsBadge();
        }

        /// <summary>
        /// 更新字体徽章
        /// </summary>
        private void UpdateFontsBadge()
        {
            if (_fontInfo == null)
                return;

            if (_fontsTabPage != null)
            {
                if (_fontInfo.HasFontIssues)
                {
                    _fontsTabPage.Text = $"字体 ({_fontInfo.TotalFonts}, ⚠{_fontInfo.ProblematicFontsCount})";
                }
                else
                {
                    _fontsTabPage.Text = $"字体 ({_fontInfo.TotalFonts})";
                }
            }
        }

        /// <summary>
        /// 更新问题徽章
        /// </summary>
        private void UpdateIssuesBadge()
        {
            if (_currentInfo?.Issues == null)
                return;

            int issueCount = _currentInfo.Issues.Count;
            if (_issuesTabPage != null)
            {
                _issuesTabPage.Text = issueCount > 0 ? $"问题 ({issueCount})" : "问题";
            }
        }

        /// <summary>
        /// 获取严重程度图标
        /// </summary>
        private string GetSeverityIcon(IssueSeverity severity)
        {
            switch (severity)
            {
                case IssueSeverity.Error:
                    return "❌";
                case IssueSeverity.Warning:
                    return "⚠";
                case IssueSeverity.Info:
                    return "ℹ";
                default:
                    return "";
            }
        }

        /// <summary>
        /// 单位选择器变化事件
        /// </summary>
        private void UnitSelector_SelectedValueChanged(object sender, object value)
        {
            switch (_unitSelector.SelectedIndex)
            {
                case 0:
                    _currentUnit = MeasurementUnit.Millimeter;
                    break;
                case 1:
                    _currentUnit = MeasurementUnit.Inch;
                    break;
                case 2:
                    _currentUnit = MeasurementUnit.Point;
                    break;
            }

            // 刷新显示
            if (_currentInfo != null)
            {
                UpdateCurrentPageDisplay();
                UpdateAllPagesDisplay();
            }
        }

        /// <summary>
        /// 刷新按钮点击事件
        /// </summary>
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            if (_currentInfo != null && !string.IsNullOrEmpty(_currentInfo.FilePath))
            {
                LoadPdf(_currentInfo.FilePath, _currentInfo.CurrentPage);
                    AntdUI.Notification.success(this.FindForm(), "已刷新", "PDF检查器已刷新");
            }
        }

        /// <summary>
        /// 转曲按钮点击事件
        /// </summary>
        /// <summary>
        /// 转曲按钮点击事件
        /// </summary>
        private async void OutlineButton_Click(object sender, EventArgs e)
        {
            // 使用存储的文件路径，而不是 _currentInfo.FilePath
            string filePath = _loadedFilePath ?? _currentInfo?.FilePath;
            
            if (string.IsNullOrEmpty(filePath))
            {
                AntdUI.Notification.warn(this.FindForm(), "提示", "请先加载PDF文件");
                return;
            }

            try
            {
                // 显示处理中提示
                UpdateStatus("正在转曲，请稍候...");
                _outlineButton.Enabled = false;

                // 触发开始事件（显示进度条）
                OnFontOutlineStarted();

                // 执行转曲（在后台线程中）
                byte[] outlinedPdfBytes = await Task.Run(() => _fontOutlineService.ConvertTextToOutlinesBytes(filePath));

                if (outlinedPdfBytes != null && outlinedPdfBytes.Length > 0)
                {
                    // 触发转曲完成事件，通知父窗口
                    OnFontOutlineCompleted(outlinedPdfBytes, filePath);
                    
                    UpdateStatus("已转曲 (未保存)");
                }
                else
                {
                    AntdUI.Notification.error(this.FindForm(), "转曲失败", 
                        "字体转曲失败，请查看日志了解详情");
                    UpdateStatus("转曲失败");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"字体转曲失败: {ex.Message}", ex);
                AntdUI.Notification.error(this.FindForm(), "转曲失败", ex.Message);
                UpdateStatus("转曲失败");
            }
            finally
            {
                _outlineButton.Enabled = true;
            }
        }

        /// <summary>
        /// 更新状态文本（如果有状态栏）
        /// </summary>
        private void UpdateStatus(string message)
        {
            // 这里可以添加状态栏更新逻辑
            LogHelper.Info($"[PdfInspectorControl] {message}");
        }

        /// <summary>
        /// 字体转曲完成事件
        /// </summary>
        public event EventHandler<FontOutlineCompletedEventArgs> FontOutlineCompleted;

        /// <summary>
        /// 字体转曲开始事件
        /// </summary>
        public event EventHandler FontOutlineStarted;

        /// <summary>
        /// 触发字体转曲开始事件
        /// </summary>
        protected virtual void OnFontOutlineStarted()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(OnFontOutlineStarted));
                return;
            }
            FontOutlineStarted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 触发字体转曲完成事件
        /// </summary>
        protected virtual void OnFontOutlineCompleted(byte[] pdfBytes, string originalFilePath)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<byte[], string>(OnFontOutlineCompleted), pdfBytes, originalFilePath);
                return;
            }
            FontOutlineCompleted?.Invoke(this, new FontOutlineCompletedEventArgs
            {
                PdfBytes = pdfBytes,
                OriginalFilePath = originalFilePath
            });
        }

        /// <summary>
        /// 页面表格单元格点击事件
        /// </summary>
        private void PagesTable_CellClick(object sender, AntdUI.TableClickEventArgs e)
        {
            if (e.Record != null)
            {
                var pageNumber = Convert.ToInt32(e.Record.GetType().GetProperty("page")?.GetValue(e.Record));
                PageSelected?.Invoke(this, pageNumber);
            }
        }

        /// <summary>
        /// 问题表格单元格点击事件
        /// </summary>
        private void IssuesTable_CellClick(object sender, AntdUI.TableClickEventArgs e)
        {
            if (e.Record != null)
            {
                var pageStr = e.Record.GetType().GetProperty("page")?.GetValue(e.Record)?.ToString();
                if (pageStr != "全部" && int.TryParse(pageStr, out int pageNumber))
                {
                    PageSelected?.Invoke(this, pageNumber);
                }
            }
        }

        /// <summary>
        /// 切换到指定页面
        /// </summary>
        public void SwitchToPage(int pageNumber)
        {
            if (_currentInfo != null && pageNumber >= 1 && pageNumber <= _currentInfo.TotalPages)
            {
                _currentInfo.CurrentPage = pageNumber;
                _currentInfo.CurrentPageBoxes = _currentInfo.AllPageBoxes.FirstOrDefault(p => p.PageNumber == pageNumber);
                UpdateCurrentPageDisplay();
            }
        }
    }

    /// <summary>
    /// 字体转曲完成事件参数
    /// </summary>
    public class FontOutlineCompletedEventArgs : EventArgs
    {
        public byte[] PdfBytes { get; set; }
        public string OriginalFilePath { get; set; }
    }
}
