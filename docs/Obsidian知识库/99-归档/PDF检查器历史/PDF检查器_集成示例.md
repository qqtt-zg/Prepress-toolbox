# PDF检查器集成示例

## 编译错误已修复 ✅

所有命名空间冲突和基类方法问题已解决：
- ✅ 使用 `WinFormsPanel` 别名解决 `Panel` 命名空间冲突
- ✅ 修正为 `OnActivated()` 和 `OnDeactivated()` 方法
- ✅ 所有文件编译通过

## 快速集成到现有项目

### 方式1: 添加到主窗体的面板系统

如果你的项目使用了面板管理系统（如 `MainShellForm`），可以直接注册：

```csharp
// 在 MainShellForm 或面板管理器中注册
public void RegisterPanels()
{
    // ... 其他面板注册
    
    // 注册PDF检查器面板
    var pdfInspectorPanel = new PdfInspectorPanel();
    RegisterPanel(pdfInspectorPanel);
}
```

### 方式2: 作为独立窗口打开

在现有的PDF操作界面中添加"检查器"按钮：

```csharp
// 在 PdfOperationsPanel 或其他PDF相关界面中
private void ShowInspectorButton_Click(object sender, EventArgs e)
{
    // 创建检查器窗口
    var inspectorForm = new Form
    {
        Text = "PDF检查器 - " + Path.GetFileName(currentPdfPath),
        Size = new Size(1200, 800),
        StartPosition = FormStartPosition.CenterParent,
        Icon = this.FindForm()?.Icon
    };
    
    // 添加检查器面板
    var inspectorPanel = new PdfInspectorPanel
    {
        Dock = DockStyle.Fill
    };
    
    inspectorForm.Controls.Add(inspectorPanel);
    
    // 如果已经打开了PDF，自动加载
    if (!string.IsNullOrEmpty(currentPdfPath) && File.Exists(currentPdfPath))
    {
        inspectorForm.Shown += (s, args) =>
        {
            inspectorPanel.LoadPdf(currentPdfPath);
        };
    }
    
    inspectorForm.ShowDialog(this);
}
```

### 方式3: 集成到现有的PDF预览界面

在现有的PDF预览界面旁边添加检查器侧边栏：

```csharp
public class ExistingPdfViewerForm : Form
{
    private PdfPreviewControl _pdfPreview;
    private PdfInspectorControl _inspector;
    private SplitContainer _splitter;
    private bool _inspectorVisible = false;
    
    private void InitializeInspector()
    {
        // 创建分割容器
        _splitter = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 800,
            FixedPanel = FixedPanel.Panel2
        };
        
        // 左侧：现有的PDF预览
        _pdfPreview = new PdfPreviewControl { Dock = DockStyle.Fill };
        _splitter.Panel1.Controls.Add(_pdfPreview);
        
        // 右侧：检查器（初始隐藏）
        _inspector = new PdfInspectorControl 
        { 
            Dock = DockStyle.Fill,
            Width = 400
        };
        _splitter.Panel2.Controls.Add(_inspector);
        _splitter.Panel2Collapsed = true; // 初始隐藏
        
        // 监听页面选择事件
        _inspector.PageSelected += (s, pageNumber) =>
        {
            _pdfPreview.CurrentPageIndex = pageNumber - 1;
        };
        
        // 监听预览页面变化
        _pdfPreview.PageChanged += (s, e) =>
        {
            if (!_splitter.Panel2Collapsed)
            {
                _inspector.SwitchToPage(_pdfPreview.CurrentPageIndex + 1);
            }
        };
        
        this.Controls.Add(_splitter);
    }
    
    // 切换检查器显示/隐藏
    private void ToggleInspector()
    {
        _inspectorVisible = !_inspectorVisible;
        _splitter.Panel2Collapsed = !_inspectorVisible;
        
        if (_inspectorVisible && !string.IsNullOrEmpty(currentPdfPath))
        {
            _inspector.LoadPdf(currentPdfPath, _pdfPreview.CurrentPageIndex + 1);
        }
    }
    
    // 在工具栏添加切换按钮
    private void CreateInspectorToggleButton()
    {
        var toggleButton = new AntdUI.Button
        {
            Text = "检查器",
            IconSvg = "FileSearchOutlined",
            Type = TTypeMini.Default
        };
        toggleButton.Click += (s, e) => ToggleInspector();
        
        // 添加到工具栏
        toolbarPanel.Controls.Add(toggleButton);
    }
}
```

### 方式4: 在MaterialSelectFormModern中集成

在材料选择窗体中添加PDF检查功能：

```csharp
// 在 MaterialSelectFormModern.cs 中
private PdfInspectorControl _pdfInspector;

private void InitializePdfInspector()
{
    // 在PDF预览面板旁边添加检查器
    _pdfInspector = new PdfInspectorControl
    {
        Dock = DockStyle.Right,
        Width = 350,
        Visible = false // 初始隐藏
    };
    
    // 添加到预览容器
    pdfPreviewContainer.Controls.Add(_pdfInspector);
    
    // 监听页面选择
    _pdfInspector.PageSelected += (s, pageNumber) =>
    {
        if (pdfPreview != null)
        {
            pdfPreview.CurrentPageIndex = pageNumber - 1;
        }
    };
}

// 添加"显示检查器"按钮
private void CreateShowInspectorButton()
{
    var showInspectorBtn = new AntdUI.Button
    {
        Text = "检查器",
        Location = new Point(btnPreview.Right + 10, btnPreview.Top),
        Width = 80,
        Type = TTypeMini.Default
    };
    
    showInspectorBtn.Click += (s, e) =>
    {
        _pdfInspector.Visible = !_pdfInspector.Visible;
        
        if (_pdfInspector.Visible && !string.IsNullOrEmpty(CurrentFileName))
        {
            _pdfInspector.LoadPdf(CurrentFileName, pdfPreview.CurrentPageIndex + 1);
        }
        
        showInspectorBtn.Type = _pdfInspector.Visible ? TTypeMini.Primary : TTypeMini.Default;
    };
    
    this.Controls.Add(showInspectorBtn);
}
```

## 编程方式使用检查器服务

### 示例1: 在文件处理前检查PDF

```csharp
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Models;

public bool ValidatePdfBeforeProcessing(string pdfPath)
{
    var inspector = new PdfInspectorService();
    var info = inspector.InspectPdf(pdfPath);
    
    // 检查是否有错误级别的问题
    var errors = info.Issues.Where(i => i.Severity == IssueSeverity.Error).ToList();
    
    if (errors.Any())
    {
        string errorMsg = "PDF文件存在以下错误:\n" + 
                         string.Join("\n", errors.Select(e => e.Description));
        MessageBox.Show(errorMsg, "PDF验证失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }
    
    // 检查出血
    var firstPage = info.AllPageBoxes.FirstOrDefault();
    if (firstPage != null)
    {
        var bleedInfo = inspector.GetBleedInfo(firstPage);
        if (!bleedInfo.IsUniform || bleedInfo.UniformValue < 2.5)
        {
            var result = MessageBox.Show(
                $"PDF出血不足（当前: {bleedInfo}），建议至少3mm。是否继续？",
                "出血警告",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );
            
            if (result == DialogResult.No)
                return false;
        }
    }
    
    return true;
}
```

### 示例2: 批量检查文件夹中的PDF

```csharp
public void BatchInspectPdfs(string folderPath)
{
    var inspector = new PdfInspectorService();
    var results = new List<(string FileName, int ErrorCount, int WarningCount)>();
    
    foreach (var pdfFile in Directory.GetFiles(folderPath, "*.pdf"))
    {
        var info = inspector.InspectPdf(pdfFile);
        
        int errorCount = info.Issues.Count(i => i.Severity == IssueSeverity.Error);
        int warningCount = info.Issues.Count(i => i.Severity == IssueSeverity.Warning);
        
        results.Add((Path.GetFileName(pdfFile), errorCount, warningCount));
    }
    
    // 显示结果
    var reportForm = new Form { Text = "批量检查报告", Size = new Size(600, 400) };
    var dataGrid = new DataGridView 
    { 
        Dock = DockStyle.Fill,
        DataSource = results.Select(r => new 
        {
            文件名 = r.FileName,
            错误 = r.ErrorCount,
            警告 = r.WarningCount,
            状态 = r.ErrorCount == 0 ? "✓ 正常" : "✗ 有问题"
        }).ToList()
    };
    
    reportForm.Controls.Add(dataGrid);
    reportForm.ShowDialog();
}
```

### 示例3: 获取PDF成品尺寸用于排版计算

```csharp
public (double width, double height) GetTrimSize(string pdfPath)
{
    var inspector = new PdfInspectorService();
    var info = inspector.InspectPdf(pdfPath);
    
    if (info.AllPageBoxes.Any())
    {
        var firstPage = info.AllPageBoxes[0];
        var trimBox = firstPage.TrimBox;
        
        if (trimBox.IsDefined)
        {
            return (trimBox.WidthMm, trimBox.HeightMm);
        }
        
        // 如果没有TrimBox，使用CropBox
        var cropBox = firstPage.CropBox;
        return (cropBox.WidthMm, cropBox.HeightMm);
    }
    
    return (0, 0);
}

// 使用示例
var (width, height) = GetTrimSize("test.pdf");
Console.WriteLine($"成品尺寸: {width} × {height} mm");
```

## 自定义和扩展

### 自定义检测规则

```csharp
// 扩展 PdfInspectorService
public class CustomPdfInspectorService : PdfInspectorService
{
    // 添加自定义检测：检查是否符合印刷厂标准
    public List<PageBoxIssue> CheckPrintShopStandards(PdfInspectorInfo info)
    {
        var issues = new List<PageBoxIssue>();
        
        foreach (var page in info.AllPageBoxes)
        {
            // 检查出血是否为3mm
            var bleedInfo = GetBleedInfo(page);
            if (bleedInfo.IsUniform && Math.Abs(bleedInfo.UniformValue - 3.0) > 0.5)
            {
                issues.Add(new PageBoxIssue
                {
                    PageNumber = page.PageNumber,
                    Type = IssueType.IncorrectOrder,
                    Severity = IssueSeverity.Warning,
                    BoxType = "BleedBox",
                    Description = $"第{page.PageNumber}页: 出血值({bleedInfo.UniformValue}mm)不符合标准(3mm)"
                });
            }
            
            // 检查是否为标准尺寸
            var trimBox = page.TrimBox;
            bool isStandardSize = IsStandardSize(trimBox.WidthMm, trimBox.HeightMm);
            
            if (!isStandardSize)
            {
                issues.Add(new PageBoxIssue
                {
                    PageNumber = page.PageNumber,
                    Type = IssueType.InconsistentSize,
                    Severity = IssueSeverity.Info,
                    BoxType = "TrimBox",
                    Description = $"第{page.PageNumber}页: 非标准尺寸({trimBox.WidthMm}×{trimBox.HeightMm}mm)"
                });
            }
        }
        
        return issues;
    }
    
    private bool IsStandardSize(double width, double height)
    {
        // 常见标准尺寸（允许±1mm误差）
        var standardSizes = new[]
        {
            (210.0, 297.0), // A4
            (148.0, 210.0), // A5
            (420.0, 297.0), // A3
            (90.0, 54.0),   // 名片
            (216.0, 279.0)  // Letter
        };
        
        foreach (var (w, h) in standardSizes)
        {
            if ((Math.Abs(width - w) < 1 && Math.Abs(height - h) < 1) ||
                (Math.Abs(width - h) < 1 && Math.Abs(height - w) < 1))
            {
                return true;
            }
        }
        
        return false;
    }
}
```

### 自定义UI主题

```csharp
// 自定义检查器控件的颜色主题
public class ThemedPdfInspectorControl : PdfInspectorControl
{
    public void ApplyDarkTheme()
    {
        this.BackColor = Color.FromArgb(30, 30, 30);
        // 更多主题设置...
    }
    
    public void ApplyLightTheme()
    {
        this.BackColor = Color.White;
        // 更多主题设置...
    }
}
```

## 性能优化建议

### 异步加载大文件

```csharp
private async Task LoadPdfAsync(string filePath)
{
    // 显示加载提示
    var loading = AntdUI.Message.loading(this, "正在检查PDF...");
    
    try
    {
        // 异步执行检查
        var info = await Task.Run(() =>
        {
            var service = new PdfInspectorService();
            return service.InspectPdf(filePath);
        });
        
        // 更新UI（在UI线程）
        this.Invoke(new Action(() =>
        {
            inspector.LoadPdf(filePath);
            UpdateInspectorDisplay(info);
        }));
        
        loading.Close();
        
        // 显示结果摘要
        string summary = info.Issues.Count == 0 
            ? "✓ 未发现问题" 
            : $"发现 {info.Issues.Count} 个问题";
        AntdUI.Message.success(this, summary);
    }
    catch (Exception ex)
    {
        loading.Close();
        AntdUI.Message.error(this, "检查失败: " + ex.Message);
        LogHelper.Error($"PDF检查失败: {ex.Message}");
    }
}
```

### 缓存检查结果

```csharp
private Dictionary<string, PdfInspectorInfo> _inspectorCache = 
    new Dictionary<string, PdfInspectorInfo>();

public PdfInspectorInfo GetInspectorInfo(string filePath)
{
    // 检查缓存
    if (_inspectorCache.ContainsKey(filePath))
    {
        var cachedInfo = _inspectorCache[filePath];
        var fileInfo = new FileInfo(filePath);
        
        // 验证文件是否被修改
        if (fileInfo.LastWriteTime == cachedInfo.LastModified)
        {
            return cachedInfo;
        }
    }
    
    // 重新检查
    var service = new PdfInspectorService();
    var info = service.InspectPdf(filePath);
    info.LastModified = new FileInfo(filePath).LastWriteTime;
    
    // 更新缓存
    _inspectorCache[filePath] = info;
    
    return info;
}
```

## 故障排除

### 问题1: 检查器显示空白
**原因**: PDF文件路径无效或文件损坏  
**解决**: 检查文件是否存在，查看日志输出

### 问题2: 页面框显示"未定义"
**原因**: PDF只定义了MediaBox，其他页面框使用默认值  
**解决**: 这是正常的，PDF规范允许只定义MediaBox

### 问题3: 出血计算不准确
**原因**: TrimBox或BleedBox未正确定义  
**解决**: 检查PDF是否正确设置了页面框

### 问题4: 性能慢
**原因**: 大文件同步加载  
**解决**: 使用异步加载方法

## 下一步

- ✅ 基础功能已完成
- ⏳ 添加页面框可视化（在预览上叠加显示）
- ⏳ 添加页面框编辑功能
- ⏳ 添加导出报告功能

需要帮助实现这些扩展功能吗？
