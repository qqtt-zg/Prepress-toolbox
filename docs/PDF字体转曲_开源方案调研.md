# PDF 字体转曲 - 开源方案调研

## 调研概述

调研了多种 PDF 字体转曲（将文字转换为路径/曲线）的开源方案和商业方案。

---

## 方案一：Ghostscript（推荐 ⭐⭐⭐⭐⭐）

### 简介
- **类型**：开源命令行工具
- **许可证**：AGPL / 商业许可
- **平台**：跨平台（Windows/Linux/macOS）
- **官网**：[https://ghostscript.com](https://ghostscript.com)

### 实现方式

#### 方法 1：Ghostscript 9.15+ （推荐）
使用 `-dNoOutputFonts` 参数，一步完成转曲：

```bash
# Windows
gswin64c -o output.pdf -dNoOutputFonts -sDEVICE=pdfwrite input.pdf

# Linux/macOS
gs -o output.pdf -dNoOutputFonts -sDEVICE=pdfwrite input.pdf
```

**优点**：
- ✅ 一步完成，简单快速
- ✅ Ghostscript 9.15+ 官方支持
- ✅ 稳定可靠

#### 方法 2：Ghostscript 9.14 及更早版本
使用 `-dNOCACHE` 参数，需要两步：

```bash
# 步骤 1：PDF → PostScript（转曲）
gs -o temp.ps -dNOCACHE -sDEVICE=pswrite input.pdf

# 步骤 2：PostScript → PDF
gs -o output.pdf -sDEVICE=pdfwrite temp.ps
```

**注意**：`-dNOCACHE` 参数可能在未来版本中移除，不推荐长期使用。

### 在 C# 中集成

#### 方式 1：调用 Ghostscript 命令行
```csharp
using System.Diagnostics;

public bool ConvertTextToOutlines(string inputPath, string outputPath)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "gswin64c.exe",  // 或 gswin32c.exe
        Arguments = $"-o \"{outputPath}\" -dNoOutputFonts -sDEVICE=pdfwrite \"{inputPath}\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

    using (var process = Process.Start(startInfo))
    {
        process.WaitForExit();
        return process.ExitCode == 0;
    }
}
```

#### 方式 2：使用 Ghostscript.NET 库
NuGet 包：`Ghostscript.NET`

```csharp
using Ghostscript.NET;
using Ghostscript.NET.Processor;

public bool ConvertTextToOutlines(string inputPath, string outputPath)
{
    var version = GhostscriptVersionInfo.GetLastInstalledVersion();
    
    using (var processor = new GhostscriptProcessor(version))
    {
        var args = new[]
        {
            "-o", outputPath,
            "-dNoOutputFonts",
            "-sDEVICE=pdfwrite",
            inputPath
        };
        
        processor.Process(args);
        return true;
    }
}
```

### 优缺点

**优点**：
- ✅ 完全免费开源（AGPL）
- ✅ 功能强大，久经考验
- ✅ 跨平台支持
- ✅ 命令行调用简单
- ✅ 有 .NET 封装库可用

**缺点**：
- ❌ 需要安装 Ghostscript
- ❌ AGPL 许可证要求开源（商业使用需购买许可）
- ❌ 可能改变图像质量（需要额外参数控制）
- ❌ 文件大小可能增加

### 部署方式

1. **独立安装**：用户需要安装 Ghostscript
2. **打包分发**：将 Ghostscript 可执行文件打包到应用程序中
3. **NuGet 包**：使用 Ghostscript.NET 包，自动处理依赖

---

## 方案二：GcPdf（商业方案）

### 简介
- **类型**：商业 .NET 库
- **开发商**：MESCIUS (原 GrapeCity)
- **许可证**：商业许可（需购买）
- **官网**：[https://developer.mescius.com](https://developer.mescius.com)

### 实现方式

```csharp
using GrapeCity.Documents.Pdf;

public int ConvertTextToOutlines(string inputPath, string outputPath)
{
    var doc = new GcPdfDocument();
    
    using (var fs = File.OpenRead(inputPath))
    {
        // 加载原始 PDF
        var srcDoc = new GcPdfDocument();
        srcDoc.Load(fs);
        
        // 遍历所有页面
        foreach (var srcPage in srcDoc.Pages)
        {
            // 创建新页面
            var page = doc.Pages.Add(srcPage.Size);
            
            // 关键：设置为绘制文字为路径
            page.Graphics.DrawTextAsPath = true;
            
            // 绘制原页面到新页面
            srcPage.Draw(page.Graphics, srcPage.Bounds);
        }
    }
    
    // 保存
    using (var fs = File.Create(outputPath))
    {
        doc.Save(fs);
    }
    
    return doc.Pages.Count;
}
```

### 优缺点

**优点**：
- ✅ 纯 .NET 实现，无需外部依赖
- ✅ API 简洁易用
- ✅ 性能优秀
- ✅ 商业支持和文档完善

**缺点**：
- ❌ 需要购买商业许可证（价格较高）
- ❌ 不是开源方案

---

## 方案三：iText 7（部分开源）

### 简介
- **类型**：开源/商业混合
- **许可证**：AGPL / 商业许可
- **官网**：[https://itextpdf.com](https://itextpdf.com)

### 实现难度
iText 7 本身不直接提供字体转曲功能，需要：
1. 解析 PDF 内容流
2. 提取文本渲染操作
3. 获取字体字形轮廓
4. 转换为路径
5. 重写内容流

**复杂度**：⭐⭐⭐⭐⭐（非常复杂）

### 优缺点

**优点**：
- ✅ 功能强大，可以精细控制
- ✅ 已经在项目中使用

**缺点**：
- ❌ 实现非常复杂
- ❌ 需要深入了解 PDF 规范
- ❌ AGPL 许可证限制

---

## 方案四：PDFsharp（开源）

### 简介
- **类型**：开源 .NET 库
- **许可证**：MIT License
- **官网**：[http://pdfsharp.net](http://pdfsharp.net)
- **GitHub**：[https://github.com/empira/PDFsharp](https://github.com/empira/PDFsharp)

### 字体转曲支持
PDFsharp 主要用于创建和修改 PDF，**不直接支持字体转曲功能**。

---

## 推荐方案

### 方案 A：Ghostscript + 命令行调用（推荐 ⭐⭐⭐⭐⭐）

**适用场景**：
- 需要免费开源方案
- 可以接受 AGPL 许可证或购买商业许可
- 不介意外部依赖

**实现步骤**：
1. 在应用程序中打包 Ghostscript 可执行文件
2. 使用 `Process.Start()` 调用 Ghostscript
3. 传递 `-dNoOutputFonts` 参数
4. 监控进程输出和退出代码

**代码示例**：见上文"方式 1：调用 Ghostscript 命令行"

### 方案 B：Ghostscript.NET 库（推荐 ⭐⭐⭐⭐）

**适用场景**：
- 需要更好的 .NET 集成
- 希望避免直接调用命令行
- 可以接受 AGPL 许可证

**实现步骤**：
1. 安装 NuGet 包：`Ghostscript.NET`
2. 使用 `GhostscriptProcessor` 类
3. 传递相同的参数

**代码示例**：见上文"方式 2：使用 Ghostscript.NET 库"

### 方案 C：GcPdf（商业方案）

**适用场景**：
- 预算充足
- 需要商业支持
- 希望纯 .NET 实现

---

## 实施建议

### 短期（立即实施）
使用 **Ghostscript 命令行调用**：
1. 下载 Ghostscript 安装包
2. 将 `gswin64c.exe` 打包到应用程序
3. 实现命令行调用逻辑
4. 添加错误处理和日志

### 中期（优化）
切换到 **Ghostscript.NET 库**：
1. 安装 NuGet 包
2. 重构代码使用库 API
3. 改进错误处理
4. 添加进度回调

### 长期（评估）
如果预算允许，评估 **GcPdf**：
1. 申请试用许可
2. 测试性能和质量
3. 评估成本效益
4. 决定是否采购

---

## 许可证说明

### Ghostscript 许可证
- **AGPL**：免费使用，但要求应用程序也开源
- **商业许可**：需要购买，可以闭源使用
- **价格**：联系 Artifex Software 获取报价

### 建议
1. 如果应用程序是开源的，使用 AGPL 版本
2. 如果应用程序是商业闭源的，需要购买商业许可
3. 或者，将 Ghostscript 作为可选的外部工具，由用户自行安装

---

## 参考资源

### Ghostscript
- [官方文档](https://ghostscript.com/docs/)
- [Stack Overflow 讨论](https://stackoverflow.com/questions/28797418/)
- [Ghostscript.NET GitHub](https://github.com/jhabjan/Ghostscript.NET)

### GcPdf
- [官方博客](https://developer.mescius.com/blogs/outlining-pdf-fonts-in-your-c-sharp-applications)
- [在线演示](https://developer.mescius.com/gcdocs/demos)

### 其他
- [PDF 规范](https://www.adobe.com/devnet/pdf/pdf_reference.html)
- [iText 文档](https://itextpdf.com/resources)

---

## 总结

**最佳方案**：使用 **Ghostscript** 通过命令行调用实现字体转曲功能。

**理由**：
1. ✅ 完全免费（AGPL）或可购买商业许可
2. ✅ 功能成熟稳定
3. ✅ 实现简单快速
4. ✅ 跨平台支持
5. ✅ 社区支持良好

**下一步**：
1. 下载并测试 Ghostscript
2. 实现命令行调用逻辑
3. 集成到现有的 `PdfFontOutlineService`
4. 测试各种 PDF 文件
5. 优化错误处理和用户体验

---

**调研日期**：2026-01-19  
**调研人**：Kiro AI Assistant  
**状态**：✅ 已完成，推荐使用 Ghostscript 方案
