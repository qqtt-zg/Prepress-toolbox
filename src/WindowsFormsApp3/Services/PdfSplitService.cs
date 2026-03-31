using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using iText.Kernel.Pdf;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// PDF拆分信息
    /// </summary>
    public class SplitFileInfo
    {
        /// <summary>
        /// 输出文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int PageCount { get; set; }
    }

    /// <summary>
    /// 页数范围信息
    /// </summary>
    public class PageRangeInfo
    {
        /// <summary>
        /// 序号（1-based）
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 输出文件名
        /// </summary>
        public string OutputFileName { get; set; }

        /// <summary>
        /// 起始页（1-based）
        /// </summary>
        public int StartPage { get; set; }

        /// <summary>
        /// 结束页（1-based）
        /// </summary>
        public int EndPage { get; set; }

        /// <summary>
        /// 页数
        /// </summary>
        public int PageCount => EndPage - StartPage + 1;
    }

    /// <summary>
    /// PDF拆分服务
    /// 使用iText7实现PDF按页数范围拆分
    /// </summary>
    public class PdfSplitService
    {
        /// <summary>
        /// 计算页数范围（根据文件名和页数列表，自动累计）
        /// </summary>
        /// <param name="fileInfos">文件信息列表</param>
        /// <returns>页数范围列表</returns>
        public List<PageRangeInfo> CalculatePageRanges(List<SplitFileInfo> fileInfos)
        {
            var pageRanges = new List<PageRangeInfo>();

            if (fileInfos == null || fileInfos.Count == 0)
                return pageRanges;

            int currentPage = 1;
            for (int i = 0; i < fileInfos.Count; i++)
            {
                var info = fileInfos[i];
                if (info.PageCount <= 0)
                    continue;

                var range = new PageRangeInfo
                {
                    Index = i + 1,
                    OutputFileName = info.FileName,
                    StartPage = currentPage,
                    EndPage = currentPage + info.PageCount - 1
                };

                pageRanges.Add(range);
                currentPage += info.PageCount;
            }

            return pageRanges;
        }

        /// <summary>
        /// 获取源PDF的总页数
        /// </summary>
        /// <param name="sourcePdfPath">源PDF路径</param>
        /// <returns>总页数，失败返回null</returns>
        public int? GetSourcePageCount(string sourcePdfPath)
        {
            return IText7PdfTools.GetPageCount(sourcePdfPath);
        }

        /// <summary>
        /// 根据页数范围列表拆分PDF
        /// </summary>
        /// <param name="sourcePdfPath">源PDF路径</param>
        /// <param name="outputDir">输出目录</param>
        /// <param name="pageRanges">页数范围列表</param>
        /// <param name="progressCallback">进度回调 (current, total, message)</param>
        /// <returns>是否全部成功</returns>
        public bool SplitPdfByPageRanges(string sourcePdfPath, string outputDir, List<PageRangeInfo> pageRanges, Action<int, int, string> progressCallback = null)
        {
            return SplitPdfByPageRanges(sourcePdfPath, outputDir, pageRanges, CancellationToken.None, progressCallback);
        }

        /// <summary>
        /// 根据页数范围列表拆分PDF（支持取消）
        /// </summary>
        /// <param name="sourcePdfPath">源PDF路径</param>
        /// <param name="outputDir">输出目录</param>
        /// <param name="pageRanges">页数范围列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="progressCallback">进度回调 (current, total, message)</param>
        /// <returns>是否全部成功</returns>
        public bool SplitPdfByPageRanges(string sourcePdfPath, string outputDir, List<PageRangeInfo> pageRanges, CancellationToken cancellationToken, Action<int, int, string> progressCallback = null)
        {
            if (string.IsNullOrEmpty(sourcePdfPath) || !File.Exists(sourcePdfPath))
            {
                LogHelper.Error($"PDF拆分失败：源文件不存在 - {sourcePdfPath}");
                return false;
            }

            if (pageRanges == null || pageRanges.Count == 0)
            {
                LogHelper.Error("PDF拆分失败：页数范围列表为空");
                return false;
            }

            if (string.IsNullOrEmpty(outputDir))
            {
                outputDir = Path.GetDirectoryName(sourcePdfPath);
            }

            try
            {
                // 确保输出目录存在
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // 获取源PDF总页数
                int? totalSourcePages = GetSourcePageCount(sourcePdfPath);
                if (!totalSourcePages.HasValue)
                {
                    LogHelper.Error($"PDF拆分失败：无法读取源PDF页数 - {sourcePdfPath}");
                    return false;
                }

                int totalFiles = pageRanges.Count;
                int currentFile = 0;

                LogHelper.Info($"开始拆分PDF - 源文件: {sourcePdfPath}, 总页数: {totalSourcePages}, 将拆分为: {totalFiles} 个文件");

                // 打开源PDF
                using (PdfReader reader = new PdfReader(sourcePdfPath))
                using (PdfDocument sourceDocument = new PdfDocument(reader))
                {
                    foreach (var range in pageRanges)
                    {
                        // 检查取消请求
                        cancellationToken.ThrowIfCancellationRequested();

                        currentFile++;

                        // 验证页数范围
                        if (range.StartPage < 1 || range.EndPage > totalSourcePages.Value)
                        {
                            LogHelper.Warn($"文件 {range.Index} ({range.OutputFileName}) 页数范围 {range.StartPage}-{range.EndPage} 超出源文件范围 (1-{totalSourcePages})，跳过");
                            progressCallback?.Invoke(currentFile, totalFiles, $"跳过 {range.OutputFileName}：页数范围超出");
                            continue;
                        }

                        // 构建输出文件路径
                        string outputFileName = range.OutputFileName;
                        if (!outputFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            outputFileName += ".pdf";
                        }
                        string outputPath = Path.Combine(outputDir, outputFileName);

                        // 避免覆盖：如果文件已存在，添加序号
                        outputPath = GetUniqueFilePath(outputPath);

                        try
                        {
                            // 创建新的PDF文档
                            using (PdfWriter writer = new PdfWriter(outputPath))
                            using (PdfDocument outputDocument = new PdfDocument(writer))
                            {
                                // 使用CopyPagesTo复制页面范围（这是正确的页面复制方式）
                                sourceDocument.CopyPagesTo(range.StartPage, range.EndPage, outputDocument);

                                outputDocument.Close();
                            }

                            LogHelper.Info($"PDF拆分成功 - 文件 {currentFile}/{totalFiles}: {outputPath} (页数: {range.StartPage}-{range.EndPage})");
                            progressCallback?.Invoke(currentFile, totalFiles, $"已生成 {range.OutputFileName}");
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Error($"PDF拆分失败 - 文件: {outputPath}, 错误: {ex.Message}");
                            progressCallback?.Invoke(currentFile, totalFiles, $"失败: {range.OutputFileName}");
                        }
                    }
                }

                LogHelper.Info($"PDF拆分完成 - 成功处理 {currentFile} 个文件");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"PDF拆分异常 - 源文件: {sourcePdfPath}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取唯一的文件路径（如果文件已存在，添加序号）
        /// </summary>
        private string GetUniqueFilePath(string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;

            string directory = Path.GetDirectoryName(filePath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            int counter = 1;
            string newFilePath;
            do
            {
                newFilePath = Path.Combine(directory, $"{fileNameWithoutExt}_{counter}{extension}");
                counter++;
            } while (File.Exists(newFilePath));

            return newFilePath;
        }
    }
}
