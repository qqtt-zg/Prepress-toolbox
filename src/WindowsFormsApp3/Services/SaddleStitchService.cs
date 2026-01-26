using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 骑马订拼版服务
    /// </summary>
    public class SaddleStitchService : ImpositionBase
    {
        public SaddleStitchService(IPdfInfoProvider pdfInfoProvider = null)
            : base(pdfInfoProvider)
        {
        }

        /// <summary>
        /// 骑马订拼版配置
        /// </summary>
        public class SaddleStitchConfig
        {
            /// <summary>
            /// 输入PDF路径
            /// </summary>
            public string InputPdfPath { get; set; }

            /// <summary>
            /// 输出PDF路径
            /// </summary>
            public string OutputPdfPath { get; set; }

            /// <summary>
            /// 成品纸张大小（使用iText PageSize）
            /// </summary>
            public iText.Kernel.Geom.PageSize FinishedPageSize { get; set; }

            /// <summary>
            /// 爬移补偿值（mm，向内偏移）
            /// </summary>
            public float CreepCompensationMm { get; set; }
        }

        /// <summary>
        /// 生成骑马订PDF
        /// </summary>
        public async Task<string> GenerateSaddleStitchPdfAsync(
            SaddleStitchConfig config,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateInput(config.InputPdfPath);
                ReportProgress(progress, 5);

                // 1. 分析原始PDF
                LogHelper.Info($"[SaddleStitch] 开始分析PDF: {config.InputPdfPath}");
                var pdfInfo = await AnalyzePdfAsync(config.InputPdfPath, cancellationToken);
                ReportProgress(progress, 15);

                // 2. 计算页面排列顺序
                var originalPages = pdfInfo.PageCount ?? 0;
                if (originalPages == 0)
                    throw new InvalidOperationException("PDF页数为0或无法读取");
                    
                var totalPages = EnsurePageMultiple(originalPages, 4); // 骑马订需要4的倍数
                var blankPages = totalPages - originalPages;

                LogHelper.Info($"[SaddleStitch] 原始页数: {originalPages}, 补充空白页: {blankPages}, 总页数: {totalPages}");

                var pageOrder = CalculatePageOrder(totalPages);
                ReportProgress(progress, 25);

                // 3. 生成输出路径
                var outputPath = string.IsNullOrEmpty(config.OutputPdfPath)
                    ? GenerateOutputPath(config.InputPdfPath, "saddle_stitch")
                    : config.OutputPdfPath;

                // 4. 生成拼版PDF
                LogHelper.Info($"[SaddleStitch] 开始生成拼版PDF: {outputPath}");
                await GeneratePdfAsync(config, pageOrder, originalPages, outputPath, progress, cancellationToken);

                ReportProgress(progress, 100);
                LogHelper.Info($"[SaddleStitch] 拼版完成: {outputPath}");

                return outputPath;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[SaddleStitch] 生成失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 计算骑马订页面顺序
        /// </summary>
        /// <param name="totalPages">总页数（必须是4的倍数）</param>
        /// <returns>页面配对列表 (正面左, 正面右, 反面左, 反面右)</returns>
        private List<(int frontLeft, int frontRight, int backLeft, int backRight)> CalculatePageOrder(int totalPages)
        {
            if (totalPages % 4 != 0)
                throw new ArgumentException("骑马订页数必须是4的倍数");

            var result = new List<(int, int, int, int)>();
            int left = 1, right = totalPages;

            while (left < right)
            {
                // 每张纸正反面的页面排列
                // 正面: 右侧(最后页), 左侧(第一页)
                // 反面: 左侧(第二页), 右侧(倒数第二页)
                result.Add((
                    left,      // 正面左侧
                    right,     // 正面右侧
                    left + 1,  // 反面左侧
                    right - 1  // 反面右侧
                ));

                left += 2;
                right -= 2;
            }

            return result;
        }

        /// <summary>
        /// 计算爬移偏移量（pt）
        /// </summary>
        private float CalculateCreepOffset(int sheetIndex, int totalSheets, float creepPerSheetMm)
        {
            // 外层纸张不偏移，内层向内偏移
            // 每往内一层，偏移量增加 creepPerSheet
            var offsetMm = (totalSheets - sheetIndex - 1) * creepPerSheetMm;
            return offsetMm * 72f / 25.4f; // mm to pt
        }

        /// <summary>
        /// 生成拼版PDF
        /// </summary>
        private async Task GeneratePdfAsync(
            SaddleStitchConfig config,
            List<(int frontLeft, int frontRight, int backLeft, int backRight)> pageOrder,
            int originalPages,
            string outputPath,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                using (var srcPdf = new PdfDocument(new PdfReader(config.InputPdfPath)))
                using (var destPdf = new PdfDocument(new PdfWriter(outputPath)))
                {
                    // 计算成品纸张大小（对折后的大小）
                    var finishedSize = config.FinishedPageSize ?? iText.Kernel.Geom.PageSize.A4;
                    
                    // 大纸尺寸（成品的2倍宽）
                    var sheetWidth = finishedSize.GetWidth() * 2;
                    var sheetHeight = finishedSize.GetHeight();
                    var sheetSize = new iText.Kernel.Geom.PageSize(sheetWidth, sheetHeight);

                    int totalSheets = pageOrder.Count;
                    int processedSheets = 0;

                    foreach (var (frontLeft, frontRight, backLeft, backRight) in pageOrder)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // 计算爬移偏移
                        var creepOffset = CalculateCreepOffset(processedSheets, totalSheets, config.CreepCompensationMm);

                        // 正面（Front side）
                        // 骑马订正面从右往左看：第一个是最后一页(frontRight)，第二个是第一页(frontLeft)
                        var frontPage = destPdf.AddNewPage(sheetSize);
                        var frontCanvas = new PdfCanvas(frontPage);

                        // 放置正面左侧页 (frontRight，例如第16页)
                        PlacePageOnCanvas(frontCanvas, srcPdf, frontRight, originalPages,
                            x: creepOffset,  // 向右偏移（爬移补偿）
                            y: 0,
                            width: finishedSize.GetWidth(),
                            height: finishedSize.GetHeight());

                        // 放置正面右侧页 (frontLeft，例如第1页)
                        PlacePageOnCanvas(frontCanvas, srcPdf, frontLeft, originalPages,
                            x: finishedSize.GetWidth() - creepOffset,  // 向左偏移（爬移补偿）
                            y: 0,
                            width: finishedSize.GetWidth(),
                            height: finishedSize.GetHeight());

                        // 反面（Back side）
                        // 骑马订反面从左往右看：第一个是第二页(backLeft)，第二个是倒数第二页(backRight)
                        var backPage = destPdf.AddNewPage(sheetSize);
                        var backCanvas = new PdfCanvas(backPage);

                        // 放置反面左侧页 (backLeft，例如第2页)
                        PlacePageOnCanvas(backCanvas, srcPdf, backLeft, originalPages,
                            x: creepOffset,
                            y: 0,
                            width: finishedSize.GetWidth(),
                            height: finishedSize.GetHeight());

                        // 放置反面右侧页 (backRight，例如第15页)
                        PlacePageOnCanvas(backCanvas, srcPdf, backRight, originalPages,
                            x: finishedSize.GetWidth() - creepOffset,
                            y: 0,
                            width: finishedSize.GetWidth(),
                            height: finishedSize.GetHeight());

                        processedSheets++;
                        ReportProgress(progress, 25 + (int)(processedSheets * 70.0 / totalSheets));
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 在画布上放置页面
        /// </summary>
        private void PlacePageOnCanvas(
            PdfCanvas canvas,
            PdfDocument srcPdf,
            int pageNum,
            int originalPages,
            float x, float y,
            float width, float height)
        {
            // 如果是空白页（超出原始页数），跳过
            if (pageNum > originalPages)
                return;

            // 获取源页面
            var srcPage = srcPdf.GetPage(pageNum);
            var pageCopy = srcPage.CopyAsFormXObject(canvas.GetDocument());

            // 计算缩放比例（保持宽高比）
            var srcSize = srcPage.GetPageSize();
            var scaleX = width / srcSize.GetWidth();
            var scaleY = height / srcSize.GetHeight();
            var scale = Math.Min(scaleX, scaleY);

            // 计算居中偏移
            var scaledWidth = srcSize.GetWidth() * scale;
            var scaledHeight = srcSize.GetHeight() * scale;
            var offsetX = (width - scaledWidth) / 2;
            var offsetY = (height - scaledHeight) / 2;

            // 添加到画布
            canvas.AddXObjectFittedIntoRectangle(pageCopy,
                new Rectangle(x + offsetX, y + offsetY, scaledWidth, scaledHeight));
        }
    }
}
