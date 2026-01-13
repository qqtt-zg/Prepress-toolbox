using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using iTextSharp.text.pdf;

namespace FontCompatibilityTest
{
    /// <summary>
    /// 测试 iTextSharp.LGPLv2.Core 对系统字体的支持比例
    /// </summary>
    class Program
    {
        private static readonly string WindowsFontsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");

        // 字体名称映射
        private static readonly Dictionary<string, string> FontNameMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Microsoft YaHei", "msyh.ttc" },
            { "微软雅黑", "msyh.ttc" },
            { "SimSun", "simsun.ttc" },
            { "宋体", "simsun.ttc" },
            { "SimHei", "simhei.ttf" },
            { "黑体", "simhei.ttf" },
            { "KaiTi", "simkai.ttf" },
            { "楷体", "simkai.ttf" },
            { "FangSong", "simfang.ttf" },
            { "仿宋", "simfang.ttf" },
            { "Arial", "arial.ttf" },
            { "Calibri", "calibri.ttf" },
            { "Times New Roman", "times.ttf" },
        };

        static void Main(string[] args)
        {
            Console.WriteLine("=== iTextSharp.LGPLv2.Core 字体兼容性测试 ===\n");

            // 统计变量
            int totalFonts = 0;
            int compatibleFonts = 0;
            int incompatibleFonts = 0;
            
            var compatibleList = new List<string>();
            var incompatibleList = new List<string>();
            
            var ttfCompatible = new List<string>();
            var ttfIncompatible = new List<string>();
            var ttcCompatible = new List<string>();
            var ttcIncompatible = new List<string>();

            // 加载系统字体
            using (InstalledFontCollection installedFonts = new InstalledFontCollection())
            {
                totalFonts = installedFonts.Families.Length;
                Console.WriteLine($"系统字体总数: {totalFonts}\n");
                Console.WriteLine("开始测试...\n");

                foreach (var font in installedFonts.Families)
                {
                    string fontName = font.Name;
                    bool isCompatible = TestFontCompatibility(fontName);
                    
                    if (isCompatible)
                    {
                        compatibleFonts++;
                        compatibleList.Add(fontName);
                        
                        // 判断字体格式
                        string fontFile = FindSystemFontFile(fontName);
                        if (!string.IsNullOrEmpty(fontFile))
                        {
                            string ext = Path.GetExtension(fontFile).ToLower();
                            if (ext == ".ttc")
                            {
                                ttcCompatible.Add(fontName);
                            }
                            else if (ext == ".ttf")
                            {
                                ttfCompatible.Add(fontName);
                            }
                        }
                        
                        Console.WriteLine($"✓ {fontName}");
                    }
                    else
                    {
                        incompatibleFonts++;
                        incompatibleList.Add(fontName);
                        
                        // 判断字体格式
                        string fontFile = FindSystemFontFile(fontName);
                        if (!string.IsNullOrEmpty(fontFile))
                        {
                            string ext = Path.GetExtension(fontFile).ToLower();
                            if (ext == ".ttc")
                            {
                                ttcIncompatible.Add(fontName);
                            }
                            else if (ext == ".ttf")
                            {
                                ttfIncompatible.Add(fontName);
                            }
                        }
                        
                        Console.WriteLine($"✗ {fontName}");
                    }
                }
            }

            // 输出统计结果
            Console.WriteLine("\n=== 统计结果 ===\n");
            Console.WriteLine($"总字体数: {totalFonts}");
            Console.WriteLine($"兼容字体: {compatibleFonts} ({(double)compatibleFonts / totalFonts * 100:F2}%)");
            Console.WriteLine($"不兼容字体: {incompatibleFonts} ({(double)incompatibleFonts / totalFonts * 100:F2}%)");
            
            Console.WriteLine($"\n--- 按格式分类 ---");
            Console.WriteLine($"TTF 兼容: {ttfCompatible.Count}");
            Console.WriteLine($"TTF 不兼容: {ttfIncompatible.Count}");
            Console.WriteLine($"TTC 兼容: {ttcCompatible.Count}");
            Console.WriteLine($"TTC 不兼容: {ttcIncompatible.Count}");
            
            if (ttfCompatible.Count + ttfIncompatible.Count > 0)
            {
                double ttfRate = (double)ttfCompatible.Count / (ttfCompatible.Count + ttfIncompatible.Count) * 100;
                Console.WriteLine($"TTF 兼容率: {ttfRate:F2}%");
            }
            
            if (ttcCompatible.Count + ttcIncompatible.Count > 0)
            {
                double ttcRate = (double)ttcCompatible.Count / (ttcCompatible.Count + ttcIncompatible.Count) * 100;
                Console.WriteLine($"TTC 兼容率: {ttcRate:F2}%");
            }

            // 保存详细报告
            SaveReport(totalFonts, compatibleFonts, incompatibleFonts, 
                compatibleList, incompatibleList,
                ttfCompatible, ttfIncompatible, ttcCompatible, ttcIncompatible);

            Console.WriteLine("\n详细报告已保存到: iTextSharp_LGPLv2_字体兼容性报告.txt");
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        static bool TestFontCompatibility(string fontName)
        {
            try
            {
                // 查找字体文件
                string fontPath = FindSystemFontFile(fontName);
                
                if (string.IsNullOrEmpty(fontPath))
                {
                    return false;
                }

                // 检查是否为 TTC 文件
                if (fontPath.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase))
                {
                    fontPath = fontPath + ",0";
                }

                // 尝试创建 BaseFont
                BaseFont baseFont = BaseFont.CreateFont(
                    fontPath,
                    BaseFont.IDENTITY_H,
                    BaseFont.EMBEDDED);

                // 测试文本宽度
                float width = baseFont.GetWidthPoint("Test", 12);
                
                return width > 0;
            }
            catch
            {
                return false;
            }
        }

        static string FindSystemFontFile(string fontName)
        {
            try
            {
                // 1. 尝试从映射表查找
                if (FontNameMapping.TryGetValue(fontName, out string fileName))
                {
                    string fullPath = Path.Combine(WindowsFontsPath, fileName);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }

                // 2. 尝试直接查找
                string normalizedName = fontName.Replace(" ", "").ToLower();
                string[] extensions = { ".ttf", ".ttc", ".otf" };
                
                foreach (var ext in extensions)
                {
                    string tryPath = Path.Combine(WindowsFontsPath, normalizedName + ext);
                    if (File.Exists(tryPath))
                    {
                        return tryPath;
                    }
                }

                // 3. 模糊查找
                var files = Directory.GetFiles(WindowsFontsPath, "*.*")
                    .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
                    .Where(f => Path.GetFileNameWithoutExtension(f)
                        .IndexOf(normalizedName, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                return files.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        static void SaveReport(int total, int compatible, int incompatible,
            List<string> compatibleList, List<string> incompatibleList,
            List<string> ttfCompatible, List<string> ttfIncompatible,
            List<string> ttcCompatible, List<string> ttcIncompatible)
        {
            using (StreamWriter writer = new StreamWriter("iTextSharp_LGPLv2_字体兼容性报告.txt"))
            {
                writer.WriteLine("=== iTextSharp.LGPLv2.Core 字体兼容性测试报告 ===");
                writer.WriteLine($"测试日期: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine();
                
                writer.WriteLine("=== 总体统计 ===");
                writer.WriteLine($"总字体数: {total}");
                writer.WriteLine($"兼容字体: {compatible} ({(double)compatible / total * 100:F2}%)");
                writer.WriteLine($"不兼容字体: {incompatible} ({(double)incompatible / total * 100:F2}%)");
                writer.WriteLine();
                
                writer.WriteLine("=== 按格式分类 ===");
                writer.WriteLine($"TTF 兼容: {ttfCompatible.Count}");
                writer.WriteLine($"TTF 不兼容: {ttfIncompatible.Count}");
                if (ttfCompatible.Count + ttfIncompatible.Count > 0)
                {
                    double ttfRate = (double)ttfCompatible.Count / (ttfCompatible.Count + ttfIncompatible.Count) * 100;
                    writer.WriteLine($"TTF 兼容率: {ttfRate:F2}%");
                }
                writer.WriteLine();
                
                writer.WriteLine($"TTC 兼容: {ttcCompatible.Count}");
                writer.WriteLine($"TTC 不兼容: {ttcIncompatible.Count}");
                if (ttcCompatible.Count + ttcIncompatible.Count > 0)
                {
                    double ttcRate = (double)ttcCompatible.Count / (ttcCompatible.Count + ttcIncompatible.Count) * 100;
                    writer.WriteLine($"TTC 兼容率: {ttcRate:F2}%");
                }
                writer.WriteLine();
                
                writer.WriteLine("=== 兼容字体列表 ===");
                foreach (var font in compatibleList.OrderBy(f => f))
                {
                    writer.WriteLine($"✓ {font}");
                }
                writer.WriteLine();
                
                writer.WriteLine("=== 不兼容字体列表 ===");
                foreach (var font in incompatibleList.OrderBy(f => f))
                {
                    writer.WriteLine($"✗ {font}");
                }
                writer.WriteLine();
                
                writer.WriteLine("=== TTF 兼容字体 ===");
                foreach (var font in ttfCompatible.OrderBy(f => f))
                {
                    writer.WriteLine($"✓ {font}");
                }
                writer.WriteLine();
                
                writer.WriteLine("=== TTC 兼容字体 ===");
                foreach (var font in ttcCompatible.OrderBy(f => f))
                {
                    writer.WriteLine($"✓ {font}");
                }
            }
        }
    }
}
