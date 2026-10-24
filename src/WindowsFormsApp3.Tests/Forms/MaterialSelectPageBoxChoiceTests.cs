using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp3.Forms.Controls;
using WindowsFormsApp3.Forms.Panels;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Presenters;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;
using Xunit;

namespace WindowsFormsApp3.Tests.Forms
{
    public class MaterialSelectPageBoxChoiceTests : IDisposable
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string pathName);

        private readonly string _testRoot;

        public MaterialSelectPageBoxChoiceTests()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "MaterialPageBoxChoice_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testRoot);
            Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", _testRoot);
            AppSettings.ResetForTesting();
            AppSettings.Initialize(new FileLogger(Path.Combine(_testRoot, "test.log")));
            AppSettings.MaterialFormPreviewExpanded = false;
        }

        [StaFact]
        public void OutsideCropBox_ShowsChoiceBetweenImpositionAndPresets()
        {
            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET" },
                fileName: @"C:\test\source.pdf",
                regexResult: "source",
                opacity: 1.0,
                width: "80",
                height: "44",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "1"))
            {
                form.Show();
                Application.DoEvents();
                var station = form.Controls["pnlRightStation"];
                var content = station.Controls["pnlStationContent"];
                var choice = content.Controls["pnlPageBoxChoiceCard"];
                Assert.NotNull(choice);
                Assert.False(choice.Visible);

                var info = new PdfInspectorInfo();
                info.AllPageBoxes.Add(new PageBoxInfo
                {
                    PageNumber = 1,
                    MediaBox = new BoxDimension { IsDefined = true, Left = 0, Bottom = 0, Right = 226.7713, Top = 124.7243 },
                    CropBox = new BoxDimension { IsDefined = true, Left = -0.1273, Bottom = -0.2726, Right = 226.8993, Top = 124.9973 }
                });
                info.Issues.Add(new PageBoxIssue
                {
                    PageNumber = 1,
                    Type = IssueType.OutOfBounds,
                    Severity = IssueSeverity.Warning,
                    BoxType = "CropBox"
                });

                form.UpdatePageBoxChoiceFromInspection(info);
                Application.DoEvents();

                Assert.True(choice.Visible);
                Assert.True(content.Controls["pnlImpositionDashboardCard"].Bottom <= choice.Top);
                Assert.True(choice.Bottom <= content.Controls["presetBrowserCard"].Top);
                Assert.Equal(PdfPageBoxSource.MediaBox, form.SelectedPageBoxSource);
                Assert.Equal(26, choice.Height);
                Assert.Contains("80×44", choice.Controls["btnMediaBoxChoice"].Text);
                Assert.Contains("80.1×44.2", choice.Controls["btnCropBoxChoice"].Text);
                Assert.Equal(choice.Controls["btnMediaBoxChoice"].Top, choice.Controls["btnCropBoxChoice"].Top);
                Assert.True(choice.Controls["btnMediaBoxChoice"].Right < choice.Controls["btnCropBoxChoice"].Left);
                typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(choice.Controls["btnPageBoxDetails"], new object[] { EventArgs.Empty });
                var details = (ToolStripDropDown)typeof(MaterialSelectFormModern)
                    .GetField("_pageBoxDetailsDropDown", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                Assert.True(details.Visible);
                var detailContent = ((ToolStripControlHost)details.Items[0]).Control;
                Assert.Contains("79.9999", detailContent.Controls["lblMediaBoxDetails"].Text);
                Assert.Contains("80.0899", detailContent.Controls["lblCropBoxDetails"].Text);
                details.Close();

                typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(choice.Controls["btnCropBoxChoice"], new object[] { EventArgs.Empty });
                Assert.Equal(PdfPageBoxSource.CropBox, form.SelectedPageBoxSource);
                Assert.Equal(1, form.SelectedPageBoxReferencePage);

                form.UpdatePageBoxChoiceFromInspection(new PdfInspectorInfo());
                Assert.False(choice.Visible);
                Assert.Equal(PdfPageBoxSource.Automatic, form.SelectedPageBoxSource);
            }
        }

        [StaTheory]
        [InlineData(0, 1.6)]
        [InlineData(1.6, 0)]
        [InlineData(0.2, -1)]
        public void BoxMismatch_ShowsChoiceAndUpdatesDimensionSource(float widthDifference, float heightDifference)
        {
            string file = Path.Combine(_testRoot, "different-frames.pdf");
            using (var doc = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfWriter(file)))
            {
                var page = doc.AddNewPage(new iText.Kernel.Geom.PageSize(280, 140));
                page.SetCropBox(heightDifference < 0
                    ? new iText.Kernel.Geom.Rectangle(widthDifference, 0, 280 - widthDifference, 140)
                    : new iText.Kernel.Geom.Rectangle(0, 0, 280 - widthDifference, 140 - heightDifference));
            }
            var inspection = new PdfInspectorService().InspectPdf(file);
            using (var form = new MaterialSelectFormModern(new List<string> { "PET" },
                @"C:\test\source.pdf", "source", 1, "80", "44", null, -1, -1, -1, -1, "1"))
            {
                form.Show();
                form.UpdatePageBoxChoiceFromInspection(inspection);
                Application.DoEvents();
                var choice = form.Controls["pnlRightStation"].Controls["pnlStationContent"].Controls["pnlPageBoxChoiceCard"];
                Assert.True(choice.Visible);
                Assert.Contains(heightDifference < 0 ? "位置差" : "框不同", choice.Controls["lblPageBoxChoiceTitle"].Text);
                Assert.Equal(PdfPageBoxSource.CropBox, form.SelectedPageBoxSource);
                var heightField = typeof(MaterialSelectFormModern).GetField("_initialHeight", BindingFlags.Instance | BindingFlags.NonPublic);
                var widthField = typeof(MaterialSelectFormModern).GetField("_initialWidth", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.Equal(inspection.AllPageBoxes[0].CropBox.Width * 25.4 / 72, (double)widthField.GetValue(form));
                Assert.Equal(inspection.AllPageBoxes[0].CropBox.Height * 25.4 / 72, (double)heightField.GetValue(form));
                typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(choice.Controls["btnMediaBoxChoice"], new object[] { EventArgs.Empty });
                Assert.Equal(PdfPageBoxSource.MediaBox, form.SelectedPageBoxSource);
                Assert.Equal(inspection.AllPageBoxes[0].MediaBox.Width * 25.4 / 72, (double)widthField.GetValue(form));
                Assert.Equal(inspection.AllPageBoxes[0].MediaBox.Height * 25.4 / 72, (double)heightField.GetValue(form));
            }
        }

        [WinFormsFact]
        public async Task ScrolledBatchRefresh_ShouldPreserveLogicalCardOriginAndScrollExtent()
        {
            using (var form = new MaterialSelectFormModern(new List<string> { "PET" },
                "", "滚动布局", 1, "80", "45", null, -1, -1, -1, -1, "1"))
            {
                form.SetPendingFiles(Enumerable.Range(1, 24).Select(index => Path.Combine(_testRoot, $"滚动{index}.pdf")));
                form.Location = new Point(-4000, -4000);
                form.Show();
                form.ToggleBatchFileListPanel(true);
                await form.LoadBatchPageBoxChoicesAsync(form.BatchFileItems);
                Application.DoEvents();
                var viewport = (Panel)form.Controls.Find("pnlCardsContainer", true).Single();
                var card = viewport.Controls.OfType<Panel>().Single();
                Assert.True(viewport.VerticalScroll.Visible);
                int originalExtent = viewport.DisplayRectangle.Height;

                for (int i = 0; i < 3; i++)
                {
                    viewport.AutoScrollPosition = new Point(0, 100);
                    int scrollY = viewport.AutoScrollPosition.Y;
                    Assert.True(scrollY < 0);
                    if (i == 1) form.RebuildProcessGroups();
                    else form.RenderGroupCards();
                    Application.DoEvents();
                    if (i == 1) card = viewport.Controls.OfType<Panel>().Single();
                    else Assert.Same(card, viewport.Controls.OfType<Panel>().Single());
                    Assert.Equal(scrollY, viewport.AutoScrollPosition.Y);
                    // 子控件屏幕坐标必须扣除滚动偏移；逻辑原点始终只有 4px 顶部留白。
                    Assert.Equal(4, card.Top - viewport.AutoScrollPosition.Y);
                    Assert.Equal(originalExtent, viewport.DisplayRectangle.Height);

                    viewport.AutoScrollPosition = Point.Empty;
                    Assert.Equal(4, card.Top);
                }
            }
        }

        [WinFormsFact]
        public async Task ScrolledBatchGroups_ShouldKeepSpacingWhenRowsAreAddedAndGroupsCollapse()
        {
            using (var form = new MaterialSelectFormModern(new List<string> { "PET" },
                "", "分组滚动", 1, "80", "45", null, -1, -1, -1, -1, "1"))
            {
                form.SetPendingFiles(Enumerable.Range(1, 24).Select(index => Path.Combine(_testRoot, $"分组{index}.pdf")));
                var first = form.ProcessGroups.Single();
                var second = new BatchProcessGroup { GroupName = "第二组", Items = first.Items.Skip(16).ToList() };
                first.Items.RemoveRange(16, 8);
                form.ProcessGroups.Add(second);
                form.RenderGroupCards();
                form.Location = new Point(-4000, -4000);
                form.Show();
                form.ToggleBatchFileListPanel(true);
                await form.LoadBatchPageBoxChoicesAsync(form.BatchFileItems);
                Application.DoEvents();
                var viewport = (Panel)form.Controls.Find("pnlCardsContainer", true).Single();
                viewport.AutoScrollPosition = new Point(0, 100);
                form.RenderGroupCards();
                var cards = viewport.Controls.OfType<Panel>().ToList();
                Assert.Equal(4, cards[0].Top - viewport.AutoScrollPosition.Y);
                Assert.Equal(10, cards[1].Top - cards[0].Bottom);

                int previousExtent = viewport.DisplayRectangle.Height;
                form.AppendPendingFile(Path.Combine(_testRoot, "新增行.pdf"));
                await form.LoadBatchPageBoxChoicesAsync(form.BatchFileItems);
                Application.DoEvents();
                Assert.Equal(previousExtent + 26, viewport.DisplayRectangle.Height);
                Assert.Equal(4, cards[0].Top - viewport.AutoScrollPosition.Y);
                Assert.Equal(10, cards[1].Top - cards[0].Bottom);

                first.IsCollapsed = true;
                form.RenderGroupCards();
                Application.DoEvents();
                Assert.False(viewport.VerticalScroll.Visible);
                Assert.Equal(Point.Empty, viewport.AutoScrollPosition);
                Assert.Equal(4, cards[0].Top);
                Assert.Equal(10, cards[1].Top - cards[0].Bottom);

                first.IsCollapsed = false;
                form.RenderGroupCards();
                Application.DoEvents();
                Assert.True(viewport.VerticalScroll.Visible);
                Assert.Equal(4, cards[0].Top - viewport.AutoScrollPosition.Y);
                Assert.Equal(10, cards[1].Top - cards[0].Bottom);
            }
        }

        [WinFormsFact]
        public async Task PreviewData_ShouldSimulateProgressUsingRealTemporaryPdfsAndCleanUp()
        {
            string directory;
            using (var data = new MaterialSelectionPreviewData())
            using (var form = new MaterialSelectFormModern(new List<string> { "PET" },
                data.FilePaths[0], "加载演示", 1, "80", "45", null, -1, -1, -1, -1, "1"))
            {
                directory = Path.GetDirectoryName(data.FilePaths[0]);
                Assert.All(data.FilePaths, path => Assert.True(File.Exists(path)));
                Assert.Equal(0, form.PreviewMetadataDelayMilliseconds);
                form.PreviewMetadataDelayMilliseconds = 200;
                form.SetPendingFiles(data.FilePaths);
                var progress = (AntdUI.Progress)form.Controls.Find("batchMetadataProgressBar", true).Single();
                var label = (AntdUI.Label)form.Controls.Find("batchMetadataProgressLabel", true).Single();
                var loading = form.LoadBatchPageBoxChoicesAsync(form.BatchFileItems);
                Assert.Equal(0F, progress.Value);
                while (progress.Value == 0 && !loading.IsCompleted) await Task.Delay(20);
                Assert.InRange(progress.Value, 0.25F, 0.75F);
                await loading;
                Assert.Equal(1F, progress.Value);
                Assert.Equal("解析结束：完成 4，失败 0", label.Text);
                Assert.Equal(new int?[] { 1, 2, 3, 4 }, form.BatchFileItems.Select(item => item.PageCount));
                Assert.NotNull(form.BatchFileItems[1].PageBoxChoicePage);
            }
            Assert.False(Directory.Exists(directory));
        }

        [WinFormsFact]
        public async Task Startup_WhenCurrentFileIsMissing_ShouldFinishBatchAndRevealList()
        {
            string missing = Path.Combine(_testRoot, "不存在的当前文件.pdf");
            string valid = CreateBatchPdf("仍可读取的批次文件.pdf", false);
            using (var form = new MaterialSelectFormModern(new List<string> { "PET" },
                missing, "启动", 1, "80", "45", null, -1, -1, -1, -1, "1"))
            {
                form.SetPendingFiles(new[] { missing, valid, Path.Combine(_testRoot, "缺失B.pdf"), Path.Combine(_testRoot, "缺失C.pdf") });
                form.Show();
                form.ToggleBatchFileListPanel(true);
                var progress = form.Controls.Find("batchMetadataProgressPanel", true).Single();
                var label = (AntdUI.Label)form.Controls.Find("batchMetadataProgressLabel", true).Single();
                for (int i = 0; i < 40 && label.Text != "解析结束：完成 1，失败 3"; i++) await Task.Delay(100);
                Assert.Equal("解析结束：完成 1，失败 3", label.Text);
                Assert.False(progress.Visible);
                Assert.Equal(PdfPageBoxSource.Automatic, form.BatchFileItems.Single(item => item.FilePath == valid).PageBoxSource);
                Assert.Equal(4, GetGroupGrid(form).Rows.Count);
            }
        }

        [WinFormsFact]
        public async Task BatchProgress_ShouldReplaceSpinnerAndHideImmediatelyWhenLoadingEnds()
        {
            string valid = CreateBatchPdf("进度 有效.pdf", false);
            string missing = Path.Combine(_testRoot, "进度 未写完.pdf");
            using (var form = new MaterialSelectFormModern(new List<string> { "PET" },
                "", "进度", 1, "30", "20", null, -1, -1, -1, -1, "1"))
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { valid, valid.ToUpperInvariant(), missing });
                var label = (AntdUI.Label)form.Controls.Find("batchMetadataProgressLabel", true).Single();
                var progress = (AntdUI.Progress)form.Controls.Find("batchMetadataProgressBar", true).Single();
                var panel = form.Controls.Find("batchMetadataProgressPanel", true).Single();
                var header = (Control)typeof(MaterialSelectFormModern)
                    .GetField("pnlTopBatchHeader", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                Assert.Equal("正在解析 0 / 2 个文件", label.Text);
                Assert.Equal(0F, progress.Value);
                Assert.Equal(26, header.Height);
                Assert.Equal(DockStyle.Fill, panel.Dock);
                Assert.Equal("pnlFileList", panel.Parent.Name);
                Assert.Empty(form.Controls.Find("_spinBatchLoading", true));

                await form.LoadBatchPageBoxChoicesAsync(form.BatchFileItems);
                Assert.Equal("解析结束：完成 1，失败 1", label.Text);
                Assert.Equal(1F, progress.Value);
                form.Show();
                form.ToggleBatchFileListPanel(true);
                await form.LoadBatchPageBoxChoicesAsync(form.BatchFileItems);
                Assert.False(panel.Visible);
                Assert.Equal(26, header.Height);
                form.ShowBatchLoading("正在刷新工单...");
                Assert.True(panel.Visible);
                Assert.Equal("正在刷新工单...", label.Text);
                form.HideBatchLoading();
                Assert.False(panel.Visible);

                string added = CreateBatchPdf("进度 新文件.pdf", false);
                form.AppendPendingFile(added);
                await form.LoadBatchPageBoxChoicesAsync(form.BatchFileItems);
                Assert.Equal("解析结束：完成 2，失败 1", label.Text);
                Assert.Equal(1F, progress.Value);
                Assert.Equal(26, header.Height);
                Assert.False(panel.Visible);

                // 原先失败的文件写入完成后，重试将失败数归零，仍只统计三个文件。
                File.Copy(valid, missing);
                await form.LoadBatchPageBoxChoicesAsync(form.BatchFileItems);
                Assert.Equal("解析结束：完成 3，失败 0", label.Text);
                Assert.Equal(1F, progress.Value);
            }
        }

        [WinFormsFact]
        public async Task InitialBatch_ShouldDeferPdfReadsUntilBackgroundLoadAndKeepGrid()
        {
            var paths = Enumerable.Range(0, 24).Select(index => CreateBatchPdf($"导入{index}_30x20.pdf", false)).ToArray();
            using (var form = new MaterialSelectFormModern(new List<string> { "PET" },
                paths[0], "导入", 1, "30", "20", null, -1, -1, -1, -1, "1"))
            {
                form.SetPendingFiles(paths);
                Assert.Equal(24, form.BatchFileItems.Count);
                Assert.All(form.BatchFileItems, item =>
                {
                    Assert.Null(item.PageCount);
                    Assert.Null(item.PageBoxSource);
                    Assert.Equal(30, item.RawPdfWidth);
                });
                var cache = (System.Collections.IDictionary)typeof(MaterialSelectFormModern)
                    .GetField("_batchPageBoxInspections", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                Assert.Empty(cache.Keys.Cast<object>());
                var grid = GetGroupGrid(form);
                await form.LoadBatchPageBoxChoicesAsync(form.BatchFileItems);
                Assert.Same(grid, GetGroupGrid(form));
                Assert.All(form.BatchFileItems, item =>
                {
                    Assert.Equal(1, item.PageCount);
                    Assert.Equal(PdfPageBoxSource.Automatic, item.PageBoxSource);
                    Assert.Equal(98.78, item.RawPdfWidth, 2);
                });
            }
        }

        [WinFormsFact]
        public void MonitoredArrivals_ShouldMergeDeduplicateAndAppendWithoutReplacingGrid()
        {
            using (var form = new MaterialSelectFormModern(new List<string> { "PET" },
                "", "导入", 1, "30", "20", null, -1, -1, -1, -1, "1"))
            {
                form.SetPendingFiles(new[] { @"C:\test\起始.pdf" });
                var grid = GetGroupGrid(form);
                for (int i = 0; i < 100; i++)
                {
                    form.QueuePendingBatchFile($@"C:\test\新增{i}.pdf");
                    form.QueuePendingBatchFile($@"C:\test\新增{i}.pdf");
                }
                Assert.Single(form.BatchFileItems);
                form.FlushPendingBatchFiles();
                Assert.Equal(101, form.BatchFileItems.Count);
                Assert.Same(grid, GetGroupGrid(form));
                Assert.Equal(101, ((System.ComponentModel.BindingList<BatchFileItem>)grid.DataSource).Count);
                Assert.Equal(@"C:\test\新增0.pdf", form.BatchFileItems[1].FilePath);
                Assert.Equal(@"C:\test\新增99.pdf", form.BatchFileItems[100].FilePath);
            }
        }

        [WinFormsFact]
        public async Task MissingPageCounts_ShouldRetryThreeTimesAndShareReadsForDuplicatePaths()
        {
            string path = Path.Combine(_testRoot, "尚未就绪.pdf");
            using (var form = new MaterialSelectFormModern())
            {
                var items = (System.ComponentModel.BindingList<BatchFileItem>)typeof(MaterialSelectFormModern)
                    .GetField("_batchItems", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                items.Add(new BatchFileItem { FilePath = path });
                items.Add(new BatchFileItem { FilePath = path.ToUpperInvariant() });
                items.Add(new BatchFileItem { FilePath = Path.Combine(_testRoot, "已加载.pdf"), PageCount = 5 });
                items.Add(new BatchFileItem { FilePath = Path.Combine(_testRoot, "普通图片.png") });
                int reads = 0;
                int uiThread = Environment.CurrentManagedThreadId;
                Func<string, int?> reader = filePath =>
                {
                    Assert.NotEqual(uiThread, Environment.CurrentManagedThreadId);
                    Assert.Equal(path, filePath);
                    reads++;
                    return reads == 1 ? 0 : (int?)null;
                };
                for (int i = 0; i < 5; i++) await form.RetryMissingBatchPageCountsAsync(reader);
                Assert.Equal(3, reads);
                Assert.Null(items[0].PageCount);
                Assert.Null(items[1].PageCount);
                Assert.Equal(5, items[2].PageCount);
            }
        }

        [WinFormsFact]
        public async Task MissingPageCounts_ShouldRetryAutomaticallyAndKeepQuantityEditor()
        {
            Assert.True(SetDllDirectory(Path.Combine(AppContext.BaseDirectory, "x64")));
            string path = CreateBatchPdf("后台补页数.pdf", true);
            using (var form = new MaterialSelectFormModern(new List<string> { "PET" },
                path, "补页数", 1, "98", "49", null, -1, -1, -1, -1, "1"))
            {
                form.SetPendingFiles(new[] { path });
                form.Show();
                var imposition = (AntdUI.Checkbox)typeof(MaterialSelectFormModern)
                    .GetField("enableImpositionCheckbox", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                imposition.Checked = false;
                form.ToggleBatchFileListPanel(true);
                await form.LoadBatchPageBoxChoicesAsync(form.BatchFileItems);
                Application.DoEvents();
                var item = form.BatchFileItems.Single();
                var grid = GetGroupGrid(form);
                int row = FindRow(grid, item);
                int quantityColumn = grid.Columns.Cast<DataGridViewColumn>()
                    .Single(column => column.DataPropertyName == nameof(BatchFileItem.Quantity)).Index;
                int pageColumn = grid.Columns.Cast<DataGridViewColumn>()
                    .Single(column => column.DataPropertyName == nameof(BatchFileItem.PageCount)).Index;
                grid.Focus();
                grid.CurrentCell = grid.Rows[row].Cells[quantityColumn];
                grid.BeginEdit(true);
                var editor = Assert.IsType<DataGridViewTextBoxEditingControl>(grid.EditingControl);
                editor.Text = "1234";
                editor.SelectionStart = 1;
                editor.SelectionLength = 2;
                item.PageCount = null;
                item.PageBoxSource = null;
                ((System.Collections.IDictionary)typeof(MaterialSelectFormModern)
                    .GetField("_batchPageBoxInspections", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form)).Clear();
                typeof(MaterialSelectFormModern).GetMethod("ScheduleMissingBatchPageCounts", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(form, null);
                for (int i = 0; i < 50 && (!item.PageCount.HasValue || !item.PageBoxSource.HasValue); i++) await Task.Delay(100);
                // 加载进度立即收起时也不能抢走正在编辑的数量光标。
                Application.DoEvents();

                Assert.Equal(1, item.PageCount);
                Assert.Equal(1, grid.Rows[row].Cells[pageColumn].Value);
                Assert.Same(grid, GetGroupGrid(form));
                Assert.True(grid.IsCurrentCellInEditMode);
                Assert.Same(editor, grid.EditingControl);
                Assert.True(editor.Focused);
                Assert.Equal("1234", editor.Text);
                Assert.Equal(1, editor.SelectionStart);
                Assert.Equal(2, editor.SelectionLength);
                grid.EndEdit();
                Assert.Equal("1234", item.Quantity);
            }
        }

        [WinFormsFact]
        public async Task PreviewSuccess_ShouldCompleteOnlyMatchingMissingRowsAndStopRetryingThem()
        {
            Assert.True(SetDllDirectory(Path.Combine(AppContext.BaseDirectory, "x64")));
            string path = CreateBatchPdf("预览补页数.pdf", false);
            using (var form = new MaterialSelectFormModern(new List<string> { "PET" },
                path, "补页数", 1, "98", "49", null, -1, -1, -1, -1, "1"))
            {
                var items = (System.ComponentModel.BindingList<BatchFileItem>)typeof(MaterialSelectFormModern)
                    .GetField("_batchItems", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                items.Clear();
                var first = new BatchFileItem { FilePath = path };
                var duplicate = new BatchFileItem { FilePath = path.ToUpperInvariant() };
                var valid = new BatchFileItem { FilePath = path, PageCount = 7 };
                var other = new BatchFileItem { FilePath = Path.Combine(_testRoot, "其他.pdf") };
                items.Add(first);
                items.Add(duplicate);
                items.Add(valid);
                items.Add(other);

                // 即使自动补读已耗尽，之后预览成功仍应恢复对应行的页数。
                for (int i = 0; i < 3; i++) await form.RetryMissingBatchPageCountsAsync(filePath => null);
                await form.LoadPdfPreviewAsync(path);
                Assert.Equal(1, first.PageCount);
                Assert.Equal(1, duplicate.PageCount);
                Assert.Equal(7, valid.PageCount);
                Assert.Null(other.PageCount);
                var added = new BatchFileItem { FilePath = Path.Combine(_testRoot, "新进入.pdf") };
                items.Add(added);
                int reads = 0;
                await form.RetryMissingBatchPageCountsAsync(filePath =>
                {
                    Assert.Equal(added.FilePath, filePath);
                    reads++;
                    return null;
                });
                Assert.Equal(1, reads);
            }
        }

        [WinFormsFact]
        public async Task BatchQuantityClick_WithPageBoxChoices_ShouldKeepGridSelectionAndEditor()
        {
            string different = CreateBatchPdf("数量编辑 框不一致.pdf", true);
            string normal = CreateBatchPdf("数量编辑 普通文件.pdf", false);
            using (var form = new MaterialSelectFormModern(new List<string> { "PET" },
                different, "数量编辑", 1, "98", "49", null, -1, -1, -1, -1, "1"))
            {
                form.SetPendingFiles(new[] { different, normal });
                form.Show();
                var imposition = (AntdUI.Checkbox)typeof(MaterialSelectFormModern)
                    .GetField("enableImpositionCheckbox", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                imposition.Checked = false;
                form.ToggleBatchFileListPanel(true);
                await form.LoadBatchPageBoxChoicesAsync(form.BatchFileItems);
                Application.DoEvents();

                // 同时检查普通行和带尺寸按钮的行，数量编辑不应重建表格。
                foreach (string path in new[] { normal, different, normal, different })
                {
                    var item = form.BatchFileItems.Single(candidate => candidate.FilePath == path);
                    var grid = GetGroupGrid(form);
                    int rowIndex = FindRow(grid, item);
                    int columnIndex = grid.Columns.Cast<DataGridViewColumn>()
                        .Single(column => column.DataPropertyName == nameof(BatchFileItem.Quantity)).Index;
                    var cell = grid.Rows[rowIndex].Cells[columnIndex];
                    grid.Focus();
                    grid.CurrentCell = cell;
                    var click = new DataGridViewCellEventArgs(columnIndex, rowIndex);
                    typeof(DataGridView).GetMethod("OnCellClick", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(grid, new object[] { click });

                    Assert.Same(grid, GetGroupGrid(form));
                    Assert.Same(cell, grid.CurrentCell);
                    Assert.True(cell.Selected);
                    Assert.True(grid.ContainsFocus);

                    typeof(DataGridView).GetMethod("OnCellDoubleClick", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(grid, new object[] { click });
                    var editor = Assert.IsType<DataGridViewTextBoxEditingControl>(grid.EditingControl);
                    editor.Text = "1234";
                    editor.SelectionStart = 2;
                    editor.SelectionLength = 0;
                    // 编辑时再次同步当前文件，检查插入光标和未提交文本仍在原处。
                    typeof(MaterialSelectFormModern).GetMethod("ActivateBatchPageBoxItem", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(form, new object[] { item });
                    Application.DoEvents();

                    Assert.Same(grid, GetGroupGrid(form));
                    Assert.True(grid.IsCurrentCellInEditMode);
                    Assert.Same(editor, grid.EditingControl);
                    Assert.True(editor.Focused);
                    Assert.Equal("1234", editor.Text);
                    Assert.Equal(2, editor.SelectionStart);
                    Assert.Equal(0, editor.SelectionLength);
                    grid.EndEdit();
                    Assert.Equal("1234", item.Quantity);
                }
            }
        }

        [WinFormsFact]
        public async Task BatchRows_SelectWithOneClickAndKeepEachFileChoiceAfterRefresh()
        {
            string first = CreateBatchPdf("中文路径 一号.pdf", true);
            string second = CreateBatchPdf("中文路径 二号.pdf", true);
            string normal = CreateBatchPdf("普通文件.pdf", false);
            using (var form = new MaterialSelectFormModern(new List<string> { "PET" },
                first, "一号", 1, "98", "49", null, -1, -1, -1, -1, "1"))
            {
                form.SetPendingFiles(new[] { first, second, normal });
                form.Show();
                var imposition = (AntdUI.Checkbox)typeof(MaterialSelectFormModern)
                    .GetField("enableImpositionCheckbox", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                imposition.Checked = false;
                form.ToggleBatchFileListPanel(true);
                await form.LoadBatchPageBoxChoicesAsync(form.BatchFileItems);
                Application.DoEvents();

                var firstItem = form.BatchFileItems.Single(item => item.FilePath == first);
                var secondItem = form.BatchFileItems.Single(item => item.FilePath == second);
                var normalItem = form.BatchFileItems.Single(item => item.FilePath == normal);
                var grid = GetGroupGrid(form);
                int firstRow = FindRow(grid, firstItem);
                int normalRow = FindRow(grid, normalItem);
                var column = grid.Columns.Cast<DataGridViewColumn>()
                    .Single(candidate => candidate.DataPropertyName == nameof(BatchFileItem.Dimensions));
                Assert.Equal(116, column.Width);
                Assert.Equal(46, grid.Rows[firstRow].Height);
                Assert.Equal(26, grid.Rows[normalRow].Height);
                Assert.Equal(grid.ColumnHeadersHeight + grid.Rows.Cast<DataGridViewRow>().Sum(row => row.Height) + 2, grid.Height);

                var cell = (BatchPageBoxChoiceCell)grid.Rows[firstRow].Cells[column.Index];
                var bounds = BatchPageBoxChoiceCell.GetChoiceBounds(cell.Size, PdfPageBoxSource.MediaBox);
                // 默认走控件鼠标入口；交互桌面上可额外开启真实鼠标输入验证。
                if (Environment.GetEnvironmentVariable("PREPRESS_NATIVE_UI_TEST") == "1")
                {
                    var originalCursor = Cursor.Position;
                    try
                    {
                        var screen = Screen.PrimaryScreen.WorkingArea;
                        form.Location = new Point(screen.Left + 20, screen.Top + 20);
                        form.Opacity = 1;
                        form.TopMost = true;
                        form.Activate();
                        await Task.Delay(350);
                        // 异步刷新会重建表格，必须在点击前重新定位当前控件。
                        grid = GetGroupGrid(form);
                        firstRow = FindRow(grid, firstItem);
                        cell = (BatchPageBoxChoiceCell)grid.Rows[firstRow].Cells[column.Index];
                        bounds = BatchPageBoxChoiceCell.GetChoiceBounds(cell.Size, PdfPageBoxSource.MediaBox);
                        var displayedCell = grid.GetCellDisplayRectangle(column.Index, firstRow, false);
                        int mouseX = displayedCell.Left + bounds.Left + 5;
                        int mouseY = displayedCell.Top + bounds.Top + 5;
                        Assert.Equal(column.Index, grid.HitTest(mouseX, mouseY).ColumnIndex);
                        FlaUI.Core.Input.Mouse.LeftClick(grid.PointToScreen(new Point(mouseX, mouseY)));
                        await Task.Delay(100);
                    }
                    finally
                    {
                        Cursor.Position = originalCursor;
                    }
                }
                else
                {
                    typeof(BatchPageBoxChoiceCell).GetMethod("OnMouseClick", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(cell, new object[] { new DataGridViewCellMouseEventArgs(column.Index, firstRow,
                            bounds.Left + 5, bounds.Top + 5, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)) });
                }
                Assert.Equal(PdfPageBoxSource.MediaBox, firstItem.PageBoxSource);
                Assert.Equal(PdfPageBoxSource.CropBox, secondItem.PageBoxSource);
                Assert.Equal(PdfPageBoxSource.Automatic, normalItem.PageBoxSource);
                Assert.Equal(PdfPageBoxSource.MediaBox, form.SelectedPageBoxSource);
                Assert.NotEqual(firstItem.Dimensions, secondItem.Dimensions);

                await form.LoadBatchPageBoxChoicesAsync(form.BatchFileItems, refresh: true);
                Assert.Equal(PdfPageBoxSource.MediaBox, firstItem.PageBoxSource);
                Assert.Equal(PdfPageBoxSource.CropBox, secondItem.PageBoxSource);
                form.SelectBatchPageBoxSource(secondItem, PdfPageBoxSource.MediaBox);
                form.SelectBatchPageBoxSource(firstItem, PdfPageBoxSource.CropBox);
                Assert.Equal(PdfPageBoxSource.MediaBox, secondItem.PageBoxSource);
                Assert.Equal(PdfPageBoxSource.CropBox, firstItem.PageBoxSource);

                grid = GetGroupGrid(form);
                cell = (BatchPageBoxChoiceCell)grid.Rows[FindRow(grid, firstItem)].Cells[column.Index];
                var key = new KeyEventArgs(Keys.Space);
                typeof(BatchPageBoxChoiceCell).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(cell, new object[] { key, FindRow(grid, firstItem) });
                Assert.True(key.Handled);
                Assert.Equal(PdfPageBoxSource.MediaBox, firstItem.PageBoxSource);

                string snapshotPath = Environment.GetEnvironmentVariable("PREPRESS_UI_SNAPSHOT_PATH");
                if (!string.IsNullOrEmpty(snapshotPath))
                {
                    using (var bitmap = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                        bitmap.Save(snapshotPath);
                    }
                }
                form.ToggleBatchFileListPanel(false);
                Assert.Equal(PdfPageBoxSource.MediaBox, form.SelectedPageBoxSource);
            }
        }

        [WinFormsFact]
        public async Task BatchProcessing_WritesEachSelectedBoxAndLeavesNormalFileUnchanged()
        {
            string media = CreateBatchPdf("媒体文件.pdf", true);
            string crop = CreateBatchPdf("裁剪文件.pdf", true);
            string normal = CreateBatchPdf("普通文件.pdf", false);
            string output = Path.Combine(_testRoot, "实际输出");
            Directory.CreateDirectory(output);
            var items = new[] { media, crop, normal }.Select((path, index) => new BatchFileItem
            {
                FilePath = path, FileName = Path.GetFileName(path), Quantity = "1", SerialNumber = (index + 1).ToString()
            }).ToList();
            foreach (var item in items)
                BatchPageBoxSelectionService.ApplyInspection(item, new PdfInspectorService().InspectPdf(item.FilePath));
            BatchPageBoxSelectionService.SelectSource(items[0], PdfPageBoxSource.MediaBox);
            BatchPageBoxSelectionService.SelectSource(items[1], PdfPageBoxSource.CropBox);
            var selection = new MaterialSelectionResult
            {
                BatchItems = items, ExportPath = output, PageBoxSource = PdfPageBoxSource.MediaBox,
                PageBoxReferencePage = 1, EnableImposition = false, CopyCount = 1, DuplicateCount = 1,
                CopyType = CopyType.Layout, CornerRadius = "0", Dimensions = "98x49"
            };
            var view = new Moq.Mock<IFileRenamePanelView>();
            var presenter = new FileRenamePanelPresenter(view.Object);
            var method = typeof(FileRenamePanelPresenter).GetMethod("ProcessApplyToAllBatchAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);
            await (Task)method.Invoke(presenter, new object[] { selection });
            view.Verify(candidate => candidate.ShowError(Moq.It.IsAny<string>()), Moq.Times.Never);
            view.Verify(candidate => candidate.UpdateStatus(Moq.It.Is<string>(text => text.Contains("成功 3"))), Moq.Times.Once);
            string[] outputs = Directory.GetFiles(output, "*.pdf");
            Assert.Equal(3, outputs.Length);
            int cropCount = 0;
            int mediaCount = 0;
            int normalCount = 0;
            foreach (string path in outputs)
            {
                using (var document = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(path)))
                {
                    var page = document.GetPage(1);
                    if (Math.Abs(page.GetMediaBox().GetWidth() - 240) < 0.001)
                    {
                        cropCount++;
                        Assert.Equal(10, page.GetMediaBox().GetX());
                        Assert.Equal(240, page.GetCropBox().GetWidth());
                    }
                    else if (Math.Abs(page.GetTrimBox().GetWidth() - 230) < 0.001)
                    {
                        normalCount++;
                        Assert.Equal(280, page.GetMediaBox().GetWidth());
                    }
                    else
                    {
                        mediaCount++;
                        Assert.Equal(280, page.GetMediaBox().GetWidth());
                        Assert.Equal(280, page.GetCropBox().GetWidth());
                    }
                }
            }
            Assert.Equal(1, cropCount);
            Assert.Equal(1, mediaCount);
            Assert.Equal(1, normalCount);
        }

        [WinFormsFact]
        public async Task BleedChange_UpdatesBothInlineChoicesWithoutChangingPageBoxSources()
        {
            string first = CreateBatchPdf("出血联动.pdf", true);
            string locked = CreateBatchPdf("锁定组出血.pdf", true);
            string normal = CreateBatchPdf("正常行出血.pdf", false);
            using (var form = new MaterialSelectFormModern(new List<string> { "PET" },
                first, "出血", 1, "98", "49", null, -1, -1, -1, -1, "1"))
            {
                form.SetPendingFiles(new[] { first, locked, normal });
                form.Show();
                var imposition = (AntdUI.Checkbox)typeof(MaterialSelectFormModern)
                    .GetField("enableImpositionCheckbox", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                imposition.Checked = false;
                await form.LoadBatchPageBoxChoicesAsync(form.BatchFileItems);
                var firstItem = form.BatchFileItems.Single(item => item.FilePath == first);
                var lockedItem = form.BatchFileItems.Single(item => item.FilePath == locked);
                var normalItem = form.BatchFileItems.Single(item => item.FilePath == normal);
                lockedItem.IsLocked = true;
                form.SelectBatchPageBoxSource(firstItem, PdfPageBoxSource.MediaBox);
                var setBleed = typeof(MaterialSelectFormModern).GetMethod("ApplyV5BleedValue",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                setBleed.Invoke(form, new object[] { 0d });
                Assert.Equal("98.8×49.4", BatchPageBoxChoiceCell.GetDimensionText(firstItem, PdfPageBoxSource.MediaBox));
                Assert.Equal("84.7×42.3", BatchPageBoxChoiceCell.GetDimensionText(firstItem, PdfPageBoxSource.CropBox));

                setBleed.Invoke(form, new object[] { 2d });
                Assert.Equal(2, form.SelectedTetBleed);
                foreach (var item in new[] { firstItem, lockedItem })
                {
                    Assert.Equal("94.8×45.4", BatchPageBoxChoiceCell.GetDimensionText(item, PdfPageBoxSource.MediaBox));
                    Assert.Equal("80.7×38.3", BatchPageBoxChoiceCell.GetDimensionText(item, PdfPageBoxSource.CropBox));
                }
                Assert.Equal(PdfPageBoxSource.MediaBox, firstItem.PageBoxSource);
                Assert.Equal(PdfPageBoxSource.CropBox, lockedItem.PageBoxSource);
                Assert.True(lockedItem.IsLocked);
                Assert.Equal("94.8x45.4", firstItem.Dimensions);
                Assert.Equal("94.8x45.4", normalItem.Dimensions);

                form.SelectBatchPageBoxSource(firstItem, PdfPageBoxSource.CropBox);
                Assert.Equal("80.7x38.3", firstItem.Dimensions);
                setBleed.Invoke(form, new object[] { 0d });
                Assert.Equal("98.8×49.4", BatchPageBoxChoiceCell.GetDimensionText(firstItem, PdfPageBoxSource.MediaBox));
                Assert.Equal("84.7×42.3", BatchPageBoxChoiceCell.GetDimensionText(firstItem, PdfPageBoxSource.CropBox));
                Assert.Equal("84.7x42.3", firstItem.Dimensions);
                Assert.Equal(280, firstItem.PageBoxChoicePage.MediaBox.Width);
                Assert.Equal(240, firstItem.PageBoxChoicePage.CropBox.Width);
            }
        }

        [WinFormsFact]
        public async Task BatchPdfAnalysis_UsesAffectedPageDimensionsAndRotation()
        {
            string path = Path.Combine(_testRoot, "后续页面旋转.pdf");
            using (var document = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfWriter(path)))
            {
                document.AddNewPage(new iText.Kernel.Geom.PageSize(320, 200));
                var page = document.AddNewPage(new iText.Kernel.Geom.PageSize(280, 140));
                page.SetCropBox(new iText.Kernel.Geom.Rectangle(10, 10, 240, 120));
                page.SetRotation(90);
            }
            var item = new BatchFileItem { FilePath = path };
            BatchPageBoxSelectionService.ApplyInspection(item, new PdfInspectorService().InspectPdf(path));
            Assert.Equal(2, item.PageBoxReferencePage);
            var pdfInfo = await new ImpositionService().AnalyzePdfFileAsync(path, item.PageBoxSource.Value, item.PageBoxReferencePage);
            Assert.Equal(90, pdfInfo.PageRotation);
            Assert.Equal(item.RawPdfWidth, pdfInfo.GetEffectivePageWidth(), 4);
            Assert.Equal(item.RawPdfHeight, pdfInfo.GetEffectivePageHeight(), 4);
        }

        private string CreateBatchPdf(string name, bool differentBoxes)
        {
            string path = Path.Combine(_testRoot, name);
            using (var document = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfWriter(path)))
            {
                var page = document.AddNewPage(new iText.Kernel.Geom.PageSize(280, 140));
                if (differentBoxes) page.SetCropBox(new iText.Kernel.Geom.Rectangle(10, 10, 240, 120));
                page.SetTrimBox(new iText.Kernel.Geom.Rectangle(15, 15, 230, 110));
            }
            return path;
        }

        private static DataGridView GetGroupGrid(MaterialSelectFormModern form)
        {
            return form.Controls.Find("pnlCardsContainer", true).Single().Controls.Cast<Control>()
                .SelectMany(card => card.Controls.Cast<Control>()).OfType<DataGridView>().First();
        }

        private static int FindRow(DataGridView grid, BatchFileItem item)
        {
            return grid.Rows.Cast<DataGridViewRow>().Single(row => ReferenceEquals(row.DataBoundItem, item)).Index;
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", null);
            AppSettings.ResetForTesting();
        }
    }
}
