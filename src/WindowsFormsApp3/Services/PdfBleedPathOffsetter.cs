using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 将单条闭合裁切轮廓向外扩展为出血轮廓。无法确认结果时拒绝生成。
    /// </summary>
    internal static class PdfBleedPathOffsetter
    {
        private const float FlatnessPoints = 0.01f;
        private const float SimplificationTolerancePoints = (float)(0.008 * 72 / 25.4);
        private const int MaximumPoints = 4096;

        internal static bool CanOffset(GraphicsPath cutPath)
        {
            if (cutPath == null) return false;
            try
            {
                var contours = GetClosedContours(cutPath);
                return contours.Count == 1 && contours[0].Count <= MaximumPoints && IsSimple(contours[0]);
            }
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
        }

        internal static GraphicsPath CreateOutset(GraphicsPath cutPath, float distancePoints, bool simplify = true)
        {
            if (cutPath == null) throw new ArgumentNullException(nameof(cutPath));
            if (float.IsNaN(distancePoints) || float.IsInfinity(distancePoints) || distancePoints <= 0)
                throw new ArgumentOutOfRangeException(nameof(distancePoints));

            var cutContours = GetClosedContours(cutPath);
            if (cutContours.Count != 1 || cutContours[0].Count > MaximumPoints || !IsSimple(cutContours[0]))
                throw new InvalidOperationException("裁切路径必须是单条无自交的闭合轮廓");

            var cut = cutContours[0];
            var outer = new List<PointF>(cut.Count);
            double orientation = Math.Sign(SignedArea(cut));
            for (int i = 0; i < cut.Count; i++)
            {
                PointF previous = cut[(i - 1 + cut.Count) % cut.Count];
                PointF vertex = cut[i];
                PointF next = cut[(i + 1) % cut.Count];
                PointF beforeDirection = Unit(previous, vertex);
                PointF afterDirection = Unit(vertex, next);
                PointF beforeNormal = OutwardNormal(beforeDirection, orientation);
                PointF afterNormal = OutwardNormal(afterDirection, orientation);
                PointF beforeStart = Add(vertex, beforeNormal, distancePoints);
                PointF afterStart = Add(vertex, afterNormal, distancePoints);
                double denominator = Cross(beforeDirection, afterDirection);
                if (denominator * orientation > 0.00001)
                {
                    // 凸角采用圆连接，避免尖角交点伸出裁切边界加出血的范围。
                    AddRoundJoin(outer, vertex, beforeNormal, afterNormal, distancePoints, orientation);
                    continue;
                }
                PointF offset;
                if (Math.Abs(denominator) < 0.00001)
                {
                    if (beforeDirection.X * afterDirection.X + beforeDirection.Y * afterDirection.Y < 0)
                        throw new InvalidOperationException("裁切路径存在折返边");
                    offset = beforeStart;
                }
                else
                {
                    // 凹角使用相邻平行偏移边的交点，保留原有凹形缺口。
                    double shiftX = afterStart.X - beforeStart.X;
                    double shiftY = afterStart.Y - beforeStart.Y;
                    double along = (shiftX * afterDirection.Y - shiftY * afterDirection.X) / denominator;
                    offset = new PointF((float)(beforeStart.X + along * beforeDirection.X),
                        (float)(beforeStart.Y + along * beforeDirection.Y));
                }
                if (DistanceSquared(vertex, offset) > distancePoints * distancePoints * 100)
                    throw new InvalidOperationException("裁切路径尖角的偏移距离过大");
                outer.Add(offset);
            }

            if (outer.Count > MaximumPoints || !IsSimple(outer) || Math.Abs(SignedArea(outer)) <= Math.Abs(SignedArea(cut)))
                throw new InvalidOperationException("无法生成可靠的出血轮廓");

            var result = new GraphicsPath();
            result.AddPolygon(outer.ToArray());
            foreach (var point in cut)
            {
                if (!result.IsVisible(point))
                {
                    result.Dispose();
                    throw new InvalidOperationException("出血轮廓未完整包围裁切路径");
                }
            }
            if (!simplify) return result;
            using (result)
                return SimplifyClosedPath(result, cutPath);
        }

        /// <summary>
        /// 在0.008mm偏差内简化闭合出血轮廓，为PDF坐标写入舍入保留余量。
        /// 保留极值和尖角；不安全的简化返回原路径副本。
        /// </summary>
        internal static GraphicsPath SimplifyClosedPath(GraphicsPath path, GraphicsPath requiredInsidePath = null)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            var contours = GetClosedContours(path);
            if (contours.Count != 1 || contours[0].Count <= 4)
                return (GraphicsPath)path.Clone();

            var original = contours[0];
            var anchors = new HashSet<int>();
            int left = 0, right = 0, bottom = 0, top = 0;
            for (int i = 0; i < original.Count; i++)
            {
                if (original[i].X < original[left].X) left = i;
                if (original[i].X > original[right].X) right = i;
                if (original[i].Y < original[bottom].Y) bottom = i;
                if (original[i].Y > original[top].Y) top = i;
                var before = Unit(original[(i - 1 + original.Count) % original.Count], original[i]);
                var after = Unit(original[i], original[(i + 1) % original.Count]);
                if (before.X * after.X + before.Y * after.Y < 0.8660254f)
                    anchors.Add(i);
            }
            anchors.Add(left); anchors.Add(right); anchors.Add(bottom); anchors.Add(top);
            var sortedAnchors = new List<int>(anchors);
            sortedAnchors.Sort();
            var simplified = new List<PointF>();
            for (int i = 0; i < sortedAnchors.Count; i++)
            {
                int start = sortedAnchors[i];
                int end = sortedAnchors[(i + 1) % sortedAnchors.Count];
                if (end <= start) end += original.Count;
                var section = new List<PointF>();
                for (int index = start; index <= end; index++)
                    section.Add(original[index % original.Count]);
                AppendSimplifiedSection(simplified, section);
            }

            bool validPolyline = simplified.Count < original.Count && IsSimple(simplified) &&
                SignedArea(simplified) * SignedArea(original) > 0;
            var result = validPolyline ? new GraphicsPath() : (GraphicsPath)path.Clone();
            if (validPolyline) result.AddPolygon(simplified.ToArray());
            if (requiredInsidePath != null)
            {
                var requiredContours = GetClosedContours(requiredInsidePath);
                foreach (var required in requiredContours)
                {
                    if (validPolyline && !ContainsContour(result, simplified, required))
                    {
                        result.Dispose();
                        result = (GraphicsPath)path.Clone();
                        validPolyline = false;
                    }
                }
            }

            var curved = PdfBleedPathCurveFitter.Fit(original, sortedAnchors, SimplificationTolerancePoints);
            List<List<PointF>> curvedContours;
            try { curvedContours = GetClosedContours(curved); }
            catch (InvalidOperationException)
            {
                // 曲线离散后过于复杂时继续使用已经校验合格的折线。
                curved.Dispose();
                return result;
            }
            bool validCurve = CountNodes(curved) < CountNodes(result) && curvedContours.Count == 1 &&
                IsSimple(curvedContours[0]) && SignedArea(curvedContours[0]) * SignedArea(original) > 0;
            if (validCurve && requiredInsidePath != null)
                foreach (var required in GetClosedContours(requiredInsidePath))
                    if (!ContainsContour(curved, curvedContours[0], required)) { validCurve = false; break; }
            if (validCurve)
            {
                result.Dispose();
                result = curved;
            }
            else curved.Dispose();
            LogHelper.Debug($"出血路径节点简化: {original.Count} -> {CountNodes(result)}, 路径点(含控制点)={result.PointCount}, 最大简化偏差=0.008mm");
            return result;
        }

        private static int CountNodes(GraphicsPath path)
        {
            byte[] types = path.PathTypes;
            int nodes = 0;
            for (int i = 0; i < types.Length; i++)
            {
                nodes++;
                if ((types[i] & 7) == 3) i += 2;
            }
            PointF[] points = path.PathPoints;
            if (points.Length > 1 && points[0] == points[points.Length - 1]) nodes--;
            return nodes;
        }

        private static void AppendSimplifiedSection(List<PointF> output, IList<PointF> points)
        {
            var keep = new bool[points.Count];
            keep[0] = true;
            keep[points.Count - 1] = true;
            var pending = new Stack<(int start, int end)>();
            pending.Push((0, points.Count - 1));
            double toleranceSquared = SimplificationTolerancePoints * SimplificationTolerancePoints;
            while (pending.Count > 0)
            {
                var section = pending.Pop();
                int farthest = -1;
                double largestDistance = toleranceSquared;
                for (int i = section.start + 1; i < section.end; i++)
                {
                    double distance = DistanceToSegmentSquared(points[i], points[section.start], points[section.end]);
                    if (distance > largestDistance)
                    {
                        largestDistance = distance;
                        farthest = i;
                    }
                }
                if (farthest < 0) continue;
                keep[farthest] = true;
                pending.Push((section.start, farthest));
                pending.Push((farthest, section.end));
            }
            // 每段的末点由下一段负责写入，避免闭合起点及各锚点重复。
            for (int i = 0; i < points.Count - 1; i++)
                if (keep[i]) output.Add(points[i]);
        }

        private static bool ContainsContour(GraphicsPath outline, IList<PointF> outlinePoints, IList<PointF> contour)
        {
            for (int i = 0; i < contour.Count; i++)
            {
                if (!outline.IsVisible(contour[i])) return false;
                PointF next = contour[(i + 1) % contour.Count];
                for (int j = 0; j < outlinePoints.Count; j++)
                    if (SegmentsIntersect(contour[i], next, outlinePoints[j], outlinePoints[(j + 1) % outlinePoints.Count]))
                        return false;
            }
            return true;
        }

        private static void AddRoundJoin(List<PointF> output, PointF vertex, PointF beforeNormal,
            PointF afterNormal, float distance, double orientation)
        {
            double start = Math.Atan2(beforeNormal.Y, beforeNormal.X);
            double sweep = Math.Atan2(afterNormal.Y, afterNormal.X) - start;
            if (orientation > 0 && sweep < 0) sweep += 2 * Math.PI;
            if (orientation < 0 && sweep > 0) sweep -= 2 * Math.PI;
            double maxAngle = Math.Max(0.0001, Math.Min(Math.PI / 18,
                2 * Math.Acos(Math.Max(-1, 1 - FlatnessPoints / distance))));
            int steps = Math.Max(1, (int)Math.Ceiling(Math.Abs(sweep) / maxAngle));
            AddContourPoint(output, Add(vertex, beforeNormal, distance));
            for (int i = 1; i < steps; i++)
            {
                double angle = start + sweep * i / steps;
                AddContourPoint(output, new PointF((float)(vertex.X + distance * Math.Cos(angle)),
                    (float)(vertex.Y + distance * Math.Sin(angle))));
            }
            AddContourPoint(output, Add(vertex, afterNormal, distance));
        }

        private static PointF Unit(PointF start, PointF end)
        {
            float dx = end.X - start.X, dy = end.Y - start.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.001f) throw new InvalidOperationException("裁切路径包含过短的边");
            return new PointF(dx / length, dy / length);
        }

        private static PointF OutwardNormal(PointF direction, double orientation)
        {
            return orientation > 0 ? new PointF(direction.Y, -direction.X) : new PointF(-direction.Y, direction.X);
        }

        private static PointF Add(PointF point, PointF direction, float distance)
        {
            return new PointF(point.X + direction.X * distance, point.Y + direction.Y * distance);
        }

        private static List<List<PointF>> GetClosedContours(GraphicsPath path)
        {
            var contours = new List<List<PointF>>();
            List<PointF> current = null;
            PointF[] points = path.PathPoints;
            byte[] types = path.PathTypes;
            for (int i = 0; i < points.Length; i++)
            {
                if ((types[i] & 7) == 0)
                {
                    if (current != null && current.Count > 0)
                        throw new InvalidOperationException("路径包含未闭合轮廓");
                    current = new List<PointF>();
                }
                if (current == null) throw new InvalidOperationException("路径缺少起点");
                if ((types[i] & 7) == 3)
                {
                    if (current.Count == 0 || i + 2 >= points.Length)
                        throw new InvalidOperationException("贝塞尔路径数据不完整");
                    AppendBezier(current, current[current.Count - 1], points[i], points[i + 1], points[i + 2], 0);
                    i += 2;
                }
                else
                {
                    AddContourPoint(current, points[i]);
                }
                if ((types[i] & 0x80) != 0)
                {
                    if (current.Count > 1 && DistanceSquared(current[0], current[current.Count - 1]) < 0.000001f)
                        current.RemoveAt(current.Count - 1);
                    if (current.Count < 3) throw new InvalidOperationException("闭合轮廓点数不足");
                    contours.Add(current);
                    current = null;
                }
            }
            if (current != null) throw new InvalidOperationException("路径包含未闭合轮廓");
            return contours;
        }

        private static void AddContourPoint(List<PointF> contour, PointF point)
        {
            if (contour.Count == 0 || DistanceSquared(contour[contour.Count - 1], point) > 0.000001f)
                contour.Add(point);
            if (contour.Count > MaximumPoints) throw new InvalidOperationException("偏移路径过于复杂");
        }

        private static void AppendBezier(List<PointF> contour, PointF p0, PointF p1, PointF p2, PointF p3, int depth)
        {
            if (depth >= 18 ||
                Math.Max(DistanceToSegmentSquared(p1, p0, p3), DistanceToSegmentSquared(p2, p0, p3)) <= FlatnessPoints * FlatnessPoints)
            {
                AddContourPoint(contour, p3);
                return;
            }
            // 自适应二分曲线，避免GDI+离散时对坐标进行额外舍入。
            PointF a = Midpoint(p0, p1), b = Midpoint(p1, p2), c = Midpoint(p2, p3);
            PointF d = Midpoint(a, b), e = Midpoint(b, c), middle = Midpoint(d, e);
            AppendBezier(contour, p0, a, d, middle, depth + 1);
            AppendBezier(contour, middle, e, c, p3, depth + 1);
        }

        private static PointF Midpoint(PointF a, PointF b)
        {
            return new PointF((a.X + b.X) / 2, (a.Y + b.Y) / 2);
        }

        private static double DistanceToSegmentSquared(PointF point, PointF start, PointF end)
        {
            double dx = end.X - start.X, dy = end.Y - start.Y;
            double lengthSquared = dx * dx + dy * dy;
            double t = lengthSquared > 0
                ? Math.Max(0, Math.Min(1, ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / lengthSquared))
                : 0;
            double x = point.X - start.X - t * dx, y = point.Y - start.Y - t * dy;
            return x * x + y * y;
        }

        private static bool IsSimple(IList<PointF> points)
        {
            int count = points.Count;
            if (count < 3 || Math.Abs(SignedArea(points)) < 0.01) return false;
            for (int i = 0; i < count; i++)
            {
                PointF a = points[i], b = points[(i + 1) % count];
                if (DistanceSquared(a, b) < 0.000001f) return false;
                for (int j = i + 2; j < count; j++)
                {
                    if (i == 0 && j == count - 1) continue;
                    if (SegmentsIntersect(a, b, points[j], points[(j + 1) % count])) return false;
                }
            }
            return true;
        }

        private static bool SegmentsIntersect(PointF a, PointF b, PointF c, PointF d)
        {
            double abC = Cross(a, b, c), abD = Cross(a, b, d);
            double cdA = Cross(c, d, a), cdB = Cross(c, d, b);
            return abC * abD <= 0 && cdA * cdB <= 0 &&
                   Math.Max(Math.Min(a.X, b.X), Math.Min(c.X, d.X)) <= Math.Min(Math.Max(a.X, b.X), Math.Max(c.X, d.X)) &&
                   Math.Max(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y)) <= Math.Min(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y));
        }

        private static double Cross(PointF a, PointF b, PointF c)
        {
            return (double)(b.X - a.X) * (c.Y - a.Y) - (double)(b.Y - a.Y) * (c.X - a.X);
        }

        private static double Cross(PointF a, PointF b)
        {
            return (double)a.X * b.Y - (double)a.Y * b.X;
        }

        private static double SignedArea(IList<PointF> points)
        {
            double sum = 0;
            for (int i = 0; i < points.Count; i++)
            {
                PointF a = points[i], b = points[(i + 1) % points.Count];
                sum += (double)a.X * b.Y - (double)b.X * a.Y;
            }
            return sum / 2;
        }

        private static float DistanceSquared(PointF a, PointF b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }
    }
}
