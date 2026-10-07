using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
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

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", null);
            AppSettings.ResetForTesting();
        }
    }
}
