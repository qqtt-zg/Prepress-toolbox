using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using iText.Kernel.Pdf;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Layer;
using iText.Kernel.Colors;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 异形PDF处理器 - 为PDF添加裁切路径图层和出血线图层
    /// 从模板页（最后一页）提取裁切路径，应用到所有内容页
    /// </summary>
    public static class PdfSpecialShapeProcessor
    {
        /// <summary>
        /// 处理异形PDF文件：提取模板页裁切路径，添加图层，删除模板页
        /// </summary>
        public static bool ProcessSpecialShapePdf(string filePath, double bleedMm = 0,
            bool normalizeDisplayedSize = true)
        {
            LogHelper.Debug("ProcessSpecialShapePdf: " + filePath);
            string tempFilePath = null;
            string preparedInputPath = null;
            try
            {
                if (!File.Exists(filePath))
                {
                    LogHelper.Debug("文件不存在: " + filePath);
                    return false;
                }

                if (double.IsNaN(bleedMm) || double.IsInfinity(bleedMm) || bleedMm < 0)
                {
                    LogHelper.Debug("出血值无效: " + bleedMm);
                    return false;
                }

                if (!System.IO.Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                string tempFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PDFToolCache");
                Directory.CreateDirectory(tempFolder);
                tempFilePath = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName() + ".pdf");

                PdfReader reader = null;
                PdfWriter writer = null;
                PdfDocument document = null;
                GraphicsPath cutPath = null;
                GraphicsPath bleedPath = null;

                try
                {
                    // 先在副本上确定最终统一裁剪框，再调整路径，避免后续改框导致目标宽高失配。
                    preparedInputPath = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName() + ".pdf");
                    File.Copy(filePath, preparedInputPath);
                    if (!PdfTools.SetAllPageBoxesToCropBox(preparedInputPath, normalizeDisplayedSize))
                        return false;

                    reader = new PdfReader(preparedInputPath);
                    writer = new PdfWriter(tempFilePath);
                    document = new PdfDocument(reader, writer);

                    if (document.GetNumberOfPages() < 2)
                    {
                        LogHelper.Debug("文档页数不足2页，无法执行异形处理");
                        return false;
                    }

                    List<int> originalPageRotations = new List<int>();
                    for (int i = 1; i <= document.GetNumberOfPages(); i++)
                    {
                        PdfPage page = document.GetPage(i);
                        originalPageRotations.Add(page.GetRotation());
                    }

                    int lastPageIndex = document.GetNumberOfPages();
                    PdfPage lastPage = document.GetPage(lastPageIndex);
                    int originalLastPageRotation = lastPage.GetRotation();
                    lastPage.SetRotation(0);

                    PdfLayer addCounterLayer = new PdfLayer("Dots_AddCounter", document);
                    PdfLayer bleedLayer = new PdfLayer("Dots_L_B_出血线", document);

                    Rectangle lastPageSize = lastPage.GetCropBox() ?? lastPage.GetMediaBox();
                    float templateCropLeft = (float)(lastPageSize.GetLeft());
                    float templateCropBottom = (float)(lastPageSize.GetBottom());
                    LogHelper.Debug("模板页CropBox原点: (" + templateCropLeft + ", " + templateCropBottom + ")");

                    cutPath = PdfCutPathExtractor.ExtractClosedCutPath(lastPage);
                    if (cutPath == null)
                    {
                        LogHelper.Debug("模板页没有唯一闭合裁切路径，跳过异形处理");
                        return false;
                    }
                    // 以统一页框中心调整裁切线，所有内容页共用目标尺寸和完整出血值。
                    bleedPath = FitCutPathToCropBox(cutPath, lastPageSize, (float)(bleedMm * 72 / 25.4),
                        out float scaleX, out float scaleY);
                    LogHelper.Debug($"异形裁切路径按页面中心适配: scaleX={scaleX:0.######}, scaleY={scaleY:0.######}, bleed={bleedMm}mm, CropBox={lastPageSize}");

                    for (int i = 1; i < lastPageIndex; i++)
                    {
                        PdfPage currentPage = document.GetPage(i);
                        int originalCurrentPageRotation = currentPage.GetRotation();
                        currentPage.SetRotation(0);

                        Rectangle currentPageSize = currentPage.GetCropBox() ?? currentPage.GetMediaBox();
                        float centerX = (float)((currentPageSize.GetWidth() - lastPageSize.GetWidth()) / 2);
                        float centerY = (float)((currentPageSize.GetHeight() - lastPageSize.GetHeight()) / 2);
                        LogHelper.Debug("第" + i + "页居中位置: X=" + centerX + ", Y=" + centerY);

                        float offsetX = currentPageSize.GetLeft() - templateCropLeft + centerX;
                        float offsetY = currentPageSize.GetBottom() - templateCropBottom + centerY;
                        var bleedBounds = bleedPath.GetBounds();
                        if (GetPageOverflow(bleedBounds, currentPageSize, offsetX, offsetY) > 0.01f)
                        {
                            LogHelper.Debug("出血轮廓超出页面可视范围，第" + i + "页");
                            return false;
                        }

                        // 两条路径共用同一个定位变换，避免模板页和内容页尺寸不同时错位。
                        DrawLayerPath(currentPage, bleedLayer, bleedPath, ColorConstants.GREEN, offsetX, offsetY);
                        DrawLayerPath(currentPage, addCounterLayer, cutPath, ColorConstants.RED, offsetX, offsetY);

                        currentPage.SetRotation(originalCurrentPageRotation);
                    }

                    lastPage.SetRotation(originalLastPageRotation);
                    document.RemovePage(lastPageIndex);
                    LogHelper.Debug("删除最后一页，剩余页数: " + document.GetNumberOfPages());

                    for (int i = 1; i <= document.GetNumberOfPages(); i++)
                    {
                        document.GetPage(i).SetRotation(originalPageRotations[i - 1]);
                    }

                    LogHelper.Debug("保存修改后的文档到临时文件：" + tempFilePath);
                }
                finally
                {
                    bleedPath?.Dispose();
                    cutPath?.Dispose();
                    document?.Close();
                    writer?.Close();
                    reader?.Close();
                }

                if (!WindowsFormsApp3.Utils.PdfTools.SetAllPageBoxesToCropBox(tempFilePath))
                {
                    LogHelper.Debug("ProcessSpecialShapePdf最终页面框统一失败");
                    return false;
                }
                File.Delete(filePath);
                File.Move(tempFilePath, filePath);

                LogHelper.Debug("ProcessSpecialShapePdf执行GC清理，处理完成");

                GC.Collect();
                GC.WaitForPendingFinalizers();

                return true;
            }
            catch (iText.Kernel.Exceptions.PdfException pdfEx)
            {
                LogHelper.Debug("iText 7 PDF异常: " + pdfEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.Debug("处理异形PDF失败: " + ex.Message);
                return false;
            }
            finally
            {
                if (tempFilePath != null)
                {
                    try { File.Delete(tempFilePath); }
                    catch (IOException) { }
                }
                if (preparedInputPath != null)
                {
                    try { File.Delete(preparedInputPath); }
                    catch (IOException) { }
                }
            }
        }

        /// <summary>
        /// 将裁切轮廓的实际外接框调整至裁剪框四边各减出血的区域，目标中心与页面中心重合。
        /// 宽高分别缩放，可放大或缩小；只在成功时修改输入路径。
        /// </summary>
        internal static GraphicsPath FitCutPathToCropBox(GraphicsPath cutPath, Rectangle cropBox,
            float bleedPoints, out float scaleX, out float scaleY)
        {
            if (cutPath == null) throw new ArgumentNullException(nameof(cutPath));
            if (cropBox == null) throw new ArgumentNullException(nameof(cropBox));
            if (float.IsNaN(bleedPoints) || float.IsInfinity(bleedPoints) || bleedPoints < 0)
                throw new ArgumentOutOfRangeException(nameof(bleedPoints));
            if (bleedPoints == 0 && !PdfBleedPathOffsetter.CanOffset(cutPath))
                throw new InvalidOperationException("裁切路径必须是单条无自交的闭合轮廓");

            scaleX = 1;
            scaleY = 1;
            float targetWidth = cropBox.GetWidth() - bleedPoints * 2;
            float targetHeight = cropBox.GetHeight() - bleedPoints * 2;
            if (targetWidth <= 0 || targetHeight <= 0)
                throw new InvalidOperationException("裁剪框尺寸不足以容纳两侧出血");

            var bounds = GetGeometricBounds(cutPath);
            if (bounds.Width <= 0 || bounds.Height <= 0)
                throw new InvalidOperationException("裁切路径尺寸无效");
            float horizontalScale = targetWidth / bounds.Width;
            float verticalScale = targetHeight / bounds.Height;
            var transformedPoints = cutPath.PathPoints;
            for (int i = 0; i < transformedPoints.Length; i++)
            {
                transformedPoints[i] = new System.Drawing.PointF(
                    (float)(cropBox.GetLeft() + bleedPoints + (double)(transformedPoints[i].X - bounds.Left) * horizontalScale),
                    (float)(cropBox.GetBottom() + bleedPoints + (double)(transformedPoints[i].Y - bounds.Top) * verticalScale));
            }
            using (var fittedCut = new GraphicsPath(transformedPoints, cutPath.PathTypes, cutPath.FillMode))
            {
                GraphicsPath candidateBleed = bleedPoints > 0
                    ? PdfBleedPathOffsetter.CreateOutset(fittedCut, bleedPoints)
                    : (GraphicsPath)fittedCut.Clone();
                try
                {
                    var bleedBounds = GetGeometricBounds(candidateBleed);
                    float overflow = GetPageOverflow(bleedBounds, cropBox, 0, 0);
                    if (overflow > 0.01f)
                        throw new InvalidOperationException($"适配后的完整出血轮廓超出裁剪框: {overflow}pt, bounds={bleedBounds}");
                    cutPath.Reset();
                    cutPath.AddPath(fittedCut, false);
                    scaleX = horizontalScale;
                    scaleY = verticalScale;
                    GraphicsPath result = candidateBleed;
                    candidateBleed = null;
                    return result;
                }
                finally
                {
                    candidateBleed?.Dispose();
                }
            }
        }

        /// <summary>计算线段与三次贝塞尔曲线的真实极值，排除不在轮廓上的控制点。</summary>
        internal static System.Drawing.RectangleF GetGeometricBounds(GraphicsPath path)
        {
            var points = path.PathPoints;
            var types = path.PathTypes;
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            Action<double, double> include = (x, y) =>
            {
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            };
            System.Drawing.PointF current = default;
            for (int i = 0; i < points.Length; i++)
            {
                if ((types[i] & 7) != 3)
                {
                    current = points[i];
                    include(current.X, current.Y);
                    continue;
                }
                if (i + 2 >= points.Length) throw new InvalidOperationException("贝塞尔路径数据不完整");
                var p0 = current; var p1 = points[i]; var p2 = points[i + 1]; var p3 = points[i + 2];
                include(p3.X, p3.Y);
                Action<double> evaluate = t =>
                {
                    if (t <= 0 || t >= 1) return;
                    double u = 1 - t;
                    include(u * u * u * p0.X + 3 * u * u * t * p1.X + 3 * u * t * t * p2.X + t * t * t * p3.X,
                        u * u * u * p0.Y + 3 * u * u * t * p1.Y + 3 * u * t * t * p2.Y + t * t * t * p3.Y);
                };
                IncludeBezierExtrema(p0.X, p1.X, p2.X, p3.X, evaluate);
                IncludeBezierExtrema(p0.Y, p1.Y, p2.Y, p3.Y, evaluate);
                current = p3;
                i += 2;
            }
            if (points.Length == 0) throw new InvalidOperationException("裁切路径为空");
            return System.Drawing.RectangleF.FromLTRB((float)minX, (float)minY, (float)maxX, (float)maxY);
        }

        private static void IncludeBezierExtrema(double p0, double p1, double p2, double p3, Action<double> evaluate)
        {
            double a = -p0 + 3 * p1 - 3 * p2 + p3;
            double b = 2 * (p0 - 2 * p1 + p2);
            double c = p1 - p0;
            if (Math.Abs(a) < 1e-10)
            {
                if (Math.Abs(b) > 1e-10) evaluate(-c / b);
                return;
            }
            double discriminant = b * b - 4 * a * c;
            if (discriminant < 0) return;
            double root = Math.Sqrt(discriminant);
            evaluate((-b + root) / (2 * a));
            evaluate((-b - root) / (2 * a));
        }

        private static float GetPageOverflow(System.Drawing.RectangleF bounds, Rectangle pageBox, float offsetX, float offsetY)
        {
            return Math.Max(0, Math.Max(
                Math.Max(pageBox.GetLeft() - bounds.Left - offsetX, bounds.Right + offsetX - pageBox.GetRight()),
                Math.Max(pageBox.GetBottom() - bounds.Top - offsetY, bounds.Bottom + offsetY - pageBox.GetTop())));
        }

        private static void DrawLayerPath(PdfPage page, PdfLayer layer,
            GraphicsPath path, iText.Kernel.Colors.Color color, float offsetX, float offsetY)
        {
            // 隔离原页面遗留的虚线、变换和裁剪状态，防止连续刀路被导出为多段抬落刀。
            var canvas = new PdfCanvas(page, true);
            canvas.SaveState();
            canvas.BeginLayer(layer);
            canvas.ConcatMatrix(1, 0, 0, 1, offsetX, offsetY);
            canvas.SetLineDash(Array.Empty<float>(), 0);
            canvas.SetLineCapStyle(0);
            canvas.SetLineJoinStyle(0);
            canvas.SetLineWidth(0.01f);
            canvas.SetStrokeColor(color);
            AppendPath(canvas, path);
            canvas.Stroke();
            canvas.EndLayer();
            canvas.RestoreState();
            canvas.Release();
        }

        private static void AppendPath(PdfCanvas canvas, GraphicsPath path)
        {
            var points = path.PathPoints;
            var types = path.PathTypes;
            for (int i = 0; i < points.Length; i++)
            {
                int kind = types[i] & 7;
                if (kind == 0) canvas.MoveTo(points[i].X, points[i].Y);
                else if (kind == 1) canvas.LineTo(points[i].X, points[i].Y);
                else if (kind == 3 && i + 2 < points.Length)
                {
                    canvas.CurveTo(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y,
                        points[i + 2].X, points[i + 2].Y);
                    i += 2;
                }
                else throw new InvalidOperationException("不支持的裁切路径指令");

                if ((types[i] & 0x80) != 0) canvas.ClosePath();
            }
        }
    }
}
