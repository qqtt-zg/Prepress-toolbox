using System;
using System.Text;
using System.IO;
using iText.Kernel.Pdf;

class Program
{
    static PdfObject Resolve(PdfObject obj)
    {
        if (obj is PdfIndirectReference indRef) return indRef.GetRefersTo();
        return obj;
    }
    
    static void Main()
    {
        string goodFile = @"C:\Users\admin\Desktop\60x60圆_处理结果.pdf";
        string badFile  = @"C:\Users\admin\Desktop\&ID-13&MT-b&DN-1F&DP-黑白光膜&CU-60x60圆 (1)&MK-60x60Y&Row-6&Col-4.pdf";
        
        Console.WriteLine("========== 良好文件（点源可识别）==========");
        AnalyzeFile(goodFile);
        
        Console.WriteLine("\n\n========== 问题文件（点源不可识别）==========");
        AnalyzeFile(badFile);
    }
    
    static void AnalyzeFile(string path)
    {
        if (!File.Exists(path)) { Console.WriteLine("文件不存在: " + path); return; }
        
        using (var reader = new PdfReader(path))
        using (var doc = new PdfDocument(reader))
        {
            Console.WriteLine($"页数: {doc.GetNumberOfPages()}");
            PdfPage page = doc.GetPage(1);
            var mb = page.GetMediaBox();
            Console.WriteLine($"第1页 MediaBox: [{mb.GetLeft():F2}, {mb.GetBottom():F2}, {mb.GetRight():F2}, {mb.GetTop():F2}]");
            
            // OCG
            var ocg = doc.GetCatalog().GetPdfObject().GetAsDictionary(PdfName.OCProperties);
            if (ocg != null)
            {
                var ocgs = ocg.GetAsArray(PdfName.OCGs);
                Console.WriteLine($"图层: {ocgs?.Size()}");
                if (ocgs != null)
                {
                    for (int j = 0; j < ocgs.Size(); j++)
                    {
                        var r = Resolve(ocgs.Get(j));
                        if (r is PdfDictionary d)
                            Console.WriteLine($"  [{j}] {d.GetAsString(PdfName.Name)?.ToUnicodeString()}");
                    }
                }
            }
            
            // 内容流
            PdfObject contents = page.GetPdfObject().Get(PdfName.Contents);
            if (contents is PdfArray arr)
            {
                Console.WriteLine($"内容流: {arr.Size()} 项");
                for (int j = 0; j < arr.Size(); j++)
                {
                    var resolved = Resolve(arr.Get(j));
                    if (resolved is PdfStream s)
                    {
                        byte[] bytes = s.GetBytes();
                        string content = Encoding.Latin1.GetString(bytes);
                        bool hasBDC = content.Contains("BDC");
                        bool hasEMC = content.Contains("EMC");
                        bool hasPath = content.Contains(" m\n") || content.Contains(" m\r");
                        bool hasLayer = content.Contains("Dots_AddCounter") || content.Contains("Dots_L_B");
                        bool hasOC = content.Contains("/OC ");
                        string label = "";
                        if (hasBDC || hasEMC || hasOC) label += " [OCG]";
                        if (hasPath) label += " [路径]";
                        if (hasLayer) label += " [图层名]";
                        
                        Console.WriteLine($"\n--- 流{j} ({bytes.Length}字节){label} ---");
                        // 只打印包含BDC/EMC/路径的关键流，或小于500字节的流
                        if (content.Length < 500 || hasBDC || hasOC)
                        {
                            Console.WriteLine(content);
                        }
                        else
                        {
                            // 只打印前300字符
                            Console.WriteLine(content.Substring(0, 300) + "\n... (截断)");
                        }
                        Console.WriteLine($"--- END 流{j} ---");
                    }
                }
            }
            else if (contents is PdfStream s2)
            {
                Console.WriteLine("单内容流:");
                Console.WriteLine(Encoding.Latin1.GetString(s2.GetBytes()));
            }
            
            // XObject
            var xobjects = page.GetResources().GetResource(PdfName.XObject);
            if (xobjects is PdfDictionary xobjDict)
            {
                Console.WriteLine($"\nXObject: {xobjDict.KeySet().Count} 项");
                foreach (var entry in xobjDict.EntrySet())
                {
                    var resolved = Resolve(entry.Value);
                    if (resolved is PdfStream xs)
                    {
                        Console.WriteLine($"  {entry.Key}: {xs.GetBytes().Length} 字节");
                    }
                }
            }
        }
    }
}
