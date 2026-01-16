using System;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;

class TestChineseFontFilter
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("测试中文字体过滤功能\n");
        
        using (InstalledFontCollection installedFonts = new InstalledFontCollection())
        {
            var allFonts = installedFonts.Families.ToList();
            Console.WriteLine("系统字体总数: " + allFonts.Count + "\n");
            
            int chineseCount = 0;
            int nonChineseCount = 0;
            
            Console.WriteLine("支持中文的字体:");
            Console.WriteLine("================");
            
            foreach (var fontFamily in allFonts)
            {
                if (SupportsChinese(fontFamily))
                {
                    chineseCount++;
                    Console.WriteLine(chineseCount + ". " + fontFamily.Name);
                }
                else
                {
                    nonChineseCount++;
                }
            }
            
            Console.WriteLine("\n统计结果:");
            Console.WriteLine("================");
            Console.WriteLine("支持中文: " + chineseCount + " 个");
            Console.WriteLine("不支持中文: " + nonChineseCount + " 个");
            Console.WriteLine("总计: " + allFonts.Count + " 个");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
    
    static bool SupportsChinese(FontFamily fontFamily)
    {
        try
        {
            // 测试常用中文字符
            string testChars = "中文测试";
            
            using (var font = new Font(fontFamily, 12F, FontStyle.Regular))
            using (var bitmap = new Bitmap(1, 1))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                // 测试每个中文字符是否有字形
                foreach (char c in testChars)
                {
                    // 使用 MeasureString 测试字符是否可以渲染
                    var size = graphics.MeasureString(c.ToString(), font);
                    
                    // 如果宽度为0或太小，说明字体不支持该字符
                    if (size.Width < 1)
                    {
                        return false;
                    }
                }
                
                return true;
            }
        }
        catch
        {
            // 如果测试失败，认为不支持中文
            return false;
        }
    }
}
