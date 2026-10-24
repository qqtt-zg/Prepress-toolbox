using System;
using System.IO;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using WindowsFormsApp3.Utils;
using Path = System.IO.Path;

namespace WindowsFormsApp3.Services
{
    /// <summary>独立预览入口使用的真实 PDF 样例，关闭预览后清理本次临时目录。</summary>
    internal sealed class MaterialSelectionPreviewData : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "PrepressMaterialPreview", Guid.NewGuid().ToString("N"));
        public string[] FilePaths { get; }

        public MaterialSelectionPreviewData()
        {
            Directory.CreateDirectory(_directory);
            FilePaths = new[]
            {
                Path.Combine(_directory, "铭牌_A款_80×45mm.pdf"),
                Path.Combine(_directory, "铭牌_B款_80×45mm.pdf"),
                Path.Combine(_directory, "铭牌_C款_80×45mm.pdf"),
                Path.Combine(_directory, "徳佰仕75x15.pdf")
            };
            try
            {
                for (int index = 0; index < FilePaths.Length; index++)
                {
                    float width = (index == 3 ? 75 : 80) * 72F / 25.4F;
                    float height = (index == 3 ? 15 : 45) * 72F / 25.4F;
                    using (var writer = new PdfWriter(FilePaths[index]))
                    using (var document = new PdfDocument(writer))
                    {
                        for (int pageIndex = 0; pageIndex <= index; pageIndex++)
                        {
                            var page = document.AddNewPage(new iText.Kernel.Geom.PageSize(width, height));
                            // 第二款模拟媒体框与裁剪框不一致，供行内尺寸切换演示。
                            if (index == 1) page.SetCropBox(new Rectangle(5, 5, width - 10, height - 10));
                            new PdfCanvas(page).SetLineWidth(0.5F).Rectangle(8, 8, width - 16, height - 16).Stroke();
                        }
                    }
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"预览临时 PDF 清理失败: {ex.Message}");
            }
        }
    }
}
