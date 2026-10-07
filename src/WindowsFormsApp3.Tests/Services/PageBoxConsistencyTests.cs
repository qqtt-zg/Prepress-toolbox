using System;
using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;
using Xunit;
using Path = System.IO.Path;

namespace WindowsFormsApp3.Tests.Services
{
    public class PageBoxConsistencyTests
    {
        [Fact]
        public async Task SameWidthSmallerCrop_UsesCropForRollLayout()
        {
            string file = CreatePdf(new Rectangle(0, 0, Mm(101.5), Mm(50.0798)),
                new Rectangle(0, 0, Mm(101.5), Mm(49.5)));
            try
            {
                var service = new ImpositionService();
                var info = await service.AnalyzePdfFileAsync(file);
                Assert.True(info.HasCropBox);
                using (var doc = new PdfDocument(new PdfReader(file)))
                    Assert.InRange(Math.Abs(info.GetEffectivePageHeight() - doc.GetFirstPage().GetCropBox().GetHeight() * 25.4 / 72), 0, 0.00001);
                var result = await service.CalculateRollMaterialLayoutAsync(new RollMaterialConfiguration
                {
                    FixedWidth = 320, MinLength = 150,
                    MarginLeft = 5, MarginRight = 5, MarginTop = 0, MarginBottom = 0
                }, info);
                Assert.True(result.Success);
                Assert.Equal(4, result.Rows);
                Assert.Equal(3, result.Columns);
            }
            finally { File.Delete(file); }
        }

        [Theory]
        [InlineData(0, 1.6)]
        public void DifferentSizeOrOriginInsideOutOfBoundsTolerance_ReportsBoxMismatch(float shift, float shrink)
        {
            string file = CreatePdf(new Rectangle(0, 0, 280, 140),
                new Rectangle(shift, 0, 280, 140 - shrink));
            try
            {
                var info = new PdfInspectorService().InspectPdf(file);
                Assert.Contains(info.Issues, issue => issue.PageNumber == 1 &&
                    issue.BoxType == "CropBox" && issue.Description.Contains("不一致"));
            }
            finally { File.Delete(file); }
        }

        [Fact]
        public void IdenticalBoxes_DoNotReportMismatch()
        {
            string file = CreatePdf(new Rectangle(0, 0, 280, 140), new Rectangle(0, 0, 280, 140));
            try
            {
                Assert.DoesNotContain(new PdfInspectorService().InspectPdf(file).Issues,
                    issue => issue.BoxType == "CropBox" && issue.Description.Contains("不一致"));
            }
            finally { File.Delete(file); }
        }

        [Theory]
        [InlineData(80.011, 44.011, 80.029, 44.029)]
        [InlineData(80.029, 44.029, 80.011, 44.011)]
        [InlineData(101.511, 49.511, 101.539, 49.539)]
        [InlineData(80.101, 44.101, 80.141, 44.141)]
        public async Task HiddenDecimalDifferences_DoNotPromptAndPreserveLayoutPrecision(
            double mediaWidth, double mediaHeight, double cropWidth, double cropHeight)
        {
            string file = CreatePdf(new Rectangle(0, 0, Mm(mediaWidth), Mm(mediaHeight)),
                new Rectangle(0, 0, Mm(cropWidth), Mm(cropHeight)));
            try
            {
                Assert.DoesNotContain(new PdfInspectorService().InspectPdf(file).Issues,
                    issue => issue.BoxType == "CropBox" && (issue.Type == IssueType.DifferentPageBoxes || issue.Type == IssueType.OutOfBounds));
                var info = await new ImpositionService().AnalyzePdfFileAsync(file, PdfPageBoxSource.CropBox, 1);
                using (var document = new PdfDocument(new PdfReader(file)))
                    Assert.InRange(Math.Abs(info.GetEffectivePageHeight() - document.GetFirstPage().GetCropBox().GetHeight() * 25.4 / 72), 0, 0.00001);
            }
            finally { File.Delete(file); }
        }

        [Theory]
        [InlineData(80.04, 80.06)]
        [InlineData(101.44, 101.56)]
        [InlineData(80.06, 80.04)]
        public void DifferentDisplayedDimensions_ReportMismatch(double mediaWidth, double cropWidth)
        {
            string file = CreatePdf(new Rectangle(0, 0, Mm(mediaWidth), Mm(44)),
                new Rectangle(0, 0, Mm(cropWidth), Mm(44)));
            try
            {
                Assert.Contains(new PdfInspectorService().InspectPdf(file).Issues,
                    issue => issue.BoxType == "CropBox" && (issue.Type == IssueType.DifferentPageBoxes || issue.Type == IssueType.OutOfBounds));
            }
            finally { File.Delete(file); }
        }

        [Theory]
        [InlineData(0, 0.02)]
        [InlineData(0, -0.04)]
        [InlineData(0.11, 0.14)]
        [InlineData(-0.11, -0.14)]
        public void HiddenPositionDifferences_DoNotPromptAndPreserveCoordinates(double mediaOrigin, double cropOrigin)
        {
            string file = CreatePdf(new Rectangle(Mm(mediaOrigin), Mm(mediaOrigin), Mm(80.3), Mm(44.3)),
                new Rectangle(Mm(cropOrigin), Mm(cropOrigin), Mm(80.3), Mm(44.3)));
            try
            {
                Assert.DoesNotContain(new PdfInspectorService().InspectPdf(file).Issues,
                    issue => issue.BoxType == "CropBox" && (issue.Type == IssueType.DifferentPageBoxes || issue.Type == IssueType.OutOfBounds));
                Assert.True(PdfTools.SetAllPageBoxesToCropBox(file, pageBoxSource: PdfPageBoxSource.CropBox));
                using (var document = new PdfDocument(new PdfReader(file)))
                {
                    var box = document.GetFirstPage().GetMediaBox();
                    Assert.InRange(Math.Abs(box.GetLeft() * 25.4 / 72 - cropOrigin), 0, 0.005);
                    Assert.InRange(Math.Abs(box.GetBottom() * 25.4 / 72 - cropOrigin), 0, 0.005);
                }
            }
            finally { File.Delete(file); }
        }

        [Theory]
        [InlineData(0.04, 0.06, true)]
        [InlineData(-0.04, -0.06, true)]
        [InlineData(0.04, 0.06, false)]
        [InlineData(-0.04, -0.06, false)]
        public void DifferentDisplayedPositions_ReportMismatch(double mediaOrigin, double cropOrigin, bool shiftX)
        {
            string file = CreatePdf(new Rectangle(Mm(mediaOrigin), Mm(mediaOrigin), Mm(80.3), Mm(44.3)),
                new Rectangle(Mm(shiftX ? cropOrigin : mediaOrigin), Mm(shiftX ? mediaOrigin : cropOrigin), Mm(80.3), Mm(44.3)));
            try
            {
                Assert.Contains(new PdfInspectorService().InspectPdf(file).Issues,
                    issue => issue.BoxType == "CropBox" && issue.Type == IssueType.OutOfBounds);
            }
            finally { File.Delete(file); }
        }

        [Theory]
        [InlineData(0.04)]
        [InlineData(-0.04)]
        public void SizeMismatchWithHiddenOriginOffset_DoesNotReportFalseOutOfBounds(double cropOrigin)
        {
            string file = CreatePdf(new Rectangle(0, 0, Mm(101.5), Mm(50.1)),
                new Rectangle(Mm(cropOrigin), 0, Mm(101.5), Mm(49.5)));
            try
            {
                var issues = new PdfInspectorService().InspectPdf(file).Issues;
                Assert.Contains(issues, issue => issue.BoxType == "CropBox" && issue.Type == IssueType.DifferentPageBoxes);
                Assert.DoesNotContain(issues, issue => issue.BoxType == "CropBox" && issue.Type == IssueType.OutOfBounds);
            }
            finally { File.Delete(file); }
        }

        [Theory]
        [InlineData(PdfPageBoxSource.MediaBox, 3)]
        [InlineData(PdfPageBoxSource.CropBox, 4)]
        public async Task SelectedBox_DrivesLayoutAndUnifiedOutput(PdfPageBoxSource source, int expectedRows)
        {
            string file = CreatePdf(new Rectangle(0, 0, Mm(101.5), Mm(50.0798)),
                new Rectangle(0, 0, Mm(101.5), Mm(49.5)));
            try
            {
                var service = new ImpositionService();
                var info = await service.AnalyzePdfFileAsync(file, source, 1);
                var result = await service.CalculateRollMaterialLayoutAsync(new RollMaterialConfiguration
                {
                    FixedWidth = 320, MinLength = 150,
                    MarginLeft = 5, MarginRight = 5, MarginTop = 0, MarginBottom = 0
                }, info);
                Assert.True(result.Success);
                Assert.Equal(expectedRows, result.Rows);
                Assert.Equal(3, result.Columns);
                Assert.True(PdfTools.SetAllPageBoxesToCropBox(file, pageBoxSource: source));
                using (var doc = new PdfDocument(new PdfReader(file)))
                {
                    var page = doc.GetFirstPage();
                    Assert.Equal(page.GetMediaBox().GetHeight(), page.GetCropBox().GetHeight());
                    Assert.InRange(Math.Abs(page.GetCropBox().GetHeight() * 25.4 / 72 - info.GetEffectivePageHeight()), 0, 0.005);
                }
            }
            finally { File.Delete(file); }
        }

        [Fact]
        public async Task ReferencePageSelection_UsesSameFrameAsFileProcessing()
        {
            string file = CreatePdf(new Rectangle(0, 0, 280, 140), new Rectangle(0, 0, 280, 140));
            try
            {
                string prepared = file + ".prepared";
                using (var doc = new PdfDocument(new PdfReader(file), new PdfWriter(prepared)))
                    doc.AddNewPage(new iText.Kernel.Geom.PageSize(180, 100)).SetCropBox(new Rectangle(0, 0, 170, 90));
                File.Copy(prepared, file, true);
                File.Delete(prepared);
                var info = await new ImpositionService().AnalyzePdfFileAsync(file, PdfPageBoxSource.CropBox, 2);
                Assert.InRange(info.GetEffectivePageWidth(), 170 * 25.4 / 72 - 0.0001, 170 * 25.4 / 72 + 0.0001);
                Assert.True(PdfTools.SetAllPageBoxesToCropBox(file, pageBoxSource: PdfPageBoxSource.CropBox, referencePageNumber: 2));
                using (var doc = new PdfDocument(new PdfReader(file)))
                    Assert.Equal(170, doc.GetFirstPage().GetCropBox().GetWidth());
            }
            finally
            {
                File.Delete(file);
                File.Delete(file + ".prepared");
            }
        }

        [Fact]
        public async Task InvalidExplicitReference_DoesNotSilentlyFallBack()
        {
            string file = CreatePdf(new Rectangle(0, 0, 280, 140), new Rectangle(0, 0, 280, 140));
            try
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    new ImpositionService().AnalyzePdfFileAsync(file, PdfPageBoxSource.MediaBox, 2));
            }
            finally { File.Delete(file); }
        }

        private static float Mm(double value) => (float)(value * 72 / 25.4);

        private static string CreatePdf(Rectangle media, Rectangle crop)
        {
            string file = Path.Combine(Path.GetTempPath(), "PageBoxConsistency_" + Guid.NewGuid().ToString("N") + ".pdf");
            using (var doc = new PdfDocument(new PdfWriter(file)))
                doc.AddNewPage(new iText.Kernel.Geom.PageSize(media)).SetMediaBox(media).SetCropBox(crop);
            return file;
        }
    }
}
