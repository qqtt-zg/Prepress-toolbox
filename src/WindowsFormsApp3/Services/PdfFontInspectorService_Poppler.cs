using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;
using IOPath = System.IO.Path;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 基于 Poppler pdffonts 工具的 PDF 字体检查服务
    /// 提供比 iText 更准确的字体检测，与 Adobe Acrobat 结果一致
    /// </summary>
    public class PdfFontInspectorService_Poppler
    {
        private const string PDFFONTS_EXE = "pdffonts.exe";

        /// <summary>
        /// 检查PDF文件的字体信息
        /// </summary>
        public DocumentFontInfo InspectFonts(string filePath)
        {
            LogHelper.Info($"[Poppler] ========== 开始字体检测 ==========");
            LogHelper.Info($"[Poppler] 文件路径: {filePath}");
            
            var docInfo = new DocumentFontInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            };

            try
            {
                if (!File.Exists(filePath))
                {
                    LogHelper.Error($"[Poppler] PDF文件不存在: {filePath}");
                    return docInfo;
                }

                string pdffontsTool = FindPdffonts();
                if (string.IsNullOrEmpty(pdffontsTool))
                {
                    LogHelper.Warn("[Poppler] 未找到 pdffonts 工具，回退到 iText 解析");
                    // 回退到原有的 iText 实现
                    var itextService = new PdfFontInspectorService();
                    return itextService.InspectFonts(filePath);
                }

                LogHelper.Info($"[Poppler] ✓ 使用 pdffonts 工具: {pdffontsTool}");

                // 执行 pdffonts 命令
                string output = ExecutePdffonts(pdffontsTool, filePath);
                if (string.IsNullOrEmpty(output))
                {
                    LogHelper.Error("[Poppler] pdffonts 未返回数据，回退到 iText");
                    var itextService = new PdfFontInspectorService();
                    return itextService.InspectFonts(filePath);
                }

                // 解析输出
                docInfo.Fonts = ParsePdffontsOutput(output);
                
                LogHelper.Info($"[Poppler] ========== 字体检测完成 ==========");
                LogHelper.Info($"[Poppler] 总字体数: {docInfo.TotalFonts}, 问题字体数: {docInfo.ProblematicFontsCount}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[Poppler] 字体检测失败: {ex.Message}", ex);
                LogHelper.Warn("[Poppler] 回退到 iText 解析");
                var itextService = new PdfFontInspectorService();
                return itextService.InspectFonts(filePath);
            }

            return docInfo;
        }

        /// <summary>
        /// 执行 pdffonts 命令
        /// </summary>
        private string ExecutePdffonts(string pdffontsTool, string filePath)
        {
            try
            {
                LogHelper.Info($"[Poppler] 执行命令: \"{pdffontsTool}\" \"{filePath}\"");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = pdffontsTool,
                    Arguments = $"\"{filePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using (var process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    LogHelper.Info($"[Poppler] 退出代码: {process.ExitCode}");
                    LogHelper.Info($"[Poppler] 输出长度: {output?.Length ?? 0} 字符");

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        LogHelper.Warn($"[Poppler] 错误输出: {error}");
                    }

                    if (process.ExitCode != 0)
                    {
                        LogHelper.Error($"[Poppler] pdffonts 返回错误代码: {process.ExitCode}");
                        return null;
                    }

                    if (!string.IsNullOrEmpty(output))
                    {
                        LogHelper.Debug($"[Poppler] 原始输出:\n{output}");
                    }

                    return output;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[Poppler] 执行 pdffonts 失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 解析 pdffonts 输出
        /// 输出格式示例：
        /// name                                 type              encoding         emb sub uni object ID
        /// ------------------------------------ ----------------- ---------------- --- --- --- ---------
        /// ABCDEE+SimSun                        CID Type 0C       Identity-H       yes yes yes      8  0
        /// </summary>
        private List<FontInfo> ParsePdffontsOutput(string output)
        {
            var fonts = new List<FontInfo>();
            
            try
            {
                LogHelper.Info($"[Poppler] 开始解析输出，长度: {output.Length} 字符");
                
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                LogHelper.Info($"[Poppler] 总行数: {lines.Length}");
                
                if (lines.Length < 3)
                {
                    LogHelper.Warn($"[Poppler] 输出行数太少，可能没有字体数据");
                    return fonts;
                }
                
                // 跳过前两行（标题行和分隔线）
                for (int i = 2; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    LogHelper.Debug($"[Poppler] 解析行 {i}: {line}");
                    
                    var fontInfo = ParseFontLine(line);
                    if (fontInfo != null)
                    {
                        fonts.Add(fontInfo);
                        LogHelper.Info($"[Poppler] ✓ 解析字体: {fontInfo.FontName} (类型: {fontInfo.FontSubtype}, 嵌入: {fontInfo.EmbeddingStatus})");
                    }
                    else
                    {
                        LogHelper.Warn($"[Poppler] ✗ 无法解析行: {line}");
                    }
                }
                
                LogHelper.Info($"[Poppler] 解析完成，共 {fonts.Count} 个字体");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[Poppler] 解析 pdffonts 输出失败: {ex.Message}", ex);
            }

            return fonts;
        }

        /// <summary>
        /// 解析单行字体信息
        /// pdffonts 输出使用固定列宽格式：
        /// name(0-36) type(37-54) encoding(55-71) emb(72-75) sub(76-79) uni(80-83) object ID(84+)
        /// </summary>
        private FontInfo ParseFontLine(string line)
        {
            try
            {
                // 确保行长度足够（至少要包含emb列）
                if (line.Length < 75)
                {
                    LogHelper.Warn($"[Poppler] 行长度不足 ({line.Length}字符): {line}");
                    return null;
                }

                // 按固定列宽提取字段
                string name = line.Substring(0, Math.Min(37, line.Length)).Trim();
                string type = line.Length > 37 ? line.Substring(37, Math.Min(18, line.Length - 37)).Trim() : "";
                string encoding = line.Length > 55 ? line.Substring(55, Math.Min(17, line.Length - 55)).Trim() : "";
                string emb = line.Length > 72 ? line.Substring(72, Math.Min(4, line.Length - 72)).Trim() : "";
                string sub = line.Length > 76 ? line.Substring(76, Math.Min(4, line.Length - 76)).Trim() : "";
                string uni = line.Length > 80 ? line.Substring(80, Math.Min(4, line.Length - 80)).Trim() : "";

                LogHelper.Debug($"[Poppler] 解析结果: name='{name}', type='{type}', encoding='{encoding}', emb='{emb}', sub='{sub}', uni='{uni}'");

                // 验证必需字段
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
                {
                    LogHelper.Warn($"[Poppler] 缺少必需字段: {line}");
                    return null;
                }

                // 创建FontInfo对象
                var fontInfo = new FontInfo
                {
                    FontName = name,
                    FontSubtype = type,
                    Encoding = string.IsNullOrEmpty(encoding) ? "-" : encoding,
                    UsageCount = 1
                };

                // 判断嵌入状态
                if (emb.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    if (sub.Equals("yes", StringComparison.OrdinalIgnoreCase))
                    {
                        fontInfo.EmbeddingStatus = FontEmbeddingStatus.SubsetEmbedded;
                        fontInfo.IsSubset = true;
                    }
                    else
                    {
                        fontInfo.EmbeddingStatus = FontEmbeddingStatus.FullyEmbedded;
                    }
                }
                else
                {
                    fontInfo.EmbeddingStatus = FontEmbeddingStatus.NotEmbedded;
                }

                // 检查是否为标准字体（在移除子集前缀之前检查）
                fontInfo.IsStandardFont = IsStandardFont(fontInfo.FontName);

                // 移除子集前缀 (6个大写字母 + '+')
                // 例如: EPROCH+MicrosoftYaHei -> MicrosoftYaHei
                if (Regex.IsMatch(fontInfo.FontName, @"^[A-Z]{6}\+"))
                {
                    fontInfo.FontName = fontInfo.FontName.Substring(7);
                }

                // 检测问题
                DetectFontIssues(fontInfo);

                return fontInfo;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[Poppler] 解析字体行失败: {line}, 错误: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 检查是否为标准字体
        /// </summary>
        private bool IsStandardFont(string fontName)
        {
            var standardFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Times-Roman", "Times-Bold", "Times-Italic", "Times-BoldItalic",
                "Helvetica", "Helvetica-Bold", "Helvetica-Oblique", "Helvetica-BoldOblique",
                "Courier", "Courier-Bold", "Courier-Oblique", "Courier-BoldOblique",
                "Symbol", "ZapfDingbats"
            };

            // 移除子集前缀
            string cleanName = fontName;
            if (fontName.Length > 7 && fontName[6] == '+')
            {
                cleanName = fontName.Substring(7);
            }

            return standardFonts.Contains(cleanName);
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
            if (fontInfo.FontSubtype == "Type 3")
            {
                fontInfo.Issues.Add("Type3字体可能存在兼容性问题");
                fontInfo.HasIssues = true;
            }
        }

        /// <summary>
        /// 查找 pdffonts 可执行文件
        /// </summary>
        private string FindPdffonts()
        {
            LogHelper.Info("[Poppler] 开始查找 pdffonts 工具");
            
            // 1. 检查应用程序目录下的打包版本
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            LogHelper.Info($"[Poppler] 应用程序目录: {appDir}");
            
            // 检查 poppler 子目录
            string portablePath = IOPath.Combine(appDir, "poppler", "bin", PDFFONTS_EXE);
            LogHelper.Debug($"[Poppler] 检查路径 1: {portablePath}");
            if (File.Exists(portablePath))
            {
                LogHelper.Info($"[Poppler] ✓ 找到便携版 pdffonts: {portablePath}");
                return portablePath;
            }
            
            // 检查应用程序根目录
            string localPath = IOPath.Combine(appDir, PDFFONTS_EXE);
            LogHelper.Debug($"[Poppler] 检查路径 2: {localPath}");
            if (File.Exists(localPath))
            {
                LogHelper.Info($"[Poppler] ✓ 找到本地 pdffonts: {localPath}");
                return localPath;
            }

            // 2. 检查 PATH 环境变量
            LogHelper.Debug("[Poppler] 检查 PATH 环境变量");
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                foreach (string path in pathEnv.Split(';'))
                {
                    string fullPath = IOPath.Combine(path.Trim(), PDFFONTS_EXE);
                    if (File.Exists(fullPath))
                    {
                        LogHelper.Info($"[Poppler] ✓ 在 PATH 中找到 pdffonts: {fullPath}");
                        return fullPath;
                    }
                }
            }

            LogHelper.Warn("[Poppler] ✗ 未找到 pdffonts 工具");
            LogHelper.Warn($"[Poppler] 请确保 poppler\\bin\\{PDFFONTS_EXE} 存在于应用程序目录");
            return null;
        }

        /// <summary>
        /// 检查 pdffonts 是否可用
        /// </summary>
        public bool IsPdffontsAvailable()
        {
            return !string.IsNullOrEmpty(FindPdffonts());
        }

        /// <summary>
        /// 获取 pdffonts 版本信息
        /// </summary>
        public string GetPdffontsVersion()
        {
            try
            {
                string pdffontsTool = FindPdffonts();
                if (string.IsNullOrEmpty(pdffontsTool))
                {
                    return null;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = pdffontsTool,
                    Arguments = "-v",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    // pdffonts 的版本信息可能在 stdout 或 stderr
                    string version = !string.IsNullOrEmpty(output) ? output : error;
                    return version.Trim();
                }
            }
            catch
            {
                return null;
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
