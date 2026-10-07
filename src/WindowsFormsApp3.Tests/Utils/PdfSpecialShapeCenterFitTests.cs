using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindowsFormsApp3.Services;
using Xunit;
using PdfRectangle = iText.Kernel.Geom.Rectangle;

namespace WindowsFormsApp3.Tests.Utils
{
    public class PdfSpecialShapeCenterFitTests
    {
        [Fact]
        public void FullPageContour_AdjustsExactlyToInsetAndPreservesFullBleed()
        {
            using (var cut = new GraphicsPath())
            {
                cut.AddRectangle(new RectangleF(0, 0, 200, 100));
                using (var bleed = PdfSpecialShapeProcessor.FitCutPathToCropBox(
                    cut, new PdfRectangle(0, 0, 200, 100), 5, out float scaleX, out float scaleY))
                {
                    Assert.InRange(scaleX, 0.9499f, 0.9501f);
                    Assert.InRange(scaleY, 0.8999f, 0.9001f);
                    AssertBounds(cut.GetBounds(), 5, 5, 190, 90);
                    AssertBounds(bleed.GetBounds(), 0, 0, 200, 100);
                }
            }
        }

        [Fact]
        public void OffCenterContour_AdjustsToPageCenterWithNonZeroOrigin()
        {
            using (var cut = new GraphicsPath())
            {
                cut.AddRectangle(new RectangleF(110, 205, 85, 60));
                using (var bleed = PdfSpecialShapeProcessor.FitCutPathToCropBox(
                    cut, new PdfRectangle(100, 200, 100, 100), 10, out float scaleX, out float scaleY))
                {
                    Assert.InRange(scaleX, 0.9410f, 0.9413f);
                    Assert.InRange(scaleY, 1.3332f, 1.3335f);
                    AssertBounds(cut.GetBounds(), 110, 210, 80, 80);
                    AssertBounds(bleed.GetBounds(), 100, 200, 100, 100);
                }
            }
        }

        [Fact]
        public void SmallContour_ExpandsToInsetAndIsCentered()
        {
            using (var cut = new GraphicsPath())
            {
                cut.AddRectangle(new RectangleF(30, 40, 60, 50));
                using (var bleed = PdfSpecialShapeProcessor.FitCutPathToCropBox(
                    cut, new PdfRectangle(0, 0, 200, 150), 5, out float scaleX, out float scaleY))
                {
                    Assert.True(scaleX > 1 && scaleY > 1);
                    AssertBounds(cut.GetBounds(), 5, 5, 190, 140);
                    AssertBounds(bleed.GetBounds(), 0, 0, 200, 150);
                }
            }
        }

        [Fact]
        public void ConcaveContour_WithFractionalCoordinates_FitsExactInset()
        {
            using (var cut = new GraphicsPath())
            {
                cut.AddPolygon(new[]
                {
                    new PointF(4.23f, 4.23f), new PointF(215.77f, 4.23f), new PointF(215.77f, 70),
                    new PointF(70, 70), new PointF(70, 215.77f), new PointF(4.23f, 215.77f)
                });
                float bleedPoints = (float)(1.5 * 72 / 25.4);
                using (var bleed = PdfSpecialShapeProcessor.FitCutPathToCropBox(
                    cut, new PdfRectangle(0, 0, 220, 220), bleedPoints, out _, out _))
                {
                    AssertBounds(cut.GetBounds(), bleedPoints, bleedPoints,
                        220 - 2 * bleedPoints, 220 - 2 * bleedPoints);
                    AssertBounds(bleed.GetBounds(), 0, 0, 220, 220);
                }
            }
        }

        [Fact]
        public void BezierContour_UsesCurveExtremaAndKeepsBezierSegments()
        {
            using (var cut = new GraphicsPath())
            {
                cut.AddBezier(40, 100, 40, 40, 160, 40, 160, 100);
                cut.AddBezier(160, 100, 160, 160, 40, 160, 40, 100);
                cut.CloseFigure();
                using (var bleed = PdfSpecialShapeProcessor.FitCutPathToCropBox(
                    cut, new PdfRectangle(0, 0, 220, 220), 5, out _, out _))
                {
                    AssertBounds(PdfSpecialShapeProcessor.GetGeometricBounds(cut), 5, 5, 210, 210);
                    Assert.Contains(cut.PathTypes, type => (type & 7) == 3);
                    AssertBounds(bleed.GetBounds(), 0, 0, 220, 220);
                }
            }
        }

        [Fact]
        public void AcuteConvexCorner_DoesNotExtendBleedPastCropBox()
        {
            using (var cut = new GraphicsPath())
            {
                cut.AddPolygon(new[] { new PointF(0, 0), new PointF(100, 50), new PointF(0, 100) });
                using (var bleed = PdfSpecialShapeProcessor.FitCutPathToCropBox(
                    cut, new PdfRectangle(0, 0, 200, 200), 5, out _, out _))
                {
                    AssertBounds(cut.GetBounds(), 5, 5, 190, 190);
                    AssertBounds(bleed.GetBounds(), 0, 0, 200, 200);
                }
            }
        }

        [Fact]
        public void BleedLargerThanPageHalf_IsRejectedWithoutChangingCutPath()
        {
            using (var cut = new GraphicsPath())
            {
                cut.AddRectangle(new RectangleF(0, 0, 100, 80));
                PointF[] original = cut.PathPoints;
                Assert.Throws<InvalidOperationException>(() =>
                    PdfSpecialShapeProcessor.FitCutPathToCropBox(cut, new PdfRectangle(0, 0, 100, 80), 40, out _, out _));
                Assert.Equal(original, cut.PathPoints);
            }
        }

        [Fact]
        public void ZeroBleed_AdjustsCutToEntireCropBox()
        {
            using (var cut = new GraphicsPath())
            {
                cut.AddRectangle(new RectangleF(30, 40, 60, 50));
                using (var bleed = PdfSpecialShapeProcessor.FitCutPathToCropBox(
                    cut, new PdfRectangle(10, 20, 200, 150), 0, out _, out _))
                {
                    AssertBounds(cut.GetBounds(), 10, 20, 200, 150);
                    Assert.Equal(cut.PathPoints, bleed.PathPoints);
                }
            }
        }

        [Fact]
        public void SelfIntersectionWithZeroBleed_IsStillRejected()
        {
            using (var cut = new GraphicsPath())
            {
                cut.AddPolygon(new[]
                {
                    new PointF(0, 0), new PointF(100, 100), new PointF(0, 100), new PointF(100, 0)
                });
                Assert.Throws<InvalidOperationException>(() =>
                    PdfSpecialShapeProcessor.FitCutPathToCropBox(cut, new PdfRectangle(0, 0, 200, 200), 0, out _, out _));
            }
        }

        private static void AssertBounds(RectangleF actual, float x, float y, float width, float height)
        {
            Assert.InRange(actual.Left, x - 0.02f, x + 0.02f);
            Assert.InRange(actual.Top, y - 0.02f, y + 0.02f);
            Assert.InRange(actual.Width, width - 0.02f, width + 0.02f);
            Assert.InRange(actual.Height, height - 0.02f, height + 0.02f);
        }
    }
}
