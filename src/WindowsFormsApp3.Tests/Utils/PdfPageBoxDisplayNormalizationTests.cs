using System;
using System.IO;
using iText.Kernel.Pdf;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;
using Xunit;
using PageSize = iText.Kernel.Geom.PageSize;
using Rectangle = iText.Kernel.Geom.Rectangle;

namespace WindowsFormsApp3.Tests.Utils
{
    public class PdfPageBoxDisplayNormalizationTests
    {
        [Fact]
        public void SetAllPageBoxesToCropBox_WhenDisplayAlignmentRequested_SnapsDimensionsAndKeepsCenter()
        {
            string path = Path.Combine(Path.GetTempPath(), "PdfPageBoxSnap_" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                float rawWidth = (float)(100.06 * 72 / 25.4);
                float rawHeight = (float)(50.04 * 72 / 25.4);
                using (var document = new PdfDocument(new PdfWriter(path)))
                {
                    PdfPage page = document.AddNewPage(new PageSize(rawWidth, rawHeight));
                    page.SetCropBox(new Rectangle(10, 20, rawWidth, rawHeight));
                    page.SetMediaBox(new Rectangle(0, 0, rawWidth + 20, rawHeight + 40));
                }

                Assert.True(PdfTools.SetAllPageBoxesToCropBox(path, normalizeDisplayedSize: true));

                using (var document = new PdfDocument(new PdfReader(path)))
                {
                    PdfPage page = document.GetPage(1);
                    Rectangle box = page.GetCropBox();
                    Assert.InRange(box.GetWidth() * 25.4 / 72, 100.099, 100.101);
                    Assert.InRange(box.GetHeight() * 25.4 / 72, 49.999, 50.001);
                    Assert.InRange(box.GetLeft() + box.GetWidth() / 2, 10 + rawWidth / 2 - 0.01, 10 + rawWidth / 2 + 0.01);
                    Assert.InRange(box.GetBottom() + box.GetHeight() / 2, 20 + rawHeight / 2 - 0.01, 20 + rawHeight / 2 + 0.01);
                    Assert.Equal(box.GetWidth(), page.GetMediaBox().GetWidth());
                    Assert.Equal(box.GetHeight(), page.GetTrimBox().GetHeight());
                    Assert.Equal(box.GetWidth(), page.GetBleedBox().GetWidth());
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void SetAllPageBoxesToCropBox_DefaultMode_PreservesOriginalPrecision()
        {
            string path = Path.Combine(Path.GetTempPath(), "PdfPageBoxOriginal_" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                float rawWidth = (float)(100.06 * 72 / 25.4);
                float rawHeight = (float)(50.04 * 72 / 25.4);
                using (var document = new PdfDocument(new PdfWriter(path)))
                    document.AddNewPage(new PageSize(rawWidth, rawHeight));

                Assert.True(PdfTools.SetAllPageBoxesToCropBox(path));

                using (var document = new PdfDocument(new PdfReader(path)))
                {
                    Rectangle box = document.GetPage(1).GetCropBox();
                    Assert.InRange(box.GetWidth() * 25.4 / 72, 100.055, 100.065);
                    Assert.InRange(box.GetHeight() * 25.4 / 72, 50.035, 50.045);
                }
            }
            finally { File.Delete(path); }
        }

        [Theory]
        [InlineData(PdfPageBoxSource.MediaBox, 79.9999, 44.0000)]
        [InlineData(PdfPageBoxSource.CropBox, 80.0899, 44.1924)]
        public void SetAllPageBoxesToCropBox_UsesSelectedBoxForEveryPage(
            PdfPageBoxSource selectedSource, double expectedWidthMm, double expectedHeightMm)
        {
            string path = Path.Combine(Path.GetTempPath(), "PdfPageBoxChoice_" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                using (var document = new PdfDocument(new PdfWriter(path)))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        PdfPage page = document.AddNewPage();
                        page.SetMediaBox(new Rectangle(18.1587f, 24.5117f, 226.7713f, 124.7243f));
                        page.SetCropBox(new Rectangle(18.0314f, 24.2391f, 227.0266f, 125.2699f));
                    }
                }

                Assert.True(PdfTools.SetAllPageBoxesToCropBox(path,
                    normalizeDisplayedSize: false, pageBoxSource: selectedSource, referencePageNumber: 1));

                using (var document = new PdfDocument(new PdfReader(path)))
                {
                    for (int pageNumber = 1; pageNumber <= 2; pageNumber++)
                    {
                        PdfPage page = document.GetPage(pageNumber);
                        Rectangle media = page.GetMediaBox();
                        Rectangle crop = page.GetCropBox();
                        Assert.InRange(media.GetWidth() * 25.4 / 72, expectedWidthMm - 0.005, expectedWidthMm + 0.005);
                        Assert.InRange(media.GetHeight() * 25.4 / 72, expectedHeightMm - 0.005, expectedHeightMm + 0.005);
                        Assert.Equal(media.GetWidth(), crop.GetWidth());
                        Assert.Equal(media.GetHeight(), page.GetTrimBox().GetHeight());
                        Assert.Equal(media.GetWidth(), page.GetBleedBox().GetWidth());
                        Assert.Equal(media.GetHeight(), page.GetArtBox().GetHeight());
                    }
                }
            }
            finally { File.Delete(path); }
        }
    }
}
