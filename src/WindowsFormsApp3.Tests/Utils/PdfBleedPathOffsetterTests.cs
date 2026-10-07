using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindowsFormsApp3.Services;
using Xunit;

namespace WindowsFormsApp3.Tests.Utils
{
    public class PdfBleedPathOffsetterTests
    {
        [Fact]
        public void Rectangle_OffsetsAllFourEdges()
        {
            using (var cutPath = new GraphicsPath())
            {
                cutPath.AddRectangle(new RectangleF(20, 30, 80, 60));
                using (var bleedPath = PdfBleedPathOffsetter.CreateOutset(cutPath, 5))
                {
                    AssertBounds(bleedPath.GetBounds(), 15, 25, 90, 70);
                }
            }
        }

        [Fact]
        public void Circle_RemainsCircularAfterOffset()
        {
            using (var cutPath = new GraphicsPath())
            {
                cutPath.AddEllipse(20, 20, 60, 60);
                using (var bleedPath = PdfBleedPathOffsetter.CreateOutset(cutPath, 5))
                {
                    AssertBounds(bleedPath.GetBounds(), 15, 15, 70, 70);
                    Assert.False(bleedPath.IsVisible(18, 18));
                    Assert.True(bleedPath.IsVisible(50, 17));
                }
            }
        }

        [Fact]
        public void RoundedRectangle_PreservesCurvedCorners()
        {
            using (var cutPath = new GraphicsPath())
            {
                cutPath.AddArc(20, 20, 20, 20, 180, 90);
                cutPath.AddArc(80, 20, 20, 20, 270, 90);
                cutPath.AddArc(80, 60, 20, 20, 0, 90);
                cutPath.AddArc(20, 60, 20, 20, 90, 90);
                cutPath.CloseFigure();

                using (var bleedPath = PdfBleedPathOffsetter.CreateOutset(cutPath, 5))
                {
                    AssertBounds(bleedPath.GetBounds(), 15, 15, 90, 70);
                    Assert.False(bleedPath.IsVisible(16, 16));
                    Assert.True(bleedPath.IsVisible(25, 17));
                }
            }
        }

        [Fact]
        public void ConcaveShape_DoesNotBecomeBoundingRectangle()
        {
            using (var cutPath = new GraphicsPath())
            {
                cutPath.AddPolygon(new[]
                {
                    new PointF(20, 20), new PointF(80, 20), new PointF(80, 35),
                    new PointF(35, 35), new PointF(35, 80), new PointF(20, 80)
                });

                using (var bleedPath = PdfBleedPathOffsetter.CreateOutset(cutPath, 5))
                {
                    AssertBounds(bleedPath.GetBounds(), 15, 15, 70, 70);
                    Assert.False(bleedPath.IsVisible(70, 70));
                    Assert.True(bleedPath.IsVisible(38, 38));
                }
            }
        }

        [Fact]
        public void SelfIntersectingContour_IsRejected()
        {
            using (var cutPath = new GraphicsPath())
            {
                cutPath.AddPolygon(new[]
                {
                    new PointF(20, 20), new PointF(80, 80),
                    new PointF(20, 80), new PointF(80, 20)
                });
                Assert.Throws<InvalidOperationException>(() => PdfBleedPathOffsetter.CreateOutset(cutPath, 5));
            }
        }

        private static void AssertBounds(RectangleF actual, float x, float y, float width, float height)
        {
            Assert.InRange(actual.X, x - 0.3f, x + 0.3f);
            Assert.InRange(actual.Y, y - 0.3f, y + 0.3f);
            Assert.InRange(actual.Width, width - 0.3f, width + 0.3f);
            Assert.InRange(actual.Height, height - 0.3f, height + 0.3f);
        }
    }
}
