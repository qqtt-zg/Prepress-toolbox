using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using Spire.Pdf.Graphics.Layer;
using iText.Kernel.Pdf;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// PDF处理服务实现，负责PDF相关的操作
    /// 统一使用IText7PdfTools作为PDF尺寸数据的来源
    /// </summary>
    public class PdfProcessingService : IPdfProcessingService
    {
        private readonly IPdfDimensionService _dimensionService;

        /// <summary>
        /// 构造函数，使用默认的PDF尺寸服务
        /// </summary>
        public PdfProcessingService()
        {
            _dimensionService = PdfDimensionServiceFactory.GetInstance();
        }

        /// <summary>
        /// 构造函数，支持依赖注入
        /// </summary>
        /// <param name="dimensionService">PDF尺寸服务</param>
        public PdfProcessingService(IPdfDimensionService dimensionService)
        {
            _dimensionService = dimensionService ?? throw new ArgumentNullException(nameof(dimensionService));
        }
        /// <summary>
        /// 向PDF文件添加自定义图层
        /// </summary>
        /// <param name="sourcePdfPath">源PDF文件的完整路径</param>
        /// <param name="outputPdfPath">输出PDF文件的完整路径</param>
        /// <param name="layerInfo">包含图层名称、内容和样式等信息的图层对象</param>
        /// <returns>添加图层操作是否成功</returns>
        public bool AddLayerToPdf(string sourcePdfPath, string outputPdfPath, PdfLayerInfo layerInfo)
        {
            // 参数验证
            if (string.IsNullOrEmpty(sourcePdfPath) || string.IsNullOrEmpty(outputPdfPath) || layerInfo == null)
            {
                return false;
            }

            if (!File.Exists(sourcePdfPath))
            {
                return false;
            }

            try
            {
                using (Spire.Pdf.PdfDocument document = new Spire.Pdf.PdfDocument())
                {
                    document.LoadFromFile(sourcePdfPath);

                    // 获取或创建图层
                    PdfLayer layer = document.Layers.AddLayer(layerInfo.LayerName);

                    // 为所有页面添加图层内容
                    foreach (PdfPageBase page in document.Pages)
                    {
                        // 获取图层的图形对象
                        PdfCanvas layerCanvas = layer.CreateGraphics(page.Canvas);

                        // 创建绘制内容
                        if (!string.IsNullOrEmpty(layerInfo.Content))
                        {
                            // 设置字体
                            Spire.Pdf.Graphics.PdfFont font = new Spire.Pdf.Graphics.PdfFont(Spire.Pdf.Graphics.PdfFontFamily.Helvetica, layerInfo.FontSize);
                            Spire.Pdf.Graphics.PdfBrush brush = Spire.Pdf.Graphics.PdfBrushes.Black;

                            // 绘制文本
                            layerCanvas.DrawString(layerInfo.Content, font, brush, layerInfo.X, layerInfo.Y);
                        }
                    }

                    // 保存修改后的PDF文件
                    document.SaveToFile(outputPdfPath);

                    // 关闭文档以释放资源
                    document.Close();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("添加图层失败: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 将多个PDF文件合并为一个文件
        /// </summary>
        /// <param name="sourceFiles">要合并的PDF源文件路径列表，按顺序合并</param>
        /// <param name="outputFile">合并后输出的PDF文件路径</param>
        /// <returns>合并操作是否成功</returns>
        public bool MergePdfFiles(List<string> sourceFiles, string outputFile)
        {
            if (sourceFiles == null || sourceFiles.Count == 0 || string.IsNullOrEmpty(outputFile))
            {
                return false;
            }

            try
            {
                using (Spire.Pdf.PdfDocument mergedDocument = new Spire.Pdf.PdfDocument())
                {
                    // 遍历所有源文件并合并
                    foreach (string sourceFile in sourceFiles)
                    {
                        if (!File.Exists(sourceFile))
                        {
                            Console.WriteLine("源文件不存在: " + sourceFile);
                            continue;
                        }

                        // 检查文件扩展名是否为PDF
                        if (!Path.GetExtension(sourceFile).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("跳过非PDF文件: " + sourceFile);
                            continue;
                        }

                        using (Spire.Pdf.PdfDocument sourceDocument = new Spire.Pdf.PdfDocument())
                        {
                            sourceDocument.LoadFromFile(sourceFile);
                            // 将源文档的所有页面添加到合并文档
                            mergedDocument.AppendPage(sourceDocument);
                        }
                    }

                    // 确保输出目录存在
                    string outputDirectory = Path.GetDirectoryName(outputFile);
                    if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }

                    // 保存合并后的文档
                    mergedDocument.SaveToFile(outputFile);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("合并PDF文件失败: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 获取指定PDF文件的总页数
        /// </summary>
        /// <param name="filePath">PDF文件的完整路径</param>
        /// <returns>PDF文件的总页数</returns>
        public int GetPdfPageCount(string filePath)
        {
            try
            {
                // 统一使用IText7PdfTools通过服务接口
                int? pageCount = _dimensionService.GetPageCount(filePath);
                return pageCount ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("获取PDF页数失败: " + ex.Message);
                return 0;
            }
        }
        
        /// <summary>
        /// 检查PDF文件中是否存在指定的图层
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="layerNames">要检查的图层名称列表</param>
        /// <returns>如果所有指定图层都存在则返回true，否则返回false</returns>
        public bool CheckPdfLayersExist(string filePath, params string[] layerNames)
        {
            return PdfTools.CheckPdfLayersExist(filePath, layerNames);
        }
        
        /// <summary>
        /// 获取PDF文件第一页的尺寸（毫米）
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="width">输出参数：页面宽度（毫米）</param>
        /// <param name="height">输出参数：页面高度（毫米）</param>
        /// <returns>是否成功获取尺寸</returns>
        public bool GetFirstPageSize(string filePath, out double width, out double height)
        {
            // 统一使用IText7PdfTools通过服务接口
            return _dimensionService.GetFirstPageSize(filePath, out width, out height);
        }
        
        /// <summary>
        /// 为PDF文件添加名为"Dots_AddCounter"的图层
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="finalDimensions">最终尺寸，格式为"宽度x高度"（毫米）</param>
        /// <param name="cornerRadius">圆角半径，"0"表示直角矩形，"R"表示圆形，其他数值表示圆角矩形</param>
        /// <param name="usePdfLastPage">是否使用PDF最后一页逻辑</param>
        /// <returns>是否成功添加图层</returns>
        [Obsolete("请使用AddDotsAddCounterLayer(string, string, ShapeType, double)方法")]
        public bool AddDotsAddCounterLayer(string filePath, string finalDimensions, string cornerRadius = "0", bool usePdfLastPage = false)
        {
            // 转换旧参数到新的枚举方式
            ShapeType shapeType;
            double roundRadius = 0;

            if (usePdfLastPage || string.Equals(cornerRadius, "Y", StringComparison.OrdinalIgnoreCase))
            {
                shapeType = ShapeType.Special;
            }
            else if (string.Equals(cornerRadius, "R", StringComparison.OrdinalIgnoreCase))
            {
                shapeType = ShapeType.Circle;
            }
            else if (double.TryParse(cornerRadius, out double radius) && radius > 0)
            {
                shapeType = ShapeType.RoundRect;
                roundRadius = radius;
            }
            else
            {
                shapeType = ShapeType.RightAngle;
            }

            return AddDotsAddCounterLayer(filePath, finalDimensions, shapeType, roundRadius);
        }

        /// <summary>
        /// 为PDF添加Dots_AddCounter图层（新的枚举版本）
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="dimensions">最终尺寸</param>
        /// <param name="shapeType">形状类型</param>
        /// <param name="roundRadius">圆角半径（仅用于圆角矩形）</param>
        /// <returns>是否成功添加图层</returns>
        public bool AddDotsAddCounterLayer(string pdfPath, string dimensions, ShapeType shapeType, double roundRadius = 0)
        {
            return PdfTools.AddDotsAddCounterLayer(pdfPath, dimensions, shapeType, roundRadius);
        }
        
        /// <summary>
        /// 计算最终尺寸（毫米）
        /// </summary>
        /// <param name="width">原始宽度（毫米）</param>
        /// <param name="height">原始高度（毫米）</param>
        /// <param name="tetBleed">出血值（毫米）</param>
        /// <param name="cornerRadius">圆角半径</param>
        /// <param name="addPdfLayers">是否添加PDF图层</param>
        /// <returns>最终尺寸字符串</returns>
        public string CalculateFinalDimensions(double width, double height, double tetBleed, string cornerRadius = "0", bool addPdfLayers = false)
        {
            return PdfTools.CalculateFinalDimensions(width, height, tetBleed, cornerRadius, addPdfLayers);
        }

        /// <summary>
        /// 复制PDF页面（支持一式N份，按123123顺序复制）
        /// </summary>
        public async Task<bool> CopyPdfPagesAsync(
            string sourcePdfPath,
            string outputPdfPath,
            int copySetCount,
            IProgress<(int current, int total, string message)> progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sourcePdfPath) || string.IsNullOrEmpty(outputPdfPath))
                return false;
            if (copySetCount < 1)
                return false;
            if (!File.Exists(sourcePdfPath))
                return false;

            return await Task.Run(() =>
            {
                try
                {
                    using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(sourcePdfPath))
                    using (iText.Kernel.Pdf.PdfDocument sourceDocument = new iText.Kernel.Pdf.PdfDocument(reader))
                    using (iText.Kernel.Pdf.PdfWriter writer = new iText.Kernel.Pdf.PdfWriter(outputPdfPath))
                    using (iText.Kernel.Pdf.PdfDocument outputDocument = new iText.Kernel.Pdf.PdfDocument(writer))
                    {
                        int totalPages = sourceDocument.GetNumberOfPages();
                        int totalOutputPages = totalPages * copySetCount;

                        int current = 0;
                        // 按123123顺序复制：每份按顺序复制所有页
                        for (int copy = 0; copy < copySetCount; copy++)
                        {
                            for (int page = 1; page <= totalPages; page++)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                sourceDocument.CopyPagesTo(page, page, outputDocument);
                                current++;
                                progressCallback?.Report((current, totalOutputPages, $"复制页面 {current}/{totalOutputPages}"));
                            }
                        }

                        return true;
                    }
                }
                catch (OperationCanceledException)
                {
                    if (File.Exists(outputPdfPath))
                    {
                        try { File.Delete(outputPdfPath); } catch { }
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"[PdfProcessingService] 复制页面失败: {ex.Message}", ex);
                    return false;
                }
            }, cancellationToken);
        }
    }
}