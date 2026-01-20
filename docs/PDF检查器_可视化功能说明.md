# PDF检查器 - 页面框可视化功能

## 功能概述

页面框可视化功能在PDF预览上叠加显示不同颜色的页面框边界，类似Enfocus PitStop Pro的Inspector可视化功能。

## 新增组件

### 1. PdfBoxOverlay.cs
**页面框可视化叠加层**

在PDF预览上绘制半透明的页面框边界，每种页面框使用不同颜色：

| 页面框 | 颜色 | 说明 |
|--------|------|------|
| MediaBox | 红色 | PDF页面的物理尺寸 |
| CropBox | 蓝色 | 页面显示和打印的区域 |
| TrimBox | 绿色 | 成品的最终尺寸 |
| BleedBox | 黄色 | 包含出血的区域 |
| ArtBox | 灰色 | 有意义内容的区域 |

**功能特性**：
- ✅ 半透明边框绘制
- ✅ 页面框名称标签
- ✅ 尺寸标注（毫米）
- ✅ 独立显示/隐藏控制
- ✅ 支持缩放和偏移

### 2. PdfPreviewWithInspector.cs
**带检查器功能的PDF预览控件**

集成了PDF预览和页面框叠加层的复合控件。

**功能特性**：
- ✅ 自动检查PDF
- ✅ 页面切换同步
- ✅ 叠加层开关
- ✅ 独立页面框显示控制

### 3. 集成到PdfOperationsPanel
**PDF操作面板增强**

在现有的PDF操作面板中添加了检查器功能。

**新增功能**：
- ✅ 检查器按钮（工具栏）
- ✅ 侧边栏检查器面板
- ✅ 预览与检查器同步
- ✅ 可折叠/展开

## 使用方法

### 在MainShellForm中使用

检查器已集成到PDF操作面板，无需额外配置：

```csharp
// 在MainShellForm.cs中，PDF操作面板已自动包含检查器功能
// 用户只需点击工具栏的"检查器"按钮即可显示/隐藏
```

### 操作步骤

1. **打开PDF操作面板**
   - 在主界面左侧导航菜单点击"PDF操作"

2. **打开PDF文件**
   - 点击工具栏的"打开PDF"按钮
   - 选择要检查的PDF文件

3. **显示检查器**
   - 点击工具栏的"检查器"按钮
   - 右侧会滑出检查器面板

4. **查看页面框信息**
   - 检查器显示当前页面的所有页面框参数
   - 包括尺寸、位置、出血等信息

5. **切换页面**
   - 在PDF预览中切换页面，检查器自动同步
   - 或在检查器的"所有页面"标签中点击页面跳转

6. **查看问题**
   - 切换到"问题"标签查看检测到的问题
   - 点击问题可跳转到对应页面

## 界面布局

```
┌─────────────────────────────────────────────────────────────┐
│  [打开PDF] [保存] │ 印刷标记: [裁切标记] [套准标记] │ [检查器] │
├─────────────────────────────────────┬───────────────────────┤
│                                     │                       │
│                                     │  ┌─────────────────┐  │
│                                     │  │ 当前页面        │  │
│         PDF 预览区域                 │  ├─────────────────┤  │
│                                     │  │ 页码: 1 / 10    │  │
│                                     │  │ 旋转: 0°        │  │
│                                     │  │                 │  │
│                                     │  │ MediaBox        │  │
│                                     │  │ ✓ 已定义        │  │
│                                     │  │ 尺寸: 210×297mm │  │
│                                     │  │                 │  │
│                                     │  │ CropBox         │  │
│                                     │  │ ✓ 已定义        │  │
│                                     │  │ ...             │  │
│                                     │  └─────────────────┘  │
├─────────────────────────────────────┴───────────────────────┤
│  状态: 已加载 test.pdf | 页面尺寸: 210.0 × 297.0 mm         │
└─────────────────────────────────────────────────────────────┘
```

## 页面框可视化（待实现）

### 计划功能

在PDF预览上叠加显示页面框边界：

```csharp
// 未来版本将支持
var preview = new PdfPreviewWithInspector();
preview.OverlayEnabled = true;  // 启用叠加层
preview.ShowMediaBox = true;    // 显示MediaBox
preview.ShowCropBox = true;     // 显示CropBox
preview.ShowTrimBox = true;     // 显示TrimBox
preview.ShowBleedBox = true;    // 显示BleedBox
```

### 可视化效果

```
┌─────────────────────────────────────┐
│ MediaBox (红色)                      │
│  ┌───────────────────────────────┐  │
│  │ CropBox (蓝色)                 │  │
│  │  ┌─────────────────────────┐  │  │
│  │  │ BleedBox (黄色)          │  │  │
│  │  │  ┌───────────────────┐  │  │  │
│  │  │  │ TrimBox (绿色)     │  │  │  │
│  │  │  │                   │  │  │  │
│  │  │  │   PDF 内容        │  │  │  │
│  │  │  │                   │  │  │  │
│  │  │  └───────────────────┘  │  │  │
│  │  │  210 × 297 mm          │  │  │
│  │  └─────────────────────────┘  │  │
│  └───────────────────────────────┘  │
└─────────────────────────────────────┘
```

## 快捷键（计划）

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+I` | 显示/隐藏检查器 |
| `Ctrl+B` | 切换页面框叠加层 |
| `F5` | 刷新检查器 |

## 技术实现

### 架构

```
PdfOperationsPanel (主面板)
├── SplitContainer (分割容器)
│   ├── Panel1 (左侧 - PDF预览)
│   │   ├── Toolbar (工具栏)
│   │   └── CefPdfPreviewControl (PDF预览)
│   └── Panel2 (右侧 - 检查器)
│       └── PdfInspectorControl (检查器控件)
```

### 同步机制

1. **预览 → 检查器**
   ```csharp
   _pdfPreview.PageChanged += (s, e) => {
       _inspector.SwitchToPage(e.CurrentPage);
   };
   ```

2. **检查器 → 预览**
   ```csharp
   _inspector.PageSelected += (s, pageNumber) => {
       _pdfPreview.GoToPageAsync(pageNumber);
   };
   ```

### 性能优化

- ✅ 检查器按需加载（点击按钮时才加载）
- ✅ 页面框信息缓存
- ✅ 异步PDF检查
- ✅ 延迟初始化

## 与PitStop Pro对比

| 功能 | PitStop Pro | 本实现 | 说明 |
|------|-------------|--------|------|
| 检查器面板 | ✓ | ✓ | 完全实现 |
| 页面框参数显示 | ✓ | ✓ | 完全实现 |
| 问题检测 | ✓ | ✓ | 基础实现 |
| 页面框可视化 | ✓ | ⏳ | 代码已完成，待集成 |
| 页面框编辑 | ✓ | ✗ | 待实现 |
| 实时预览 | ✓ | ✓ | 完全实现 |

## 常见问题

### Q: 如何显示检查器？
A: 打开PDF文件后，点击工具栏的"检查器"按钮。

### Q: 检查器可以独立使用吗？
A: 可以。检查器是独立的控件，可以在任何窗体中使用。

### Q: 如何隐藏检查器？
A: 再次点击"检查器"按钮，或拖动分割条到最右侧。

### Q: 页面框可视化在哪里？
A: 页面框可视化代码已完成（PdfBoxOverlay.cs），但尚未集成到CefPdfPreviewControl中。需要在未来版本中实现。

### Q: 检查器会影响性能吗？
A: 不会。检查器只在显示时才加载和检查PDF，隐藏时不占用资源。

## 扩展开发

### 添加自定义检测规则

```csharp
// 在PdfInspectorService中添加
public List<PageBoxIssue> CheckCustomRules(PdfInspectorInfo info)
{
    var issues = new List<PageBoxIssue>();
    
    // 自定义检测逻辑
    foreach (var page in info.AllPageBoxes)
    {
        // 检查自定义规则
        if (/* 条件 */)
        {
            issues.Add(new PageBoxIssue
            {
                PageNumber = page.PageNumber,
                Type = IssueType.Custom,
                Severity = IssueSeverity.Warning,
                Description = "自定义问题描述"
            });
        }
    }
    
    return issues;
}
```

### 自定义检查器UI

```csharp
// 继承PdfInspectorControl
public class CustomPdfInspectorControl : PdfInspectorControl
{
    protected override void CreateCustomTab()
    {
        // 添加自定义标签页
    }
}
```

## 更新日志

### v1.1.0 (2026-01-19)
- ✅ 添加PdfBoxOverlay页面框叠加层
- ✅ 添加PdfPreviewWithInspector复合控件
- ✅ 集成检查器到PdfOperationsPanel
- ✅ 实现预览与检查器双向同步
- ✅ 添加检查器显示/隐藏切换

### v1.0.0 (2026-01-19)
- ✅ 基础检查器功能
- ✅ 页面框参数显示
- ✅ 问题检测

## 下一步计划

1. **页面框可视化集成** - 将PdfBoxOverlay集成到CefPdfPreviewControl
2. **页面框编辑** - 支持修改页面框参数
3. **批量操作** - 支持批量修改多个页面
4. **导出报告** - 生成PDF检查报告

---

**版本**: v1.1.0  
**更新日期**: 2026-01-19  
**状态**: ✅ 检查器已集成，可视化代码已完成
