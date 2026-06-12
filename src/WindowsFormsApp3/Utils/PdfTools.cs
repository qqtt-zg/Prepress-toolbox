using System;
using System.IO;
using System.Reflection;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using Spire.Pdf.Graphics.Layer;
using System.Drawing;
using iText.Kernel.Pdf;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Layer;
using iText.Kernel.Colors;
using SystemPath = System.IO.Path;
using System.Linq;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using System.Collections.Generic;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;

namespace WindowsFormsApp3.Utils
{
    /// <summary>
    /// PDF工具类，封装PDF操作相关功能，使用Spire.Pdf和iText7库实现
    /// 注意：PDF尺寸相关功能已迁移至IText7PdfTools，此类仅保留非尺寸相关的功能
    /// 已完成PDFsharp到iText7的迁移，页面框设置功能现在使用iText7实现
    /// </summary>
    public static class PdfTools
    {
  
        /// <summary>
        /// 检查PDF文件中是否存在指定的图层
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="layerNames">要检查的图层名称列表</param>
        /// <returns>如果所有指定图层都存在则返回true，否则返回false</returns>
        public static bool CheckPdfLayersExist(string filePath, params string[] layerNames)
        {
            try
            {
                // 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    return false;
                }

                // 检查文件扩展名是否为PDF
                if (!SystemPath.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                using (Spire.Pdf.PdfDocument document = new Spire.Pdf.PdfDocument())
                {
                    document.LoadFromFile(filePath);

                    // 检查所有指定的图层是否存在
                    foreach (string layerName in layerNames)
                        {
                            bool layerExists = false;
                            foreach (Spire.Pdf.Graphics.Layer.PdfLayer layer in document.Layers)
                            {
                                if (string.Equals(layer.Name, layerName, StringComparison.OrdinalIgnoreCase))
                                {
                                    layerExists = true;
                                    break;
                                }
                            }

                            // 如果任一指定的图层不存在，则返回false
                            if (!layerExists)
                            {
                                return false;
                            }
                        }

                    return true; // 所有指定的图层都存在
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("检查PDF图层失败: " + ex.Message);
                return false;
            }
        }

  
        /// <summary>
        /// 为PDF文件添加名为"Dots_AddCounter"的图层（新的枚举版本）
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="finalDimensions">最终尺寸，格式为"宽度x高度"（毫米）</param>
        /// <param name="shapeType">形状类型</param>
        /// <param name="roundRadius">圆角半径（仅用于圆角矩形）</param>
        /// <returns>是否成功添加图层</returns>
        /// <summary>
        /// 检测PDF文件最后一页是否有可提取的裁切路径
        /// </summary>
        public static bool CanExtractCutPathFromLastPage(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;
                using (var reader = new iText.Kernel.Pdf.PdfReader(filePath))
                using (var doc = new iText.Kernel.Pdf.PdfDocument(reader))
                {
                    if (doc.GetNumberOfPages() < 2) return false;
                    var lastPage = doc.GetPage(doc.GetNumberOfPages());
                    float[] bounds;
                    byte[] pathBytes = ExtractAndConvertCutPath(lastPage, doc, out bounds);
                    return pathBytes != null && pathBytes.Length > 0;
                }
            }
            catch (Exception ex) { LogHelper.Debug("CanExtractCutPathFromLastPage 异常: " + ex.Message); return false; }
        }

        public static bool AddDotsAddCounterLayer(string filePath, string finalDimensions, ShapeType shapeType, double roundRadius = 0)
        {
            LogHelper.Debug($"AddDotsAddCounterLayer调用（枚举版本），参数: filePath={filePath}, shapeType={shapeType}, roundRadius={roundRadius}");

            try
            {
                // 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    LogHelper.Debug("文件不存在: " + filePath);
                    return false;
                }

                // 检查文件扩展名是否为PDF
                if (!SystemPath.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // 异形处理直接调用专用方法
                if (shapeType == ShapeType.Special)
                {
                    LogHelper.Debug("检测到异形，调用ProcessSpecialShapePdf");
                    return ProcessSpecialShapePdf(filePath);
                }

                // 跳过页面重排 - 文件已经通过ProcessPdfFileInternalSync完成页面重排和标识页插入
                LogHelper.Debug("跳过页面重排 - 文件已经完成页面重排和标识页插入");
                Dictionary<int, PageTransformInfo> transformMap = new Dictionary<int, PageTransformInfo>();

                using (Spire.Pdf.PdfDocument document = new Spire.Pdf.PdfDocument())
                {
                    document.LoadFromFile(filePath);

                    // 获取或创建图层
                    Spire.Pdf.Graphics.Layer.PdfLayer layer = document.Layers.AddLayer("Dots_AddCounter");
                    // 创建Dots_L_B_出血线图层
                    Spire.Pdf.Graphics.Layer.PdfLayer bleedLayer = document.Layers.AddLayer("Dots_L_B_出血线");

                    // 解析finalDimensions参数
                    float rectWidth = 0;
                    float rectHeight = 0;
                    if (!string.IsNullOrEmpty(finalDimensions))
                    {
                        LogHelper.Debug("解析finalDimensions: " + finalDimensions);
                        string[] dimensions = finalDimensions.Split('x');
                        if (dimensions.Length == 2)
                        {
                            // 提取宽度（去掉任何非数字字符）
                            string widthStr = ExtractNumericValue(dimensions[0]);

                            // 提取高度（去掉任何非数字字符和后续的形状代号）
                            string heightStr = ExtractNumericValue(dimensions[1]);

                            LogHelper.Debug("提取的尺寸: 宽=" + widthStr + ", 高=" + heightStr);

                            // 转换毫米到点（1mm≈2.83465点）
                            if (float.TryParse(widthStr, out float widthMm) && float.TryParse(heightStr, out float heightMm))
                            {
                                float calculatedWidth = (float)(widthMm * 2.83465);
                                float calculatedHeight = (float)(heightMm * 2.83465);

                                // 获取页面方向信息（通过页面宽高比判断）- 使用iText7
                                bool isPageLandscape = false;
                                try
                                {
                                    using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(filePath))
                                    using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader))
                                    {
                                        if (pdfDoc.GetNumberOfPages() > 0)
                                        {
                                            iText.Kernel.Pdf.PdfPage pdfPage = pdfDoc.GetPage(1);
                                            iText.Kernel.Geom.Rectangle pageSize = pdfPage.GetCropBox() ?? pdfPage.GetMediaBox();
                                            if (pageSize != null)
                                            {
                                                isPageLandscape = pageSize.GetWidth() > pageSize.GetHeight();
                                                LogHelper.Debug("iText7页面方向检测成功: " + (isPageLandscape ? "横向" : "纵向"));
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.Debug("iText7页面方向检测失败: " + ex.Message + "，使用默认方向");
                                }

                                // 检查计算出的尺寸与页面方向是否匹配
                                bool isCalculatedLandscape = calculatedWidth > calculatedHeight;

                                // 如果页面方向和计算出的尺寸方向不一致，则交换宽度和高度
                                if (isPageLandscape != isCalculatedLandscape)
                                {
                                    rectWidth = calculatedHeight;
                                    rectHeight = calculatedWidth;
                                    LogHelper.Debug("检测到方向不匹配，交换宽度和高度: 宽=" + rectWidth + ", 高=" + rectHeight);
                                }
                                else
                                {
                                    rectWidth = calculatedWidth;
                                    rectHeight = calculatedHeight;
                                    LogHelper.Debug("转换后的尺寸(点): 宽=" + rectWidth + ", 高=" + rectHeight);
                                }
                            }
                        }
                    }

                    // 为所有页面添加图层内容
                    for (int i = 0; i < document.Pages.Count; i++)
                    {
                        Spire.Pdf.PdfPageBase page = document.Pages[i];

                        // 计算实际尺寸
                        float actualRectWidth = rectWidth;
                        float actualRectHeight = rectHeight;

                        if (actualRectWidth <= 0 || actualRectHeight <= 0)
                        {
                            LogHelper.Debug("未提供有效的finalDimensions，使用默认尺寸");
                            float minSize = 50f;
                            float maxWidthRatio = 0.8f;
                            float maxHeightRatio = 0.8f;

                            actualRectWidth = Math.Max(minSize, page.Size.Width * maxWidthRatio);
                            actualRectHeight = Math.Max(minSize, page.Size.Height * maxHeightRatio);

                            LogHelper.Debug("使用默认尺寸(点): 宽=" + actualRectWidth + ", 高=" + actualRectHeight);
                        }

                        // 计算居中位置
                        float rectX = (page.Size.Width - actualRectWidth) / 2;
                        float rectY = (page.Size.Height - actualRectHeight) / 2;

                        LogHelper.Debug($"第{i+1}页形状绘制信息: 位置=({rectX}, {rectY}), 尺寸=({actualRectWidth}x{actualRectHeight}), 形状类型={shapeType}");

                        // 创建红色描边的笔，粗细为0.01
                        Spire.Pdf.Graphics.PdfPen redPen = new Spire.Pdf.Graphics.PdfPen(System.Drawing.Color.Red, 0.01f);

                        // 获取图层的图形对象
                        Spire.Pdf.Graphics.PdfCanvas layerCanvas = layer.CreateGraphics(page.Canvas);

                        // 根据shapeType绘制不同的形状
                        switch (shapeType)
                        {
                            case ShapeType.Circle:
                                LogHelper.Debug("绘制圆形");
                                layerCanvas.DrawEllipse(redPen, rectX, rectY, actualRectWidth, actualRectHeight);
                                break;

                            case ShapeType.RoundRect:
                                LogHelper.Debug($"绘制圆角矩形，半径={roundRadius}毫米");
                                float cornerRadiusPt = (float)(roundRadius * 2.83465); // 转换毫米到点

                                // 使用PdfPath手动创建圆角矩形
                                Spire.Pdf.Graphics.PdfPath path = new Spire.Pdf.Graphics.PdfPath();

                                // 确保圆角半径不会太大
                                float radiusPt = Math.Min(cornerRadiusPt, Math.Min(actualRectWidth, actualRectHeight) / 2);

                                LogHelper.Debug($"圆角半径计算: 原始={cornerRadiusPt}点, 限制到={radiusPt}点");

                                // 绘制圆角矩形的各个角和边
                                path.AddArc(rectX + actualRectWidth - 2 * radiusPt, rectY, 2 * radiusPt, 2 * radiusPt, 270, 90);
                                path.AddLine(rectX + actualRectWidth, rectY + radiusPt, rectX + actualRectWidth, rectY + actualRectHeight - radiusPt);
                                path.AddArc(rectX + actualRectWidth - 2 * radiusPt, rectY + actualRectHeight - 2 * radiusPt, 2 * radiusPt, 2 * radiusPt, 0, 90);
                                path.AddLine(rectX + actualRectWidth - radiusPt, rectY + actualRectHeight, rectX + radiusPt, rectY + actualRectHeight);
                                path.AddArc(rectX, rectY + actualRectHeight - 2 * radiusPt, 2 * radiusPt, 2 * radiusPt, 90, 90);
                                path.AddLine(rectX, rectY + actualRectHeight - radiusPt, rectX, rectY + radiusPt);
                                path.AddArc(rectX, rectY, 2 * radiusPt, 2 * radiusPt, 180, 90);
                                path.AddLine(rectX + radiusPt, rectY, rectX + actualRectWidth - radiusPt, rectY);

                                path.CloseAllFigures();
                                layerCanvas.DrawPath(redPen, path);
                                break;

                            case ShapeType.RightAngle:
                            default:
                                LogHelper.Debug("绘制直角矩形");
                                layerCanvas.DrawRectangle(redPen, rectX, rectY, actualRectWidth, actualRectHeight);
                                break;
                        }

                        // 在Dots_L_B_出血线图层上绘制绿色居中矩形
                        float bleedRectX = 0;
                        float bleedRectY = 0;
                        float bleedRectWidth = page.Size.Width;
                        float bleedRectHeight = page.Size.Height;

                        Spire.Pdf.Graphics.PdfPen greenPen = new Spire.Pdf.Graphics.PdfPen(System.Drawing.Color.Green, 0.01f);
                        Spire.Pdf.Graphics.PdfCanvas bleedLayerCanvas = bleedLayer.CreateGraphics(page.Canvas);
                        bleedLayerCanvas.DrawRectangle(greenPen, bleedRectX, bleedRectY, bleedRectWidth, bleedRectHeight);
                    }

                    // 保存修改后的PDF文件
                    string cacheFolder = SystemPath.Combine(SystemPath.GetTempPath(), "PDFToolCache");
                    Directory.CreateDirectory(cacheFolder);
                    string tempFilePath = SystemPath.Combine(cacheFolder, $"temp_{Guid.NewGuid()}.pdf");
                    document.SaveToFile(tempFilePath);

                    document.Close();

                    File.Delete(filePath);
                    File.Move(tempFilePath, filePath);

                    LogHelper.Debug("AddDotsAddCounterLayer（枚举版本）完成");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug("添加图层失败: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 为PDF文件添加名为"Dots_AddCounter"的图层（兼容旧版本）
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="finalDimensions">最终尺寸，格式为"宽度x高度"（毫米）</param>
        /// <param name="cornerRadius">圆角半径，"0"表示直角矩形，"R"表示圆形，其他数值表示圆角矩形</param>
        /// <param name="usePdfLastPage">是否使用PDF最后一页逻辑</param>
        /// <returns>是否成功添加图层</returns>
        [Obsolete("请使用AddDotsAddCounterLayer(string, string, ShapeType, double)方法")]
        public static bool AddDotsAddCounterLayer(string filePath, string finalDimensions, string cornerRadius = "0", bool usePdfLastPage = false)
        {
            // 将旧参数转换为新的枚举类型
            ShapeType shapeType;
            double roundRadius = 0;

            if (usePdfLastPage || string.Equals(cornerRadius, "Y", StringComparison.OrdinalIgnoreCase))
            {
                shapeType = ShapeType.Special;
            }
            else if (string.Equals(cornerRadius, "R", StringComparison.OrdinalIgnoreCase))
            {
                shapeType = ShapeType.Circle;
            }
            else if (double.TryParse(cornerRadius, out double radius) && radius > 0)
            {
                shapeType = ShapeType.RoundRect;
                roundRadius = radius;
            }
            else
            {
                shapeType = ShapeType.RightAngle;
            }

            // 调用新的枚举版本
            return AddDotsAddCounterLayer(filePath, finalDimensions, shapeType, roundRadius);
        }

        /// <summary>
        /// 处理异形PDF文件逻辑（使用iText 7实现）
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <summary>
        /// 从PDF页面中提取裁切路径，并将坐标通过cm逆变换转换到页面坐标系
        /// </summary>
        private static byte[] ExtractAndConvertCutPath(iText.Kernel.Pdf.PdfPage page, iText.Kernel.Pdf.PdfDocument doc, out float[] pageBounds)
        {
            pageBounds = null;
            try
            {
                iText.Kernel.Pdf.PdfDictionary pageDict = page.GetPdfObject();
                iText.Kernel.Pdf.PdfObject contents = pageDict.Get(iText.Kernel.Pdf.PdfName.Contents);
                byte[] contentBytes = null;
                if (contents is iText.Kernel.Pdf.PdfStream singleStream)
                    contentBytes = singleStream.GetBytes();
                else if (contents is iText.Kernel.Pdf.PdfArray arr)
                {
                    using (var ms = new System.IO.MemoryStream())
                    {
                        for (int j = 0; j < arr.Size(); j++)
                        {
                            var obj = arr.Get(j);
                            iText.Kernel.Pdf.PdfStream cs = obj is iText.Kernel.Pdf.PdfStream s ? s :
                                (obj is iText.Kernel.Pdf.PdfIndirectReference indRef && indRef.GetRefersTo() is iText.Kernel.Pdf.PdfStream indS) ? indS : null;
                            if (cs != null) { byte[] csBytes = cs.GetBytes(); ms.Write(csBytes, 0, csBytes.Length); }
                        }
                        contentBytes = ms.ToArray();
                    }
                }
                if (contentBytes == null || contentBytes.Length == 0) { LogHelper.Debug("ExtractAndConvertCutPath: 内容流为空"); return null; }
                string content = System.Text.Encoding.ASCII.GetString(contentBytes);
                if (!content.Contains(" m\n") && !content.Contains(" m\r") && !content.Contains(" c\n") && !content.Contains(" c\r"))
                { LogHelper.Debug("ExtractAndConvertCutPath: 无路径操作符"); return null; }
                string[] contentLines = content.Split(new[] { "\n", "\r\n", "\r" }, StringSplitOptions.None);
                float cmTx = 0, cmTy = 0; int pathColorLine = -1;
                for (int li = 0; li < contentLines.Length; li++)
                {
                    string line = contentLines[li].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.EndsWith(" cm"))
                    {
                        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 7)
                        {
                            float.TryParse(parts[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float e);
                            float.TryParse(parts[5], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f);
                            cmTx += e; cmTy += f;
                        }
                    }
                    // 支持CMYK(K)、RGB(RG)、灰度(G)、CS+SCN(命名颜色空间)等描边颜色操作符
                    if ((line.EndsWith(" K") || line.EndsWith(" RG") || line.EndsWith(" G") || (line.Contains(" CS") && line.EndsWith(" SCN"))) && pathColorLine < 0)
                    {
                        // 在颜色行前后各6行范围内搜索 w 和 m 操作符
                        // 某些PDF中 w 可能在颜色行之前（如 CS+SCN 模式）
                        bool hasW = false, hasM = false;
                        for (int j = Math.Max(0, li - 6); j < Math.Min(li + 6, contentLines.Length); j++)
                        { string next = contentLines[j].Trim(); if (next.EndsWith(" w")) hasW = true; if (next.EndsWith(" m")) hasM = true; }
                        if (hasW && hasM) pathColorLine = li;
                    }
                }
                if (pathColorLine < 0) { LogHelper.Debug("ExtractAndConvertCutPath: 未找到颜色(K/RG/G/CS+SCN)+w+m组合"); return null; }
                string colorLine = "", widthLine = ""; int pathStartLine = -1;
                for (int li = pathColorLine; li < contentLines.Length; li++)
                {
                    string line = contentLines[li].Trim();
                    if (line.EndsWith(" K") || line.EndsWith(" RG") || line.EndsWith(" G") || (line.Contains(" CS") && line.EndsWith(" SCN"))) { colorLine = line; continue; }
                    if (line.EndsWith(" w")) { widthLine = line; continue; }
                    if (line.EndsWith(" m") || line.EndsWith(" c")) { pathStartLine = li; break; }
                }
                if (pathStartLine < 0) return null;
                var pathLineList = new System.Collections.Generic.List<string>();
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                for (int li = pathStartLine; li < contentLines.Length; li++)
                {
                    string line = contentLines[li].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.EndsWith(" m") || line.EndsWith(" c") || line.EndsWith(" l") || line == "h")
                    { string converted = ConvertLineCoords(line, cmTx, cmTy); pathLineList.Add(converted); UpdateBounds(converted, ref minX, ref minY, ref maxX, ref maxY); }
                    else if (line == "S" || line == "s" || line == "f" || line == "f*" || line == "B" || line == "B*")
                    { pathLineList.Add(line); break; }
                    else break;
                }
                if (pathLineList.Count == 0) return null;
                pageBounds = new float[] { minX, minY, maxX, maxY };
                using (var ms = new System.IO.MemoryStream())
                {
                    ms.Write(System.Text.Encoding.ASCII.GetBytes("q\n"), 0, 2);
                    // 统一为标准裁切路径样式：红色描边，线宽0.01pt
                    byte[] colorBytes = System.Text.Encoding.ASCII.GetBytes("1 0 0 RG\n"); ms.Write(colorBytes, 0, colorBytes.Length);
                    byte[] widthBytes = System.Text.Encoding.ASCII.GetBytes("0.01 w\n"); ms.Write(widthBytes, 0, widthBytes.Length);
                    foreach (string pl in pathLineList) { byte[] plBytes = System.Text.Encoding.ASCII.GetBytes(pl + "\n"); ms.Write(plBytes, 0, plBytes.Length); }
                    ms.Write(System.Text.Encoding.ASCII.GetBytes("Q\n"), 0, 2);
                    LogHelper.Debug("ExtractAndConvertCutPath: cm偏移=(" + cmTx + "," + cmTy + "), 路径行=" + pathLineList.Count + ", 边界=(" + minX + "," + minY + ")-(" + maxX + "," + maxY + ")");
                    return ms.ToArray();
                }
            }
            catch (Exception ex) { LogHelper.Debug("ExtractAndConvertCutPath 异常: " + ex.Message); return null; }
        }

        private static string ConvertLineCoords(string line, float cmTx, float cmTy)
        {
            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string op = parts[parts.Length - 1];
            if (op == "h") return line;
            var nums = new System.Collections.Generic.List<string>();
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (float.TryParse(parts[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float val))
                { if (i % 2 == 0) val += cmTx; else val += cmTy; nums.Add(val.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
                else nums.Add(parts[i]);
            }
            return string.Join(" ", nums) + " " + op;
        }

        private static void UpdateBounds(string line, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string op = parts[parts.Length - 1];
            if (op == "h") return;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (float.TryParse(parts[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float val))
                { if (i % 2 == 0) { if (val < minX) minX = val; if (val > maxX) maxX = val; } else { if (val < minY) minY = val; if (val > maxY) maxY = val; } }
            }
        }

        /// <returns>是否处理成功</returns>
        public static bool ProcessSpecialShapePdf(string filePath)
        {
            LogHelper.Debug("ProcessSpecialShapePdf调用（iText 7版本），文件路径: " + filePath);
            try
            {
                // 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    LogHelper.Debug("文件不存在: " + filePath);
                    return false;
                }

                // 检查文件扩展名是否为PDF
                if (!SystemPath.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // 异形处理不需要页面重排，直接进行图层处理

                // 创建临时文件路径
                string tempFolder = SystemPath.Combine(SystemPath.GetTempPath(), "PDFToolCache");
                Directory.CreateDirectory(tempFolder);
                string tempFilePath = SystemPath.Combine(tempFolder, SystemPath.GetRandomFileName() + ".pdf");

                iText.Kernel.Pdf.PdfReader reader = null;
                iText.Kernel.Pdf.PdfWriter writer = null;
                iText.Kernel.Pdf.PdfDocument document = null;

                try
                {
                    reader = new iText.Kernel.Pdf.PdfReader(filePath);
                    writer = new iText.Kernel.Pdf.PdfWriter(tempFilePath);
                    document = new iText.Kernel.Pdf.PdfDocument(reader, writer);

                    // 检查文档页数
                    if (document.GetNumberOfPages() < 2)
                    {
                        LogHelper.Debug("文档页数不足2页，无法执行异形处理");
                        return false; // 至少需要2页才能执行异形处理
                    }

                    // 保存原始旋转状态
                    List<int> originalPageRotations = new List<int>();
                    for (int i = 1; i <= document.GetNumberOfPages(); i++)
                    {
                        iText.Kernel.Pdf.PdfPage page = document.GetPage(i);
                        originalPageRotations.Add(page.GetRotation());
                    }

                    // 获取最后一页作为模板
                    int lastPageIndex = document.GetNumberOfPages();
                    iText.Kernel.Pdf.PdfPage lastPage = document.GetPage(lastPageIndex);

                    // 保存最后一页的原始旋转状态
                    int originalLastPageRotation = lastPage.GetRotation();

                    // 临时清除原页面旋转 - 防止旋转导致的内容截断问题
                    lastPage.SetRotation(0); // 重置为0度

                    // 创建图层（使用iText 7的OCG功能）
                    // 创建Dots_AddCounter图层
                    iText.Kernel.Pdf.Layer.PdfLayer addCounterLayer = new iText.Kernel.Pdf.Layer.PdfLayer("Dots_AddCounter", document);
                    // 创建Dots_L_B_出血线图层
                    iText.Kernel.Pdf.Layer.PdfLayer bleedLayer = new iText.Kernel.Pdf.Layer.PdfLayer("Dots_L_B_出血线", document);

                    // 获取最后一页的尺寸
                    iText.Kernel.Geom.Rectangle lastPageSize = lastPage.GetCropBox() ?? lastPage.GetMediaBox();


                    // 获取模板页的CropBox原点，用于坐标偏移计算
                    float templateCropLeft = (float)(lastPageSize.GetLeft());
                    float templateCropBottom = (float)(lastPageSize.GetBottom());
                    LogHelper.Debug("模板页CropBox原点: (" + templateCropLeft + ", " + templateCropBottom + ")");
                    
                    // 提取最后一页的裁切路径（坐标已在可视区域空间中）
                    float[] cutPathBounds;
                    byte[] convertedCutPath = ExtractAndConvertCutPath(lastPage, document, out cutPathBounds);
                    if (convertedCutPath == null || convertedCutPath.Length == 0)
                    {
                        LogHelper.Debug("无法提取裁切路径，跳过异形处理");
                        return false;
                    }
                    // 为文档的所有页面添加模板内容和出血线
                    for (int i = 1; i <= document.GetNumberOfPages(); i++)
                    {
                        iText.Kernel.Pdf.PdfPage currentPage = document.GetPage(i);

                        // 保存当前页面的原始旋转状态
                        int originalCurrentPageRotation = currentPage.GetRotation();

                        // 临时清除当前页面旋转 - 防止旋转导致的内容截断
                        currentPage.SetRotation(0);

                        // 获取当前页面尺寸
                        iText.Kernel.Geom.Rectangle currentPageSize = currentPage.GetCropBox() ?? currentPage.GetMediaBox();

                        // 计算居中位置 - 确保内容居中显示，避免内容被截断
                        float centerX = (float)((currentPageSize.GetWidth() - lastPageSize.GetWidth()) / 2);
                        float centerY = (float)((currentPageSize.GetHeight() - lastPageSize.GetHeight()) / 2);
                        LogHelper.Debug("第" + i + "页居中位置: X=" + centerX + ", Y=" + centerY);

                        // 1. 先创建出血线画布（在内容流中排在前面，视觉上在底层）
                        {
                            // 出血线偏移（ConcatMatrix在BDC内可能不生效，需验证）
                            float bleedOffsetX = (float)(currentPageSize.GetLeft() - templateCropLeft + centerX);
                            float bleedOffsetY = (float)(currentPageSize.GetBottom() - templateCropBottom + centerY);
                            iText.Kernel.Pdf.Canvas.PdfCanvas bleedCanvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(currentPage.NewContentStreamAfter(), currentPage.GetResources(), document);
                            bleedCanvas.BeginLayer(bleedLayer);
                            bleedCanvas.ConcatMatrix(1, 0, 0, 1, bleedOffsetX, bleedOffsetY);
                            bleedCanvas.SetLineWidth(0.01f);
                            bleedCanvas.SetStrokeColor(ColorConstants.GREEN);
                            bleedCanvas.Rectangle((float)lastPageSize.GetLeft(), (float)lastPageSize.GetBottom(), (float)lastPageSize.GetWidth(), (float)lastPageSize.GetHeight());
                            bleedCanvas.Stroke();
                            bleedCanvas.EndLayer();
                            bleedCanvas.Release();
                        }
                        
                        // 2. 创建裁切路径画布（坐标已在CropBox空间中，无需ConcatMatrix偏移）
                        {
                            if (convertedCutPath != null && convertedCutPath.Length > 0)
                            {
                                iText.Kernel.Pdf.Canvas.PdfCanvas addCounterCanvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(currentPage.NewContentStreamAfter(), currentPage.GetResources(), document);
                                addCounterCanvas.BeginLayer(addCounterLayer);
                                iText.Kernel.Pdf.PdfStream contentStream = addCounterCanvas.GetContentStream();
                                string bdcLine = System.Text.Encoding.ASCII.GetString(contentStream.GetBytes()).Trim();
                                using (var ms = new System.IO.MemoryStream())
                                {
                                    // 写入BDC标记和裁切路径
                                    ms.Write(System.Text.Encoding.ASCII.GetBytes(bdcLine + "\n"), 0, bdcLine.Length + 1);
                                    ms.Write(convertedCutPath, 0, convertedCutPath.Length);
                                    contentStream.SetData(ms.ToArray());
                                }
                                addCounterCanvas.EndLayer();
                                addCounterCanvas.Release();
                                LogHelper.Debug("第" + i + "页 Dots_AddCounter 图层写入完成");
                            }
                        }


                        // 恢复当前页面的原始旋转状态
                        currentPage.SetRotation(originalCurrentPageRotation);
                    }

                    // 恢复最后一页的原始旋转状态
                    lastPage.SetRotation(originalLastPageRotation);

                    // 删除文档的最后一页
                    document.RemovePage(lastPageIndex);
                    LogHelper.Debug("删除最后一页，剩余页数: " + document.GetNumberOfPages());

                    // 恢复除最后一页外的所有页面的原始旋转状态（因为最后一页已被删除）
                    for (int i = 1; i <= document.GetNumberOfPages(); i++)
                    {
                        document.GetPage(i).SetRotation(originalPageRotations[i - 1]);
                    }

                    LogHelper.Debug("保存修改后的文档到临时文件：" + tempFilePath);
                }
                finally
                {
                    // 确保资源被正确释放
                    document?.Close();
                    writer?.Close();
                    reader?.Close();
                }

                // 删除原始文件并将临时文件重命名为原始文件名
                File.Delete(filePath);
                File.Move(tempFilePath, filePath);

                // 已在方法开始处调用间接引用重排，此处不需要再调用
                LogHelper.Debug("ProcessSpecialShapePdf执行GC清理，处理完成");
                GC.Collect();
                GC.WaitForPendingFinalizers();

                return true;
            }
            catch (iText.Kernel.Exceptions.PdfException pdfEx)
            {
                LogHelper.Debug("iText 7 PDF异常: " + pdfEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.Debug("处理异形PDF失败: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 使用iText7库将所有页面的MediaBox、TrimBox、BleedBox和ArtBox设置为与CropBox相同
        /// 这是避免页面内容被截断的核心方法
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <returns>是否设置成功</returns>
/// <summary>
/// 1x1布局重排的页面变换信息
/// </summary>
public class PageTransformInfo
{
    public int OriginalPageNumber { get; set; }
    public float OriginalWidth { get; set; }
    public float OriginalHeight { get; set; }
    public float ScaleX { get; set; }
    public float ScaleY { get; set; }
    public float TranslateX { get; set; }
    public float TranslateY { get; set; }
    public int Rotation { get; set; } // 添加页面旋转信息
    public iText.Kernel.Geom.Rectangle SourceArea { get; set; }
    public iText.Kernel.Geom.Rectangle TargetArea { get; set; }
}

/// <summary>
/// 虚拟页面容器，用于间接引用管理
/// </summary>
public class VirtualPageContainer
{
    public float ContainerWidth { get; set; }
    public float ContainerHeight { get; set; }
    public iText.Kernel.Geom.Rectangle BoundingBox { get; set; }
    public List<PageTransformInfo> PageTransforms { get; set; } = new List<PageTransformInfo>();
    
    /// <summary>
    /// 创建基于首页尺寸的标准化容器（保持Adobe Acrobat兼容性）
    /// </summary>
    public static VirtualPageContainer CreateFromFirstPage(iText.Kernel.Pdf.PdfDocument document)
    {
        var container = new VirtualPageContainer();

        // 获取首页作为基准尺寸 - 优先使用MediaBox保持Adobe Acrobat兼容性
        var firstPage = document.GetPage(1);
        var mediaBox = firstPage.GetMediaBox();
        var cropBox = firstPage.GetCropBox();

        // 使用Adobe Acrobat兼容逻辑：如果CropBox超出MediaBox范围，则使用MediaBox
        iText.Kernel.Geom.Rectangle baseBox;
        if (cropBox != null && mediaBox != null)
        {
            // 检查CropBox是否超出MediaBox范围
            bool cropExceedsMedia = cropBox.GetLeft() < mediaBox.GetLeft() ||
                                  cropBox.GetBottom() < mediaBox.GetBottom() ||
                                  cropBox.GetRight() > mediaBox.GetRight() ||
                                  cropBox.GetTop() > mediaBox.GetTop();

            if (cropExceedsMedia)
            {
                baseBox = mediaBox;
                LogHelper.Debug("CropBox超出MediaBox范围，使用MediaBox作为容器基准");
            }
            else
            {
                baseBox = cropBox;
                LogHelper.Debug("使用CropBox作为容器基准");
            }
        }
        else
        {
            baseBox = cropBox ?? mediaBox;
            LogHelper.Debug("使用可用的页面框作为容器基准");
        }

        // 使用高精度浮点数避免精度损失
        container.ContainerWidth = (float)Math.Round(baseBox.GetWidth(), 6);
        container.ContainerHeight = (float)Math.Round(baseBox.GetHeight(), 6);
        container.BoundingBox = new iText.Kernel.Geom.Rectangle(0, 0, container.ContainerWidth, container.ContainerHeight);

        LogHelper.Debug($"虚拟容器创建完成: 尺寸={container.ContainerWidth:F6}x{container.ContainerHeight:F6}, 基准框={baseBox.GetWidth():F6}x{baseBox.GetHeight():F6}");

        return container;
    }
}

/// <summary>
/// 使用间接引用技术和1x1布局重排PDF页面
/// </summary>
/// <summary>
/// 统一坐标系统处理器
/// </summary>
public static class UnifiedCoordinateSystem
{
    /// <summary>
    /// 将任意页面坐标转换为统一坐标系统（基于首页尺寸）
    /// </summary>
    public static System.Drawing.PointF TransformToUnifiedCoordinates(
        float x, float y,
        PageTransformInfo transform,
        PdfCoordinateHelper.PageCoordinateInfo coordInfo)
    {
        try
        {
            // 第一步：将原始页面坐标转换为源页面的实际坐标
            float actualX = coordInfo.OriginX + x;
            float actualY = coordInfo.IsInvertedY ? 
                coordInfo.OriginY + coordInfo.UsableHeight - y : 
                coordInfo.OriginY + y;
            
            // 第二步：应用页面变换矩阵（缩放和偏移）
            float unifiedX = actualX * transform.ScaleX + transform.TranslateX;
            float unifiedY = actualY * transform.ScaleY + transform.TranslateY;
            
            return new System.Drawing.PointF(unifiedX, unifiedY);
        }
        catch (Exception ex)
        {
            LogHelper.Debug($"坐标转换失败: {ex.Message}");
            return new System.Drawing.PointF(x, y);
        }
    }
    
    /// <summary>
    /// 将统一坐标系统坐标转换回特定页面坐标
    /// </summary>
    public static System.Drawing.PointF TransformFromUnifiedCoordinates(
        float unifiedX, float unifiedY,
        PageTransformInfo transform,
        PdfCoordinateHelper.PageCoordinateInfo coordInfo)
    {
        try
        {
            // 反向应用页面变换矩阵
            float actualX = (unifiedX - transform.TranslateX) / transform.ScaleX;
            float actualY = (unifiedY - transform.TranslateY) / transform.ScaleY;
            
            // 转换回页面相对坐标
            float pageX = actualX - coordInfo.OriginX;
            float pageY = coordInfo.IsInvertedY ? 
                coordInfo.OriginY + coordInfo.UsableHeight - actualY :
                actualY - coordInfo.OriginY;
                
            return new System.Drawing.PointF(pageX, pageY);
        }
        catch (Exception ex)
        {
            LogHelper.Debug($"反向坐标转换失败: {ex.Message}");
            return new System.Drawing.PointF(unifiedX, unifiedY);
        }
    }
    
    /// <summary>
    /// 创建统一坐标系统的变换矩阵字典
    /// </summary>
    public static Dictionary<int, PageTransformInfo> CreateUnifiedTransformMap(
        VirtualPageContainer container, 
        iText.Kernel.Pdf.PdfDocument document)
    {
        var transformMap = new Dictionary<int, PageTransformInfo>();
        
        for (int i = 0; i < container.PageTransforms.Count && i < document.GetNumberOfPages(); i++)
        {
            var transform = container.PageTransforms[i];
            transformMap[transform.OriginalPageNumber] = transform;
        }
        
        return transformMap;
    }
}

/// <summary>
/// 高级页面重排管理器（集成坐标系统统一）
/// </summary>
public static class AdvancedPageReorganizer
{
    /// <summary>
    /// 执行完整的页面重排和坐标系统统一
    /// </summary>
    public static bool ExecuteAdvancedReorganization(string filePath, out Dictionary<int, PageTransformInfo> transformMap)
    {
        transformMap = null;
        
        LogHelper.Debug("=== 开始高级页面重排（间接引用 + 坐标统一）===");
        
        try
        {
            // 第一步：执行基础重排
            if (!ReorganizePagesWithIndirectReference(filePath))
            {
                LogHelper.Debug("基础重排失败，终止高级重排");
                return false;
            }
            
            // 第二步：分析重排后的坐标系统
            using (var reader = new iText.Kernel.Pdf.PdfReader(filePath))
            using (var document = new iText.Kernel.Pdf.PdfDocument(reader))
            {
                var virtualContainer = VirtualPageContainer.CreateFromFirstPage(document);
                
                // 重建变换信息
                for (int i = 1; i <= document.GetNumberOfPages(); i++)
                {
                    var page = document.GetPage(i);
                    var box = page.GetCropBox() ?? page.GetMediaBox();
                    
                    var transform = new PageTransformInfo
                    {
                        OriginalPageNumber = i,
                        OriginalWidth = box.GetWidth(),
                        OriginalHeight = box.GetHeight(),
                        SourceArea = box,
                        TargetArea = virtualContainer.BoundingBox,
                        ScaleX = 1.0f, // 重排后已经是统一尺寸
                        ScaleY = 1.0f,
                        TranslateX = 0,
                        TranslateY = 0
                    };
                    
                    virtualContainer.PageTransforms.Add(transform);
                }
                
                // 创建变换映射
                transformMap = UnifiedCoordinateSystem.CreateUnifiedTransformMap(virtualContainer, document);
                
                LogHelper.Debug($"高级重排完成，生成{transformMap?.Count ?? 0}个页面变换映射");
            }
            
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Debug($"高级重排失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 在统一坐标系统中定位元素
    /// </summary>
    public static System.Drawing.RectangleF LocateElementInUnifiedSystem(
        float elementX, float elementY, float elementWidth, float elementHeight,
        int sourcePageNumber,
        Dictionary<int, PageTransformInfo> transformMap,
        iText.Kernel.Pdf.PdfDocument document)
    {
        try
        {
            if (!transformMap.ContainsKey(sourcePageNumber))
            {
                LogHelper.Debug($"页面{sourcePageNumber}的变换信息不存在");
                return System.Drawing.RectangleF.Empty;
            }
            
            var transform = transformMap[sourcePageNumber];
            var page = document.GetPage(sourcePageNumber);
            var coordInfo = PdfCoordinateHelper.AnalyzePageCoordinateSystem(page);
            
            // 转换元素四个角点到统一坐标系
            var topLeft = UnifiedCoordinateSystem.TransformToUnifiedCoordinates(
                elementX, elementY + elementHeight, transform, coordInfo);
            var bottomRight = UnifiedCoordinateSystem.TransformToUnifiedCoordinates(
                elementX + elementWidth, elementY, transform, coordInfo);
            
            return new System.Drawing.RectangleF(
                Math.Min(topLeft.X, bottomRight.X),
                Math.Min(topLeft.Y, bottomRight.Y),
                Math.Abs(bottomRight.X - topLeft.X),
                Math.Abs(bottomRight.Y - topLeft.Y)
            );
        }
        catch (Exception ex)
        {
            LogHelper.Debug($"统一坐标系定位失败: {ex.Message}");
            return System.Drawing.RectangleF.Empty;
        }
    }
}

public static bool ReorganizePagesWithIndirectReference(string filePath)
{
    LogHelper.Debug("ReorganizePagesWithIndirectReference调用（间接引用技术版本），文件路径: " + filePath);
    try
    {
        // 检查文件是否存在
        if (!File.Exists(filePath))
        {
            LogHelper.Debug("文件不存在: " + filePath);
            return false;
        }

        // 检查文件扩展名是否为PDF
        if (!SystemPath.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 创建临时文件路径
        string tempFolder = SystemPath.Combine(SystemPath.GetTempPath(), "PDFToolCache");
        Directory.CreateDirectory(tempFolder);
        string tempFilePath = SystemPath.Combine(tempFolder, SystemPath.GetRandomFileName() + ".pdf");

        iText.Kernel.Pdf.PdfReader reader = null;
        iText.Kernel.Pdf.PdfWriter writer = null;
        iText.Kernel.Pdf.PdfDocument sourceDoc = null;
        iText.Kernel.Pdf.PdfDocument targetDoc = null;

        try
        {
            // 打开源文档
            reader = new iText.Kernel.Pdf.PdfReader(filePath);
            sourceDoc = new iText.Kernel.Pdf.PdfDocument(reader);
            
            // 创建目标文档
            writer = new iText.Kernel.Pdf.PdfWriter(tempFilePath);
            targetDoc = new iText.Kernel.Pdf.PdfDocument(writer);
            
            LogHelper.Debug("=== 开始间接引用技术1x1布局重排 ===");
            
            // 第一步：创建虚拟页面容器（基于首页尺寸）
            var virtualContainer = VirtualPageContainer.CreateFromFirstPage(sourceDoc);
            LogHelper.Debug($"虚拟容器尺寸: {virtualContainer.ContainerWidth:F6}x{virtualContainer.ContainerHeight:F6}");
            
            // 第二步：分析所有页面并生成变换信息（高精度计算）
            LogHelper.Debug("=== 分析页面并生成变换矩阵 ===");
            for (int i = 1; i <= sourceDoc.GetNumberOfPages(); i++)
            {
                var sourcePage = sourceDoc.GetPage(i);
                var mediaBox = sourcePage.GetMediaBox();
                var cropBox = sourcePage.GetCropBox();

                // 使用Adobe Acrobat兼容逻辑选择源页面框
                iText.Kernel.Geom.Rectangle sourceBox;
                if (cropBox != null && mediaBox != null)
                {
                    // 检查CropBox是否超出MediaBox范围
                    bool cropExceedsMedia = cropBox.GetLeft() < mediaBox.GetLeft() ||
                                          cropBox.GetBottom() < mediaBox.GetBottom() ||
                                          cropBox.GetRight() > mediaBox.GetRight() ||
                                          cropBox.GetTop() > mediaBox.GetTop();

                    sourceBox = cropExceedsMedia ? mediaBox : cropBox;
                }
                else
                {
                    sourceBox = cropBox ?? mediaBox;
                }

                // 获取页面旋转信息
                int rotation = sourcePage.GetRotation();
                LogHelper.Debug($"页面{i} 旋转角度: {rotation}°");

                // 使用高精度浮点数
                double sourceWidth = Math.Round(sourceBox.GetWidth(), 6);
                double sourceHeight = Math.Round(sourceBox.GetHeight(), 6);
                double containerWidth = Math.Round(virtualContainer.ContainerWidth, 6);
                double containerHeight = Math.Round(virtualContainer.ContainerHeight, 6);

                var transformInfo = new PageTransformInfo
                {
                    OriginalPageNumber = i,
                    OriginalWidth = (float)sourceWidth,
                    OriginalHeight = (float)sourceHeight,
                    Rotation = rotation, // 保存旋转信息
                    SourceArea = sourceBox,
                    TargetArea = virtualContainer.BoundingBox
                };

                // 计算缩放比例（保持宽高比的适配缩放）- 使用高精度计算
                double scaleX = containerWidth / sourceWidth;
                double scaleY = containerHeight / sourceHeight;
                double uniformScale = Math.Min(scaleX, scaleY);

                // 如果页面尺寸已经匹配容器尺寸，则避免缩放
                if (Math.Abs(uniformScale - 1.0) < 1e-6)
                {
                    uniformScale = 1.0;
                    LogHelper.Debug($"页面{i} 尺寸已匹配，跳过缩放");
                }

                transformInfo.ScaleX = (float)uniformScale;
                transformInfo.ScaleY = (float)uniformScale;

                // 计算居中偏移（考虑PDF坐标系：左下角为原点）
                double scaledWidth = sourceWidth * uniformScale;
                double scaledHeight = sourceHeight * uniformScale;
                double sourceLeft = Math.Round(sourceBox.GetLeft(), 6);
                double sourceBottom = Math.Round(sourceBox.GetBottom(), 6);

                double translateX = (containerWidth - scaledWidth) / 2 - sourceLeft * uniformScale;
                double translateY = (containerHeight - scaledHeight) / 2 - sourceBottom * uniformScale;

                transformInfo.TranslateX = (float)Math.Round(translateX, 6);
                transformInfo.TranslateY = (float)Math.Round(translateY, 6);

                // 记录变换信息用于调试（使用高精度显示）
                LogHelper.Debug($"页面{i} 变换详情:");
                LogHelper.Debug($"  原始尺寸: {sourceWidth:F6}x{sourceHeight:F6}");
                LogHelper.Debug($"  目标尺寸: {containerWidth:F6}x{containerHeight:F6}");
                LogHelper.Debug($"  缩放比例: {uniformScale:F6}");
                LogHelper.Debug($"  缩放后尺寸: {scaledWidth:F6}x{scaledHeight:F6}");
                LogHelper.Debug($"  原始位置: ({sourceLeft:F6}, {sourceBottom:F6})");
                LogHelper.Debug($"  居中偏移: ({translateX:F6}, {translateY:F6})");

                virtualContainer.PageTransforms.Add(transformInfo);

                LogHelper.Debug($"页面{i} 变换: 原始{transformInfo.OriginalWidth:F6}x{transformInfo.OriginalHeight:F6} -> " +
                               $"缩放{uniformScale:F6}, 偏移({transformInfo.TranslateX:F6}, {transformInfo.TranslateY:F6})");
            }
            
            // 第三步：使用间接引用创建重排后的页面
            LogHelper.Debug("=== 创建重排后的页面（间接引用技术） ===");
            for (int i = 0; i < virtualContainer.PageTransforms.Count; i++)
            {
                var transform = virtualContainer.PageTransforms[i];
                var sourcePage = sourceDoc.GetPage(transform.OriginalPageNumber);
                
                // 创建新页面，使用虚拟容器尺寸
                var pageSize = new iText.Kernel.Geom.PageSize(virtualContainer.BoundingBox);
                var newPage = targetDoc.AddNewPage(pageSize);

                // 使用高精度变换矩阵
                double scaleX = Math.Round(transform.ScaleX, 6);
                double scaleY = Math.Round(transform.ScaleY, 6);
                double translateX = Math.Round(transform.TranslateX, 6);
                double translateY = Math.Round(transform.TranslateY, 6);

                // 创建变换矩阵（使用高精度数值，转换为float）
                var matrix = new iText.Kernel.Geom.Matrix(
                    (float)scaleX, 0, 0,
                    (float)scaleY,
                    (float)translateX,
                    (float)translateY
                );

                // 使用PdfFormXObject作为间接引用容器
                var xobject = sourcePage.CopyAsFormXObject(targetDoc);

                // 创建画布并应用变换
                var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(newPage.NewContentStreamBefore(), newPage.GetResources(), targetDoc);
                canvas.SaveState();

                // 应用高精度变换矩阵
                var matrixArray = new iText.Kernel.Pdf.PdfArray();
                matrixArray.Add(new iText.Kernel.Pdf.PdfNumber(scaleX));
                matrixArray.Add(new iText.Kernel.Pdf.PdfNumber(0));
                matrixArray.Add(new iText.Kernel.Pdf.PdfNumber(0));
                matrixArray.Add(new iText.Kernel.Pdf.PdfNumber(scaleY));
                matrixArray.Add(new iText.Kernel.Pdf.PdfNumber(translateX));
                matrixArray.Add(new iText.Kernel.Pdf.PdfNumber(translateY));

                canvas.ConcatMatrix(matrixArray);

                LogHelper.Debug($"页面{i+1} 应用变换矩阵: scale=({scaleX:F6}, {scaleY:F6}), translate=({translateX:F6}, {translateY:F6})");

                // 绘制页面内容（通过间接引用）
                canvas.AddXObject(xobject);

                canvas.RestoreState();
                
                // 设置统一的页面框（使用高精度坐标，转换为float）
                var unifiedBox = new iText.Kernel.Geom.Rectangle(
                    (float)Math.Round(virtualContainer.BoundingBox.GetLeft(), 6),
                    (float)Math.Round(virtualContainer.BoundingBox.GetBottom(), 6),
                    (float)Math.Round(virtualContainer.BoundingBox.GetWidth(), 6),
                    (float)Math.Round(virtualContainer.BoundingBox.GetHeight(), 6)
                );

                newPage.SetMediaBox(unifiedBox);
                newPage.SetCropBox(unifiedBox);
                newPage.SetTrimBox(unifiedBox);
                newPage.SetBleedBox(unifiedBox);
                newPage.SetArtBox(unifiedBox);

                // 恢复原始页面的旋转信息
                if (transform.Rotation != 0)
                {
                    newPage.SetRotation(transform.Rotation);
                    LogHelper.Debug($"页面{i+1} 恢复旋转角度: {transform.Rotation}°");
                }

                LogHelper.Debug($"页面{i+1} 统一页面框设置: {unifiedBox.GetWidth():F6}x{unifiedBox.GetHeight():F6}");

                LogHelper.Debug($"页面{i + 1} 重排完成，使用间接引用技术，保持旋转{transform.Rotation}°");
            }
            
            LogHelper.Debug("=== 间接引用技术1x1布局重排完成 ===");
        }
        finally
        {
            // 确保资源被正确释放
            targetDoc?.Close();
            sourceDoc?.Close();
            writer?.Close();
            reader?.Close();
        }

        // 删除原始文件并将临时文件重命名为原始文件名
        File.Delete(filePath);
        File.Move(tempFilePath, filePath);

        return true;
    }
    catch (Exception ex)
    {
        LogHelper.Debug($"间接引用技术重排失败: {ex.Message}");
        LogHelper.Debug($"异常类型: {ex.GetType().FullName}");
        if (ex.InnerException != null)
        {
            LogHelper.Debug($"内部异常: {ex.InnerException.Message}");
        }
        return false;
    }
}

        public static bool SetAllPageBoxesToCropBox(string filePath)
        {
            LogHelper.Debug("SetAllPageBoxesToCropBox调用（iText7版本），文件路径: " + filePath);
            try
            {
                // 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    LogHelper.Debug("文件不存在: " + filePath);
                    return false;
                }

                // 检查文件扩展名是否为PDF
                if (!SystemPath.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // 创建临时文件路径
                // 使用系统临时文件夹中的PDFToolCache目录创建临时文件路径
                string tempFolder = SystemPath.Combine(SystemPath.GetTempPath(), "PDFToolCache");
                Directory.CreateDirectory(tempFolder); // 确保目录存在
                string tempFilePath = SystemPath.Combine(tempFolder, SystemPath.GetRandomFileName() + ".pdf");

                // 使用iText7处理文件
                iText.Kernel.Pdf.PdfReader reader = null;
                iText.Kernel.Pdf.PdfWriter writer = null;
                iText.Kernel.Pdf.PdfDocument document = null;

                try
                {
                    reader = new iText.Kernel.Pdf.PdfReader(filePath);
                    writer = new iText.Kernel.Pdf.PdfWriter(tempFilePath);
                    document = new iText.Kernel.Pdf.PdfDocument(reader, writer);

                // 首先分析所有页面，确定统一的基准框尺寸
                iText.Kernel.Geom.Rectangle unifiedBaseBox = null;
                var pageSizes = new List<(int page, iText.Kernel.Geom.Rectangle mediaBox, iText.Kernel.Geom.Rectangle cropBox)>();

                LogHelper.Debug("=== 第一阶段：分析所有页面尺寸 ===");
                for (int i = 1; i <= document.GetNumberOfPages(); i++)
                {
                    iText.Kernel.Pdf.PdfPage page = document.GetPage(i);
                    iText.Kernel.Geom.Rectangle mediaBox = page.GetMediaBox();
                    iText.Kernel.Geom.Rectangle cropBox = page.GetCropBox();

                    pageSizes.Add((i, mediaBox, cropBox));

                    LogHelper.Debug($"页面{i} - MediaBox: {mediaBox?.GetWidth() ?? -1}x{mediaBox?.GetHeight() ?? -1}, CropBox: {cropBox?.GetWidth() ?? -1}x{cropBox?.GetHeight() ?? -1}");
                }

                // 确定统一的基准框：优先使用最常见的有效CropBox，否则使用最常见的MediaBox
                var cropBoxGroups = pageSizes.Where(p => p.cropBox != null && p.cropBox.GetWidth() > 0 && p.cropBox.GetHeight() > 0)
                                            .GroupBy(p => $"{p.cropBox.GetWidth():F3}x{p.cropBox.GetHeight():F3}")
                                            .OrderByDescending(g => g.Count())
                                            .FirstOrDefault();

                if (cropBoxGroups != null)
                {
                    unifiedBaseBox = cropBoxGroups.First().cropBox;
                    LogHelper.Debug($"采用最常见的CropBox作为统一基准: {unifiedBaseBox.GetWidth():F3}x{unifiedBaseBox.GetHeight():F3} (出现{cropBoxGroups.Count()}次)");
                }
                else
                {
                    var mediaBoxGroups = pageSizes.Where(p => p.mediaBox != null && p.mediaBox.GetWidth() > 0 && p.mediaBox.GetHeight() > 0)
                                                .GroupBy(p => $"{p.mediaBox.GetWidth():F3}x{p.mediaBox.GetHeight():F3}")
                                                .OrderByDescending(g => g.Count())
                                                .FirstOrDefault();

                    if (mediaBoxGroups != null)
                    {
                        unifiedBaseBox = mediaBoxGroups.First().mediaBox;
                        LogHelper.Debug($"采用最常见的MediaBox作为统一基准: {unifiedBaseBox.GetWidth():F3}x{unifiedBaseBox.GetHeight():F3} (出现{mediaBoxGroups.Count()}次)");
                    }
                }

                if (unifiedBaseBox == null)
                {
                    LogHelper.Debug("无法确定统一的基准框，终止处理");
                    return false;
                }

                // 第二阶段：将所有页面的页面框设置为统一尺寸
                LogHelper.Debug("=== 第二阶段：应用统一基准框到所有页面 ===");
                for (int i = 1; i <= document.GetNumberOfPages(); i++)
                {
                    iText.Kernel.Pdf.PdfPage page = document.GetPage(i);
                    LogHelper.Debug("=== 开始处理页面，索引: " + (i - 1) + ", 页码: " + i + " ===");

                    // 获取各种页面框
                    iText.Kernel.Geom.Rectangle mediaBox = page.GetMediaBox();
                    iText.Kernel.Geom.Rectangle cropBox = page.GetCropBox();
                    iText.Kernel.Geom.Rectangle trimBox = page.GetTrimBox();
                    iText.Kernel.Geom.Rectangle bleedBox = page.GetBleedBox();
                    iText.Kernel.Geom.Rectangle artBox = page.GetArtBox();

                    // 记录原始页面框尺寸
                    LogHelper.Debug("原始页面框信息：");
                    LogHelper.Debug("- MediaBox: " + (mediaBox?.GetWidth() ?? -1) + "x" + (mediaBox?.GetHeight() ?? -1));
                    LogHelper.Debug("- CropBox: " + (cropBox?.GetWidth() ?? -1) + "x" + (cropBox?.GetHeight() ?? -1));
                    LogHelper.Debug("- TrimBox: " + (trimBox?.GetWidth() ?? -1) + "x" + (trimBox?.GetHeight() ?? -1));
                    LogHelper.Debug("- BleedBox: " + (bleedBox?.GetWidth() ?? -1) + "x" + (bleedBox?.GetHeight() ?? -1));
                    LogHelper.Debug("- ArtBox: " + (artBox?.GetWidth() ?? -1) + "x" + (artBox?.GetHeight() ?? -1));

                    LogHelper.Debug($"使用统一基准框: {unifiedBaseBox.GetWidth():F3}x{unifiedBaseBox.GetHeight():F3}");

                    // 将所有页面框设置为统一基准框 - 保持Adobe Acrobat兼容性
                    page.SetMediaBox(unifiedBaseBox);
                    page.SetCropBox(unifiedBaseBox);
                    page.SetTrimBox(unifiedBaseBox);
                    page.SetBleedBox(unifiedBaseBox);
                    page.SetArtBox(unifiedBaseBox);

                        // 记录设置后的页面框尺寸
                        LogHelper.Debug("设置后的页面框信息：");
                        LogHelper.Debug("- MediaBox: " + page.GetMediaBox().GetWidth() + "x" + page.GetMediaBox().GetHeight());
                        LogHelper.Debug("- CropBox: " + page.GetCropBox().GetWidth() + "x" + page.GetCropBox().GetHeight());
                        LogHelper.Debug("- TrimBox: " + page.GetTrimBox().GetWidth() + "x" + page.GetTrimBox().GetHeight());
                        LogHelper.Debug("- BleedBox: " + page.GetBleedBox().GetWidth() + "x" + page.GetBleedBox().GetHeight());
                        LogHelper.Debug("- ArtBox: " + page.GetArtBox().GetWidth() + "x" + page.GetArtBox().GetHeight());
                        LogHelper.Debug("=== 页面处理完成，索引: " + (i - 1) + " ===");
                    }

                    LogHelper.Debug("保存修改后的文档到临时文件：" + tempFilePath);
                }
                finally
                {
                    // 确保资源被正确释放
                    document?.Close();
                    writer?.Close();
                    reader?.Close();
                }

                // 删除原始文件并将临时文件重命名为原始文件名
                File.Delete(filePath);
                File.Move(tempFilePath, filePath);

                return true;
            }
            catch (iText.Kernel.Exceptions.PdfException pdfEx)
            {
                LogHelper.Debug("=== iText7 PDF异常处理开始 ===");
                LogHelper.Debug("PDF异常类型: " + pdfEx.GetType().FullName);
                LogHelper.Debug("PDF异常消息: " + pdfEx.Message);
                LogHelper.Debug("处理文件: " + filePath);

                // 提供更具体的PDF异常处理建议
                if (pdfEx.Message.Contains("password") || pdfEx.Message.Contains("encrypted"))
                {
                    LogHelper.Debug("建议: PDF文件可能受密码保护，请检查文件权限");
                }
                else if (pdfEx.Message.Contains("corrupt") || pdfEx.Message.Contains("damaged"))
                {
                    LogHelper.Debug("建议: PDF文件可能已损坏，请尝试修复或重新生成文件");
                }
                else if (pdfEx.Message.Contains("header"))
                {
                    LogHelper.Debug("建议: PDF文件头可能无效，请确认文件完整性");
                }

                if (pdfEx.InnerException != null)
                {
                    LogHelper.Debug("内部异常: " + pdfEx.InnerException.Message);
                }
                LogHelper.Debug("=== iText7 PDF异常处理结束 ===");
                return false;
            }
            catch (UnauthorizedAccessException accessEx)
            {
                LogHelper.Debug("=== 文件访问权限异常 ===");
                LogHelper.Debug("权限异常: " + accessEx.Message);
                LogHelper.Debug("建议: 请检查文件权限，确保有读写权限");
                LogHelper.Debug("处理文件: " + filePath);
                LogHelper.Debug("=== 文件访问权限异常结束 ===");
                return false;
            }
            catch (IOException ioEx)
            {
                LogHelper.Debug("=== IO异常处理开始 ===");
                LogHelper.Debug("IO异常: " + ioEx.Message);
                LogHelper.Debug("建议: 文件可能被占用或磁盘空间不足");
                LogHelper.Debug("处理文件: " + filePath);
                LogHelper.Debug("=== IO异常处理结束 ===");
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.Debug("=== PDF页面框处理异常开始（iText7版本） ===");
                LogHelper.Debug("异常类型: " + ex.GetType().FullName);
                LogHelper.Debug("异常消息: " + ex.Message);
                LogHelper.Debug("异常堆栈: " + ex.StackTrace);
                LogHelper.Debug("处理文件: " + filePath);
                if (ex.InnerException != null)
                {
                    LogHelper.Debug("内部异常类型: " + ex.InnerException.GetType().FullName);
                    LogHelper.Debug("内部异常消息: " + ex.InnerException.Message);
                }
                LogHelper.Debug("=== PDF页面框处理异常结束 ===");
                return false;
            }
        }

        /// <summary>
        /// 计算最终尺寸（毫米）
        /// </summary>
        /// <param name="width">原始宽度（毫米）</param>
        /// <param name="height">原始高度（毫米）</param>
        /// <param name="tetBleed">出血值（毫米）</param>
        /// <param name="cornerRadius">圆角半径</param>
        /// <param name="addPdfLayers">是否添加PDF图层</param>
        /// <returns>最终尺寸字符串</returns>
        public static string CalculateFinalDimensions(double width, double height, double tetBleed, string cornerRadius = "0", bool addPdfLayers = false)
        {
            // 应用公式: 长-tetBleed*2, 宽-tetBleed*2
            // 修正出血值计算逻辑：确保原始尺寸为PDF实际尺寸，仅减去一次双边出血值
            double finalWidth = CustomRound(width - tetBleed * 2);
            double finalHeight = CustomRound(height - tetBleed * 2);
            
            // 基础尺寸格式
            string dimensions = $"{finalWidth}x{finalHeight}";
            
            // 当addPdfLayers处于勾选状态时，根据cornerRadius添加形状信息
            if (addPdfLayers && !string.IsNullOrEmpty(cornerRadius))
            {
                string trimmedRadius = cornerRadius.Trim();
                
                // 直接从AppSettings读取形状代号配置
                var zeroShapeCode = AppSettings.Get("ZeroShapeCode") as string ?? "Z";
                var roundShapeCode = AppSettings.Get("RoundShapeCode") as string ?? "R";
                var ellipseShapeCode = AppSettings.Get("EllipseShapeCode") as string ?? "Y";
                var circleShapeCode = AppSettings.Get("CircleShapeCode") as string ?? "C";
                var hideRadiusValue = AppSettings.Get("HideRadiusValue") as bool? ?? false;
                
                // 根据形状类型添加相应代号
                if (trimmedRadius.Equals("R", StringComparison.OrdinalIgnoreCase))
                {
                    dimensions += circleShapeCode; // 使用圆形代号
                }
                else if (trimmedRadius.Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    dimensions += ellipseShapeCode; // 使用异形代号
                }
                else if (int.TryParse(trimmedRadius, out int numRadius) && numRadius > 0)
                {
                    // 对于数字半径值，添加圆角代号
                    if (hideRadiusValue)
                    {
                        // 如果隐藏半径数值复选框被勾选，只添加代号不添加数字
                        dimensions += roundShapeCode;
                    }
                    else
                    {
                        // 否则添加代号和数字
                        dimensions += roundShapeCode + numRadius;
                    }
                }
                else if (trimmedRadius.Equals("0"))
                {
                    // 当形状输入为"0"时，添加直角代号
                    dimensions += zeroShapeCode;
                }
            }
            
            return dimensions;
        }
        
        /// <summary>
        /// 从字符串中提取数值部分（支持小数）
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>提取的数值字符串</returns>
        private static string ExtractNumericValue(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "0";
                
            string result = string.Empty;
            bool hasDecimalPoint = false;
            
            foreach (char c in input)
            {
                if (char.IsDigit(c))
                {
                    result += c;
                }
                else if (c == '.' && !hasDecimalPoint)
                {
                    result += c;
                    hasDecimalPoint = true;
                }
                else if (!string.IsNullOrEmpty(result))
                {
                    // 一旦遇到非数字字符且result已经有值，则停止提取
                    // 这样可以避免将形状代号（如R3）的数字部分混入
                    break;
                }
            }
            
            return string.IsNullOrEmpty(result) ? "0" : result;
        }

        /// <summary>
        /// 自定义四舍五入方法
        /// </summary>
        /// <param name="value">要四舍五入的值</param>
        /// <returns>四舍五入后的值</returns>
        private static double CustomRound(double value)
        {
            // 正常四舍五入到十分位
            return Math.Round(value, 1);
        }

        /// <summary>
        /// 插入标识页或空白页（扩展版本，支持多页和不同位置）
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="textContent">标识页文字内容，空字符串表示空白页</param>
        /// <param name="fontSize">字体大小</param>
        /// <param name="insertPosition">插入位置：0=第一页之前，-1=最后一页之后，其他=指定页码之后</param>
        /// <param name="pageCount">插入页数，默认为1页</param>
        /// <returns>是否成功插入</returns>
        public static bool InsertIdentifierPage(string filePath, string textContent, float fontSize = 12f, int insertPosition = 0, int pageCount = 1)
        {
            // 定义页面类型，确保在try-catch块中都能访问
            string pageType = string.IsNullOrEmpty(textContent) ? "空白页" : "标识页";
            string positionDesc = GetInsertPositionDescription(insertPosition);

            try
            {
                if (!File.Exists(filePath))
                {
                    LogHelper.Error($"文件不存在: {filePath}");
                    return false;
                }

                if (!SystemPath.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    LogHelper.Error($"不是PDF文件: {filePath}");
                    return false;
                }

                if (pageCount <= 0)
                {
                    LogHelper.Debug($"页面数量无效: {pageCount}，跳过插入");
                    return true;
                }

                LogHelper.Debug($"开始插入{pageCount}个{pageType}到{positionDesc}（iTextSharp.LGPLv2版本）");

                // 使用 iTextSharp.LGPLv2.Core 实现标识页插入（支持 TTC 字体）
                return ITextSharpLGPLPdfTools.InsertIdentifierPage(filePath, textContent, fontSize, insertPosition, pageCount);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"插入{pageType}失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 使用 PDFsharp 插入标识页（已弃用，改用 iTextSharp.LGPLv2.Core）
        /// </summary>
        [Obsolete("已弃用，请使用 ITextSharpLGPLPdfTools.InsertIdentifierPage")]
        private static bool InsertIdentifierPageWithPdfSharp(string filePath, string textContent, float fontSize, int insertPosition, int pageCount)
        {
            string pageType = string.IsNullOrEmpty(textContent) ? "空白页" : "标识页";
            string tempFile = SystemPath.Combine(SystemPath.GetTempPath(), $"temp_identifier_{Guid.NewGuid()}.pdf");

            try
            {
                // 打开现有PDF文档
                using (var document = PdfSharp.Pdf.IO.PdfReader.Open(filePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify))
                {
                    if (document.PageCount == 0)
                    {
                        LogHelper.Error("文档没有页面");
                        return false;
                    }

                    // 获取参考页面尺寸
                    int refPageIndex = GetReferencePageIndex(document.PageCount, insertPosition);
                    var referencePage = document.Pages[refPageIndex];
                    double pageWidth = referencePage.Width.Point;
                    double pageHeight = referencePage.Height.Point;
                    int rotation = referencePage.Rotate;

                    LogHelper.Debug($"参考页面尺寸: {pageWidth}x{pageHeight} 点, 旋转: {rotation}°");

                    // 获取字体
                    PdfSharp.Drawing.XFont font = CreatePdfSharpFont(fontSize);
                    LogHelper.Debug($"使用字体: {font.FontFamily.Name}");

                    // 批量插入页面
                    for (int i = 0; i < pageCount; i++)
                    {
                        // 计算插入位置
                        int actualInsertIndex = CalculatePdfSharpInsertIndex(document.PageCount, insertPosition, i);

                        // 创建新页面
                        var newPage = document.InsertPage(actualInsertIndex);
                        newPage.Width = PdfSharp.Drawing.XUnit.FromPoint(pageWidth);
                        newPage.Height = PdfSharp.Drawing.XUnit.FromPoint(pageHeight);
                        newPage.Rotate = rotation;

                        // 只有第一页才添加文字内容
                        if (!string.IsNullOrEmpty(textContent) && i == 0)
                        {
                            DrawCenteredTextPdfSharp(newPage, textContent, font, pageWidth, pageHeight);
                            LogHelper.Debug($"第{i + 1}页已添加文字内容: {textContent}");
                        }
                        else
                        {
                            LogHelper.Debug($"第{i + 1}页为空白{pageType}");
                        }
                    }

                    // 保存到临时文件
                    document.Save(tempFile);
                    LogHelper.Debug($"成功插入{pageCount}个{pageType}");
                }

                // 替换原文件
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                if (File.Exists(tempFile) && new FileInfo(tempFile).Length > 0)
                {
                    File.Move(tempFile, filePath);
                    LogHelper.Debug("PDFsharp标识页插入成功完成");
                    return true;
                }
                else
                {
                    LogHelper.Error("生成的PDF文件不存在或为空");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"PDFsharp插入标识页失败: {ex.Message}", ex);
                
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
        /// 获取参考页面索引（PDFsharp版本）
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
        /// 计算PDFsharp插入索引
        /// </summary>
        private static int CalculatePdfSharpInsertIndex(int currentPageCount, int insertPosition, int currentIndex)
        {
            return insertPosition switch
            {
                0 => currentIndex,  // 在开头插入
                -1 => currentPageCount,  // 在末尾插入
                > 0 => insertPosition + currentIndex,  // 在指定位置之后插入
                _ => currentIndex
            };
        }

        /// <summary>
        /// 创建 PDFsharp 字体（直接使用用户选择的字体，无回退）
        /// </summary>
        private static PdfSharp.Drawing.XFont CreatePdfSharpFont(float fontSize)
        {
            try
            {
                // 从AppSettings读取用户选择的字体
                string selectedFont = AppSettings.GetValue<string>("IdentifierPageFont") ?? "Microsoft YaHei";
                LogHelper.Debug($"使用字体: {selectedFont}");

                // 直接创建字体，不做回退
                var font = new PdfSharp.Drawing.XFont(selectedFont, fontSize, PdfSharp.Drawing.XFontStyleEx.Regular);
                LogHelper.Debug($"✓ 成功创建字体: {selectedFont}");
                return font;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"创建PDFsharp字体失败: {ex.Message}", ex);
                throw new Exception($"无法创建字体，请在设置中选择兼容的字体。错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 使用 PDFsharp 绘制居中文字
        /// </summary>
        private static void DrawCenteredTextPdfSharp(PdfSharp.Pdf.PdfPage page, string text, PdfSharp.Drawing.XFont font, double pageWidth, double pageHeight)
        {
            try
            {
                using (var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page))
                {
                    // 绘制白色背景
                    gfx.DrawRectangle(PdfSharp.Drawing.XBrushes.White, 0, 0, pageWidth, pageHeight);

                    // 设置文字格式
                    var format = new PdfSharp.Drawing.XStringFormat
                    {
                        Alignment = PdfSharp.Drawing.XStringAlignment.Center,
                        LineAlignment = PdfSharp.Drawing.XLineAlignment.Center
                    };

                    // 计算页边距和可用区域
                    double margin = 20;
                    double maxTextWidth = pageWidth - (margin * 2);
                    double lineHeight = font.Height * 1.2;

                    // 处理文本换行
                    var lines = WrapTextPdfSharp(gfx, text, font, maxTextWidth);

                    // 计算总文本高度
                    double totalTextHeight = lines.Count * lineHeight;

                    // 计算起始Y坐标（垂直居中）
                    double startY = (pageHeight - totalTextHeight) / 2 + font.Height / 2;

                    // 绘制每一行
                    for (int i = 0; i < lines.Count; i++)
                    {
                        double y = startY + (i * lineHeight);
                        gfx.DrawString(lines[i], font, PdfSharp.Drawing.XBrushes.Black, 
                            new PdfSharp.Drawing.XPoint(pageWidth / 2, y), format);
                    }

                    LogHelper.Debug($"PDFsharp绘制文字完成，共{lines.Count}行");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"PDFsharp绘制文字失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// PDFsharp 文本换行处理
        /// </summary>
        private static List<string> WrapTextPdfSharp(PdfSharp.Drawing.XGraphics gfx, string text, PdfSharp.Drawing.XFont font, double maxWidth)
        {
            var lines = new List<string>();
            
            if (string.IsNullOrEmpty(text))
                return lines;

            // 先按换行符分割
            string[] paragraphs = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

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
                    var size = gfx.MeasureString(remaining, font);
                    
                    if (size.Width <= maxWidth)
                    {
                        lines.Add(remaining);
                        break;
                    }

                    // 需要换行，找到合适的断点
                    int breakIndex = FindBreakIndex(gfx, remaining, font, maxWidth);
                    
                    if (breakIndex <= 0)
                    {
                        // 无法找到断点，强制在第一个字符后断开
                        breakIndex = 1;
                    }

                    lines.Add(remaining.Substring(0, breakIndex));
                    remaining = remaining.Substring(breakIndex).TrimStart();
                }
            }

            return lines;
        }

        /// <summary>
        /// 查找文本换行断点
        /// </summary>
        private static int FindBreakIndex(PdfSharp.Drawing.XGraphics gfx, string text, PdfSharp.Drawing.XFont font, double maxWidth)
        {
            // 二分查找合适的断点
            int low = 1;
            int high = text.Length;
            int result = 1;

            while (low <= high)
            {
                int mid = (low + high) / 2;
                string testStr = text.Substring(0, mid);
                var size = gfx.MeasureString(testStr, font);

                if (size.Width <= maxWidth)
                {
                    result = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return result;
        }

        /// <summary>
        /// 获取插入位置的描述
        /// </summary>
        /// <param name="insertPosition">插入位置</param>
        /// <returns>位置描述</returns>
        private static string GetInsertPositionDescription(int insertPosition)
        {
            return insertPosition switch
            {
                0 => "文档开头",
                -1 => "文档末尾",
                > 0 => $"第{insertPosition}页之后",
                _ => "默认位置"
            };
        }

        /// <summary>
        /// 获取参考页面用于复制尺寸和属性
        /// </summary>
        /// <param name="pdfDoc">PDF文档</param>
        /// <param name="insertPosition">插入位置</param>
        /// <returns>参考页面</returns>
        private static iText.Kernel.Pdf.PdfPage GetReferencePage(iText.Kernel.Pdf.PdfDocument pdfDoc, int insertPosition)
        {
            int totalPages = pdfDoc.GetNumberOfPages();

            return insertPosition switch
            {
                0 => pdfDoc.GetPage(1),  // 第一页之前插入，参考第一页
                -1 => pdfDoc.GetPage(totalPages),  // 最后插入，参考最后一页
                > 0 when insertPosition <= totalPages => pdfDoc.GetPage(insertPosition),
                _ => pdfDoc.GetPage(1)  // 默认参考第一页
            };
        }

        /// <summary>
        /// 计算实际插入位置
        /// </summary>
        /// <param name="pdfDoc">PDF文档</param>
        /// <param name="insertPosition">插入位置</param>
        /// <param name="currentIndex">当前插入的页面索引（从0开始）</param>
        /// <returns>实际插入位置</returns>
        private static int CalculateActualInsertPosition(iText.Kernel.Pdf.PdfDocument pdfDoc, int insertPosition, int currentIndex)
        {
            int totalPages = pdfDoc.GetNumberOfPages();

            return insertPosition switch
            {
                0 => 1 + currentIndex,  // 第一页之前，每次插入后位置+1
                -1 => totalPages + 1,   // 文档末尾，总是在最后
                > 0 when insertPosition <= totalPages => insertPosition + 1 + currentIndex,  // 指定页之后
                _ => 1 + currentIndex   // 默认在开头
            };
        }

        /// <summary>
        /// 复制页面框属性
        /// </summary>
        /// <param name="sourcePage">源页面</param>
        /// <param name="targetPage">目标页面</param>
        private static void CopyPageBoxProperties(iText.Kernel.Pdf.PdfPage sourcePage, iText.Kernel.Pdf.PdfPage targetPage)
        {
            // 复制所有页面框属性
            var originalMediaBox = sourcePage.GetMediaBox();
            var originalCropBox = sourcePage.GetCropBox();
            var originalTrimBox = sourcePage.GetTrimBox();
            var originalBleedBox = sourcePage.GetBleedBox();

            if (originalMediaBox != null)
            {
                var mediaBoxCopy = new iText.Kernel.Geom.Rectangle(
                    originalMediaBox.GetLeft(),
                    originalMediaBox.GetBottom(),
                    originalMediaBox.GetRight(),
                    originalMediaBox.GetTop()
                );
                targetPage.SetMediaBox(mediaBoxCopy);
            }

            if (originalCropBox != null)
            {
                var cropBoxCopy = new iText.Kernel.Geom.Rectangle(
                    originalCropBox.GetLeft(),
                    originalCropBox.GetBottom(),
                    originalCropBox.GetRight(),
                    originalCropBox.GetTop()
                );
                targetPage.SetCropBox(cropBoxCopy);
            }
            else if (originalMediaBox != null)
            {
                // 如果没有CropBox，使用MediaBox作为CropBox
                var mediaBoxCopy = new iText.Kernel.Geom.Rectangle(
                    originalMediaBox.GetLeft(),
                    originalMediaBox.GetBottom(),
                    originalMediaBox.GetRight(),
                    originalMediaBox.GetTop()
                );
                targetPage.SetCropBox(mediaBoxCopy);
            }

            if (originalTrimBox != null)
            {
                var trimBoxCopy = new iText.Kernel.Geom.Rectangle(
                    originalTrimBox.GetLeft(),
                    originalTrimBox.GetBottom(),
                    originalTrimBox.GetRight(),
                    originalTrimBox.GetTop()
                );
                targetPage.SetTrimBox(trimBoxCopy);
            }

            if (originalBleedBox != null)
            {
                var bleedBoxCopy = new iText.Kernel.Geom.Rectangle(
                    originalBleedBox.GetLeft(),
                    originalBleedBox.GetBottom(),
                    originalBleedBox.GetRight(),
                    originalBleedBox.GetTop()
                );
                targetPage.SetBleedBox(bleedBoxCopy);
            }
        }

        /// <summary>
        /// 便捷方法：插入空白页
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="pageCount">要插入的空白页数量</param>
        /// <param name="insertPosition">插入位置：0=第一页之前，-1=最后一页之后，其他=指定页码之后</param>
        /// <returns>是否成功插入</returns>
        public static bool InsertBlankPages(string filePath, int pageCount, int insertPosition = 0)
        {
            return InsertIdentifierPage(filePath, "", 12f, insertPosition, pageCount);
        }

        /// <summary>
        /// 便捷方法：在文档末尾插入空白页
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="pageCount">要插入的空白页数量</param>
        /// <returns>是否成功插入</returns>
        public static bool AppendBlankPages(string filePath, int pageCount)
        {
            return InsertBlankPages(filePath, pageCount, -1);
        }

        /// <summary>
        /// 便捷方法：在文档开头插入空白页
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="pageCount">要插入的空白页数量</param>
        /// <returns>是否成功插入</returns>
        public static bool PrependBlankPages(string filePath, int pageCount)
        {
            return InsertBlankPages(filePath, pageCount, 0);
        }

        /// <summary>
        /// 旋转PDF文档中所有页面（根据布局计算结果）
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="rotationAngle">旋转角度（0°、0°等）</param>
        /// <returns>是否成功</returns>
        public static bool RotateAllPages(string filePath, int rotationAngle)
        {
            // 如果旋转角度为0，不需要旋转
            if (rotationAngle == 0)
            {
                LogHelper.Debug($"旋转角度为0°，无需旋转，跳过处理");
                return true;
            }

            // 规范化旋转角度到 0-360 范围
            rotationAngle = ((rotationAngle % 360) + 360) % 360;

            LogHelper.Debug($"=== 开始旋转所有页面：{rotationAngle}° ===");
            LogHelper.Debug($"PDF文件路径: {filePath}");

            string tempFile = SystemPath.Combine(SystemPath.GetTempPath(), $"temp_rotate_{Guid.NewGuid()}.pdf");

            try
            {
                if (!File.Exists(filePath))
                {
                    LogHelper.Error($"文件不存在: {filePath}");
                    return false;
                }

                using (var reader = new iText.Kernel.Pdf.PdfReader(filePath))
                using (var writer = new iText.Kernel.Pdf.PdfWriter(tempFile))
                using (var document = new iText.Kernel.Pdf.PdfDocument(reader, writer))
                {
                    int pageCount = document.GetNumberOfPages();
                    LogHelper.Debug($"文档总页数: {pageCount}");

                    // 旋转所有页面
                    for (int i = 1; i <= pageCount; i++)
                    {
                        iText.Kernel.Pdf.PdfPage page = document.GetPage(i);
                        int currentRotation = page.GetRotation();
                        
                        // 计算新的旋转角度（叠加现有旋转）
                        int newRotation = (currentRotation + rotationAngle) % 360;
                        
                        page.SetRotation(newRotation);
                        
                        if (i == 1 || i == pageCount)
                        {
                            LogHelper.Debug($"页面{i}: 原始旋转={currentRotation}°, 新旋转={newRotation}°");
                        }
                    }

                    LogHelper.Debug($"✓ 所有页面旋转完成：{rotationAngle}°");
                }

                // 强制垃圾回收，释放文件句柄
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();  // 二次回收
                System.Threading.Thread.Sleep(300);  // 增加等待时间

                // 替换原文件（带重试）
                if (File.Exists(filePath))
                {
                    bool deleteSuccess = false;
                    int retryCount = 0;
                    const int maxRetries = 5;  // 增加重试次数

                    while (retryCount < maxRetries && !deleteSuccess)
                    {
                        try
                        {
                            // 再次强制垃圾回收
                            if (retryCount > 0)
                            {
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
                            
                            File.Delete(filePath);
                            deleteSuccess = true;
                            LogHelper.Debug("原文件删除成功");
                        }
                        catch (Exception deleteEx)
                        {
                            retryCount++;
                            if (retryCount < maxRetries)
                            {
                                LogHelper.Debug($"原文件删除失败（第{retryCount}次），等待后重试: {deleteEx.Message}");
                                System.Threading.Thread.Sleep(500);  // 增加等待时间到500ms
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                }

                if (File.Exists(tempFile) && new FileInfo(tempFile).Length > 0)
                {
                    File.Move(tempFile, filePath);
                    LogHelper.Debug("页面旋转成功完成");
                    return true;
                }
                else
                {
                    LogHelper.Error("生成的PDF文件不存在或为空");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"旋转页面失败: {ex.Message}", ex);
                
                // 异常处理中清理临时文件
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                        LogHelper.Debug($"已清理临时文件: {tempFile}");
                    }
                }
                catch (Exception cleanEx)
                {
                    LogHelper.Debug($"清理临时文件失败: {cleanEx.Message}");
                }

                return false;
            }
        }

        /// <summary>
        /// 创建支持中文的字体（iText7版本）- 直接使用系统字体文件
        /// </summary>
        private static iText.Kernel.Font.PdfFont CreateChineseFontiText7(float fontSize)
        {
            try
            {
                // 从AppSettings读取用户选择的字体（现在保存的是系统字体名，如 "Microsoft YaHei"）
                string selectedFont = AppSettings.GetValue<string>("IdentifierPageFont") ?? "Microsoft YaHei";
                LogHelper.Debug($"尝试加载用户选择的字体: {selectedFont}");

                // 尝试从系统字体目录加载字体
                string fontPathWithIndex = GetSystemFontPath(selectedFont);
                if (!string.IsNullOrEmpty(fontPathWithIndex))
                {
                    var font = LoadFontFromPath(fontPathWithIndex);
                    if (font != null)
                    {
                        LogHelper.Debug($"✓ 成功加载系统字体: {selectedFont} ({fontPathWithIndex})");
                        return font;
                    }
                }

                // 备用方案：尝试常用中文字体
                string[] fallbackFonts = { 
                    "C:\\Windows\\Fonts\\simhei.ttf",      // 黑体（单字体文件，优先）
                    "C:\\Windows\\Fonts\\simkai.ttf",      // 楷体
                    "C:\\Windows\\Fonts\\simfang.ttf",     // 仿宋
                    "C:\\Windows\\Fonts\\msyh.ttc,0",      // 微软雅黑
                    "C:\\Windows\\Fonts\\simsun.ttc,0"     // 宋体
                };
                
                foreach (string fallbackFontPath in fallbackFonts)
                {
                    var font = LoadFontFromPath(fallbackFontPath);
                    if (font != null)
                    {
                        LogHelper.Debug($"✓ 使用备用字体: {fallbackFontPath}");
                        return font;
                    }
                }

                // 尝试使用项目中的思源黑体字体
                try
                {
                    string currentDir = SystemPath.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string notoFontPath = SystemPath.Combine(currentDir, "Resources", "Fonts", "NotoSansSC-Regular.ttf");

                    if (File.Exists(notoFontPath))
                    {
                        var fontProgram = iText.IO.Font.FontProgramFactory.CreateFont(notoFontPath);
                        if (fontProgram != null)
                        {
                            var font = iText.Kernel.Font.PdfFontFactory.CreateFont(
                                fontProgram, 
                                iText.IO.Font.PdfEncodings.IDENTITY_H,
                                iText.Kernel.Font.PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                            LogHelper.Debug($"✓ 使用思源黑体字体: {notoFontPath}");
                            return font;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Debug($"思源黑体加载失败: {ex.Message}");
                }

                // 最终回退：使用内置字体
                var basicFont = iText.Kernel.Font.PdfFontFactory.CreateFont(
                    iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
                LogHelper.Debug("使用基础HELVETICA_BOLD字体（回退方案）");
                return basicFont;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"创建iText7字体失败: {ex.Message}");
                return iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
            }
        }

        /// <summary>
        /// 从路径加载字体（支持ttf和ttc格式）
        /// </summary>
        private static iText.Kernel.Font.PdfFont LoadFontFromPath(string fontPathWithIndex)
        {
            try
            {
                // 分离文件路径和索引
                string actualFilePath;
                int fontIndex = 0;
                
                if (fontPathWithIndex.Contains(","))
                {
                    int lastComma = fontPathWithIndex.LastIndexOf(',');
                    actualFilePath = fontPathWithIndex.Substring(0, lastComma);
                    int.TryParse(fontPathWithIndex.Substring(lastComma + 1), out fontIndex);
                }
                else
                {
                    actualFilePath = fontPathWithIndex;
                }

                if (!File.Exists(actualFilePath))
                {
                    LogHelper.Debug($"字体文件不存在: {actualFilePath}");
                    return null;
                }

                string ext = SystemPath.GetExtension(actualFilePath).ToLower();
                
                // 对于 TTC 文件，使用特殊的加载方式
                if (ext == ".ttc")
                {
                    // iText7 加载 TTC 文件的正确方式：使用字节数组 + TrueTypeCollection
                    byte[] fontBytes = File.ReadAllBytes(actualFilePath);
                    var ttc = new iText.IO.Font.TrueTypeCollection(fontBytes);
                    var fontProgram = ttc.GetFontByTccIndex(fontIndex);
                    
                    if (fontProgram != null)
                    {
                        var font = iText.Kernel.Font.PdfFontFactory.CreateFont(
                            fontProgram,
                            iText.IO.Font.PdfEncodings.IDENTITY_H,
                            iText.Kernel.Font.PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                        LogHelper.Debug($"TTC字体加载成功: {actualFilePath}, 索引: {fontIndex}");
                        return font;
                    }
                }
                else
                {
                    // 对于 TTF/OTF 文件，直接加载
                    var fontProgram = iText.IO.Font.FontProgramFactory.CreateFont(actualFilePath);
                    if (fontProgram != null)
                    {
                        var font = iText.Kernel.Font.PdfFontFactory.CreateFont(
                            fontProgram,
                            iText.IO.Font.PdfEncodings.IDENTITY_H,
                            iText.Kernel.Font.PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                        LogHelper.Debug($"TTF字体加载成功: {actualFilePath}");
                        return font;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"加载字体失败: {fontPathWithIndex}, 错误: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// 根据系统字体名获取字体文件路径
        /// </summary>
        private static string GetSystemFontPath(string fontName)
        {
            if (string.IsNullOrEmpty(fontName))
                return null;

            string fontsDir = "C:\\Windows\\Fonts";
            
            // 常用字体名到文件名的映射（包含完整路径格式）
            var fontFileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // 微软雅黑系列
                { "Microsoft YaHei", "msyh.ttc,0" },
                { "Microsoft YaHei UI", "msyh.ttc,1" },
                { "Microsoft YaHei Light", "msyhl.ttc,0" },
                { "微软雅黑", "msyh.ttc,0" },
                { "微软雅黑 Light", "msyhl.ttc,0" },
                
                // 中文基础字体
                { "SimHei", "simhei.ttf" },
                { "黑体", "simhei.ttf" },
                { "SimSun", "simsun.ttc,0" },
                { "宋体", "simsun.ttc,0" },
                { "NSimSun", "simsun.ttc,1" },
                { "新宋体", "simsun.ttc,1" },
                { "KaiTi", "simkai.ttf" },
                { "楷体", "simkai.ttf" },
                { "FangSong", "simfang.ttf" },
                { "仿宋", "simfang.ttf" },
                
                // 华文字体
                { "STXihei", "STXIHEI.TTF" },
                { "华文细黑", "STXIHEI.TTF" },
                { "STKaiti", "STKAITI.TTF" },
                { "华文楷体", "STKAITI.TTF" },
                { "STSong", "STSONG.TTF" },
                { "华文宋体", "STSONG.TTF" },
                { "STFangsong", "STFANGS.TTF" },
                { "华文仿宋", "STFANGS.TTF" },
                { "STZhongsong", "STZHONGS.TTF" },
                { "华文中宋", "STZHONGS.TTF" },
                
                // 英文常用字体
                { "Arial", "arial.ttf" },
                { "Arial Black", "ariblk.ttf" },
                { "Times New Roman", "times.ttf" },
                { "Courier New", "cour.ttf" },
                { "Verdana", "verdana.ttf" },
                { "Tahoma", "tahoma.ttf" },
                { "Georgia", "georgia.ttf" },
                { "Consolas", "consola.ttf" },
                { "Segoe UI", "segoeui.ttf" },
                { "Calibri", "calibri.ttf" },
                { "Cambria", "cambria.ttc,0" }
            };

            // 先尝试从映射表查找
            if (fontFileMap.TryGetValue(fontName, out string mappedFile))
            {
                string[] parts = mappedFile.Split(',');
                string filePath = SystemPath.Combine(fontsDir, parts[0]);
                if (File.Exists(filePath))
                {
                    string result = parts.Length > 1 ? $"{filePath},{parts[1]}" : filePath;
                    LogHelper.Debug($"从映射表找到字体: {fontName} -> {result}");
                    return result;
                }
            }

            // 尝试直接在字体目录中查找匹配的文件
            try
            {
                string[] extensions = { ".ttf", ".ttc", ".otf", ".TTF", ".TTC", ".OTF" };
                string normalizedName = fontName.Replace(" ", "").ToLower();

                // 尝试多种文件名格式
                string[] possibleNames = {
                    fontName,
                    fontName.Replace(" ", ""),
                    normalizedName,
                    fontName.ToLower(),
                    fontName.ToUpper()
                };

                foreach (string name in possibleNames)
                {
                    foreach (string ext in extensions)
                    {
                        string testPath = SystemPath.Combine(fontsDir, name + ext);
                        if (File.Exists(testPath))
                        {
                            string result = ext.ToLower() == ".ttc" ? $"{testPath},0" : testPath;
                            LogHelper.Debug($"直接匹配找到字体: {fontName} -> {result}");
                            return result;
                        }
                    }
                }

                // 遍历字体目录查找包含字体名的文件
                foreach (string file in Directory.GetFiles(fontsDir))
                {
                    string ext = SystemPath.GetExtension(file).ToLower();
                    if (ext != ".ttf" && ext != ".ttc" && ext != ".otf")
                        continue;
                        
                    string fileName = SystemPath.GetFileNameWithoutExtension(file).ToLower();
                    if (fileName.Contains(normalizedName) || normalizedName.Contains(fileName))
                    {
                        string result = ext == ".ttc" ? $"{file},0" : file;
                        LogHelper.Debug($"模糊匹配找到字体: {fontName} -> {result}");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"搜索字体文件失败: {ex.Message}");
            }

            LogHelper.Debug($"未找到字体文件: {fontName}");
            return null;
        }

        /// <summary>
        /// PDF坐标系统兼容性处理类
        /// </summary>
        public static class PdfCoordinateHelper
        {
            /// <summary>
            /// 获取页面的坐标系统信息
            /// </summary>
            public class PageCoordinateInfo
            {
                public float PageWidth { get; set; }
                public float PageHeight { get; set; }
                public float OriginX { get; set; }
                public float OriginY { get; set; }
                public float UsableWidth { get; set; }
                public float UsableHeight { get; set; }
                public int Rotation { get; set; }
                public bool IsInvertedY { get; set; }
                public iText.Kernel.Geom.Rectangle EffectiveArea { get; set; }

                public override string ToString()
                {
                    return $"Size: {PageWidth}x{PageHeight}, Origin: ({OriginX}, {OriginY}), " +
                           $"Usable: {UsableWidth}x{UsableHeight}, Rotation: {Rotation}°, " +
                           $"InvertedY: {IsInvertedY}";
                }
            }

            /// <summary>
            /// 分析页面并获取坐标系统信息
            /// </summary>
            public static PageCoordinateInfo AnalyzePageCoordinateSystem(iText.Kernel.Pdf.PdfPage page)
            {
                var info = new PageCoordinateInfo();

                try
                {
                    // 获取页面旋转信息
                    info.Rotation = page.GetRotation();
                    LogHelper.Debug($"页面旋转角度: {info.Rotation}°");

                    // 获取各种页面框
                    var mediaBox = page.GetMediaBox();
                    var cropBox = page.GetCropBox() ?? mediaBox;
                    var trimBox = page.GetTrimBox() ?? cropBox;
                    var bleedBox = page.GetBleedBox() ?? cropBox;
                    var artBox = page.GetArtBox() ?? cropBox;

                    LogHelper.Debug($"页面框信息 - MediaBox: {mediaBox}, CropBox: {cropBox}, TrimBox: {trimBox}");
                    LogHelper.Debug($"页面框信息 - BleedBox: {bleedBox}, ArtBox: {artBox}");

                    // 确定有效区域（优先使用ArtBox，然后是TrimBox，最后是CropBox）
                    info.EffectiveArea = artBox;
                    info.PageWidth = cropBox.GetWidth();
                    info.PageHeight = cropBox.GetHeight();
                    info.UsableWidth = info.EffectiveArea.GetWidth();
                    info.UsableHeight = info.EffectiveArea.GetHeight();

                    // 计算坐标原点偏移
                    // PDF坐标系：左下角为原点(0,0)，向上为Y轴正方向
                    // 但由于页面框可能有偏移，需要计算实际的原点位置
                    info.OriginX = info.EffectiveArea.GetLeft();
                    info.OriginY = info.EffectiveArea.GetBottom();

                    // 检查Y轴是否需要反转（某些PDF可能使用不同的坐标系）
                    info.IsInvertedY = ShouldInvertYAxis(cropBox, info.EffectiveArea, info.Rotation);

                    LogHelper.Debug($"坐标系统分析: {info}");
                    LogHelper.Debug($"有效区域: 左={info.EffectiveArea.GetLeft()}, 底={info.EffectiveArea.GetBottom()}, " +
                                   $"右={info.EffectiveArea.GetRight()}, 顶={info.EffectiveArea.GetTop()}");

                    return info;
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"分析页面坐标系统失败: {ex.Message}", ex);

                    // 返回默认值
                    info.PageWidth = 595; // A4宽度
                    info.PageHeight = 842; // A4高度
                    info.UsableWidth = info.PageWidth;
                    info.UsableHeight = info.PageHeight;
                    info.OriginX = 0;
                    info.OriginY = 0;
                    info.Rotation = 0;
                    info.IsInvertedY = false;

                    return info;
                }
            }

            /// <summary>
            /// 判断是否需要反转Y轴
            /// </summary>
            private static bool ShouldInvertYAxis(iText.Kernel.Geom.Rectangle cropBox, iText.Kernel.Geom.Rectangle effectiveArea, int rotation)
            {
                try
                {
                    // 如果页面有旋转，可能需要特殊处理
                    if (rotation % 180 != 0)
                    {
                        LogHelper.Debug($"页面旋转{rotation}度，可能需要Y轴反转");
                        return true;
                    }

                    // 如果有效区域的底部坐标小于顶部坐标的负值，可能需要反转
                    if (effectiveArea.GetBottom() < 0 && effectiveArea.GetTop() > 0)
                    {
                        LogHelper.Debug("有效区域跨越Y轴零点，使用标准PDF坐标系");
                        return false;
                    }

                    // 检查是否是异常的坐标系统
                    float expectedBottom = cropBox.GetBottom();
                    float actualBottom = effectiveArea.GetBottom();

                    if (Math.Abs(actualBottom - expectedBottom) > cropBox.GetHeight() * 0.1f)
                    {
                        LogHelper.Debug($"检测到异常坐标偏移: 期望{expectedBottom}, 实际{actualBottom}");
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    LogHelper.Debug($"判断Y轴反转时出错: {ex.Message}，使用默认值");
                    return false;
                }
            }

            /// <summary>
            /// 将标准坐标转换为页面实际坐标
            /// </summary>
            public static System.Drawing.PointF TransformCoordinates(float x, float y, PageCoordinateInfo coordInfo)
            {
                try
                {
                    // 输入的x,y是相对于有效区域的坐标，原点在左下角
                    float transformedX = coordInfo.OriginX + x;
                    float transformedY;

                    if (coordInfo.IsInvertedY)
                    {
                        // 如果需要反转Y轴，从顶部开始计算
                        transformedY = coordInfo.OriginY + coordInfo.UsableHeight - y;
                    }
                    else
                    {
                        // 标准PDF坐标系，从底部开始计算
                        transformedY = coordInfo.OriginY + y;
                    }

                    // 应用页面旋转的影响
                    if (coordInfo.Rotation != 0)
                    {
                        var rotated = ApplyRotation(transformedX, transformedY, coordInfo.PageWidth, coordInfo.PageHeight, coordInfo.Rotation);
                        return new System.Drawing.PointF(rotated.X, rotated.Y);
                    }

                    return new System.Drawing.PointF(transformedX, transformedY);
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"坐标转换失败: {ex.Message}");
                    return new System.Drawing.PointF(x, y);
                }
            }

            /// <summary>
            /// 应用页面旋转
            /// </summary>
            private static System.Drawing.PointF ApplyRotation(float x, float y, float pageWidth, float pageHeight, int rotation)
            {
                float centerX = pageWidth / 2;
                float centerY = pageHeight / 2;

                // 将坐标移到页面中心
                float translatedX = x - centerX;
                float translatedY = y - centerY;

                float angleRad = (float)(rotation * Math.PI / 180);
                float rotatedX = (float)(translatedX * Math.Cos(angleRad) - translatedY * Math.Sin(angleRad));
                float rotatedY = (float)(translatedX * Math.Sin(angleRad) + translatedY * Math.Cos(angleRad));

                // 将坐标移回
                return new System.Drawing.PointF(rotatedX + centerX, rotatedY + centerY);
            }

            /// <summary>
            /// 计算居中位置
            /// </summary>
            public static System.Drawing.RectangleF CalculateCenteredPosition(float contentWidth, float contentHeight, PageCoordinateInfo coordInfo, float margin = 0)
            {
                try
                {
                    // 计算可用区域（减去边距）
                    float availableWidth = Math.Max(0, coordInfo.UsableWidth - margin * 2);
                    float availableHeight = Math.Max(0, coordInfo.UsableHeight - margin * 2);

                    // 计算居中位置
                    float centerX = margin + (availableWidth - contentWidth) / 2;
                    float centerY = margin + (availableHeight - contentHeight) / 2;

                    // 转换为页面坐标
                    var topLeft = TransformCoordinates(centerX, centerY + contentHeight, coordInfo);
                    var bottomRight = TransformCoordinates(centerX + contentWidth, centerY, coordInfo);

                    return new System.Drawing.RectangleF(
                        Math.Min(topLeft.X, bottomRight.X),
                        Math.Min(topLeft.Y, bottomRight.Y),
                        Math.Abs(bottomRight.X - topLeft.X),
                        Math.Abs(bottomRight.Y - topLeft.Y)
                    );
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"计算居中位置失败: {ex.Message}");
                    // 返回默认位置
                    return new System.Drawing.RectangleF(margin, margin, contentWidth, contentHeight);
                }
            }
        }

        /// <summary>
        /// 使用iText7绘制居中文字（支持自动换行）
        /// </summary>
        /// <summary>
        /// 使用iText7绘制居中文字（支持自动换行）
        /// </summary>
        private static void DrawCenteredTextiText7(iText.Kernel.Pdf.PdfPage page, string text, iText.Kernel.Font.PdfFont font, float pageWidth, float pageHeight, float fontSize, iText.Kernel.Pdf.PdfDocument pdfDoc)
        {
            try
            {
                LogHelper.Debug($"开始绘制iText7文字: {text}, 字体大小: {fontSize}");

                // 获取页面旋转角度
                int rotation = page.GetRotation();
                LogHelper.Debug($"页面旋转角度: {rotation}°");

                // 获取页面的实际可用区域（使用CropBox，如果没有则使用MediaBox）
                iText.Kernel.Geom.Rectangle cropBox = page.GetCropBox() ?? page.GetMediaBox();
                float cropWidth = cropBox.GetWidth();
                float cropHeight = cropBox.GetHeight();

                // 根据页面旋转角度调整可用区域的宽高概念
                float usableWidth, usableHeight;
                if (rotation == 90 || rotation == 270)
                {
                    // 90度或270度旋转时，交换宽高
                    usableWidth = cropHeight;
                    usableHeight = cropWidth;
                    LogHelper.Debug($"页面{rotation}°旋转: 交换宽高 - 原始: {cropWidth:F2}x{cropHeight:F2} -> 调整后: {usableWidth:F2}x{usableHeight:F2}");
                }
                else
                {
                    // 0度或180度旋转时，保持原宽高
                    usableWidth = cropWidth;
                    usableHeight = cropHeight;
                    LogHelper.Debug($"页面{rotation}°旋转: 保持宽高 - {usableWidth:F2}x{usableHeight:F2}");
                }

                // 创建画布
                var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdfDoc);

                // 绘制白色背景（覆盖整个有效区域）
                canvas.SetFillColor(iText.Kernel.Colors.ColorConstants.WHITE)
                      .Rectangle(cropBox.GetLeft(), cropBox.GetBottom(), cropBox.GetWidth(), cropBox.GetHeight())
                      .Fill();

                LogHelper.Debug($"已绘制白色背景: {cropBox.GetWidth():F2}x{cropBox.GetHeight():F2}");

                // 设置文字颜色
                canvas.SetFillColor(iText.Kernel.Colors.ColorConstants.BLACK);

                // 计算页边距和最大文本宽度
                float margin = 10f; // 固定页边距为10点
                float maxTextWidth = usableWidth - (margin * 2);
                float lineHeight = fontSize * 1.2f; // 行高为字体大小的1.2倍（调整为更紧凑）

                LogHelper.Debug($"文本参数 - 页边距: {margin:F2}, 最大宽度: {maxTextWidth:F2}, 行高: {lineHeight:F2}");

                // 处理文本自动换行
                var lines = ProcessTextWithLineBreaks(text, font, fontSize, maxTextWidth);

                // 如果没有换行处理结果（可能是空文本），则使用原始文本
                if (lines.Count == 0 && !string.IsNullOrWhiteSpace(text))
                {
                    lines = new List<string> { text };
                }

                LogHelper.Debug($"文本处理完成，共{lines.Count}行");

                // 计算总文本高度
                float totalTextHeight = lines.Count * lineHeight;

                // 添加垂直页边距控制
                float maxTextHeight = usableHeight - (margin * 2); // 垂直方向的最大可用高度
                LogHelper.Debug($"垂直边界检查 - 总文本高度: {totalTextHeight:F2}, 最大可用高度: {maxTextHeight:F2}");

                // 如果文本高度超出垂直可用空间，进行自适应调整
                if (totalTextHeight > maxTextHeight)
                {
                    // 计算最小行高（字体大小的80%，确保文本不会重叠）
                    float minLineHeight = fontSize * 0.8f;

                    // 计算在最小行高限制下最大可容纳的行数
                    int maxLinesWithMinHeight = (int)(maxTextHeight / minLineHeight);

                    LogHelper.Debug($"垂直空间不足 - 总行数: {lines.Count}, 最大可容纳行数: {maxLinesWithMinHeight}");

                    if (lines.Count > maxLinesWithMinHeight)
                    {
                        // 文本行数太多，截断到最大可容纳的行数
                        lines = lines.Take(maxLinesWithMinHeight).ToList();
                        if (lines.Count > 0)
                        {
                            // 在最后一行添加省略号表示有更多内容
                            string lastLine = lines[lines.Count - 1];
                            if (lastLine.Length > 3)
                            {
                                lines[lines.Count - 1] = lastLine.Substring(0, lastLine.Length - 3) + "...";
                            }
                            else
                            {
                                lines[lines.Count - 1] += "...";
                            }
                        }

                        LogHelper.Debug($"文本行数过多，截断到 {lines.Count} 行，最后一行添加省略号");
                    }

                    // 使用最小行高重新计算布局
                    lineHeight = minLineHeight;
                    totalTextHeight = lines.Count * lineHeight;

                    LogHelper.Debug($"使用最小行高: {lineHeight:F2}");
                    LogHelper.Debug($"最终文本总高度: {totalTextHeight:F2}");
                }

                // 计算起始Y坐标（考虑垂直页边距的居中）
                // 新公式：页面顶部 + 垂直页边距 + (垂直可用空间 - 文本总高度) / 2
                float startY = margin + (maxTextHeight - totalTextHeight) / 2f + lineHeight;

                LogHelper.Debug($"最终布局 - 垂直页边距: {margin:F2}, 起始Y: {startY:F2}, 文本总高度: {totalTextHeight:F2}");

                // 绘制每一行文本
                canvas.BeginText()
                      .SetFontAndSize(font, fontSize);

                // 根据页面旋转角度应用文字变换
                if (rotation != 0)
                {
                    LogHelper.Debug($"应用全局文字旋转变换: {rotation}°");

                    // 对于旋转页面，使用原始页面的坐标系统计算中心点
                    float centerX, centerY;

                    if (rotation == 90 || rotation == 270)
                    {
                        // 90度或270度旋转时，中心点需要使用原始坐标系统
                        centerX = cropHeight / 2f;  // 使用原始高度作为X方向的中心
                        centerY = cropWidth / 2f;   // 使用原始宽度作为Y方向的中心
                        LogHelper.Debug($"旋转页面中心点: ({centerX:F2}, {centerY:F2}) - 使用原始坐标系统");
                    }
                    else
                    {
                        // 0度或180度旋转时，使用正常的中心点计算
                        centerX = cropWidth / 2f;
                        centerY = cropHeight / 2f;
                        LogHelper.Debug($"标准页面中心点: ({centerX:F2}, {centerY:F2})");
                    }

                    // 应用文字旋转变换矩阵（只应用一次）
                    // 对于旋转页面，我们需要调整变换顺序和参数
                    if (rotation == 270)
                    {
                        // 270度旋转的特殊处理：先平移到正确位置，再旋转
                        canvas.ConcatMatrix(0, -1, 1, 0, 0, cropHeight);
                        LogHelper.Debug("应用270度旋转变换矩阵");
                    }
                    else if (rotation == 90)
                    {
                        // 90度旋转
                        canvas.ConcatMatrix(0, 1, -1, 0, cropWidth, 0);
                        LogHelper.Debug("应用90度旋转变换矩阵");
                    }
                    else if (rotation == 180)
                    {
                        // 180度旋转
                        canvas.ConcatMatrix(-1, 0, 0, -1, cropWidth, cropHeight);
                        LogHelper.Debug("应用180度旋转变换矩阵");
                    }
                }

                for (int i = 0; i < lines.Count; i++)
                {
                    string lineText = lines[i];
                    if (string.IsNullOrWhiteSpace(lineText))
                        continue;

                    // 计算当前行的宽度
                    float lineWidth = font.GetWidth(lineText, fontSize);

                    // 计算当前行的X坐标（水平居中）
                    float lineX = (usableWidth - lineWidth) / 2f;

                    // 计算当前行的Y坐标
                    float lineY = startY - (i * lineHeight);

                    LogHelper.Debug($"绘制第{i + 1}行: '{lineText}' at ({lineX:F2}, {lineY:F2}), 宽度: {lineWidth:F2}");

                    // 设置文字位置（如果已应用旋转变换，则此位置会在旋转坐标系中正确显示）
                    if (rotation == 0)
                    {
                        // 无旋转时直接设置位置
                        canvas.SetTextMatrix(lineX, lineY);
                    }
                    else
                    {
                        // 有旋转时，在旋转坐标系中设置相对位置
                        // 注意：由于已经应用了旋转变换，这里直接使用计算的坐标即可
                        canvas.SetTextMatrix(lineX, lineY);
                    }

                    // 显示当前行文本
                    canvas.ShowText(lineText);

                    // 如果无旋转，需要重置文字矩阵为下一行做准备
                    if (rotation == 0)
                    {
                        canvas.SetTextMatrix(0, 0); // 重置变换矩阵
                    }
                }

                canvas.EndText();
                canvas.Release();
                LogHelper.Debug($"iText7多行文字绘制完成: 共{lines.Count}行，已居中显示");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"iText7绘制居中文字失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 处理文字自动换行
        /// </summary>
        private static List<string> ProcessTextWithLineBreaks(string text, iText.Kernel.Font.PdfFont font, float fontSize, float maxWidth)
        {
            var lines = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
                return lines;

            LogHelper.Debug($"开始处理文字换行: '{text}', 最大宽度: {maxWidth}");

            // 首先按换行符分割
            string[] paragraphs = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string paragraph in paragraphs)
            {
                if (string.IsNullOrWhiteSpace(paragraph))
                    continue;

                // 处理每个段落的自动换行
                var paragraphLines = ProcessParagraphWithLineBreaks(paragraph.Trim(), font, fontSize, maxWidth);
                lines.AddRange(paragraphLines);
            }

            LogHelper.Debug($"文字换行处理完成，共{lines.Count}行");
            return lines;
        }

        /// <summary>
        /// 处理单个段落的自动换行
        /// </summary>
        private static List<string> ProcessParagraphWithLineBreaks(string paragraph, iText.Kernel.Font.PdfFont font, float fontSize, float maxWidth)
        {
            var lines = new List<string>();

            if (string.IsNullOrWhiteSpace(paragraph))
                return lines;

            LogHelper.Debug($"开始处理文字换行: '{paragraph}', 最大宽度: {maxWidth}");

            // 对于包含空格的文本，使用更智能的换行算法
            // 保持原始文本的空格和格式
            int currentIndex = 0;
            int paragraphLength = paragraph.Length;

            while (currentIndex < paragraphLength)
            {
                // 尝试添加更多字符直到达到最大宽度
                int testEnd = currentIndex + 1;
                string testLine = paragraph.Substring(currentIndex, testEnd - currentIndex);

                // 继续扩展测试行，直到超出宽度或到达段落末尾
                while (testEnd <= paragraphLength)
                {
                    float testWidth = font.GetWidth(testLine, fontSize);
                    
                    if (testWidth > maxWidth)
                    {
                        // 超出宽度，回退到上一个可接受的位置
                        if (testLine.Length > 1)
                        {
                            // 尝试在空格处断行
                            int lastSpaceIndex = testLine.LastIndexOf(' ');
                            if (lastSpaceIndex > 0 && lastSpaceIndex < testLine.Length - 1)
                            {
                                // 在空格处断行
                                string lineToAdd = testLine.Substring(0, lastSpaceIndex);
                                if (!string.IsNullOrWhiteSpace(lineToAdd))
                                {
                                    lines.Add(lineToAdd.Trim());
                                    LogHelper.Debug($"添加换行: '{lineToAdd.Trim()}'");
                                }
                                currentIndex = currentIndex + lastSpaceIndex + 1; // 跳过空格
                                break;
                            }
                            else
                            {
                                // 没有空格，强制在字符处断行
                                if (testLine.Length > 1)
                                {
                                    string lineToAdd = testLine.Substring(0, testLine.Length - 1);
                                    lines.Add(lineToAdd.Trim());
                                    LogHelper.Debug($"强制换行: '{lineToAdd.Trim()}'");
                                    currentIndex = currentIndex + lineToAdd.Length;
                                    break;
                                }
                                else
                                {
                                    // 单个字符就超出宽度，强制添加
                                    lines.Add(testLine.Trim());
                                    LogHelper.Debug($"单字符换行: '{testLine.Trim()}'");
                                    currentIndex = testEnd;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // 单个字符，强制添加
                            lines.Add(testLine.Trim());
                            LogHelper.Debug($"单字符强制换行: '{testLine.Trim()}'");
                            currentIndex = testEnd;
                            break;
                        }
                    }
                    else
                    {
                        // 还未超出宽度，继续扩展
                        testEnd++;
                        if (testEnd <= paragraphLength)
                        {
                            testLine = paragraph.Substring(currentIndex, testEnd - currentIndex);
                        }
                    }

                    if (testEnd > paragraphLength)
                    {
                        // 到达段落末尾，添加剩余文本
                        string remainingText = paragraph.Substring(currentIndex);
                        if (!string.IsNullOrWhiteSpace(remainingText))
                        {
                            lines.Add(remainingText.Trim());
                            LogHelper.Debug($"添加最后行: '{remainingText.Trim()}'");
                        }
                        currentIndex = paragraphLength;
                        break;
                    }
                }
            }

            return lines;
        }

        /// <summary>
        /// 处理超长单词的字符级分割
        /// </summary>
        private static List<string> ProcessLongWord(string word, iText.Kernel.Font.PdfFont font, float fontSize, float maxWidth)
        {
            var lines = new List<string>();
            string currentLine = "";

            foreach (char c in word)
            {
                string testLine = currentLine + c;
                float testWidth = font.GetWidth(testLine, fontSize);

                if (testWidth <= maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        lines.Add(currentLine);
                        LogHelper.Debug($"添加字符分割行: '{currentLine}'");
                    }
                    currentLine = c.ToString();
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
                LogHelper.Debug($"添加最后字符行: '{currentLine}'");
            }

            return lines;
        }
    }
}
