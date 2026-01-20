using System;
using System.IO;
using iText.Kernel.Pdf;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Colors;
using WindowsFormsApp3.Services;

namespace WindowsFormsApp3.Utils
{
    /// <summary>
    /// PDF印前标记工具类 - 提供裁切标记、套准标记等专业印刷标记功能
    /// </summary>
    public static class PdfPrepressMarks
    {
        #region 常量定义

        /// <summary>
        /// 点到毫米转换系数 (1 inch = 72 points = 25.4 mm)
        /// </summary>
        private const double PT_TO_MM = 25.4 / 72.0;

        /// <summary>
        /// 毫米到点转换系数
        /// </summary>
        private const double MM_TO_PT = 72.0 / 25.4;

        #endregion

        #region 裁切标记

        /// <summary>
        /// 为PDF添加裁切标记
        /// </summary>
        /// <param name="inputPath">输入PDF文件路径</param>
        /// <param name="outputPath">输出PDF文件路径</param>
        /// <param name="options">裁切标记配置参数</param>
        /// <returns>是否成功添加标记</returns>
        public static bool AddCropMarks(string inputPath, string outputPath, CropMarksOptions options)
        {
            try
            {
                LogHelper.Info($"[PdfPrepressMarks] 开始添加裁切标记: {System.IO.Path.GetFileName(inputPath)}");

                using (PdfReader reader = new PdfReader(inputPath))
                using (PdfWriter writer = new PdfWriter(outputPath))
                using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
                {
                    int totalPages = pdfDoc.GetNumberOfPages();
                    
                    // 确定要处理的页面
                    for (int i = 1; i <= totalPages; i++)
                    {
                        if (!ShouldProcessPage(i, totalPages, options.PageRange, options.PageRangeType))
                            continue;

                        PdfPage page = pdfDoc.GetPage(i);
                        // 优先使用TrimBox(成品尺寸) -> CropBox(裁切框) -> MediaBox(纸张尺寸)
                        Rectangle referenceBox = page.GetTrimBox() ?? page.GetCropBox() ?? page.GetMediaBox();
                        PdfCanvas canvas = new PdfCanvas(page);

                        // 绘制四个角的裁切标记
                        DrawCropMark(canvas, referenceBox, options, CornerPosition.TopLeft);
                        DrawCropMark(canvas, referenceBox, options, CornerPosition.TopRight);
                        DrawCropMark(canvas, referenceBox, options, CornerPosition.BottomLeft);
                        DrawCropMark(canvas, referenceBox, options, CornerPosition.BottomRight);

                        LogHelper.Info($"[PdfPrepressMarks] 已为第{i}页添加裁切标记");
                    }
                }

                LogHelper.Info($"[PdfPrepressMarks] 裁切标记添加完成: {System.IO.Path.GetFileName(outputPath)}");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PdfPrepressMarks] 添加裁切标记失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 绘制单个角的裁切标记
        /// </summary>
        private static void DrawCropMark(PdfCanvas canvas, Rectangle mediaBox, CropMarksOptions options, CornerPosition corner)
        {
            // 转换毫米参数为点
            float markLength = (float)(options.MarkLengthMM * MM_TO_PT);
            float offset = (float)(options.OffsetMM * MM_TO_PT);
            float lineWidth = (float)(options.LineWidthMM * MM_TO_PT);

            // 设置绘制属性
            canvas.SaveState();
            canvas.SetStrokeColor(DeviceCmyk.BLACK); // 套版黑 CMYK(100,100,100,100)
            canvas.SetLineWidth(lineWidth);

            float width = mediaBox.GetWidth();
            float height = mediaBox.GetHeight();

            // 根据角位置计算标记坐标
            switch (corner)
            {
                case CornerPosition.TopLeft:
                    // 左上角 - 水平线(向左)
                    canvas.MoveTo(0, height - offset);
                    canvas.LineTo(-markLength, height - offset);
                    // 左上角 - 垂直线(向上)
                    canvas.MoveTo(offset, height);
                    canvas.LineTo(offset, height + markLength);
                    break;

                case CornerPosition.TopRight:
                    // 右上角 - 水平线(向右)
                    canvas.MoveTo(width, height - offset);
                    canvas.LineTo(width + markLength, height - offset);
                    // 右上角 - 垂直线(向上)
                    canvas.MoveTo(width - offset, height);
                    canvas.LineTo(width - offset, height + markLength);
                    break;

                case CornerPosition.BottomLeft:
                    // 左下角 - 水平线(向左)
                    canvas.MoveTo(0, offset);
                    canvas.LineTo(-markLength, offset);
                    // 左下角 - 垂直线(向下)
                    canvas.MoveTo(offset, 0);
                    canvas.LineTo(offset, -markLength);
                    break;

                case CornerPosition.BottomRight:
                    // 右下角 - 水平线(向右)
                    canvas.MoveTo(width, offset);
                    canvas.LineTo(width + markLength, offset);
                    // 右下角 - 垂直线(向下)
                    canvas.MoveTo(width - offset, 0);
                    canvas.LineTo(width - offset, -markLength);
                    break;
            }

            canvas.Stroke();
            canvas.RestoreState();
        }

        #endregion

        #region 套准标记

        /// <summary>
        /// 为PDF添加套准标记
        /// </summary>
        /// <param name="inputPath">输入PDF文件路径</param>
        /// <param name="outputPath">输出PDF文件路径</param>
        /// <param name="options">套准标记配置参数</param>
        /// <returns>是否成功添加标记</returns>
        public static bool AddRegistrationMarks(string inputPath, string outputPath, RegMarksOptions options)
        {
            try
            {
                LogHelper.Info($"[PdfPrepressMarks] 开始添加套准标记: {System.IO.Path.GetFileName(inputPath)}");

                using (PdfReader reader = new PdfReader(inputPath))
                using (PdfWriter writer = new PdfWriter(outputPath))
                using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
                {
                    int totalPages = pdfDoc.GetNumberOfPages();

                    for (int i = 1; i <= totalPages; i++)
                    {
                        if (!ShouldProcessPage(i, totalPages, options.PageRange, options.PageRangeType))
                            continue;

                        PdfPage page = pdfDoc.GetPage(i);
                        // 优先使用TrimBox(成品尺寸) -> CropBox(裁切框) -> MediaBox(纸张尺寸)
                        Rectangle referenceBox = page.GetTrimBox() ?? page.GetCropBox() ?? page.GetMediaBox();
                        PdfCanvas canvas = new PdfCanvas(page);

                        // 绘制四个角的套准标记
                        DrawRegistrationMark(canvas, referenceBox, options, CornerPosition.TopLeft);
                        DrawRegistrationMark(canvas, referenceBox, options, CornerPosition.TopRight);
                        DrawRegistrationMark(canvas, referenceBox, options, CornerPosition.BottomLeft);
                        DrawRegistrationMark(canvas, referenceBox, options, CornerPosition.BottomRight);

                        LogHelper.Info($"[PdfPrepressMarks] 已为第{i}页添加套准标记");
                    }
                }

                LogHelper.Info($"[PdfPrepressMarks] 套准标记添加完成: {System.IO.Path.GetFileName(outputPath)}");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PdfPrepressMarks] 添加套准标记失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 绘制单个套准标记(同心圆 + 十字线)
        /// </summary>
        private static void DrawRegistrationMark(PdfCanvas canvas, Rectangle mediaBox, RegMarksOptions options, CornerPosition corner)
        {
            // 转换参数
            float outerRadius = (float)(options.OuterDiameterMM / 2 * MM_TO_PT);
            float innerRadius = (float)(options.InnerDiameterMM / 2 * MM_TO_PT);
            float crossLength = (float)(options.CrossLengthMM * MM_TO_PT);
            float offset = (float)(options.OffsetMM * MM_TO_PT);
            float lineWidth = 0.5f;

            canvas.SaveState();
            canvas.SetStrokeColor(DeviceCmyk.BLACK);
            canvas.SetLineWidth(lineWidth);

            float width = mediaBox.GetWidth();
            float height = mediaBox.GetHeight();

            // 计算标记中心点位置
            float centerX = 0, centerY = 0;
            switch (corner)
            {
                case CornerPosition.TopLeft:
                    centerX = -offset;
                    centerY = height + offset;
                    break;
                case CornerPosition.TopRight:
                    centerX = width + offset;
                    centerY = height + offset;
                    break;
                case CornerPosition.BottomLeft:
                    centerX = -offset;
                    centerY = -offset;
                    break;
                case CornerPosition.BottomRight:
                    centerX = width + offset;
                    centerY = -offset;
                    break;
            }

            // 绘制外圆
            canvas.Circle(centerX, centerY, outerRadius);
            canvas.Stroke();

            // 绘制内圆
            canvas.Circle(centerX, centerY, innerRadius);
            canvas.Stroke();

            // 绘制十字线
            float halfCross = crossLength / 2;
            // 水平线
            canvas.MoveTo(centerX - halfCross, centerY);
            canvas.LineTo(centerX + halfCross, centerY);
            // 垂直线
            canvas.MoveTo(centerX, centerY - halfCross);
            canvas.LineTo(centerX, centerY + halfCross);
            canvas.Stroke();

            canvas.RestoreState();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 判断是否应处理指定页面
        /// </summary>
        private static bool ShouldProcessPage(int pageNum, int totalPages, string pageRange, PageRangeType rangeType)
        {
            switch (rangeType)
            {
                case PageRangeType.All:
                    return true;

                case PageRangeType.Current:
                    return pageNum == 1; // 默认第一页

                case PageRangeType.Custom:
                    return IsPageInRange(pageNum, pageRange);

                default:
                    return false;
            }
        }

        /// <summary>
        /// 解析页面范围字符串,判断页码是否在范围内
        /// 支持格式: "1,3,5-7,10"
        /// </summary>
        private static bool IsPageInRange(int pageNum, string pageRange)
        {
            if (string.IsNullOrWhiteSpace(pageRange))
                return false;

            try
            {
                string[] parts = pageRange.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (string part in parts)
                {
                    string trimmed = part.Trim();
                    
                    if (trimmed.Contains("-"))
                    {
                        // 范围格式 "5-7"
                        string[] range = trimmed.Split('-');
                        if (range.Length == 2)
                        {
                            int start = int.Parse(range[0].Trim());
                            int end = int.Parse(range[1].Trim());
                            if (pageNum >= start && pageNum <= end)
                                return true;
                        }
                    }
                    else
                    {
                        // 单个页码
                        int singlePage = int.Parse(trimmed);
                        if (pageNum == singlePage)
                            return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        #endregion
    }

    #region 配置参数类

    /// <summary>
    /// 裁切标记配置参数
    /// </summary>
    public class CropMarksOptions
    {
        /// <summary>
        /// 标记长度(毫米)
        /// </summary>
        public double MarkLengthMM { get; set; } = 10.0;

        /// <summary>
        /// 偏移距离(毫米) - 距离页面边缘的距离
        /// </summary>
        public double OffsetMM { get; set; } = 3.0;

        /// <summary>
        /// 线宽(毫米)
        /// </summary>
        public double LineWidthMM { get; set; } = 0.18;

        /// <summary>
        /// 页面范围类型
        /// </summary>
        public PageRangeType PageRangeType { get; set; } = PageRangeType.All;

        /// <summary>
        /// 自定义页面范围(当PageRangeType为Custom时使用)
        /// 格式: "1,3,5-7,10"
        /// </summary>
        public string PageRange { get; set; } = "";
    }

    /// <summary>
    /// 套准标记配置参数
    /// </summary>
    public class RegMarksOptions
    {
        /// <summary>
        /// 外圆直径(毫米)
        /// </summary>
        public double OuterDiameterMM { get; set; } = 6.0;

        /// <summary>
        /// 内圆直径(毫米)
        /// </summary>
        public double InnerDiameterMM { get; set; } = 4.0;

        /// <summary>
        /// 十字线长度(毫米)
        /// </summary>
        public double CrossLengthMM { get; set; } = 8.0;

        /// <summary>
        /// 偏移距离(毫米) - 距离页面边缘的距离
        /// </summary>
        public double OffsetMM { get; set; } = 10.0;

        /// <summary>
        /// 页面范围类型
        /// </summary>
        public PageRangeType PageRangeType { get; set; } = PageRangeType.All;

        /// <summary>
        /// 自定义页面范围
        /// </summary>
        public string PageRange { get; set; } = "";
    }

    /// <summary>
    /// 页面范围类型
    /// </summary>
    public enum PageRangeType
    {
        /// <summary>
        /// 全部页面
        /// </summary>
        All,

        /// <summary>
        /// 当前页面
        /// </summary>
        Current,

        /// <summary>
        /// 自定义范围
        /// </summary>
        Custom
    }

    /// <summary>
    /// 角位置枚举
    /// </summary>
    internal enum CornerPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    #endregion
}
