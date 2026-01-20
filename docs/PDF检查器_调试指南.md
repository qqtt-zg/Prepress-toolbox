# PDF检查器 - 调试指南

## 问题：字体标签页不显示

**症状**: 在检查器窗口中只看到"页面框"标签页，没有"字体"标签页

---

## 调试步骤

### 1. 查看日志文件

日志文件位置：
```
app_2026-01-17.log  (或当前日期的日志文件)
```

### 2. 查找关键日志

运行程序并打开检查器后，在日志文件中搜索以下内容：

#### 标签页创建日志
```
[PdfInspectorControl] 开始创建标签页
[PdfInspectorControl] 页面框标签页创建完成: 页面框
[PdfInspectorControl] 开始创建字体标签页
[PdfInspectorControl] 字体标签页创建完成: Text=字体, HasControls=True
[PdfInspectorControl] 主标签页数量: Pages=2, Controls=2
```

**预期结果**:
- Pages=2 (两个标签页)
- Controls=2 (两个控件)

#### 字体检查日志
```
字体检查完成: xxx.pdf, 总字体数: X, 问题字体数: X
```

### 3. 可能的问题和解决方案

#### 问题1: 标签页创建失败
**日志特征**:
```
[PdfInspectorControl] 创建字体标签页失败: ...
```

**解决方案**: 查看具体错误信息

#### 问题2: 标签页未添加
**日志特征**:
```
[PdfInspectorControl] 主标签页数量: Pages=1, Controls=1
```

**解决方案**: 检查AntdUI版本和API

#### 问题3: 字体检查失败
**日志特征**:
```
字体检查失败: xxx.pdf, 错误: ...
```

**解决方案**: 检查PDF文件和iText库

---

## 手动测试

### 测试代码

在LoadPdf方法中添加断点或日志：

```csharp
public void LoadPdf(string filePath, int currentPage = 1)
{
    try
    {
        LogHelper.Debug($"[PdfInspectorControl] LoadPdf开始: {filePath}");
        
        _currentInfo = _inspectorService.InspectPdf(filePath, currentPage);
        LogHelper.Debug($"[PdfInspectorControl] 页面框检查完成");
        
        _fontInfo = _fontInspectorService.InspectFonts(filePath);
        LogHelper.Debug($"[PdfInspectorControl] 字体检查完成: {_fontInfo?.TotalFonts ?? 0} 个字体");

        if (_currentInfo != null)
        {
            UpdateCurrentPageDisplay();
            UpdateAllPagesDisplay();
            UpdateIssuesDisplay();
            UpdateFontsDisplay();
            LogHelper.Debug($"[PdfInspectorControl] 所有显示更新完成");
            
            UpdateIssuesBadge();
            UpdateFontsBadge();
        }
    }
    catch (Exception ex)
    {
        LogHelper.Error($"加载PDF检查器失败: {ex.Message}", ex);
    }
}
```

---

## 临时解决方案

如果标签页仍然不显示，可以尝试以下方法：

### 方案1: 简化标签页结构

暂时移除嵌套结构，将所有标签页平铺：

```csharp
private void CreateContentPanel()
{
    _contentPanel = new WinFormsPanel
    {
        Dock = DockStyle.Fill,
        BackColor = Color.White,
        Padding = new Padding(10)
    };

    _mainTabs = new AntdUI.Tabs
    {
        Dock = DockStyle.Fill,
        Gap = 10
    };

    // 创建所有标签页（不嵌套）
    CreateCurrentPageTab();
    CreateAllPagesTab();
    CreateIssuesTab();
    CreateFontsTab();

    // 添加到主Tabs
    _mainTabs.Pages.Add(_currentPageTabPage);
    _mainTabs.Pages.Add(_allPagesTabPage);
    _mainTabs.Pages.Add(_issuesTabPage);
    _mainTabs.Pages.Add(_fontsTabPage);
    
    _mainTabs.Controls.Add(_currentPageTabPage);
    _mainTabs.Controls.Add(_allPagesTabPage);
    _mainTabs.Controls.Add(_issuesTabPage);
    _mainTabs.Controls.Add(_fontsTabPage);

    _contentPanel.Controls.Add(_mainTabs);
    this.Controls.Add(_contentPanel);
}
```

### 方案2: 检查AntdUI版本

确保使用的AntdUI版本支持嵌套Tabs。

---

## 联系信息

如果问题仍然存在，请提供：

1. **日志文件内容** (搜索 "PdfInspectorControl")
2. **AntdUI版本**
3. **PDF文件信息**
4. **截图**

---

**创建日期**: 2026-01-19  
**版本**: v1.3.2-debug
