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
