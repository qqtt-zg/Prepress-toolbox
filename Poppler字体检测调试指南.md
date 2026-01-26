# Poppler 字体检测调试指南

## 问题现象

字体检测结果仍然与 Adobe Acrobat DC 不一致，怀疑 pdffonts 工具没有正常工作。

## 调试步骤

### 步骤 1: 验证 pdffonts 命令行工具

在命令行中直接测试 pdffonts 工具：

```cmd
cd poppler\bin
pdffonts.exe "..\..\你的测试文件.pdf"
```

**预期输出：**
```
name                                 type              encoding         emb sub uni object ID
------------------------------------ ----------------- ---------------- --- --- --- ---------
ABCDEE+SimSun                        CID Type 0C       Identity-H       yes yes yes      8  0
Arial-Bold                           TrueType          WinAnsi          yes no  yes     12  0
```

**如果没有输出或报错：**
- 检查 PDF 文件路径是否正确
- 检查 PDF 文件是否损坏
- 检查 DLL 依赖是否完整

### 步骤 2: 检查代码是否使用了 Poppler 服务

打开日志文件 `app_2026-01-20.log`，搜索以下关键字：

**成功使用 Poppler 的日志：**
```
[字体检测] 使用 pdffonts 工具: F:\...\poppler\bin\pdffonts.exe
[字体检测] pdffonts 错误输出: ...
字体检测完成: xxx.pdf, 总字体数: X, 问题字体数: X
```

**回退到 iText 的日志：**
```
[字体检测] 未找到 pdffonts 工具，回退到 iText 解析
```

### 步骤 3: 添加调试日志

修改 `PdfFontInspectorService_Poppler.cs`，在关键位置添加日志：

#### 3.1 在 InspectFonts 方法开始处添加：

```csharp
public DocumentFontInfo InspectFonts(string filePath)
{
    LogHelper.Info($"[Poppler] 开始检测字体: {filePath}");
    
    var docInfo = new DocumentFontInfo
    {
        FilePath = filePath,
        FileName = Path.GetFileName(filePath)
    };
    // ... 其余代码
}
```

#### 3.2 在 FindPdffonts 方法中添加：

```csharp
private string FindPdffonts()
{
    LogHelper.Info("[Poppler] 开始查找 pdffonts 工具");
    
    string appDir = AppDomain.CurrentDomain.BaseDirectory;
    LogHelper.Info($"[Poppler] 应用程序目录: {appDir}");
    
    // 检查 poppler 子目录
    string portablePath = IOPath.Combine(appDir, "poppler", "bin", PDFFONTS_EXE);
    LogHelper.Info($"[Poppler] 检查路径: {portablePath}");
    
    if (File.Exists(portablePath))
    {
        LogHelper.Info($"[Poppler] ✓ 找到便携版 pdffonts: {portablePath}");
        return portablePath;
    }
    else
    {
        LogHelper.Warn($"[Poppler] ✗ 未找到: {portablePath}");
    }
    
    // ... 其余代码
}
```

#### 3.3 在 ExecutePdffonts 方法中添加：

```csharp
private string ExecutePdffonts(string pdffontsTool, string filePath)
{
    try
    {
        LogHelper.Info($"[Poppler] 执行命令: {pdffontsTool} \"{filePath}\"");
        
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

            LogHelper.Debug($"[Poppler] 原始输出:\n{output}");
            return output;
        }
    }
    catch (Exception ex)
    {
        LogHelper.Error($"[Poppler] 执行 pdffonts 失败: {ex.Message}", ex);
        return null;
    }
}
```

#### 3.4 在 ParsePdffontsOutput 方法中添加：

```csharp
private List<FontInfo> ParsePdffontsOutput(string output)
{
    var fonts = new List<FontInfo>();
    
    try
    {
        LogHelper.Info($"[Poppler] 开始解析输出，长度: {output.Length}");
        
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        LogHelper.Info($"[Poppler] 总行数: {lines.Length}");
        
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
                LogHelper.Info($"[Poppler] ✓ 解析字体: {fontInfo.FontName} ({fontInfo.FontSubtype})");
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
```

### 步骤 4: 重新编译并测试

1. 在 Visual Studio 中打开项目
2. 重新生成解决方案（Ctrl+Shift+B）
3. 运行程序
4. 打开一个 PDF 文件
5. 点击"检查器"按钮
6. 切换到"字体"标签页
7. 查看日志文件

### 步骤 5: 分析日志

#### 场景 A: pdffonts 工具未找到

**日志内容：**
```
[Poppler] 开始查找 pdffonts 工具
[Poppler] 应用程序目录: F:\...\src\WindowsFormsApp3\bin\Debug\
[Poppler] 检查路径: F:\...\src\WindowsFormsApp3\bin\Debug\poppler\bin\pdffonts.exe
[Poppler] ✗ 未找到: F:\...\src\WindowsFormsApp3\bin\Debug\poppler\bin\pdffonts.exe
[字体检测] 未找到 pdffonts 工具，回退到 iText 解析
```

**解决方案：**
poppler 文件夹需要复制到编译输出目录（bin\Debug 或 bin\Release）

**操作：**
1. 手动复制 `poppler` 文件夹到 `src\WindowsFormsApp3\bin\Debug\`
2. 或者修改项目文件，添加自动复制

#### 场景 B: pdffonts 执行失败

**日志内容：**
```
[Poppler] ✓ 找到便携版 pdffonts: F:\...\poppler\bin\pdffonts.exe
[Poppler] 执行命令: F:\...\poppler\bin\pdffonts.exe "C:\test.pdf"
[Poppler] 退出代码: 1
[Poppler] 错误输出: Error: Couldn't open file 'C:\test.pdf'
```

**解决方案：**
- 检查 PDF 文件路径是否正确
- 检查 PDF 文件是否存在
- 检查文件权限

#### 场景 C: 解析输出失败

**日志内容：**
```
[Poppler] 退出代码: 0
[Poppler] 输出长度: 0 字符
[Poppler] 解析完成，共 0 个字体
```

**解决方案：**
- PDF 文件可能没有字体（已转曲或纯图片）
- 或者 pdffonts 输出编码问题

### 步骤 6: 对比测试

创建一个对比测试表格：

| 测试项 | Adobe Acrobat | pdffonts 命令行 | Poppler 集成 | iText | 备注 |
|--------|--------------|----------------|-------------|-------|------|
| 字体总数 | 5 | ? | ? | ? | |
| 字体名称 | SimSun, Arial... | ? | ? | ? | |
| 嵌入状态 | 2嵌入, 3未嵌入 | ? | ? | ? | |

### 步骤 7: 常见问题排查

#### 问题 1: 应用程序找不到 poppler 目录

**原因：** 编译后的程序在 `bin\Debug` 目录运行，而 poppler 在项目根目录

**解决方案 1 - 手动复制：**
```cmd
xcopy /E /I /Y poppler src\WindowsFormsApp3\bin\Debug\poppler
```

**解决方案 2 - 修改项目文件：**
在 `WindowsFormsApp3.csproj` 中添加：
```xml
<ItemGroup>
  <None Include="..\..\poppler\bin\**\*.*">
    <Link>poppler\bin\%(RecursiveDir)%(Filename)%(Extension)</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

#### 问题 2: DLL 依赖缺失

**症状：** pdffonts.exe 无法运行，提示缺少 DLL

**检查：**
```cmd
cd src\WindowsFormsApp3\bin\Debug\poppler\bin
dir *.dll
```

**应该包含：**
- poppler.dll
- cairo.dll
- freetype.dll
- 等其他依赖

#### 问题 3: 编码问题

**症状：** 中文字体名称显示乱码

**解决：** 在 ExecutePdffonts 中已设置 UTF-8 编码：
```csharp
StandardOutputEncoding = System.Text.Encoding.UTF8
```

## 快速验证脚本

创建 `test_poppler_in_bin.bat` 放在 `src\WindowsFormsApp3\bin\Debug\` 目录：

```batch
@echo off
echo 检查 poppler 工具...
if exist "poppler\bin\pdffonts.exe" (
    echo ✓ pdffonts.exe 存在
    poppler\bin\pdffonts.exe -v
) else (
    echo ✗ pdffonts.exe 不存在
    echo 请复制 poppler 文件夹到此目录
)
pause
```

## 总结

如果按照以上步骤操作后仍然不一致，请提供：
1. 完整的日志文件
2. Adobe Acrobat 显示的字体列表截图
3. pdffonts 命令行输出
4. 程序中显示的字体列表截图

这样我可以进一步分析问题所在。
