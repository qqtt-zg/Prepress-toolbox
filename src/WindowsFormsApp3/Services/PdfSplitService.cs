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
        /// 页面复制排列模式
        /// </summary>
        public enum PageCopyPattern
        {
            /// <summary>
            /// 连续重复模式 (111222333)：每页重复N次后再复制下一页
            /// </summary>
            ContinuousRepeat = 0,

            /// <summary>
            /// 交替循环模式 (123123123)：按顺序循环复制所有页面N次
            /// </summary>
            AlternatingCycle = 1
        }
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

        /// <summary>
        /// 按指定模式复制PDF页面（无取消支持）
        /// </summary>
        /// <param name="sourcePdfPath">源PDF路径</param>
        /// <param name="outputPdfPath">输出PDF路径</param>
        /// <param name="startPage">起始页（1-based，包含）</param>
        /// <param name="endPage">结束页（1-based，包含）</param>
        /// <param name="repeatCount">每页重复次数（最小为1）</param>
        /// <param name="pattern">排列模式</param>
        /// <param name="progressCallback">进度回调 (current, total, message)</param>
        /// <returns>是否复制成功</returns>
        public bool CopyPagesWithPattern(
            string sourcePdfPath,
            string outputPdfPath,
            int startPage,
            int endPage,
            int repeatCount,
            PageCopyPattern pattern,
            Action<int, int, string> progressCallback = null)
        {
            return CopyPagesWithPattern(sourcePdfPath, outputPdfPath, startPage, endPage, repeatCount, pattern, CancellationToken.None, progressCallback);
        }

        /// <summary>
        /// 按指定模式复制PDF页面（支持取消）
        /// </summary>
        /// <param name="sourcePdfPath">源PDF路径</param>
        /// <param name="outputPdfPath">输出PDF路径</param>
        /// <param name="startPage">起始页（1-based，包含）</param>
        /// <param name="endPage">结束页（1-based，包含）</param>
        /// <param name="repeatCount">每页重复次数（最小为1）</param>
        /// <param name="pattern">排列模式</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="progressCallback">进度回调 (current, total, message)</param>
        /// <returns>是否复制成功</returns>
        public bool CopyPagesWithPattern(
            string sourcePdfPath,
            string outputPdfPath,
            int startPage,
            int endPage,
            int repeatCount,
            PageCopyPattern pattern,
            CancellationToken cancellationToken,
            Action<int, int, string> progressCallback = null)
        {
            // 1. 参数验证
            if (string.IsNullOrEmpty(sourcePdfPath) || !File.Exists(sourcePdfPath))
            {
                LogHelper.Error($"[CopyPagesWithPattern] 源文件不存在 - {sourcePdfPath}");
                return false;
            }

            if (string.IsNullOrEmpty(outputPdfPath))
            {
                LogHelper.Error("[CopyPagesWithPattern] 输出路径为空");
                return false;
            }

            if (startPage < 1)
            {
                LogHelper.Error($"[CopyPagesWithPattern] 起始页必须 >= 1，实际：{startPage}");
                return false;
            }

            if (endPage < startPage)
            {
                LogHelper.Error($"[CopyPagesWithPattern] 结束页必须 >= 起始页，起始页：{startPage}，结束页：{endPage}");
                return false;
            }

            if (repeatCount < 1)
            {
                LogHelper.Warn($"[CopyPagesWithPattern] 重复次数必须 >= 1，已修正为1，实际：{repeatCount}");
                repeatCount = 1;
            }

            // 2. 获取源PDF总页数并验证范围
            int? totalSourcePages = GetSourcePageCount(sourcePdfPath);
            if (!totalSourcePages.HasValue)
            {
                LogHelper.Error($"[CopyPagesWithPattern] 无法读取源PDF页数 - {sourcePdfPath}");
                return false;
            }

            if (endPage > totalSourcePages.Value)
            {
                LogHelper.Error($"[CopyPagesWithPattern] 结束页{endPage}超出源文件范围(1-{totalSourcePages.Value})");
                return false;
            }

            int pageCount = endPage - startPage + 1;
            int totalOutputPages = pageCount * repeatCount;

            LogHelper.Info($"[CopyPagesWithPattern] 开始复制 - 源：{sourcePdfPath}，页数范围：{startPage}-{endPage}，重复：{repeatCount}次，模式：{pattern}");

            try
            {
                using (PdfReader reader = new PdfReader(sourcePdfPath))
                using (PdfDocument sourceDocument = new PdfDocument(reader))
                using (PdfWriter writer = new PdfWriter(outputPdfPath))
                using (PdfDocument outputDocument = new PdfDocument(writer))
                {
                    switch (pattern)
                    {
                        case PageCopyPattern.ContinuousRepeat:
                            CopyPagesContinuousRepeat(sourceDocument, outputDocument, startPage, endPage, repeatCount, cancellationToken, progressCallback);
                            break;

                        case PageCopyPattern.AlternatingCycle:
                            CopyPagesAlternatingCycle(sourceDocument, outputDocument, startPage, endPage, repeatCount, cancellationToken, progressCallback);
                            break;

                        default:
                            LogHelper.Error($"[CopyPagesWithPattern] 未知的排列模式：{pattern}");
                            return false;
                    }

                    outputDocument.Close();
                }

                LogHelper.Info($"[CopyPagesWithPattern] 复制完成 - 输出：{outputPdfPath}，总页数：{totalOutputPages}");
                return true;
            }
            catch (OperationCanceledException)
            {
                LogHelper.Info("[CopyPagesWithPattern] 操作已取消");
                if (File.Exists(outputPdfPath))
                {
                    try { File.Delete(outputPdfPath); } catch { }
                }
                throw;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[CopyPagesWithPattern] 复制异常 - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 连续重复模式复制（111222333）
        /// </summary>
        private void CopyPagesContinuousRepeat(
            PdfDocument sourceDocument,
            PdfDocument outputDocument,
            int startPage,
            int endPage,
            int repeatCount,
            CancellationToken cancellationToken,
            Action<int, int, string> progressCallback)
        {
            int pageCount = endPage - startPage + 1;
            int totalOperations = pageCount * repeatCount;
            int currentOperation = 0;

            for (int pageNum = startPage; pageNum <= endPage; pageNum++)
            {
                for (int rep = 0; rep < repeatCount; rep++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    sourceDocument.CopyPagesTo(pageNum, pageNum, outputDocument);

                    currentOperation++;
                    progressCallback?.Invoke(currentOperation, totalOperations, $"复制第{pageNum}页 ({rep + 1}/{repeatCount})");
                }
            }
        }

        /// <summary>
        /// 交替循环模式复制（123123123）
        /// </summary>
        private void CopyPagesAlternatingCycle(
            PdfDocument sourceDocument,
            PdfDocument outputDocument,
            int startPage,
            int endPage,
            int repeatCount,
            CancellationToken cancellationToken,
            Action<int, int, string> progressCallback)
        {
            for (int cycle = 0; cycle < repeatCount; cycle++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                sourceDocument.CopyPagesTo(startPage, endPage, outputDocument);

                progressCallback?.Invoke(cycle + 1, repeatCount, $"完成第{cycle + 1}次循环 (共{repeatCount}次)");
            }
        }
    }
}
