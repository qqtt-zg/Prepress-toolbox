using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindowsFormsApp3.Services;
using Xunit;

namespace WindowsFormsApp3.Tests.Utils
{
    public class PdfBleedPathSimplificationTests
    {
        private const float MaximumErrorPoints = (float)(0.008 * 72 / 25.4);

        [Fact]
        public void CurvedBleed_ReducesNodesWithinErrorLimitAndKeepsExtrema()
        {
            using (var cut = new GraphicsPath())
            {
                cut.AddEllipse(20, 20, 120, 80);
                PointF[] originalCut = cut.PathPoints;
                using (var baseline = PdfBleedPathOffsetter.CreateOutset(cut, 5, simplify: false))
                using (var simplified = PdfBleedPathOffsetter.CreateOutset(cut, 5))
                {
                    Assert.True(simplified.PointCount < baseline.PointCount * 0.75,
                        $"节点减少不足: {baseline.PointCount} -> {simplified.PointCount}");
                    Assert.Equal(baseline.GetBounds(), simplified.GetBounds());
                    AssertMaximumDeviation(baseline.PathPoints, simplified, MaximumErrorPoints);
                    Assert.Equal(originalCut, cut.PathPoints);
                }
            }
        }

        [Fact]
        public void RedundantStraightSegments_AreMergedIntoFourCorners()
        {
            using (var polygon = new GraphicsPath())
            {
                var points = new PointF[400];
                for (int i = 0; i < 100; i++)
                {
                    points[i] = new PointF(i, 0);
                    points[100 + i] = new PointF(100, i);
                    points[200 + i] = new PointF(100 - i, 100);
                    points[300 + i] = new PointF(0, 100 - i);
                }
                polygon.AddPolygon(points);
                using (var simplified = PdfBleedPathOffsetter.SimplifyClosedPath(polygon))
                {
                    Assert.Equal(4, simplified.PointCount);
                    Assert.Equal(polygon.GetBounds(), simplified.GetBounds());
                    AssertMaximumDeviation(points, simplified, MaximumErrorPoints);
                }
            }
        }

        [Fact]
        public void ConcaveCornerAndCutContainment_ArePreserved()
        {
            using (var cut = new GraphicsPath())
            {
                cut.AddPolygon(new[]
                {
                    new PointF(20, 20), new PointF(80, 20), new PointF(80, 35),
                    new PointF(35, 35), new PointF(35, 80), new PointF(20, 80)
                });
                using (var baseline = PdfBleedPathOffsetter.CreateOutset(cut, 5, simplify: false))
                using (var simplified = PdfBleedPathOffsetter.CreateOutset(cut, 5))
                {
                    Assert.Contains(simplified.PathPoints, point => point == new PointF(40, 40));
                    Assert.False(simplified.IsVisible(70, 70));
                    foreach (PointF point in cut.PathPoints) Assert.True(simplified.IsVisible(point));
                    AssertMaximumDeviation(baseline.PathPoints, simplified, MaximumErrorPoints);
                }
            }
        }

        [Fact]
        public void SimplificationThatCutsRequiredContour_FallsBackToOriginalNodes()
        {
            using (var polygon = new GraphicsPath())
            {
                var points = new PointF[360];
                for (int i = 0; i < points.Length; i++)
                    points[i] = new PointF((float)(50 + 40 * Math.Cos(i * Math.PI / 180)),
                        (float)(50 + 40 * Math.Sin(i * Math.PI / 180)));
                polygon.AddPolygon(points);
                using (var simplified = PdfBleedPathOffsetter.SimplifyClosedPath(polygon, polygon))
                    Assert.Equal(polygon.PathPoints, simplified.PathPoints);
            }
        }

        [Fact]
        public void SmoothBleed_UsesBezierCurvesToFurtherReduceNodesWithinSameTolerance()
        {
            using (var cut = new GraphicsPath())
            {
                cut.AddEllipse(20, 20, 120, 80);
                using (var baseline = PdfBleedPathOffsetter.CreateOutset(cut, 5, simplify: false))
                using (var fitted = PdfBleedPathOffsetter.CreateOutset(cut, 5))
                {
                    Assert.Contains(fitted.PathTypes, type => (type & 7) == 3);
                    Assert.True(fitted.PointCount < 100, $"曲线控制点仍过多: {fitted.PointCount}");
                    Assert.Equal(baseline.GetBounds(), fitted.GetBounds());
                    AssertMaximumDeviation(baseline.PathPoints, fitted, MaximumErrorPoints);
                }
            }
        }

        private static void AssertMaximumDeviation(PointF[] original, GraphicsPath fitted, float tolerance)
        {
            PointF[] simplified = SamplePath(fitted);
            foreach (PointF point in original)
            {
                double closest = double.MaxValue;
                for (int i = 0; i < simplified.Length; i++)
                {
                    PointF a = simplified[i], b = simplified[(i + 1) % simplified.Length];
                    double dx = b.X - a.X, dy = b.Y - a.Y;
                    double lengthSquared = dx * dx + dy * dy;
                    double t = lengthSquared == 0 ? 0 : Math.Max(0, Math.Min(1,
                        ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / lengthSquared));
                    double px = point.X - a.X - t * dx, py = point.Y - a.Y - t * dy;
                    closest = Math.Min(closest, Math.Sqrt(px * px + py * py));
                }
                Assert.InRange(closest, 0, tolerance + 0.0001);
            }
            foreach (PointF point in simplified)
            {
                double closest = double.MaxValue;
                for (int i = 0; i < original.Length; i++)
                {
                    PointF a = original[i], b = original[(i + 1) % original.Length];
                    double dx = b.X - a.X, dy = b.Y - a.Y;
                    double lengthSquared = dx * dx + dy * dy;
                    double t = lengthSquared == 0 ? 0 : Math.Max(0, Math.Min(1,
                        ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / lengthSquared));
                    double px = point.X - a.X - t * dx, py = point.Y - a.Y - t * dy;
                    closest = Math.Min(closest, Math.Sqrt(px * px + py * py));
                }
                Assert.InRange(closest, 0, tolerance + 0.0001);
            }
        }

        private static PointF[] SamplePath(GraphicsPath path)
        {
            var sampled = new System.Collections.Generic.List<PointF>();
            PointF[] points = path.PathPoints;
            byte[] types = path.PathTypes;
            PointF current = default;
            for (int i = 0; i < points.Length; i++)
            {
                if ((types[i] & 7) != 3)
                {
                    current = points[i];
                    sampled.Add(current);
                    continue;
                }
                PointF start = current, first = points[i], second = points[i + 1], end = points[i + 2];
                for (int step = 1; step <= 1000; step++)
                {
                    double t = step / 1000.0, u = 1 - t;
                    sampled.Add(new PointF(
                        (float)(u * u * u * start.X + 3 * u * u * t * first.X + 3 * u * t * t * second.X + t * t * t * end.X),
                        (float)(u * u * u * start.Y + 3 * u * u * t * first.Y + 3 * u * t * t * second.Y + t * t * t * end.Y)));
                }
                current = end;
                i += 2;
            }
            return sampled.ToArray();
        }
    }
}
