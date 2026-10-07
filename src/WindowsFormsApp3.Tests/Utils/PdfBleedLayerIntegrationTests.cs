using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using WindowsFormsApp3.Utils;
using Xunit;
using PdfMatrix = iText.Kernel.Geom.Matrix;
using PageSize = iText.Kernel.Geom.PageSize;

namespace WindowsFormsApp3.Tests.Utils
{
    public class PdfBleedLayerIntegrationTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SpecialShape_DoesNotInheritDashedStrokeFromContentPage(bool normalizeDisplayedSize)
        {
            string filePath = CreatePdf(true);
            try
            {
                using (var document = new PdfDocument(new PdfWriter(filePath)))
                {
                    var canvas = new PdfCanvas(document.AddNewPage(new PageSize(220, 220)));
                    // 模拟原页面留下虚线与非默认端点、连接方式，且未用q/Q恢复状态。
                    canvas.SetStrokeColor(new iText.Kernel.Colors.DeviceRgb(0, 0, 0));
                    canvas.SetLineDash(new float[] { 4, 3 }, 1).SetLineCapStyle(1).SetLineJoinStyle(2);
                    canvas.MoveTo(10, 10).LineTo(30, 10).Stroke();
                    canvas.Release();
                    var templateCanvas = new PdfCanvas(document.AddNewPage(new PageSize(220, 220)));
                    templateCanvas.Rectangle(40, 40, 140, 140).Stroke();
                    templateCanvas.Release();
                }
                Assert.True(PdfTools.AddDotsAddCounterLayer(filePath, "", ShapeType.Special, 0, 5,
                    normalizeDisplayedSize: normalizeDisplayedSize));
                using (var document = new PdfDocument(new PdfReader(filePath)))
                using (var listener = new StrokePathListener())
                {
                    new PdfCanvasProcessor(listener).ProcessPageContent(document.GetPage(1));
                    Assert.NotNull(listener.Red);
                    Assert.NotNull(listener.Green);
                    Assert.Empty(listener.RedDashPattern);
                    Assert.Empty(listener.GreenDashPattern);
                    Assert.Equal(new float[] { 4, 3 }, listener.OriginalBlackDashPattern);
                    Assert.Equal(0, listener.RedLineCap);
                    Assert.Equal(0, listener.RedLineJoin);
                    Assert.Equal(1, listener.RedSubpathCount);
                    Assert.True(listener.RedIsClosed);
                }
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Theory]
        [InlineData(ShapeType.RightAngle, 0)]
        [InlineData(ShapeType.Circle, 0)]
        [InlineData(ShapeType.RoundRect, 5)]
        public void StandardShapes_CreateOutsetBleedLine(ShapeType shape, double radius)
        {
            string filePath = CreatePdf(false);
            try
            {
                Assert.True(PdfTools.AddDotsAddCounterLayer(filePath, "50x50", shape, radius, 5));
                using (var reader = new PdfReader(filePath))
                using (var document = new PdfDocument(reader))
                using (var listener = new StrokePathListener())
                {
                    new PdfCanvasProcessor(listener).ProcessPageContent(document.GetPage(1));
                    Assert.NotNull(listener.Red);
                    Assert.NotNull(listener.Green);
                    AssertOutsetBounds(listener.Red.GetBounds(), listener.Green.GetBounds(), 5 * 72 / 25.4);
                    if (shape != ShapeType.RightAngle)
                    {
                        Assert.True(listener.Green.PointCount > 4);
                        Assert.True(listener.GreenHasBezier, "PDF中的出血线应保留贝塞尔曲线");
                    }
                    if (shape == ShapeType.Circle)
                        Assert.True(listener.Green.PointCount < 300, "圆形出血线应减少密集采样节点");
                }
            }
            finally { File.Delete(filePath); }
        }

        [Fact]
        public void SpecialConcaveShape_UsesTemplateCutContour()
        {
            string filePath = CreatePdf(true);
            try
            {
                using (var reader = new PdfReader(filePath))
                using (var document = new PdfDocument(reader))
                using (var cutPath = WindowsFormsApp3.Services.PdfCutPathExtractor.ExtractClosedCutPath(document.GetPage(2)))
                {
                    Assert.NotNull(cutPath);
                    using (var bleedPath = WindowsFormsApp3.Services.PdfBleedPathOffsetter.CreateOutset(cutPath, (float)(5 * 72 / 25.4)))
                        Assert.NotNull(bleedPath);
                }
                Assert.True(PdfTools.AddDotsAddCounterLayer(filePath, "", ShapeType.Special, 0, 5));
                using (var reader = new PdfReader(filePath))
                using (var document = new PdfDocument(reader))
                using (var listener = new StrokePathListener())
                {
                    Assert.Equal(1, document.GetNumberOfPages());
                    new PdfCanvasProcessor(listener).ProcessPageContent(document.GetPage(1));
                    Assert.NotNull(listener.Red);
                    Assert.NotNull(listener.Green);
                    AssertOutsetBounds(listener.Red.GetBounds(), listener.Green.GetBounds(), 5 * 72 / 25.4);
                    Assert.True(listener.Green.IsVisible(60, 60));
                    Assert.False(listener.Green.IsVisible(140, 140));
                    RectangleF cutBounds = listener.Red.GetBounds();
                    var cropBox = document.GetPage(1).GetCropBox();
                    double bleedPoints = 5 * 72 / 25.4;
                    Assert.InRange(cutBounds.Width, cropBox.GetWidth() - 2 * bleedPoints - 0.05,
                        cropBox.GetWidth() - 2 * bleedPoints + 0.05);
                    Assert.InRange(cutBounds.Height, cropBox.GetHeight() - 2 * bleedPoints - 0.05,
                        cropBox.GetHeight() - 2 * bleedPoints + 0.05);
                    Assert.InRange(cutBounds.Left + cutBounds.Width / 2, cropBox.GetWidth() / 2 - 0.05,
                        cropBox.GetWidth() / 2 + 0.05);
                }
            }
            finally { File.Delete(filePath); }
        }

        [Fact]
        public void ZeroBleed_UsesCutContourWithoutExpansion()
        {
            string filePath = CreatePdf(false);
            try
            {
                Assert.True(PdfTools.AddDotsAddCounterLayer(filePath, "50x50", ShapeType.Circle, 0, 0));
                using (var reader = new PdfReader(filePath))
                using (var document = new PdfDocument(reader))
                using (var listener = new StrokePathListener())
                {
                    new PdfCanvasProcessor(listener).ProcessPageContent(document.GetPage(1));
                    Assert.NotNull(listener.Red);
                    Assert.NotNull(listener.Green);
                    RectangleF red = listener.Red.GetBounds(), green = listener.Green.GetBounds();
                    Assert.InRange(Math.Abs(red.Left - green.Left), 0, 0.3);
                    Assert.InRange(Math.Abs(red.Right - green.Right), 0, 0.3);
                }
            }
            finally { File.Delete(filePath); }
        }

        [Fact]
        public void NarrowEllipse_WithExactTwoMillimeterMargin_StillProducesOutput()
        {
            string filePath = Path.Combine(Path.GetTempPath(), "PdfBleed_Narrow_" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                using (var document = new PdfDocument(new PdfWriter(filePath)))
                {
                    document.AddNewPage(new PageSize((float)(40 * 72 / 25.4), (float)(10 * 72 / 25.4)));
                }

                Assert.True(PdfTools.AddDotsAddCounterLayer(filePath, "36x6C", ShapeType.Circle, 0, 2));
                using (var reader = new PdfReader(filePath))
                using (var document = new PdfDocument(reader))
                using (var listener = new StrokePathListener())
                {
                    new PdfCanvasProcessor(listener).ProcessPageContent(document.GetPage(1));
                    Assert.NotNull(listener.Red);
                    Assert.NotNull(listener.Green);
                    AssertOutsetBounds(listener.Red.GetBounds(), listener.Green.GetBounds(), 2 * 72 / 25.4);
                }
            }
            finally { File.Delete(filePath); }
        }

        [Fact]
        public void RoundedRectangle_WithSlightlyRoundedPdfPageSize_StillProducesOutput()
        {
            string filePath = Path.Combine(Path.GetTempPath(), "PdfBleed_RoundedPage_" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                using (var document = new PdfDocument(new PdfWriter(filePath)))
                {
                    document.AddNewPage(new PageSize(153.071f, 209.754f));
                }

                Assert.True(PdfTools.AddDotsAddCounterLayer(filePath, "71x51R3", ShapeType.RoundRect, 3, 1.5));
                using (var reader = new PdfReader(filePath))
                using (var document = new PdfDocument(reader))
                using (var listener = new StrokePathListener())
                {
                    new PdfCanvasProcessor(listener).ProcessPageContent(document.GetPage(1));
                    Assert.NotNull(listener.Red);
                    Assert.NotNull(listener.Green);
                    RectangleF green = listener.Green.GetBounds();
                    Assert.True(green.Left >= -0.01f && green.Top >= -0.01f);
                    Assert.True(green.Right <= 153.081f && green.Bottom <= 209.764f);
                }
            }
            finally { File.Delete(filePath); }
        }

        [Fact]
        public void Rectangle_WithSlightlyRoundedPdfPageSize_StillProducesOutput()
        {
            string filePath = Path.Combine(Path.GetTempPath(), "PdfBleed_RectPage_" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                using (var document = new PdfDocument(new PdfWriter(filePath)))
                {
                    document.AddNewPage(new PageSize(153.071f, 209.754f));
                }

                Assert.True(PdfTools.AddDotsAddCounterLayer(filePath, "71x51Z", ShapeType.RightAngle, 0, 1.5));
                using (var reader = new PdfReader(filePath))
                using (var document = new PdfDocument(reader))
                using (var listener = new StrokePathListener())
                {
                    new PdfCanvasProcessor(listener).ProcessPageContent(document.GetPage(1));
                    Assert.NotNull(listener.Red);
                    Assert.NotNull(listener.Green);
                    Assert.True(listener.Green.GetBounds().Right <= 153.081f);
                    Assert.True(listener.Green.GetBounds().Bottom <= 209.764f);
                }
            }
            finally { File.Delete(filePath); }
        }

        [Fact]
        public void Rectangle_WithMateriallyInsufficientMargin_RemainsRejected()
        {
            string filePath = Path.Combine(Path.GetTempPath(), "PdfBleed_RectTooSmall_" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                using (var document = new PdfDocument(new PdfWriter(filePath)))
                {
                    document.AddNewPage(new PageSize(150, 200));
                }
                byte[] before = File.ReadAllBytes(filePath);

                Assert.False(PdfTools.AddDotsAddCounterLayer(filePath, "71x51Z", ShapeType.RightAngle, 0, 1.5));
                Assert.Equal(before, File.ReadAllBytes(filePath));
            }
            finally { File.Delete(filePath); }
        }

        [Fact]
        public void SpecialShape_WithMinorPageRounding_StillProducesOutput()
        {
            string filePath = Path.Combine(Path.GetTempPath(), "PdfBleed_SpecialPage_" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                using (var document = new PdfDocument(new PdfWriter(filePath)))
                {
                    document.AddNewPage(new PageSize(220, 220));
                    PdfPage template = document.AddNewPage(new PageSize(220, 220));
                    var canvas = new PdfCanvas(template);
                    canvas.MoveTo(4.23, 4.23).LineTo(215.77, 4.23).LineTo(215.77, 70)
                        .LineTo(70, 70).LineTo(70, 215.77).LineTo(4.23, 215.77).ClosePathStroke();
                    canvas.Release();
                }

                Assert.True(PdfTools.AddDotsAddCounterLayer(filePath, "", ShapeType.Special, 0, 1.5));
                using (var reader = new PdfReader(filePath))
                using (var document = new PdfDocument(reader))
                using (var listener = new StrokePathListener())
                {
                    Assert.Equal(1, document.GetNumberOfPages());
                    new PdfCanvasProcessor(listener).ProcessPageContent(document.GetPage(1));
                    Assert.NotNull(listener.Red);
                    Assert.NotNull(listener.Green);
                    RectangleF bounds = listener.Green.GetBounds();
                    Assert.True(bounds.Left >= -0.01f && bounds.Top >= -0.01f);
                    Assert.True(bounds.Right <= 220.01f && bounds.Bottom <= 220.01f);
                }
            }
            finally { File.Delete(filePath); }
        }

        [Fact]
        public void SpecialShape_WithPdfTransform_OffsetsTransformedContour()
        {
            string filePath = CreatePdf(true, transformed: true);
            try
            {
                Assert.True(PdfTools.CanExtractCutPathFromLastPage(filePath));
                Assert.True(PdfTools.AddDotsAddCounterLayer(filePath, "", ShapeType.Special, 0, 5));
                using (var reader = new PdfReader(filePath))
                using (var document = new PdfDocument(reader))
                using (var listener = new StrokePathListener())
                {
                    new PdfCanvasProcessor(listener).ProcessPageContent(document.GetPage(1));
                    Assert.NotNull(listener.Red);
                    Assert.NotNull(listener.Green);
                    AssertOutsetBounds(listener.Red.GetBounds(), listener.Green.GetBounds(), 5 * 72 / 25.4);
                }
            }
            finally { File.Delete(filePath); }
        }

        [Fact]
        public void SpecialShape_WithTwoCutCandidates_LeavesSourceUnchanged()
        {
            string filePath = CreatePdf(true, secondCut: true);
            try
            {
                byte[] before = File.ReadAllBytes(filePath);
                Assert.False(PdfTools.CanExtractCutPathFromLastPage(filePath));
                Assert.False(PdfTools.AddDotsAddCounterLayer(filePath, "", ShapeType.Special, 0, 5));
                Assert.Equal(before, File.ReadAllBytes(filePath));
            }
            finally { File.Delete(filePath); }
        }

        [Fact]
        public void SpecialBezierShape_CreatesOutsetBleedLine()
        {
            string filePath = CreatePdf(true, curved: true);
            try
            {
                Assert.True(PdfTools.AddDotsAddCounterLayer(filePath, "", ShapeType.Special, 0, 5));
                using (var reader = new PdfReader(filePath))
                using (var document = new PdfDocument(reader))
                using (var listener = new StrokePathListener())
                {
                    new PdfCanvasProcessor(listener).ProcessPageContent(document.GetPage(1));
                    Assert.NotNull(listener.Red);
                    Assert.NotNull(listener.Green);
                    AssertOutsetBounds(listener.Red.GetBounds(), listener.Green.GetBounds(), 5 * 72 / 25.4);
                    Assert.True(listener.Green.PointCount > 20);
                }
            }
            finally { File.Delete(filePath); }
        }

        [Fact]
        public void SpecialOpenPath_LeavesSourceUnchanged()
        {
            string filePath = CreatePdf(true, open: true);
            try
            {
                byte[] before = File.ReadAllBytes(filePath);
                Assert.False(PdfTools.CanExtractCutPathFromLastPage(filePath));
                Assert.False(PdfTools.AddDotsAddCounterLayer(filePath, "", ShapeType.Special, 0, 5));
                Assert.Equal(before, File.ReadAllBytes(filePath));
            }
            finally { File.Delete(filePath); }
        }

        [Fact]
        public void SpecialSelfIntersectingPath_IsRejectedByShapeValidation()
        {
            string filePath = CreatePdf(true, selfIntersecting: true);
            try
            {
                byte[] before = File.ReadAllBytes(filePath);
                Assert.False(PdfTools.CanExtractCutPathFromLastPage(filePath));
                Assert.False(PdfTools.AddDotsAddCounterLayer(filePath, "", ShapeType.Special, 0, 5));
                Assert.Equal(before, File.ReadAllBytes(filePath));
            }
            finally { File.Delete(filePath); }
        }

        [Fact]
        public void SpecialShape_WithImpossibleBleed_ReturnsFailureWithoutChangingSource()
        {
            string filePath = CreatePdf(true);
            try
            {
                byte[] before = File.ReadAllBytes(filePath);
                Assert.False(PdfTools.AddDotsAddCounterLayer(filePath, "", ShapeType.Special, 0, 40));
                Assert.Equal(before, File.ReadAllBytes(filePath));
            }
            finally { File.Delete(filePath); }
        }

        [Fact]
        public void SpecialShape_MixedPageFrames_UsesUnifiedCenteredTargetForEveryContentPage()
        {
            string filePath = Path.Combine(Path.GetTempPath(), "PdfBleed_MixedSpecial_" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                using (var document = new PdfDocument(new PdfWriter(filePath)))
                {
                    document.AddNewPage(new PageSize(220, 220));
                    document.AddNewPage(new PageSize(240, 250));
                    var canvas = new PdfCanvas(document.AddNewPage(new PageSize(220, 220)));
                    canvas.Rectangle(40, 50, 100, 80).Stroke();
                    canvas.Release();
                }
                Assert.True(PdfTools.AddDotsAddCounterLayer(filePath, "", ShapeType.Special, 0, 5,
                    normalizeDisplayedSize: false));
                using (var document = new PdfDocument(new PdfReader(filePath)))
                {
                    Assert.Equal(2, document.GetNumberOfPages());
                    for (int pageNumber = 1; pageNumber <= 2; pageNumber++)
                    {
                        using (var listener = new StrokePathListener())
                        {
                            PdfPage page = document.GetPage(pageNumber);
                            new PdfCanvasProcessor(listener).ProcessPageContent(page);
                            Assert.NotNull(listener.Red);
                            Assert.NotNull(listener.Green);
                            var cut = listener.Red.GetBounds();
                            double target = page.GetCropBox().GetWidth() - 2 * 5 * 72 / 25.4;
                            Assert.InRange(cut.Width, target - 0.03, target + 0.03);
                            Assert.InRange(cut.Height, target - 0.03, target + 0.03);
                            Assert.InRange(cut.Left + cut.Width / 2, 109.98, 110.02);
                            Assert.InRange(cut.Top + cut.Height / 2, 109.98, 110.02);
                        }
                    }
                }
            }
            finally { File.Delete(filePath); }
        }

        private static string CreatePdf(bool withSpecialTemplate, bool transformed = false, bool secondCut = false,
            bool curved = false, bool open = false, bool selfIntersecting = false)
        {
            string filePath = Path.Combine(Path.GetTempPath(), "PdfBleed_" + Guid.NewGuid().ToString("N") + ".pdf");
            using (var document = new PdfDocument(new PdfWriter(filePath)))
            {
                document.AddNewPage(new PageSize(220, 220));
                if (withSpecialTemplate)
                {
                    PdfPage template = document.AddNewPage(new PageSize(220, 220));
                    var canvas = new PdfCanvas(template);
                    canvas.SetLineWidth(0.01f);
                    if (transformed) canvas.SaveState().ConcatMatrix(0, 1, -1, 0, 200, 0);
                    if (curved)
                    {
                        canvas.MoveTo(40, 100).CurveTo(40, 40, 160, 40, 160, 100)
                            .CurveTo(160, 160, 40, 160, 40, 100).ClosePathStroke();
                    }
                    else if (open)
                    {
                        canvas.MoveTo(40, 40).LineTo(160, 40).LineTo(160, 70).Stroke();
                    }
                    else if (selfIntersecting)
                    {
                        canvas.MoveTo(40, 40).LineTo(160, 160).LineTo(40, 160)
                            .LineTo(160, 40).ClosePathStroke();
                    }
                    else
                    {
                        canvas.MoveTo(40, 40).LineTo(160, 40).LineTo(160, 70)
                            .LineTo(70, 70).LineTo(70, 160).LineTo(40, 160).ClosePathStroke();
                    }
                    if (transformed) canvas.RestoreState();
                    if (secondCut) canvas.Rectangle(10, 10, 15, 15).Stroke();
                    canvas.Release();
                }
            }
            return filePath;
        }

        private static void AssertOutsetBounds(RectangleF cut, RectangleF bleed, double distance)
        {
            Assert.InRange(cut.Left - bleed.Left, distance - 0.35, distance + 0.35);
            Assert.InRange(cut.Top - bleed.Top, distance - 0.35, distance + 0.35);
            Assert.InRange(bleed.Right - cut.Right, distance - 0.35, distance + 0.35);
            Assert.InRange(bleed.Bottom - cut.Bottom, distance - 0.35, distance + 0.35);
        }

        private sealed class StrokePathListener : IEventListener, IDisposable
        {
            public GraphicsPath Red { get; private set; }
            public GraphicsPath Green { get; private set; }
            public bool GreenHasBezier { get; private set; }
            public float[] RedDashPattern { get; private set; }
            public float[] GreenDashPattern { get; private set; }
            public float[] OriginalBlackDashPattern { get; private set; }
            public int RedLineCap { get; private set; }
            public int RedLineJoin { get; private set; }
            public int RedSubpathCount { get; private set; }
            public bool RedIsClosed { get; private set; }

            public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_PATH };

            public void EventOccurred(IEventData data, EventType type)
            {
                if (type != EventType.RENDER_PATH || !(data is PathRenderInfo info) ||
                    (info.GetOperation() & PathRenderInfo.STROKE) == 0) return;
                float[] color = info.GetStrokeColor()?.GetColorValue();
                if (color == null || color.Length != 3) return;
                if (color[0] == 0 && color[1] == 0 && color[2] == 0)
                    OriginalBlackDashPattern = info.GetLineDashPattern().GetAsArray(0).ToFloatArray();

                var points = new List<PointF>();
                bool hasBezier = false;
                PdfMatrix matrix = info.GetCtm();
                foreach (var subpath in info.GetPath().GetSubpaths())
                {
                    foreach (var segment in subpath.GetSegments())
                        if (segment is iText.Kernel.Geom.BezierCurve) hasBezier = true;
                    foreach (var point in subpath.GetPiecewiseLinearApproximation())
                    {
                        double x = point.GetX(), y = point.GetY();
                        points.Add(new PointF((float)(x * matrix.Get(PdfMatrix.I11) + y * matrix.Get(PdfMatrix.I21) + matrix.Get(PdfMatrix.I31)),
                            (float)(x * matrix.Get(PdfMatrix.I12) + y * matrix.Get(PdfMatrix.I22) + matrix.Get(PdfMatrix.I32))));
                    }
                }
                if (points.Count < 3) return;
                var path = new GraphicsPath();
                path.AddPolygon(points.ToArray());
                if (color[0] > color[1] && color[0] > color[2])
                {
                    Red?.Dispose();
                    Red = path;
                    RedDashPattern = info.GetLineDashPattern().GetAsArray(0).ToFloatArray();
                    RedLineCap = info.GetLineCapStyle();
                    RedLineJoin = info.GetLineJoinStyle();
                    RedSubpathCount = 0;
                    foreach (var subpath in info.GetPath().GetSubpaths())
                        if (subpath.GetSegments().Count > 0) RedSubpathCount++;
                    RedIsClosed = info.GetPath().GetSubpaths()[0].IsClosed();
                }
                else if (color[1] > color[0] && color[1] > color[2])
                {
                    Green?.Dispose();
                    Green = path;
                    GreenHasBezier = hasBezier;
                    GreenDashPattern = info.GetLineDashPattern().GetAsArray(0).ToFloatArray();
                }
                else path.Dispose();
            }

            public void Dispose()
            {
                Red?.Dispose();
                Green?.Dispose();
            }
        }
    }
}
