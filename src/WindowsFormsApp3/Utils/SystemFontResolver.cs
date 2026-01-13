using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdfSharp.Fonts;
using WindowsFormsApp3.Services;

namespace WindowsFormsApp3.Utils
{
    /// <summary>
    /// 系统字体解析器 - 提高 PDFsharp 对系统字体的兼容性
    /// 实现 IFontResolver 接口，直接从系统字体文件夹加载字体
    /// </summary>
    public class SystemFontResolver : IFontResolver
    {
        private static readonly string WindowsFontsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");

        // 字体文件缓存：字体名称 -> 字体文件路径
        private readonly Dictionary<string, string> _fontFileCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 字体数据缓存：faceName -> 字体文件字节数据
        private readonly Dictionary<string, byte[]> _fontDataCache = new Dictionary<string, byte[]>();

        // 字体名称映射：显示名称 -> 文件名（不含扩展名）
        private static readonly Dictionary<string, string> FontNameMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // 微软字体
            { "Microsoft YaHei", "msyh" },
            { "Microsoft YaHei UI", "msyh" },
            { "微软雅黑", "msyh" },
            { "SimSun", "simsun" },
            { "宋体", "simsun" },
            { "SimHei", "simhei" },
            { "黑体", "simhei" },
            { "KaiTi", "simkai" },
            { "楷体", "simkai" },
            { "FangSong", "simfang" },
            { "仿宋", "simfang" },
            { "NSimSun", "simsun" },
            { "新宋体", "simsun" },
            
            // 常用英文字体
            { "Arial", "arial" },
            { "Times New Roman", "times" },
            { "Courier New", "cour" },
            { "Verdana", "verdana" },
            { "Tahoma", "tahoma" },
            { "Calibri", "calibri" },
            { "Consolas", "consola" },
            { "Georgia", "georgia" },
            { "Trebuchet MS", "trebuc" },
            { "Comic Sans MS", "comic" },
            { "Impact", "impact" },
            { "Lucida Console", "lucon" },
            { "Lucida Sans Unicode", "l_10646" },
            { "Palatino Linotype", "pala" },
            { "Segoe UI", "segoeui" },
            { "Symbol", "symbol" },
            { "Webdings", "webdings" },
            { "Wingdings", "wingding" },
        };

        public SystemFontResolver()
        {
            LogHelper.Debug("初始化 SystemFontResolver");
            LoadFontFiles();
        }

        /// <summary>
        /// 加载系统字体文件列表
        /// </summary>
        private void LoadFontFiles()
        {
            try
            {
                if (!Directory.Exists(WindowsFontsPath))
                {
                    LogHelper.Warn($"Windows 字体目录不存在: {WindowsFontsPath}");
                    return;
                }

                // 扫描所有字体文件
                var fontFiles = Directory.GetFiles(WindowsFontsPath, "*.ttf")
                    .Concat(Directory.GetFiles(WindowsFontsPath, "*.ttc"))
                    .Concat(Directory.GetFiles(WindowsFontsPath, "*.otf"))
                    .ToList();

                LogHelper.Debug($"找到 {fontFiles.Count} 个字体文件");

                // 建立字体名称到文件路径的映射
                foreach (var fontFile in fontFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(fontFile).ToLower();
                    
                    // 添加到缓存（使用文件名作为键）
                    if (!_fontFileCache.ContainsKey(fileName))
                    {
                        _fontFileCache[fileName] = fontFile;
                    }

                    // 同时添加完整文件名（含扩展名）
                    string fullFileName = Path.GetFileName(fontFile).ToLower();
                    if (!_fontFileCache.ContainsKey(fullFileName))
                    {
                        _fontFileCache[fullFileName] = fontFile;
                    }
                }

                LogHelper.Debug($"字体文件缓存已建立，共 {_fontFileCache.Count} 个条目");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载字体文件列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 解析字体名称到字体文件
        /// </summary>
        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic)
        {
            try
            {
                LogHelper.Debug($"解析字体: {familyName}, Bold={bold}, Italic={italic}");

                // 1. 尝试从映射表查找
                string baseFontName = null;
                if (FontNameMapping.TryGetValue(familyName, out string mappedName))
                {
                    baseFontName = mappedName;
                    LogHelper.Debug($"字体映射: {familyName} -> {baseFontName}");
                }
                else
                {
                    // 2. 直接使用字体名称
                    baseFontName = familyName.ToLower().Replace(" ", "");
                }

                // 3. 根据样式构建字体文件名
                string faceName = BuildFaceName(baseFontName, bold, italic);

                // 4. 查找字体文件
                string fontFilePath = FindFontFile(faceName, baseFontName, bold, italic);

                if (fontFilePath != null)
                {
                    // 5. 检查是否为 TTC 文件（可能有兼容性问题）
                    string extension = Path.GetExtension(fontFilePath).ToLower();
                    if (extension == ".ttc")
                    {
                        LogHelper.Debug($"检测到 TTC 字体文件，可能存在兼容性问题: {fontFilePath}");
                        // 返回 null，让 PDFsharp 尝试使用平台字体解析器
                        return null;
                    }

                    LogHelper.Debug($"✓ 字体解析成功: {familyName} -> {fontFilePath}");
                    
                    // 返回字体信息（不需要模拟样式，因为我们直接加载对应样式的字体文件）
                    return new FontResolverInfo(faceName, false, false);
                }

                LogHelper.Debug($"✗ 字体解析失败: {familyName}，返回 null 让 PDFsharp 使用平台解析器");
                return null;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"解析字体失败 [{familyName}]: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 构建字体 faceName
        /// </summary>
        private string BuildFaceName(string baseName, bool bold, bool italic)
        {
            string suffix = "";
            if (bold && italic)
                suffix = "_bi";
            else if (bold)
                suffix = "_b";
            else if (italic)
                suffix = "_i";

            return $"{baseName}{suffix}";
        }

        /// <summary>
        /// 查找字体文件
        /// </summary>
        private string FindFontFile(string faceName, string baseName, bool bold, bool italic)
        {
            // 尝试的文件名列表（按优先级）
            var tryNames = new List<string>();

            // 1. 尝试精确匹配（带样式后缀）
            if (bold && italic)
            {
                tryNames.Add($"{baseName}bi");
                tryNames.Add($"{baseName}z");  // 中文字体的粗斜体后缀
                tryNames.Add($"{baseName}_bolditalic");
            }
            else if (bold)
            {
                tryNames.Add($"{baseName}bd");
                tryNames.Add($"{baseName}b");
                tryNames.Add($"{baseName}_bold");
            }
            else if (italic)
            {
                tryNames.Add($"{baseName}i");
                tryNames.Add($"{baseName}_italic");
            }

            // 2. 尝试基础名称
            tryNames.Add(baseName);

            // 3. 尝试带 "regular" 后缀
            tryNames.Add($"{baseName}regular");
            tryNames.Add($"{baseName}_regular");

            // 4. 查找文件
            foreach (var tryName in tryNames)
            {
                if (_fontFileCache.TryGetValue(tryName, out string fontPath))
                {
                    return fontPath;
                }

                // 尝试添加 .ttf 扩展名
                if (_fontFileCache.TryGetValue($"{tryName}.ttf", out fontPath))
                {
                    return fontPath;
                }

                // 尝试添加 .ttc 扩展名
                if (_fontFileCache.TryGetValue($"{tryName}.ttc", out fontPath))
                {
                    return fontPath;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取字体文件的字节数据
        /// </summary>
        public byte[] GetFont(string faceName)
        {
            try
            {
                // 1. 检查缓存
                if (_fontDataCache.TryGetValue(faceName, out byte[] cachedData))
                {
                    LogHelper.Debug($"从缓存获取字体数据: {faceName}");
                    return cachedData;
                }

                // 2. 解析 faceName 获取基础名称
                string baseName = faceName;
                if (faceName.EndsWith("_bi"))
                    baseName = faceName.Substring(0, faceName.Length - 3);
                else if (faceName.EndsWith("_b") || faceName.EndsWith("_i"))
                    baseName = faceName.Substring(0, faceName.Length - 2);

                // 3. 查找字体文件
                bool isBold = faceName.EndsWith("_b") || faceName.EndsWith("_bi");
                bool isItalic = faceName.EndsWith("_i") || faceName.EndsWith("_bi");
                string fontFilePath = FindFontFile(faceName, baseName, isBold, isItalic);

                if (fontFilePath == null || !File.Exists(fontFilePath))
                {
                    LogHelper.Warn($"字体文件不存在: {faceName}");
                    return null;
                }

                // 4. 读取字体文件
                byte[] fontData = File.ReadAllBytes(fontFilePath);
                
                // 5. 验证字体文件（基本检查）
                if (!ValidateFontData(fontData, fontFilePath))
                {
                    LogHelper.Warn($"字体文件验证失败: {fontFilePath}");
                    return null;
                }
                
                // 6. 缓存字体数据
                _fontDataCache[faceName] = fontData;

                LogHelper.Debug($"✓ 加载字体文件: {fontFilePath} ({fontData.Length} 字节)");
                return fontData;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"获取字体数据失败 [{faceName}]: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 验证字体文件数据（基本检查）
        /// </summary>
        private bool ValidateFontData(byte[] fontData, string fontFilePath)
        {
            try
            {
                if (fontData == null || fontData.Length < 12)
                {
                    LogHelper.Debug($"字体文件太小: {fontFilePath}");
                    return false;
                }

                // 检查文件头标识
                string extension = Path.GetExtension(fontFilePath).ToLower();
                
                if (extension == ".ttf" || extension == ".otf")
                {
                    // TrueType/OpenType 字体文件头检查
                    // TrueType: 0x00010000 或 "true" 或 "typ1"
                    // OpenType: "OTTO"
                    uint signature = (uint)((fontData[0] << 24) | (fontData[1] << 16) | (fontData[2] << 8) | fontData[3]);
                    
                    bool isValidTTF = signature == 0x00010000 || 
                                     signature == 0x74727565 || // "true"
                                     signature == 0x74797031;   // "typ1"
                    
                    bool isValidOTF = signature == 0x4F54544F; // "OTTO"
                    
                    if (!isValidTTF && !isValidOTF)
                    {
                        LogHelper.Debug($"字体文件头标识无效: {fontFilePath}, signature: 0x{signature:X8}");
                        return false;
                    }
                }
                else if (extension == ".ttc")
                {
                    // TrueType Collection 字体文件头检查
                    // TTC 文件头: "ttcf"
                    uint signature = (uint)((fontData[0] << 24) | (fontData[1] << 16) | (fontData[2] << 8) | fontData[3]);
                    
                    if (signature != 0x74746366) // "ttcf"
                    {
                        LogHelper.Debug($"TTC 字体文件头标识无效: {fontFilePath}, signature: 0x{signature:X8}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"字体文件验证异常: {fontFilePath} - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取字体解析器的字体列表（用于调试）
        /// </summary>
        public IEnumerable<string> GetAvailableFonts()
        {
            return _fontFileCache.Keys;
        }
    }
}
