using System;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;

class Program
{
    static void Main()
    {
        try
        {
            string outputPath = "f:/编程项目/Prepress-toolbox/src/WindowsFormsApp3/bin/Debug/net48/Test/TestPdf.pdf";

            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            // 创建PDF文档
            using (var writer = new PdfWriter(outputPath))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                // 添加标题
                var title = new Paragraph("测试PDF文档")
                    .SetFontSize(24)
                    .SetFontColor(ColorConstants.BLUE)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20);
                document.Add(title);

                // 添加内容
                var content = new Paragraph("这是一个测试PDF文件，用于验证CefSharp PDF预览功能。\n\n" +
                    "创建时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n\n" +
                    "功能测试要点：\n" +
                    "1. 单页模式显示\n" +
                    "2. 适应页面缩放\n" +
                    "3. 页面导航功能\n" +
                    "4. PDF渲染质量")
                    .SetFontSize(14)
                    .SetMarginBottom(20);
                document.Add(content);

                // 添加测试内容区域
                var testArea = new Paragraph("=== 测试内容区域 ===")
                    .SetFontSize(16)
                    .SetFontColor(ColorConstants.DARK_GRAY)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(20)
                    .SetMarginBottom(20);
                document.Add(testArea);

                // 添加多行文本
                for (int i = 1; i <= 10; i++)
                {
                    var line = new Paragraph($"这是第 {i} 行测试内容。CefSharp PDF预览功能测试。")
                        .SetFontSize(12)
                        .SetMarginLeft(20);
                    document.Add(line);
                }

                // 添加页面底部的页码
                var pageNumber = new Paragraph("页码: 1 / 1")
                    .SetFontSize(10)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(30);
                document.Add(pageNumber);
            }

            Console.WriteLine($"测试PDF文件已成功创建: {outputPath}");
            Console.WriteLine($"文件大小: {new FileInfo(outputPath).Length} 字节");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"创建测试PDF失败: {ex.Message}");
            Console.WriteLine($"详细错误: {ex}");
        }
    }
}