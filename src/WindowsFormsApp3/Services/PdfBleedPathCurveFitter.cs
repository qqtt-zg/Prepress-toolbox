using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace WindowsFormsApp3.Services
{
    /// <summary>保留轮廓锚点，将密集折线拟合为误差受限的三次贝塞尔曲线。</summary>
    internal static class PdfBleedPathCurveFitter
    {
        internal static GraphicsPath Fit(IList<PointF> points, IList<int> anchors, float tolerance)
        {
            var path = new GraphicsPath();
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (PointF point in points)
            {
                minX = Math.Min(minX, point.X); minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X); maxY = Math.Max(maxY, point.Y);
            }
            var bounds = RectangleF.FromLTRB(minX, minY, maxX, maxY);
            for (int i = 0; i < anchors.Count; i++)
            {
                int end = anchors[(i + 1) % anchors.Count];
                if (end <= anchors[i]) end += points.Count;
                var section = new List<PointF>();
                for (int j = anchors[i]; j <= end; j++) section.Add(points[j % points.Count]);
                Append(path, section, bounds, tolerance);
            }
            path.CloseFigure();
            return path;
        }

        private static void Append(GraphicsPath path, List<PointF> points, RectangleF bounds, float tolerance)
        {
            PointF start = points[0], end = points[points.Count - 1];
            bool straight = true;
            foreach (PointF point in points)
                if (DistanceSquared(point, start, end) > tolerance * tolerance) { straight = false; break; }
            if (straight)
            {
                path.AddLine(start, end);
                return;
            }

            double[] parameters = new double[points.Count];
            for (int i = 1; i < points.Count; i++)
                parameters[i] = parameters[i - 1] + Length(points[i - 1], points[i]);
            double total = parameters[parameters.Length - 1];
            double aa = 0, ab = 0, bb = 0, ax = 0, ay = 0, bx = 0, by = 0;
            for (int i = 1; i < points.Count - 1; i++)
            {
                double t = parameters[i] / total, u = 1 - t;
                double b0 = u * u * u, b1 = 3 * u * u * t, b2 = 3 * u * t * t, b3 = t * t * t;
                double rx = points[i].X - b0 * start.X - b3 * end.X;
                double ry = points[i].Y - b0 * start.Y - b3 * end.Y;
                aa += b1 * b1; ab += b1 * b2; bb += b2 * b2;
                ax += b1 * rx; ay += b1 * ry; bx += b2 * rx; by += b2 * ry;
            }
            double determinant = aa * bb - ab * ab;
            PointF first = determinant > 1e-12
                ? new PointF((float)((ax * bb - bx * ab) / determinant), (float)((ay * bb - by * ab) / determinant))
                : new PointF((2 * start.X + end.X) / 3, (2 * start.Y + end.Y) / 3);
            PointF second = determinant > 1e-12
                ? new PointF((float)((aa * bx - ab * ax) / determinant), (float)((aa * by - ab * ay) / determinant))
                : new PointF((start.X + 2 * end.X) / 3, (start.Y + 2 * end.Y) / 3);
            // 控制点限制在原轮廓极值内，曲线的凸包也就不会超出页面边界。
            first = Clamp(first, bounds);
            second = Clamp(second, bounds);
            var flattened = new List<PointF> { start };
            float flatness = tolerance / 16;
            Flatten(flattened, start, first, second, end, flatness, 0);
            double limit = tolerance - flatness;
            if (Covers(points, flattened, limit) && Covers(flattened, points, limit))
            {
                path.AddBezier(start, first, second, end);
                return;
            }
            // 超过误差预算则二分重试；最终单条边自然退回直线。
            int middle = points.Count / 2;
            Append(path, points.GetRange(0, middle + 1), bounds, tolerance);
            Append(path, points.GetRange(middle, points.Count - middle), bounds, tolerance);
        }

        private static bool Covers(IList<PointF> source, IList<PointF> target, double tolerance)
        {
            for (int i = 1; i < source.Count; i++)
                if (!CoversSegment(source[i - 1], source[i], target, tolerance * tolerance, 0)) return false;
            return true;
        }

        private static bool CoversSegment(PointF start, PointF end, IList<PointF> target, double limitSquared, int depth)
        {
            // 到一条线段的距离为凸函数：两端都合格即可保证整段合格。
            // 找不到共同的目标线段时细分，避免只检查采样点而漏掉中间的偏差。
            bool startInside = false, endInside = false;
            for (int i = 1; i < target.Count; i++)
            {
                bool coversStart = DistanceSquared(start, target[i - 1], target[i]) <= limitSquared;
                bool coversEnd = DistanceSquared(end, target[i - 1], target[i]) <= limitSquared;
                if (coversStart && coversEnd) return true;
                startInside |= coversStart;
                endInside |= coversEnd;
            }
            if (!startInside || !endInside) return false;
            if (depth >= 12) return false;
            PointF middle = Midpoint(start, end);
            return CoversSegment(start, middle, target, limitSquared, depth + 1) &&
                CoversSegment(middle, end, target, limitSquared, depth + 1);
        }

        private static void Flatten(List<PointF> points, PointF a, PointF b, PointF c, PointF d, float tolerance, int depth)
        {
            if (depth >= 18 || Math.Max(DistanceSquared(b, a, d), DistanceSquared(c, a, d)) <= tolerance * tolerance)
            {
                points.Add(d);
                return;
            }
            PointF ab = Midpoint(a, b), bc = Midpoint(b, c), cd = Midpoint(c, d);
            PointF abc = Midpoint(ab, bc), bcd = Midpoint(bc, cd), mid = Midpoint(abc, bcd);
            Flatten(points, a, ab, abc, mid, tolerance, depth + 1);
            Flatten(points, mid, bcd, cd, d, tolerance, depth + 1);
        }

        private static PointF Clamp(PointF point, RectangleF bounds) => new PointF(
            Math.Max(bounds.Left, Math.Min(bounds.Right, point.X)), Math.Max(bounds.Top, Math.Min(bounds.Bottom, point.Y)));
        private static PointF Midpoint(PointF a, PointF b) => new PointF((a.X + b.X) / 2, (a.Y + b.Y) / 2);
        private static double Length(PointF a, PointF b) => Math.Sqrt((double)(b.X - a.X) * (b.X - a.X) + (double)(b.Y - a.Y) * (b.Y - a.Y));
        private static double DistanceSquared(PointF point, PointF a, PointF b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, length = dx * dx + dy * dy;
            double t = length > 0 ? Math.Max(0, Math.Min(1, ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / length)) : 0;
            double x = point.X - a.X - t * dx, y = point.Y - a.Y - t * dy;
            return x * x + y * y;
        }
    }
}
