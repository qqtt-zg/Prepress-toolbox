using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 拼版服务基类，提供公共功能
    /// </summary>
    public abstract class ImpositionBase
    {
        protected readonly IPdfInfoProvider _pdfInfoProvider;

        protected ImpositionBase(IPdfInfoProvider pdfInfoProvider = null)
        {
            _pdfInfoProvider = pdfInfoProvider ?? new PdfInfoProvider();
        }

        /// <summary>
        /// 分析PDF文件
        /// </summary>
        protected async Task<PdfFileInfo> AnalyzePdfAsync(string filePath, 
            CancellationToken cancellationToken = default)
        {
            ValidateInput(filePath);
            return await Task.Run(() => _pdfInfoProvider.AnalyzePdf(filePath), cancellationToken);
        }

        /// <summary>
        /// 验证输入文件
        /// </summary>
        protected void ValidateInput(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("PDF文件路径不能为空");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"PDF文件不存在: {filePath}");
        }

        /// <summary>
        /// 报告进度
        /// </summary>
        protected void ReportProgress(IProgress<int> progress, int value)
        {
            // Math.Clamp not available in .NET Framework 4.8
            var clampedValue = value < 0 ? 0 : (value > 100 ? 100 : value);
            progress?.Report(clampedValue);
        }

        /// <summary>
        /// 生成输出文件路径
        /// </summary>
        protected string GenerateOutputPath(string inputPath, string suffix)
        {
            var dir = Path.GetDirectoryName(inputPath);
            var fileName = Path.GetFileNameWithoutExtension(inputPath);
            var ext = Path.GetExtension(inputPath);
            return Path.Combine(dir, $"{fileName}_{suffix}{ext}");
        }

        /// <summary>
        /// 确保页数是指定倍数
        /// </summary>
        protected int EnsurePageMultiple(int pageCount, int multiple)
        {
            while (pageCount % multiple != 0)
                pageCount++;
            return pageCount;
        }
    }
}
