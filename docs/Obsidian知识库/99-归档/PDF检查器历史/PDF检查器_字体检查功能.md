# PDF检查器 - 字体检查功能

## 概述

新增字体检查功能，可以检查PDF文档中使用的所有字体，显示字体详细信息和潜在问题。

**实现日期**: 2026-01-19  
**版本**: v1.3.0  
**状态**: ✅ 已实现

---

## 功能特性

### 1. 字体信息显示

显示PDF中所有使用的字体及其详细信息：

#### 基本信息
- **字体名称**: 完整的字体名称（包括子集前缀）
- **字体类型**: TrueType, Type1, Type3, CIDFont等
- **字体子类型**: 更详细的字体分类

#### 嵌入状态
- **完全嵌入**: 整个字体文件嵌入到PDF中
- **子集嵌入**: 只嵌入使用的字符（常见于中文字体）
- **未嵌入**: 字体未嵌入，依赖系统字体

#### 使用信息
- **使用页面**: 显示使用该字体的页面列表
- **使用次数**: 字体在文档中的使用频率
- **嵌入大小**: 嵌入字体文件的大小（如果嵌入）

### 2. 字体问题检测

自动检测以下字体问题：

#### 错误级别
- **未嵌入字体**: 非标准字体未嵌入，可能导致显示或打印问题
- **Type3字体**: 可能存在兼容性问题
- **字体名称未知**: 字体信息缺失

#### 警告级别
- **子集嵌入**: 编辑时可能受限（信息提示，不是错误）

### 3. 字体统计

提供文档级别的字体统计信息：

- **字体总数**: 文档中使用的不同字体数量
- **完全嵌入数**: 完全嵌入的字体数量
- **子集嵌入数**: 子集嵌入的字体数量
- **未嵌入数**: 未嵌入的字体数量
- **问题字体数**: 有问题的字体数量

### 4. 标准字体识别

自动识别PDF标准14字体：

**Serif字体**:
- Times-Roman, Times-Bold, Times-Italic, Times-BoldItalic

**Sans-Serif字体**:
- Helvetica, Helvetica-Bold, Helvetica-Oblique, Helvetica-BoldOblique

**等宽字体**:
- Courier, Courier-Bold, Courier-Oblique, Courier-BoldOblique

**符号字体**:
- Symbol, ZapfDingbats

标准字体不需要嵌入，所有PDF阅读器都支持。

---

## 用户界面

### 字体标签页

在PDF检查器窗口中新增"字体"标签页，显示字体列表表格：

#### 表格列
| 列名 | 说明 | 宽度 |
|------|------|------|
| 状态 | 图标显示字体状态 | 50px |
| 字体名称 | 完整字体名称 | 200px |
| 类型 | 字体子类型 | 80px |
| 嵌入状态 | 嵌入状态文本 | 100px |
| 使用页面 | 使用页面列表 | 120px |
| 问题 | 问题描述 | 自适应 |

#### 状态图标
- ✓ (绿色) - 完全嵌入，无问题
- ◐ (黄色) - 子集嵌入
- ✗ (红色) - 未嵌入
- ⚠ (橙色) - 有问题

#### 标签页标题
- 无问题: "字体 (5)"
- 有问题: "字体 (5, ⚠2)"

---

## 数据模型

### FontInfo
单个字体的详细信息

```csharp
public class FontInfo
{
    // 基本信息
    public string FontName { get; set; }
    public string FontType { get; set; }
    public string FontSubtype { get; set; }
    
    // 嵌入信息
    public FontEmbeddingStatus EmbeddingStatus { get; set; }
    public bool IsSubset { get; set; }
    public long? EmbeddedSize { get; set; }
    
    // 使用信息
    public List<int> UsedPages { get; set; }
    public int UsageCount { get; set; }
    
    // 问题信息
    public bool HasIssues { get; set; }
    public List<string> Issues { get; set; }
    
    // 其他
    public string Encoding { get; set; }
    public bool IsStandardFont { get; set; }
}
```

### FontEmbeddingStatus
字体嵌入状态枚举

```csharp
public enum FontEmbeddingStatus
{
    FullyEmbedded,      // 完全嵌入
    SubsetEmbedded,     // 子集嵌入
    NotEmbedded         // 未嵌入
}
```

### DocumentFontInfo
文档级别的字体信息汇总

```csharp
public class DocumentFontInfo
{
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public int TotalPages { get; set; }
    public List<FontInfo> Fonts { get; set; }
    
    // 统计属性
    public int TotalFonts { get; }
    public int FullyEmbeddedCount { get; }
    public int SubsetEmbeddedCount { get; }
    public int NotEmbeddedCount { get; }
    public int ProblematicFontsCount { get; }
    public bool HasFontIssues { get; }
}
```

---

## 技术实现

### PdfFontInspectorService
字体检查服务类

#### 核心方法

**InspectFonts(string filePath)**
- 检查PDF文件的所有字体
- 返回DocumentFontInfo对象

**ExtractFontInfo(PdfDictionary fontDict, int pageNum)**
- 从字体字典中提取字体信息
- 返回FontInfo对象

**DetermineEmbeddingStatus(PdfDictionary fontDict, FontInfo fontInfo)**
- 确定字体嵌入状态
- 检查FontDescriptor和字体文件

**DetectFontIssues(FontInfo fontInfo)**
- 检测字体问题
- 填充Issues列表

#### 使用iText 7 API

```csharp
// 获取页面资源
PdfPage page = document.GetPage(pageNum);
PdfResources resources = page.GetResources();

// 获取字体资源
PdfDictionary fonts = resources.GetResource(PdfName.Font);

// 遍历字体
foreach (PdfName fontName in fonts.KeySet())
{
    PdfDictionary fontDict = fonts.GetAsDictionary(fontName);
    
    // 获取字体信息
    PdfName baseFontName = fontDict.GetAsName(PdfName.BaseFont);
    PdfName subtype = fontDict.GetAsName(PdfName.Subtype);
    
    // 检查嵌入状态
    PdfDictionary fontDescriptor = fontDict.GetAsDictionary(PdfName.FontDescriptor);
    bool hasEmbeddedFile = fontDescriptor.ContainsKey(PdfName.FontFile) ||
                          fontDescriptor.ContainsKey(PdfName.FontFile2) ||
                          fontDescriptor.ContainsKey(PdfName.FontFile3);
}
```

### 字体去重逻辑

使用字典去重，避免重复显示相同字体：

```csharp
var fontDict = new Dictionary<string, FontInfo>();

// 遍历所有页面
for (int pageNum = 1; pageNum <= totalPages; pageNum++)
{
    // 提取字体信息
    var fontInfo = ExtractFontInfo(fontDictionary, pageNum);
    string key = fontInfo.FontName;
    
    if (fontDict.ContainsKey(key))
    {
        // 字体已存在，添加页面
        fontDict[key].UsedPages.Add(pageNum);
        fontDict[key].UsageCount++;
    }
    else
    {
        // 新字体
        fontDict[key] = fontInfo;
    }
}
```

### 子集字体识别

子集字体通常以6个大写字母+加号开头：

```csharp
// 例如: ABCDEF+SimSun
if (fontName.Length > 7 && fontName[6] == '+')
{
    fontInfo.IsSubset = true;
    // 可选：移除子集前缀
    // string cleanName = fontName.Substring(7);
}
```

---

## 使用示例

### 1. 在检查器中查看字体

```csharp
// 打开PDF检查器
var inspector = new PdfInspectorControl();
inspector.LoadPdf("document.pdf");

// 切换到字体标签页
// 用户可以在UI中点击"字体"标签
```

### 2. 使用服务直接检查

```csharp
using WindowsFormsApp3.Services;

var service = new PdfFontInspectorService();
var fontInfo = service.InspectFonts("document.pdf");

Console.WriteLine($"总字体数: {fontInfo.TotalFonts}");
Console.WriteLine($"问题字体数: {fontInfo.ProblematicFontsCount}");

// 遍历所有字体
foreach (var font in fontInfo.Fonts)
{
    Console.WriteLine($"{font.StatusIcon} {font.FontName}");
    Console.WriteLine($"  类型: {font.FontSubtype}");
    Console.WriteLine($"  嵌入: {font.EmbeddingStatusText}");
    Console.WriteLine($"  页面: {font.UsedPagesText}");
    
    if (font.HasIssues)
    {
        foreach (var issue in font.Issues)
        {
            Console.WriteLine($"  ⚠ {issue}");
        }
    }
}

// 获取摘要
string summary = service.GetFontSummary(fontInfo);
Console.WriteLine(summary);
// 输出: 共 5 个字体，2 个有问题 (完全嵌入: 2, 子集: 2, 未嵌入: 1)
```

---

## 常见问题

### Q: 为什么有些字体显示为"子集嵌入"？
A: 子集嵌入只嵌入文档中实际使用的字符，可以大幅减小PDF文件大小。这在中文字体中很常见，因为完整的中文字体文件可能有几MB甚至几十MB。

### Q: 未嵌入的字体有什么问题？
A: 未嵌入的字体依赖于查看者或打印机上安装的字体。如果对方没有安装该字体，PDF可能显示不正确或使用替代字体。

### Q: 标准14字体需要嵌入吗？
A: 不需要。PDF规范要求所有PDF阅读器都必须支持这14种标准字体，因此不需要嵌入。

### Q: Type3字体有什么问题？
A: Type3字体是用PDF图形操作符定义的字体，可能存在兼容性问题，某些打印机或PDF处理软件可能不支持。

### Q: 如何修复字体问题？
A: 
1. **未嵌入字体**: 使用Adobe Acrobat或其他PDF编辑器重新保存PDF，选择嵌入所有字体
2. **Type3字体**: 尝试将文本转换为轮廓（outline）
3. **子集嵌入**: 如果需要编辑，可以重新嵌入完整字体

---

## 与PitStop Pro对比

| 功能 | PitStop Pro | 本实现 | 说明 |
|------|-------------|--------|------|
| 字体列表 | ✓ | ✓ | 完全支持 |
| 嵌入状态检测 | ✓ | ✓ | 完全支持 |
| 字体问题检测 | ✓ | ✓ | 基础检测 |
| 字体详细信息 | ✓ | ✓ | 基础信息 |
| 字体预览 | ✓ | ✗ | 待实现 |
| 字体替换 | ✓ | ✗ | 待实现 |
| 字体嵌入/取消嵌入 | ✓ | ✗ | 待实现 |
| 字符集查看 | ✓ | ✗ | 待实现 |

---

## 下一步计划

### v1.4.0 - 图像检查
- 图像列表和缩略图
- 分辨率检测
- 色彩空间检测
- 图像问题检测

### v1.5.0 - 颜色检查
- 颜色列表
- 色彩空间检测
- 专色检测
- 叠印设置检查

### v2.0.0 - 高级功能
- 字体预览
- 字体替换
- 对象选择和检查
- 页面框编辑

---

## 相关文件

- `src/WindowsFormsApp3/Models/FontInfo.cs` - 字体数据模型
- `src/WindowsFormsApp3/Services/PdfFontInspectorService.cs` - 字体检查服务
- `src/WindowsFormsApp3/Forms/Controls/PdfInspectorControl.cs` - 检查器控件（已更新）

---

## 更新日志

### v1.3.0 (2026-01-19)
- ✅ 实现字体信息检查
- ✅ 新增字体标签页
- ✅ 字体问题检测
- ✅ 字体统计功能
- ✅ 标准字体识别

---

**实现日期**: 2026-01-19  
**版本**: v1.3.0  
**状态**: ✅ 已完成并测试通过
