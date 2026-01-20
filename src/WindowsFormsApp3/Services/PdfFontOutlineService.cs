using System;
using System.Diagnostics;
using System.IO;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using WindowsFormsApp3.Utils;
using IOPath = System.IO.Path;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// PDF字体转曲服务
    /// 将PDF中的文字转换为路径（曲线）
    /// 使用 Ghostscript 实现
    /// </summary>
    public class PdfFontOutlineService
    {
        private const string GHOSTSCRIPT_EXE_64 = "gswin64c.exe";
        private const string GHOSTSCRIPT_EXE_32 = "gswin32c.exe";

        /// <summary>
        /// 将PDF中的所有文字转换为路径（返回字节数组）
        /// </summary>
        /// <param name="inputPath">输入PDF文件路径</param>
        /// <returns>转曲后的PDF字节数组，失败返回null</returns>
        public byte[] ConvertTextToOutlinesBytes(string inputPath)
        {
            try
            {
                // 创建临时输出文件
                string tempOutput = IOPath.Combine(IOPath.GetTempPath(), $"temp_outline_{Guid.NewGuid()}.pdf");
                
                bool success = ConvertTextToOutlines(inputPath, tempOutput);
                
                if (success && File.Exists(tempOutput))
                {
                    byte[] result = File.ReadAllBytes(tempOutput);
                    
                    // 删除临时文件
                    try
                    {
                        File.Delete(tempOutput);
                    }
                    catch { }
                    
                    return result;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[字体转曲] 转换为字节数组失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 将PDF中的所有文字转换为路径
        /// </summary>
        /// <param name="inputPath">输入PDF文件路径</param>
        /// <param name="outputPath">输出PDF文件路径</param>
        /// <returns>是否成功</returns>
        public bool ConvertTextToOutlines(string inputPath, string outputPath)
        {
            try
            {
                LogHelper.Info($"[字体转曲] 开始处理: {IOPath.GetFileName(inputPath)}");

                // 检查 Ghostscript 是否可用
                string gsExe = FindGhostscript();
                if (string.IsNullOrEmpty(gsExe))
                {
                    LogHelper.Error("[字体转曲] 未找到 Ghostscript，请安装 Ghostscript 或将其放在应用程序目录");
                    return false;
                }

                LogHelper.Debug($"[字体转曲] 使用 Ghostscript: {gsExe}");

                // 构建命令行参数
                // -o: 输出文件
                // -dNoOutputFonts: 将字体转换为路径
                // -sDEVICE=pdfwrite: 使用 PDF 写入设备
                // -dPDFSETTINGS=/prepress: 高质量输出（适合印刷）
                // -dCompatibilityLevel=1.4: PDF 1.4 兼容性
                var arguments = $"-o \"{outputPath}\" " +
                               $"-dNoOutputFonts " +
                               $"-sDEVICE=pdfwrite " +
                               $"-dPDFSETTINGS=/prepress " +
                               $"-dCompatibilityLevel=1.4 " +
                               $"\"{inputPath}\"";

                LogHelper.Debug($"[字体转曲] 命令行: {gsExe} {arguments}");

                // 执行 Ghostscript
                var startInfo = new ProcessStartInfo
                {
                    FileName = gsExe,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = IOPath.GetDirectoryName(inputPath)
                };

                using (var process = Process.Start(startInfo))
                {
                    // 读取输出
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        LogHelper.Debug($"[字体转曲] 输出: {output}");
                    }

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        LogHelper.Debug($"[字体转曲] 错误: {error}");
                    }

                    if (process.ExitCode == 0)
                    {
                        LogHelper.Info($"[字体转曲] 完成: {IOPath.GetFileName(outputPath)}");
                        return true;
                    }
                    else
                    {
                        LogHelper.Error($"[字体转曲] Ghostscript 返回错误代码: {process.ExitCode}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[字体转曲] 失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 查找 Ghostscript 可执行文件
        /// </summary>
        private string FindGhostscript()
        {
            // 1. 优先检查应用程序目录下的打包版本（便携版）
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string gsExe = Environment.Is64BitProcess ? GHOSTSCRIPT_EXE_64 : GHOSTSCRIPT_EXE_32;
            
            // 检查 ghostscript 子目录
            string portablePath = IOPath.Combine(appDir, "ghostscript", gsExe);
            if (File.Exists(portablePath))
            {
                LogHelper.Debug($"[字体转曲] 找到便携版 Ghostscript: {portablePath}");
                return portablePath;
            }
            
            // 检查应用程序根目录
            string localPath = IOPath.Combine(appDir, gsExe);
            if (File.Exists(localPath))
            {
                LogHelper.Debug($"[字体转曲] 找到本地 Ghostscript: {localPath}");
                return localPath;
            }

            // 2. 检查 PATH 环境变量
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                foreach (string path in pathEnv.Split(';'))
                {
                    string fullPath = IOPath.Combine(path.Trim(), gsExe);
                    if (File.Exists(fullPath))
                    {
                        LogHelper.Debug($"[字体转曲] 在 PATH 中找到 Ghostscript: {fullPath}");
                        return fullPath;
                    }
                }
            }

            // 3. 检查常见安装位置
            string[] commonPaths = new[]
            {
                @"C:\Program Files\gs\gs10.06.0\bin",
                @"C:\Program Files\gs\gs10.05.0\bin",
                @"C:\Program Files\gs\gs10.04.0\bin",
                @"C:\Program Files\gs\gs10.03.1\bin",
                @"C:\Program Files\gs\gs10.03.0\bin",
                @"C:\Program Files\gs\gs10.02.1\bin",
                @"C:\Program Files\gs\gs10.02.0\bin",
                @"C:\Program Files\gs\gs10.01.2\bin",
                @"C:\Program Files\gs\gs10.01.1\bin",
                @"C:\Program Files\gs\gs10.01.0\bin",
                @"C:\Program Files\gs\gs10.00.0\bin",
                @"C:\Program Files (x86)\gs\gs10.06.0\bin",
                @"C:\Program Files (x86)\gs\gs10.05.0\bin",
                @"C:\Program Files (x86)\gs\gs10.04.0\bin",
                @"C:\Program Files (x86)\gs\gs10.03.1\bin",
                @"C:\Program Files (x86)\gs\gs10.03.0\bin",
                @"C:\Program Files (x86)\gs\gs10.02.1\bin",
                @"C:\Program Files (x86)\gs\gs10.02.0\bin",
                @"C:\Program Files (x86)\gs\gs10.01.2\bin",
                @"C:\Program Files (x86)\gs\gs10.01.1\bin",
                @"C:\Program Files (x86)\gs\gs10.01.0\bin",
                @"C:\Program Files (x86)\gs\gs10.00.0\bin"
            };

            foreach (string path in commonPaths)
            {
                string fullPath = IOPath.Combine(path, gsExe);
                if (File.Exists(fullPath))
                {
                    LogHelper.Debug($"[字体转曲] 在系统安装位置找到 Ghostscript: {fullPath}");
                    return fullPath;
                }
            }

            LogHelper.Warn("[字体转曲] 未找到 Ghostscript");
            return null;
        }

        /// <summary>
        /// 检查PDF是否包含文字（未转曲）
        /// </summary>
        public bool HasText(string filePath)
        {
            try
            {
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument pdfDoc = new PdfDocument(reader))
                {
                    for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                    {
                        var page = pdfDoc.GetPage(i);
                        string text = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page);
                        
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[字体转曲] 检查文字失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取PDF文字内容预览
        /// </summary>
        public string GetTextPreview(string filePath, int maxLength = 200)
        {
            try
            {
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument pdfDoc = new PdfDocument(reader))
                {
                    var text = "";
                    for (int i = 1; i <= Math.Min(3, pdfDoc.GetNumberOfPages()); i++)
                    {
                        var page = pdfDoc.GetPage(i);
                        text += iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page);
                        
                        if (text.Length > maxLength)
                        {
                            return text.Substring(0, maxLength) + "...";
                        }
                    }
                    return text;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[字体转曲] 获取文字预览失败: {ex.Message}", ex);
                return "";
            }
        }

        /// <summary>
        /// 检查 Ghostscript 是否可用
        /// </summary>
        public bool IsGhostscriptAvailable()
        {
            return !string.IsNullOrEmpty(FindGhostscript());
        }

        /// <summary>
        /// 获取 Ghostscript 版本信息
        /// </summary>
        public string GetGhostscriptVersion()
        {
            try
            {
                string gsExe = FindGhostscript();
                if (string.IsNullOrEmpty(gsExe))
                {
                    return null;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = gsExe,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    string version = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    return version;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
