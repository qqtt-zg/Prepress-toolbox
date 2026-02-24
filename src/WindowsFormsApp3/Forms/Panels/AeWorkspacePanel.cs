using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntdUI;
using WindowsFormsApp3.Controls.Printing;
using WinFormsPanel = System.Windows.Forms.Panel;
using WinFormsTabPage = System.Windows.Forms.TabPage;
using WinFormsLabel = System.Windows.Forms.Label;
using WindowsFormsApp3.Controls;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Panels
{
    public partial class AeWorkspacePanel : UserControl
    {
        private class WorkspacePdfItem
        {
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public int Pages { get; set; }
            public string Status { get; set; }
        }

        private readonly System.Collections.Generic.List<WorkspacePdfItem> _items = new System.Collections.Generic.List<WorkspacePdfItem>();
        private BindingSource _bs;

        private SplitContainer _mainSplit;
        private SplitContainer _topSplit; // 上层：左右分割（工作区 vs 标签页）
        private SplitContainer _bottomSplit; // 下层：作业列表

        private WinFormsPanel _topBar;
        private WinFormsPanel _leftNav;
        private WinFormsPanel _workArea;
        private WinFormsPanel _center;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _lblStatus;

        private TreeView _navTree;
        private DataGridView _dgvJobs;
        private TabControl _rightTabs;
        private WinFormsTabPage _tabPreview;
        private WinFormsTabPage _tabDetails;
        private WinFormsTabPage _tabLog;
        private TabbedPdfPreviewControl _pdfPreview;
        private TextBox _txtLog;

        private bool _suppressSelectionChanged;
        private bool _layoutApplied;
        private System.Windows.Forms.Timer _layoutTimer;

        public AeWorkspacePanel()
        {
            InitializeComponent();
            BuildLayout();
            BuildTopBar();
            BuildLeftNav();
            BuildCenterJobs();
            BuildRightPanel();

            this.Load += AeWorkspacePanel_Load;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Dock = DockStyle.Fill;
            this.BackColor = DesignTokens.BgSecondary;
            this.ResumeLayout(false);
        }

        private void BuildLayout()
        {
            _topBar = new WinFormsPanel
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(8, 8, 8, 8),
                BackColor = DesignTokens.BgTertiary
            };

            _statusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom
            };
            _lblStatus = new ToolStripStatusLabel { Name = "lblStatus", Text = "就绪" };
            _statusStrip.Items.Add(_lblStatus);

            _mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                SplitterDistance = 180,
                SplitterWidth = 1,
                BackColor = DesignTokens.BgSecondary,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            _leftNav = new WinFormsPanel
            {
                Dock = DockStyle.Fill,
                BackColor = DesignTokens.BgTertiary,
                Padding = new Padding(0)
            };

            _bottomSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel2,
                SplitterWidth = 1
            };

            // 上层：左右分割（工作区 vs 标签页）
            _topSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel2,
                SplitterWidth = 2
            };

            // 默认折叠左侧空白工作区（Panel1），避免树与预览之间出现多余留白
            _topSplit.Panel1Collapsed = true;

            _workArea = new WinFormsPanel
            {
                Dock = DockStyle.Fill,
                BackColor = DesignTokens.BgPrimary,
                Padding = new Padding(0)
            };

            // 默认隐藏空白的工作区，避免左侧出现多余区域
            _workArea.Visible = false;

            _center = new WinFormsPanel
            {
                Dock = DockStyle.Fill,
                BackColor = DesignTokens.BgPrimary,
                Padding = new Padding(0)
            };

            _mainSplit.Panel1.Controls.Add(_leftNav);
            _mainSplit.Panel2.Controls.Add(_bottomSplit);

            // 下层：上=工作区(左右分割)，下=作业列表
            _bottomSplit.Panel1.Controls.Add(_topSplit);
            _bottomSplit.Panel2.Controls.Add(_center);

            // 上层：左=工作区空白区（已折叠），右=标签页（预览/详情/日志）
            // _topSplit.Panel1.Controls.Add(_workArea); // 已折叠，无需添加

            this.Controls.Add(_mainSplit);
            this.Controls.Add(_statusStrip);
            this.Controls.Add(_topBar);
        }

        private void BuildTopBar()
        {
            var btnAdd = new AntdUI.Button
            {
                Text = "添加PDF",
                Type = TTypeMini.Primary,
                IconSvg = "FolderOpenOutlined",
                Width = 110,
                Height = 32,
                Location = new Point(8, 8)
            };
            btnAdd.Click += BtnAdd_Click;

            var btnRemove = new AntdUI.Button
            {
                Text = "移除",
                Type = TTypeMini.Default,
                IconSvg = "DeleteOutlined",
                Width = 90,
                Height = 32,
                Location = new Point(btnAdd.Right + 8, 8)
            };
            btnRemove.Click += (s, e) => RemoveSelectedJob();

            var btnRun = new AntdUI.Button
            {
                Text = "运行",
                Type = TTypeMini.Primary,
                IconSvg = "PlayCircleOutlined",
                Width = 90,
                Height = 32,
                Location = new Point(btnRemove.Right + 8, 8)
            };
            btnRun.Click += (s, e) => AppendLog("运行：暂未接入执行逻辑");

            var btnStop = new AntdUI.Button
            {
                Text = "停止",
                Type = TTypeMini.Default,
                IconSvg = "PauseCircleOutlined",
                Width = 90,
                Height = 32,
                Location = new Point(btnRun.Right + 8, 8)
            };
            btnStop.Click += (s, e) => AppendLog("停止：暂未接入执行逻辑");

            _topBar.Controls.Add(btnAdd);
            _topBar.Controls.Add(btnRemove);
            _topBar.Controls.Add(btnRun);
            _topBar.Controls.Add(btnStop);
        }

        private void BuildLeftNav()
        {
            var lbl = new WinFormsLabel
            {
                Text = "工作区",
                Font = DesignTokens.FontHeading,
                ForeColor = DesignTokens.PrimaryColor,
                AutoSize = true,
                Location = new Point(8, 8)
            };

            _navTree = new TreeView
            {
                Dock = DockStyle.Fill,
                Location = new Point(0, 0)
            };

            var rootJobs = new TreeNode("作业")
            {
                Nodes =
                {
                    new TreeNode("队列"),
                    new TreeNode("完成"),
                    new TreeNode("失败")
                }
            };
            var rootTools = new TreeNode("工具")
            {
                Nodes =
                {
                    new TreeNode("拼版"),
                    new TreeNode("印前标记")
                }
            };
            _navTree.Nodes.Add(rootJobs);
            _navTree.Nodes.Add(rootTools);
            _navTree.ExpandAll();

            var host = new WinFormsPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 36, 0, 0) };
            host.Controls.Add(_navTree);

            _leftNav.Controls.Add(host);
            _leftNav.Controls.Add(lbl);
        }

        private void BuildCenterJobs()
        {
            var lbl = new WinFormsLabel
            {
                Text = "作业列表",
                Font = DesignTokens.FontHeading,
                ForeColor = DesignTokens.TextPrimary,
                AutoSize = true,
                Location = new Point(8, 8)
            };

            _bs = new BindingSource();
            _bs.DataSource = _items;

            _dgvJobs = new DataGridView
            {
                Dock = DockStyle.Fill,
                Top = 36,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoGenerateColumns = false,
                BackgroundColor = DesignTokens.BgPrimary,
                BorderStyle = BorderStyle.None,
                DataSource = _bs
            };

            _dgvJobs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(WorkspacePdfItem.FileName),
                HeaderText = "文件",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 140
            });
            _dgvJobs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(WorkspacePdfItem.Pages),
                HeaderText = "页数",
                Width = 60
            });
            _dgvJobs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(WorkspacePdfItem.Status),
                HeaderText = "状态",
                Width = 90
            });

            _dgvJobs.SelectionChanged += DgvJobs_SelectionChanged;
            _dgvJobs.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) { RemoveSelectedJob(); e.Handled = true; } };

            _dgvJobs.AllowDrop = true;
            _dgvJobs.DragEnter += (s, e) =>
            {
                if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            };
            _dgvJobs.DragDrop += (s, e) =>
            {
                try
                {
                    if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;
                    var dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (dropped == null || dropped.Length == 0) return;

                    var pdfs = dropped
                        .SelectMany(p => Directory.Exists(p)
                            ? SafeGetPdfFiles(p)
                            : (File.Exists(p) && string.Equals(Path.GetExtension(p), ".pdf", StringComparison.OrdinalIgnoreCase) ? new[] { p } : Array.Empty<string>()))
                        .ToArray();

                    if (pdfs.Length > 0) _ = AddPdfFilesAsync(pdfs);
                }
                catch (Exception ex)
                {
                    AppendLog($"拖拽导入失败: {ex.Message}");
                }
            };

            var host = new WinFormsPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 36, 0, 0) };
            host.Controls.Add(_dgvJobs);

            _center.Controls.Add(host);
            _center.Controls.Add(lbl);
        }

        private void BuildRightPanel()
        {
            _rightTabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Alignment = TabAlignment.Top,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(80, 24)
            };

            _tabPreview = new WinFormsTabPage("预览");
            _tabDetails = new WinFormsTabPage("详情");
            _tabLog = new WinFormsTabPage("日志");

            _pdfPreview = new TabbedPdfPreviewControl { Dock = DockStyle.Fill };
            _pdfPreview.TabChanged += (s, e) => SelectJobByPath(e.FilePath);
            _tabPreview.Controls.Add(_pdfPreview);

            var detailsLabel = new WinFormsLabel
            {
                Text = "作业详情（待接入）",
                AutoSize = true,
                Location = new Point(12, 12),
                ForeColor = DesignTokens.TextSecondary
            };
            _tabDetails.Controls.Add(detailsLabel);

            _txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true
            };
            _tabLog.Controls.Add(_txtLog);

            _rightTabs.TabPages.Add(_tabPreview);
            _rightTabs.TabPages.Add(_tabDetails);
            _rightTabs.TabPages.Add(_tabLog);

            _topSplit.Panel2.Controls.Clear();
            _topSplit.Panel2.Controls.Add(_rightTabs);
        }

        private void AeWorkspacePanel_Load(object sender, EventArgs e)
        {
            UpdateStatus("就绪");
            StartLayoutStabilizer();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible && IsHandleCreated && !IsDisposed)
            {
                StartLayoutStabilizer();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            StartLayoutStabilizer();
        }

        private void StartLayoutStabilizer()
        {
            if (!this.Visible || IsDisposed)
                return;

            if (_layoutApplied)
                return;

            if (_layoutTimer == null)
            {
                _layoutTimer = new System.Windows.Forms.Timer();
                _layoutTimer.Interval = 120;
                _layoutTimer.Tick += (s, e) =>
                {
                    try
                    {
                        // 每次tick都输出关键信息，便于从日志定位为何不生效
                        LogHelper.Info($"[AeWorkspacePanel][Layout] tick: Visible={this.Visible}, Size={this.Width}x{this.Height}");
                        LogHelper.Info($"[AeWorkspacePanel][Layout] bottom: H={_bottomSplit?.Height}, SD={_bottomSplit?.SplitterDistance}, P1Min={_bottomSplit?.Panel1MinSize}, P2Min={_bottomSplit?.Panel2MinSize}");
                        LogHelper.Info($"[AeWorkspacePanel][Layout] top: W={_topSplit?.Width}, SD={_topSplit?.SplitterDistance}, P1Min={_topSplit?.Panel1MinSize}, P2Min={_topSplit?.Panel2MinSize}");

                        ApplyDefaultSplitterLayout();

                        if (_layoutApplied)
                        {
                            LogHelper.Info("[AeWorkspacePanel][Layout] applied -> stop timer");
                            _layoutTimer.Stop();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error("[AeWorkspacePanel][Layout] tick failed", ex);
                        _layoutTimer.Stop();
                    }
                };
            }

            if (!_layoutTimer.Enabled)
            {
                LogHelper.Info("[AeWorkspacePanel][Layout] start timer");
                _layoutTimer.Start();
            }
        }

        private void ApplyDefaultSplitterLayout()
        {
            try
            {
                if (_layoutApplied)
                {
                    LogHelper.Info("[AeWorkspacePanel][Layout] already applied, skip");
                    return;
                }

                if (_bottomSplit == null || _topSplit == null)
                {
                    LogHelper.Warn("[AeWorkspacePanel][Layout] SplitContainers not ready");
                    return;
                }

                int bottomAvailable = _bottomSplit.Height;
                int topAvailable = _topSplit.Width;

                LogHelper.Info($"[AeWorkspacePanel][Layout] bottomAvailable={bottomAvailable}, topAvailable={topAvailable}");

                // 设置最小尺寸（防止被挤压）
                _bottomSplit.Panel1MinSize = 200;
                _bottomSplit.Panel2MinSize = 180;
                _topSplit.Panel1MinSize = 200;
                _topSplit.Panel2MinSize = 360;

                // 底部作业列表目标高度 240
                int bottomDesiredPanel2 = 240;
                int bottomMin = _bottomSplit.Panel1MinSize;
                int bottomMax = bottomAvailable - _bottomSplit.Panel2MinSize;
                int bottomDesired = bottomAvailable - bottomDesiredPanel2;
                int bottomClamped = Math.Max(bottomMin, Math.Min(bottomDesired, bottomMax));

                LogHelper.Info($"[AeWorkspacePanel][Layout] bottom: desired={bottomDesiredPanel2}, min={bottomMin}, max={bottomMax}, clamped={bottomClamped}");

                if (bottomClamped != _bottomSplit.SplitterDistance)
                {
                    LogHelper.Info($"[AeWorkspacePanel][Layout] set bottomSplit.SplitterDistance={bottomClamped}");
                    _bottomSplit.SplitterDistance = bottomClamped;
                }

                // 右上标签页目标宽度 420
                int topDesiredPanel2 = 420;
                int topMin = _topSplit.Panel1MinSize;
                int topMax = topAvailable - _topSplit.Panel2MinSize;
                int topDesired = topAvailable - topDesiredPanel2;
                int topClamped = Math.Max(topMin, Math.Min(topDesired, topMax));

                LogHelper.Info($"[AeWorkspacePanel][Layout] top: desired={topDesiredPanel2}, min={topMin}, max={topMax}, clamped={topClamped}");

                if (topClamped != _topSplit.SplitterDistance)
                {
                    LogHelper.Info($"[AeWorkspacePanel][Layout] set topSplit.SplitterDistance={topClamped}");
                    _topSplit.SplitterDistance = topClamped;
                }

                _layoutApplied = true;
                LogHelper.Info("[AeWorkspacePanel][Layout] layoutApplied=true");
            }
            catch (Exception ex)
            {
                LogHelper.Error("[AeWorkspacePanel][Layout] ApplyDefaultSplitterLayout failed", ex);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "PDF Files|*.pdf";
                ofd.Multiselect = true;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _ = AddPdfFilesAsync(ofd.FileNames);
                }
            }
        }

        private async Task AddPdfFilesAsync(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;

            UpdateStatus($"正在加载 {paths.Length} 个文件...");

            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                if (_items.Any(x => string.Equals(x.FilePath, path, StringComparison.OrdinalIgnoreCase))) continue;

                var item = new WorkspacePdfItem
                {
                    FilePath = path,
                    FileName = Path.GetFileName(path),
                    Pages = -1,
                    Status = "加载中..."
                };
                _items.Add(item);
                _bs.ResetBindings(false);

                var tabIndex = await _pdfPreview.AddTabAsync(path);
                if (tabIndex >= 0)
                {
                    item.Pages = await GetPdfPageCountAsync(path);
                    item.Status = "就绪";
                }
                else
                {
                    item.Status = "加载失败";
                }

                _bs.ResetBindings(false);
            }

            UpdateStatus($"已加载 {paths.Length} 个文件");
        }

        private async Task<int> GetPdfPageCountAsync(string pdfPath)
        {
            try
            {
                using (var reader = new iText.Kernel.Pdf.PdfReader(pdfPath))
                using (var doc = new iText.Kernel.Pdf.PdfDocument(reader))
                {
                    return doc.GetNumberOfPages();
                }
            }
            catch
            {
                return -1;
            }
        }

        private void DgvJobs_SelectionChanged(object sender, EventArgs e)
        {
            if (_suppressSelectionChanged) return;

            try
            {
                if (_dgvJobs.SelectedRows == null || _dgvJobs.SelectedRows.Count == 0) return;
                if (!(_dgvJobs.SelectedRows[0].DataBoundItem is WorkspacePdfItem item)) return;

                _ = _pdfPreview.AddTabAsync(item.FilePath);
                AppendLog($"选中作业: {item.FileName}");
            }
            catch (Exception ex)
            {
                AppendLog($"SelectionChanged失败: {ex.Message}");
            }
        }

        private void SelectJobByPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || _dgvJobs == null) return;

            try
            {
                _suppressSelectionChanged = true;
                foreach (DataGridViewRow row in _dgvJobs.Rows)
                {
                    if (row.DataBoundItem is WorkspacePdfItem item &&
                        string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                    {
                        row.Selected = true;
                        _dgvJobs.CurrentCell = row.Cells[0];
                        break;
                    }
                }
            }
            finally
            {
                _suppressSelectionChanged = false;
            }
        }

        private void RemoveSelectedJob()
        {
            if (_dgvJobs.SelectedRows == null || _dgvJobs.SelectedRows.Count == 0) return;
            if (!(_dgvJobs.SelectedRows[0].DataBoundItem is WorkspacePdfItem item)) return;

            _items.Remove(item);
            _bs.ResetBindings(false);
            UpdateStatus($"已移除: {item.FileName}");
        }

        private void UpdateStatus(string text)
        {
            if (_lblStatus != null) _lblStatus.Text = text;
        }

        private void AppendLog(string line)
        {
            try
            {
                if (_txtLog == null) return;
                _txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
            }
            catch
            {
            }
        }

        private static string[] SafeGetPdfFiles(string dir)
        {
            try
            {
                return Directory.GetFiles(dir, "*.pdf", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}
