# PDF检查器 - 快速开始

## 5分钟上手指南

### 1. 最简单的使用方式

在现有窗体中添加检查器面板：

```csharp
using WindowsFormsApp3.Forms.Panels;

public class MyForm : Form
{
    public MyForm()
    {
        // 创建并添加检查器面板
        var inspectorPanel = new PdfInspectorPanel
        {
            Dock = DockStyle.Fill
        };
        
        this.Controls.Add(inspectorPanel);
    }
}
```

就这么简单！面板包含：
- PDF预览（左侧）
- 检查器（右侧）
- 工具栏（打开、翻页）

### 2. 只使用检查器控件

如果你已经有PDF预览，只需要检查器：

```csharp
using WindowsFormsApp3.Forms.Controls;

// 创建检查器
var inspector = new PdfInspectorControl
{
    Dock = DockStyle.Right,
    Width = 400
};

// 加载PDF
inspector.LoadPdf(@"C:\test.pdf", currentPage: 1);

// 添加到窗体
this.Controls.Add(inspector);
```

### 3. 编程方式检查PDF

不需要UI，只想获取数据：

```csharp
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Models;

// 创建服务
var service = new PdfInspectorService();

// 检查PDF
var info = service.InspectPdf(@"C:\test.pdf");

// 输出结果
Console.WriteLine($"文件: {info.FileName}");
Console.WriteLine($"页数: {info.TotalPages}");
Console.WriteLine($"问题: {info.Issues.Count}个");

// 查看第一页的TrimBox
var firstPage = info.AllPageBoxes[0];
Console.WriteLine($"TrimBox: {firstPage.TrimBox.WidthMm} × {firstPage.TrimBox.HeightMm} mm");
```

## 常见使用场景

### 场景1: 检查PDF是否有出血

```csharp
var service = new PdfInspectorService();
var info = service.InspectPdf(pdfPath);

foreach (var page in info.AllPageBoxes)
{
    var bleedInfo = service.GetBleedInfo(page);
    
    if (bleedInfo.IsUniform && bleedInfo.UniformValue >= 3.0)
    {
        Console.WriteLine($"第{page.PageNumber}页: 出血正常 ({bleedInfo.UniformValue}mm)");
    }
    else
    {
        Console.WriteLine($"第{page.PageNumber}页: 出血不足或不均匀");
    }
}
```

### 场景2: 检查所有页面尺寸是否一致

```csharp
var service = new PdfInspectorService();
var info = service.InspectPdf(pdfPath);

var sizeIssues = info.Issues
    .Where(i => i.Type == IssueType.InconsistentSize)
    .ToList();

if (sizeIssues.Any())
{
    Console.WriteLine("警告: 文档包含不同尺寸的页面！");
}
else
{
    Console.WriteLine("所有页面尺寸一致");
}
```

### 场景3: 获取成品尺寸（TrimBox）

```csharp
var service = new PdfInspectorService();
var info = service.InspectPdf(pdfPath);

var firstPage = info.AllPageBoxes[0];
var trimBox = firstPage.TrimBox;

Console.WriteLine($"成品尺寸: {trimBox.WidthMm} × {trimBox.HeightMm} mm");
Console.WriteLine($"成品尺寸: {trimBox.WidthInch} × {trimBox.HeightInch} in");
```

### 场景4: 检测并报告所有问题

```csharp
var service = new PdfInspectorService();
var info = service.InspectPdf(pdfPath);

if (info.Issues.Count == 0)
{
    Console.WriteLine("✓ 未发现问题");
}
else
{
    Console.WriteLine($"发现 {info.Issues.Count} 个问题:\n");
    
    foreach (var issue in info.Issues)
    {
        string icon = issue.Severity switch
        {
            IssueSeverity.Error => "❌",
            IssueSeverity.Warning => "⚠",
            IssueSeverity.Info => "ℹ",
            _ => ""
        };
        
        Console.WriteLine($"{icon} {issue.Description}");
    }
}
```

## UI集成示例

### 在现有的PDF操作面板中添加检查器按钮

```csharp
// 在工具栏添加"检查器"按钮
var inspectorButton = new AntdUI.Button
{
    Text = "检查器",
    IconSvg = "FileSearchOutlined"
};

inspectorButton.Click += (s, e) =>
{
    // 创建检查器窗口
    var inspectorForm = new Form
    {
        Text = "PDF检查器",
        Size = new Size(1200, 800),
        StartPosition = FormStartPosition.CenterParent
    };
    
    var inspectorPanel = new PdfInspectorPanel
    {
        Dock = DockStyle.Fill
    };
    
    inspectorForm.Controls.Add(inspectorPanel);
    
    // 如果已经打开了PDF，自动加载
    if (!string.IsNullOrEmpty(currentPdfPath))
    {
        inspectorPanel.LoadPdf(currentPdfPath);
    }
    
    inspectorForm.ShowDialog();
};
```

### 在侧边栏显示检查器

```csharp
// 创建可折叠的侧边栏
var sidePanel = new Panel
{
    Dock = DockStyle.Right,
    Width = 400,
    BorderStyle = BorderStyle.FixedSingle
};

var inspector = new PdfInspectorControl
{
    Dock = DockStyle.Fill
};

sidePanel.Controls.Add(inspector);
this.Controls.Add(sidePanel);

// 切换显示/隐藏
void ToggleInspector()
{
    sidePanel.Visible = !sidePanel.Visible;
}
```

## 自定义和扩展

### 监听页面选择事件

```csharp
inspector.PageSelected += (sender, pageNumber) =>
{
    Console.WriteLine($"用户选择了第 {pageNumber} 页");
    
    // 同步PDF预览
    pdfPreview.CurrentPageIndex = pageNumber - 1;
    
    // 或执行其他操作
    LoadPageDetails(pageNumber);
};
```

### 自定义单位显示

```csharp
// 检查器控件会自动处理单位切换
// 但你也可以在代码中使用不同单位

var box = pageInfo.TrimBox;

// 获取不同单位的尺寸
string mmSize = box.GetFormattedSize(MeasurementUnit.Millimeter);
string inchSize = box.GetFormattedSize(MeasurementUnit.Inch);
string ptSize = box.GetFormattedSize(MeasurementUnit.Point);

Console.WriteLine($"毫米: {mmSize}");
Console.WriteLine($"英寸: {inchSize}");
Console.WriteLine($"点: {ptSize}");
```

### 批量检查多个PDF

```csharp
var service = new PdfInspectorService();
var results = new List<(string FileName, int IssueCount)>();

foreach (var pdfFile in Directory.GetFiles(@"C:\PDFs", "*.pdf"))
{
    var info = service.InspectPdf(pdfFile);
    results.Add((Path.GetFileName(pdfFile), info.Issues.Count));
}

// 输出汇总
Console.WriteLine("批量检查结果:");
foreach (var result in results.OrderByDescending(r => r.IssueCount))
{
    Console.WriteLine($"{result.FileName}: {result.IssueCount}个问题");
}
```

## 性能提示

1. **大文件处理**：检查器会读取所有页面的页面框信息，对于大文件（>100页）可能需要几秒钟
2. **缓存结果**：如果需要多次访问同一个PDF的信息，建议缓存`PdfInspectorInfo`对象
3. **异步加载**：在UI中使用时，建议异步加载以避免阻塞界面

```csharp
// 异步加载示例
private async void LoadPdfAsync(string filePath)
{
    var loadingMessage = AntdUI.Message.loading(this, "正在检查PDF...");
    
    try
    {
        var info = await Task.Run(() => 
        {
            var service = new PdfInspectorService();
            return service.InspectPdf(filePath);
        });
        
        // 更新UI
        inspector.LoadPdf(filePath);
        
        loadingMessage.Close();
        AntdUI.Message.success(this, $"检查完成，发现{info.Issues.Count}个问题");
    }
    catch (Exception ex)
    {
        loadingMessage.Close();
        AntdUI.Message.error(this, "检查失败: " + ex.Message);
    }
}
```

## 下一步

- 查看 [PDF检查器功能说明.md](./PDF检查器功能说明.md) 了解完整功能
- 查看 `PdfInspectorService.cs` 了解API详情
- 查看 `PdfInspectorControl.cs` 了解UI实现

## 需要帮助？

如果遇到问题：
1. 检查PDF文件是否损坏
2. 确认iText 7库已正确引用
3. 查看日志输出（LogHelper）
4. 参考示例代码
