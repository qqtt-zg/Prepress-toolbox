using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iText.Kernel.Pdf;
using iText.Kernel.Font;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// PDF字体检查服务
    /// </summary>
    public class PdfFontInspectorService
    {
        // 标准14字体列表
        private static readonly HashSet<string> StandardFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Times-Roman", "Times-Bold", "Times-Italic", "Times-BoldItalic",
            "Helvetica", "Helvetica-Bold", "Helvetica-Oblique", "Helvetica-BoldOblique",
            "Courier", "Courier-Bold", "Courier-Oblique", "Courier-BoldOblique",
            "Symbol", "ZapfDingbats"
        };

        /// <summary>
        /// 检查PDF文件的字体信息
        /// </summary>
        public DocumentFontInfo InspectFonts(string filePath)
        {
            var docInfo = new DocumentFontInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            };

            try
            {
                if (!File.Exists(filePath))
                {
                    LogHelper.Error($"PDF文件不存在: {filePath}");
                    return docInfo;
                }

                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    docInfo.TotalPages = document.GetNumberOfPages();

                    // 用于去重的字典 (字体名称 -> FontInfo)
                    var fontDict = new Dictionary<string, FontInfo>();

                    // 遍历所有页面
                    for (int pageNum = 1; pageNum <= docInfo.TotalPages; pageNum++)
                    {
                        try
                        {
                            PdfPage page = document.GetPage(pageNum);
                            PdfResources resources = page.GetResources();

                            if (resources == null)
                                continue;

                            // 获取字体资源
                            PdfDictionary fonts = resources.GetResource(PdfName.Font);
                            if (fonts == null)
                                continue;

                            // 遍历页面中的所有字体
                            foreach (PdfName fontName in fonts.KeySet())
                            {
                                try
                                {
                                    PdfDictionary fontDictionary = fonts.GetAsDictionary(fontName);
                                    if (fontDictionary == null)
                                        continue;

                                    var fontInfo = ExtractFontInfo(fontDictionary, pageNum);
                                    if (fontInfo == null)
                                        continue;

                                    // 使用字体名称作为唯一标识
                                    string key = fontInfo.FontName;

                                    if (fontDict.ContainsKey(key))
                                    {
                                        // 字体已存在，添加页面和增加使用次数
                                        if (!fontDict[key].UsedPages.Contains(pageNum))
                                        {
                                            fontDict[key].UsedPages.Add(pageNum);
                                        }
                                        fontDict[key].UsageCount++;
                                    }
                                    else
                                    {
                                        // 新字体
                                        fontInfo.UsedPages.Add(pageNum);
                                        fontInfo.UsageCount = 1;
                                        fontDict[key] = fontInfo;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.Error($"解析字体失败 - 页码: {pageNum}, 字体: {fontName}, 错误: {ex.Message}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Error($"解析页面字体失败 - 页码: {pageNum}, 错误: {ex.Message}");
                        }
                    }

                    // 转换为列表并排序
                    docInfo.Fonts = fontDict.Values
                        .OrderBy(f => f.HasIssues ? 0 : 1) // 有问题的排在前面
                        .ThenBy(f => f.FontName)
                        .ToList();

                    LogHelper.Info($"字体检查完成: {filePath}, 总字体数: {docInfo.TotalFonts}, 问题字体数: {docInfo.ProblematicFontsCount}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"字体检查失败: {filePath}, 错误: {ex.Message}");
            }

            return docInfo;
        }

        /// <summary>
        /// 从字体字典中提取字体信息
        /// </summary>
        private FontInfo ExtractFontInfo(PdfDictionary fontDict, int pageNum)
        {
            try
            {
                var fontInfo = new FontInfo();

                // 获取字体类型
                PdfName type = fontDict.GetAsName(PdfName.Type);
                PdfName subtype = fontDict.GetAsName(PdfName.Subtype);

                fontInfo.FontType = type?.GetValue() ?? "Unknown";
                fontInfo.FontSubtype = subtype?.GetValue() ?? "Unknown";

                // 获取字体名称
                PdfName baseFontName = fontDict.GetAsName(PdfName.BaseFont);
                if (baseFontName != null)
                {
                    fontInfo.FontName = baseFontName.GetValue();

                    // 检查是否为子集字体（通常以6个大写字母+加号开头）
                    if (fontInfo.FontName.Length > 7 && fontInfo.FontName[6] == '+')
                    {
                        fontInfo.IsSubset = true;
                        // 移除子集前缀以获取真实字体名
                        // fontInfo.FontName = fontInfo.FontName.Substring(7);
                    }
                }
                else
                {
                    fontInfo.FontName = "Unknown";
                }

                // 检查是否为标准字体
                string cleanFontName = fontInfo.FontName;
                if (fontInfo.IsSubset && cleanFontName.Length > 7)
                {
                    cleanFontName = cleanFontName.Substring(7);
                }
                fontInfo.IsStandardFont = StandardFonts.Contains(cleanFontName);

                // 获取字体编码
                PdfObject encoding = fontDict.Get(PdfName.Encoding);
                if (encoding != null)
                {
                    if (encoding.IsName())
                    {
                        fontInfo.Encoding = ((PdfName)encoding).GetValue();
                    }
                    else if (encoding.IsDictionary())
                    {
                        PdfName baseEncoding = ((PdfDictionary)encoding).GetAsName(PdfName.BaseEncoding);
                        fontInfo.Encoding = baseEncoding?.GetValue() ?? "Custom";
                    }
                }

                // 检查嵌入状态
                DetermineEmbeddingStatus(fontDict, fontInfo);

                // 检测问题
                DetectFontIssues(fontInfo);

                return fontInfo;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"提取字体信息失败 - 页码: {pageNum}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 确定字体嵌入状态
        /// </summary>
        private void DetermineEmbeddingStatus(PdfDictionary fontDict, FontInfo fontInfo)
        {
            // 检查FontDescriptor
            PdfDictionary fontDescriptor = fontDict.GetAsDictionary(PdfName.FontDescriptor);

            if (fontDescriptor != null)
            {
                // 检查是否有嵌入的字体文件
                bool hasEmbeddedFile = fontDescriptor.ContainsKey(PdfName.FontFile) ||
                                       fontDescriptor.ContainsKey(PdfName.FontFile2) ||
                                       fontDescriptor.ContainsKey(PdfName.FontFile3);

                if (hasEmbeddedFile)
                {
                    // 有嵌入文件
                    if (fontInfo.IsSubset)
                    {
                        fontInfo.EmbeddingStatus = FontEmbeddingStatus.SubsetEmbedded;
                    }
                    else
                    {
                        fontInfo.EmbeddingStatus = FontEmbeddingStatus.FullyEmbedded;
                    }

                    // 尝试获取嵌入文件大小
                    try
                    {
                        PdfStream fontFile = fontDescriptor.GetAsStream(PdfName.FontFile) ??
                                           fontDescriptor.GetAsStream(PdfName.FontFile2) ??
                                           fontDescriptor.GetAsStream(PdfName.FontFile3);

                        if (fontFile != null)
                        {
                            byte[] fontData = fontFile.GetBytes();
                            fontInfo.EmbeddedSize = fontData?.Length;
                        }
                    }
                    catch { }
                }
                else
                {
                    // 没有嵌入文件
                    fontInfo.EmbeddingStatus = FontEmbeddingStatus.NotEmbedded;
                }
            }
            else
            {
                // 没有FontDescriptor，可能是标准字体
                if (fontInfo.IsStandardFont)
                {
                    // 标准字体不需要嵌入
                    fontInfo.EmbeddingStatus = FontEmbeddingStatus.FullyEmbedded;
                }
                else
                {
                    fontInfo.EmbeddingStatus = FontEmbeddingStatus.NotEmbedded;
                }
            }
        }

        /// <summary>
        /// 检测字体问题
        /// </summary>
        private void DetectFontIssues(FontInfo fontInfo)
        {
            fontInfo.Issues.Clear();
            fontInfo.HasIssues = false;

            // 检查未嵌入字体
            if (fontInfo.EmbeddingStatus == FontEmbeddingStatus.NotEmbedded && !fontInfo.IsStandardFont)
            {
                fontInfo.Issues.Add("字体未嵌入，可能导致显示或打印问题");
                fontInfo.HasIssues = true;
            }

            // 检查子集嵌入（警告，不是错误）
            if (fontInfo.EmbeddingStatus == FontEmbeddingStatus.SubsetEmbedded)
            {
                fontInfo.Issues.Add("字体为子集嵌入，编辑时可能受限");
            }

            // 检查Type3字体（可能有问题）
            if (fontInfo.FontSubtype == "Type3")
            {
                fontInfo.Issues.Add("Type3字体可能存在兼容性问题");
                fontInfo.HasIssues = true;
            }

            // 检查字体名称异常
            if (string.IsNullOrEmpty(fontInfo.FontName) || fontInfo.FontName == "Unknown")
            {
                fontInfo.Issues.Add("字体名称未知");
                fontInfo.HasIssues = true;
            }
        }

        /// <summary>
        /// 获取字体统计摘要
        /// </summary>
        public string GetFontSummary(DocumentFontInfo docInfo)
        {
            if (docInfo == null || docInfo.TotalFonts == 0)
            {
                return "未检测到字体";
            }

            var summary = $"共 {docInfo.TotalFonts} 个字体";

            if (docInfo.HasFontIssues)
            {
                summary += $"，{docInfo.ProblematicFontsCount} 个有问题";
            }

            summary += $" (完全嵌入: {docInfo.FullyEmbeddedCount}, 子集: {docInfo.SubsetEmbeddedCount}, 未嵌入: {docInfo.NotEmbeddedCount})";

            return summary;
        }
    }
}
