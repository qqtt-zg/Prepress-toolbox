using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text.pdf;
using WindowsFormsApp3.Services;

namespace WindowsFormsApp3.Utils
{
    /// <summary>
    /// iTextSharp.LGPLv2.Core 字体辅助类
    /// 用于加载和管理系统字体，支持 TTF 和 TTC 格式
    /// </summary>
    public static class ITextSharpLGPLFontHelper
    {
        private static readonly string WindowsFontsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");

        // 字体名称到文件名的映射
        private static readonly Dictionary<string, string> FontNameMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // 中文字体
            { "Microsoft YaHei", "msyh.ttc" },
            { "Microsoft YaHei UI", "msyh.ttc" },
            { "微软雅黑", "msyh.ttc" },
            { "SimSun", "simsun.ttc" },
            { "宋体", "simsun.ttc" },
            { "NSimSun", "simsun.ttc" },
            { "新宋体", "simsun.ttc" },
            { "SimHei", "simhei.ttf" },
            { "黑体", "simhei.ttf" },
            { "KaiTi", "simkai.ttf" },
            { "楷体", "simkai.ttf" },
            { "FangSong", "simfang.ttf" },
            { "仿宋", "simfang.ttf" },
            { "Microsoft JhengHei", "msjh.ttc" },
            { "微软正黑体", "msjh.ttc" },
            { "MingLiU", "mingliub.ttc" },
            { "细明体", "mingliub.ttc" },
            
            // 英文字体
            { "Arial", "arial.ttf" },
            { "Times New Roman", "times.ttf" },
            { "Courier New", "cour.ttf" },
            { "Verdana", "verdana.ttf" },
            { "Tahoma", "tahoma.ttf" },
            { "Calibri", "calibri.ttf" },
            { "Consolas", "consola.ttf" },
            { "Georgia", "georgia.ttf" },
            { "Trebuchet MS", "trebuc.ttf" },
            { "Comic Sans MS", "comic.ttf" },
            { "Impact", "impact.ttf" },
            { "Lucida Console", "lucon.ttf" },
            { "Lucida Sans Unicode", "l_10646.ttf" },
            { "Palatino Linotype", "pala.ttf" },
            { "Segoe UI", "segoeui.ttf" },
        };

        /// <summary>
        /// 创建 BaseFont 对象
        /// 支持 TTF 和 TTC 格式
        /// </summary>
        public static BaseFont CreateBaseFont(string fontName)
        {
            try
            {
                // 1. 查找字体文件
                string fontPath = FindSystemFontFile(fontName);
                
                if (string.IsNullOrEmpty(fontPath))
                {
                    throw new Exception($"找不到字体文件: {fontName}");
                }

                // 2. 检查是否为 TTC 文件
                string extension = Path.GetExtension(fontPath).ToLower();
                
                if (extension == ".ttc")
                {
                    // TTC 文件需要指定索引
                    // 默认使用索引 0 (Regular)
                    fontPath = fontPath + ",0";
                    LogHelper.Debug($"TTC 字体文件，使用索引 0: {fontPath}");
                }

                // 3. 创建 BaseFont
                BaseFont baseFont = BaseFont.CreateFont(
                    fontPath,
                    BaseFont.IDENTITY_H,  // 支持 Unicode
                    BaseFont.EMBEDDED);   // 嵌入字体

                LogHelper.Debug($"✓ 成功创建 iTextSharp.LGPLv2 字体: {fontName} -> {fontPath}");
                return baseFont;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"创建 iTextSharp.LGPLv2 字体失败 [{fontName}]: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 查找系统字体文件
        /// </summary>
        public static string FindSystemFontFile(string fontName)
        {
            try
            {
                // 1. 尝试从映射表查找
                if (FontNameMapping.TryGetValue(fontName, out string fileName))
                {
                    string fullPath = Path.Combine(WindowsFontsPath, fileName);
                    if (File.Exists(fullPath))
                    {
                        LogHelper.Debug($"从映射表找到字体: {fontName} -> {fileName}");
                        return fullPath;
                    }
                }

                // 2. 尝试直接查找（移除空格）
                string normalizedName = fontName.Replace(" ", "").ToLower();
                string[] extensions = { ".ttf", ".ttc", ".otf" };
                
                foreach (var ext in extensions)
                {
                    string tryPath = Path.Combine(WindowsFontsPath, normalizedName + ext);
                    if (File.Exists(tryPath))
                    {
                        LogHelper.Debug($"直接找到字体文件: {tryPath}");
                        return tryPath;
                    }
                }

                // 3. 模糊查找
                var files = Directory.GetFiles(WindowsFontsPath, "*.*")
                    .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
                    .Where(f => Path.GetFileNameWithoutExtension(f)
                        .IndexOf(normalizedName, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                if (files.Any())
                {
                    LogHelper.Debug($"模糊查找到字体文件: {files.First()}");
                    return files.First();
                }

                LogHelper.Warn($"未找到字体文件: {fontName}");
                return null;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"查找字体文件失败 [{fontName}]: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 测试字体兼容性
        /// </summary>
        public static bool TestFontCompatibility(string fontName)
        {
            try
            {
                // 尝试创建 BaseFont
                BaseFont baseFont = CreateBaseFont(fontName);
                
                if (baseFont == null)
                {
                    return false;
                }

                // 测试文本宽度
                float width = baseFont.GetWidthPoint("Test", 12);
                
                return width > 0;
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"字体兼容性测试失败 [{fontName}]: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取所有可用的字体映射
        /// </summary>
        public static Dictionary<string, string> GetFontMapping()
        {
            return new Dictionary<string, string>(FontNameMapping);
        }
    }
}
