using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using WindowsFormsApp3.Services;

namespace WindowsFormsApp3.Utils
{
    /// <summary>
    /// iTextSharp.LGPLv2.Core PDF 工具类
    /// 用于标识页插入等 PDF 操作
    /// </summary>
    public static class ITextSharpLGPLPdfTools
    {
        /// <summary>
        /// 插入标识页（使用 iTextSharp.LGPLv2.Core）
        /// </summary>
        public static bool InsertIdentifierPage(
            string filePath,
            string textContent,
            float fontSize = 12f,
            int insertPosition = 0,
            int pageCount = 1)
        {
            string pageType = string.IsNullOrEmpty(textContent) ? "空白页" : "标识页";
            string tempFile = Path.Combine(Path.GetTempPath(),
                $"temp_identifier_{Guid.NewGuid()}.pdf");

            try
            {
                LogHelper.Debug($"开始插入{pageType}，位置: {insertPosition}, 数量: {pageCount}");

                // 1. 打开现有 PDF
                PdfReader reader = new PdfReader(filePath);
                int totalPages = reader.NumberOfPages;

                if (totalPages == 0)
                {
                    LogHelper.Error("文档没有页面");
                    reader.Close();
                    return false;
                }

                // 2. 获取参考页面尺寸
                int refPageIndex = GetReferencePageIndex(totalPages, insertPosition);
                Rectangle pageSize = reader.GetPageSize(refPageIndex + 1); // iTextSharp 页码从 1 开始

                LogHelper.Debug($"参考页面尺寸: {pageSize.Width}x{pageSize.Height} 点");

                // 3. 创建字体
                string selectedFont = AppSettings.GetValue<string>("IdentifierPageFont") ?? "Microsoft YaHei";
                BaseFont baseFont = ITextSharpLGPLFontHelper.CreateBaseFont(selectedFont);
                Font font = new Font(baseFont, fontSize);

                // 4. 创建输出文档
                using (FileStream fs = new FileStream(tempFile, FileMode.Create))
                {
                    Document document = new Document(pageSize);
                    PdfCopy copy = new PdfCopy(document, fs);
                    document.Open();

                    // 5. 计算实际插入位置
                    int actualInsertIndex = CalculateInsertIndex(totalPages, insertPosition);

                    // 6. 复制页面并插入新页面
                    for (int i = 1; i <= totalPages; i++)
                    {
                        // 如果到达插入位置，先插入新页面
                        if (i == actualInsertIndex + 1)
                        {
                            for (int j = 0; j < pageCount; j++)
                            {
                                byte[] newPageBytes = CreateIdentifierPageBytes(
                                    pageSize, textContent, font, j == 0);
                                
                                if (newPageBytes != null)
                                {
                                    PdfReader newPageReader = new PdfReader(newPageBytes);
                                    PdfImportedPage newPage = copy.GetImportedPage(newPageReader, 1);
                                    copy.AddPage(newPage);
                                    newPageReader.Close();
                                    LogHelper.Debug($"已插入第 {j + 1} 个{pageType}");
                                }
                            }
                        }

                        // 复制原有页面
                        PdfImportedPage page = copy.GetImportedPage(reader, i);
                        copy.AddPage(page);
                    }

                    // 如果在末尾插入
                    if (insertPosition == -1 || insertPosition >= totalPages)
                    {
                        for (int j = 0; j < pageCount; j++)
                        {
                            byte[] newPageBytes = CreateIdentifierPageBytes(
                                pageSize, textContent, font, j == 0);
                            
                            if (newPageBytes != null)
                            {
                                PdfReader newPageReader = new PdfReader(newPageBytes);
                                PdfImportedPage newPage = copy.GetImportedPage(newPageReader, 1);
                                copy.AddPage(newPage);
                                newPageReader.Close();
                                LogHelper.Debug($"已在末尾插入第 {j + 1} 个{pageType}");
                            }
                        }
                    }

                    document.Close();
                }

                reader.Close();

                // 7. 替换原文件
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                if (File.Exists(tempFile) && new FileInfo(tempFile).Length > 0)
                {
                    File.Move(tempFile, filePath);
                    LogHelper.Info($"✓ iTextSharp.LGPLv2 成功插入 {pageCount} 个{pageType}");
                    return true;
                }
                else
                {
                    LogHelper.Error("生成的 PDF 文件不存在或为空");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"iTextSharp.LGPLv2 插入标识页失败: {ex.Message}", ex);

                // 清理临时文件
                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                catch { }

                return false;
            }
        }

        /// <summary>
        /// 创建标识页并返回 PDF 字节数据
        /// </summary>
        private static byte[] CreateIdentifierPageBytes(
            Rectangle pageSize,
            string textContent,
            Font font,
            bool addText)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Document document = new Document(pageSize);
                    PdfWriter writer = PdfWriter.GetInstance(document, ms);
                    document.Open();

                    // 绘制白色背景（使用 DirectContentUnder 确保在文字下方）
                    PdfContentByte cb = writer.DirectContentUnder;
                    cb.SetColorFill(BaseColor.White);
                    cb.Rectangle(0, 0, pageSize.Width, pageSize.Height);
                    cb.Fill();

                    // 只有第一页添加文字
                    if (addText && !string.IsNullOrEmpty(textContent))
                    {
                        DrawCenteredText(document, textContent, font, pageSize);
                    }

                    document.Close();
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"创建标识页失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 绘制居中文字
        /// </summary>
        private static void DrawCenteredText(
            Document document,
            string text,
            Font font,
            Rectangle pageSize)
        {
            try
            {
                // 计算页边距
                float margin = 20;
                float maxTextWidth = pageSize.Width - (margin * 2);

                // 处理文本换行
                var lines = WrapText(text, font, maxTextWidth);

                // 计算总文本高度
                float lineHeight = font.Size * 1.2f;
                float totalTextHeight = lines.Count * lineHeight;

                // 计算起始Y坐标（垂直居中）
                float startY = (pageSize.Height + totalTextHeight) / 2;

                // 添加每一行
                foreach (var line in lines)
                {
                    Paragraph paragraph = new Paragraph(line, font);
                    paragraph.Alignment = Element.ALIGN_CENTER;
                    paragraph.SpacingBefore = 0;
                    paragraph.SpacingAfter = lineHeight - font.Size;
                    
                    document.Add(paragraph);
                }

                LogHelper.Debug($"绘制文字完成，共 {lines.Count} 行");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"绘制文字失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 文本换行处理
        /// </summary>
        private static List<string> WrapText(string text, Font font, float maxWidth)
        {
            var lines = new List<string>();

            if (string.IsNullOrEmpty(text))
                return lines;

            // 先按换行符分割
            string[] paragraphs = text.Split(new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.None);

            foreach (string paragraph in paragraphs)
            {
                if (string.IsNullOrEmpty(paragraph))
                {
                    lines.Add("");
                    continue;
                }

                string remaining = paragraph;
                while (!string.IsNullOrEmpty(remaining))
                {
                    // 测量整行宽度
                    float width = font.BaseFont.GetWidthPoint(remaining, font.Size);

                    if (width <= maxWidth)
                    {
                        // 整行可以放下
                        lines.Add(remaining);
                        break;
                    }
                    else
                    {
                        // 需要换行，找到合适的断点
                        int breakPoint = FindBreakPoint(remaining, font, maxWidth);

                        if (breakPoint > 0)
                        {
                            lines.Add(remaining.Substring(0, breakPoint));
                            remaining = remaining.Substring(breakPoint).TrimStart();
                        }
                        else
                        {
                            // 单个字符都放不下，强制换行
                            lines.Add(remaining.Substring(0, 1));
                            remaining = remaining.Substring(1);
                        }
                    }
                }
            }

            return lines;
        }

        /// <summary>
        /// 查找合适的换行点
        /// </summary>
        private static int FindBreakPoint(string text, Font font, float maxWidth)
        {
            int left = 0;
            int right = text.Length;
            int result = 0;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                float width = font.BaseFont.GetWidthPoint(text.Substring(0, mid), font.Size);

                if (width <= maxWidth)
                {
                    result = mid;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            return result;
        }

        /// <summary>
        /// 获取参考页面索引
        /// </summary>
        private static int GetReferencePageIndex(int totalPages, int insertPosition)
        {
            return insertPosition switch
            {
                0 => 0,  // 第一页之前插入，参考第一页
                -1 => totalPages - 1,  // 最后插入，参考最后一页
                > 0 when insertPosition <= totalPages => insertPosition - 1,
                _ => 0  // 默认参考第一页
            };
        }

        /// <summary>
        /// 计算插入索引
        /// </summary>
        private static int CalculateInsertIndex(int totalPages, int insertPosition)
        {
            return insertPosition switch
            {
                0 => 0,  // 在开头插入
                -1 => totalPages,  // 在末尾插入
                > 0 => insertPosition,  // 在指定位置之后插入
                _ => 0
            };
        }
    }
}
