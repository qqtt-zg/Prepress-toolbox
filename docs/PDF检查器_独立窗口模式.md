# PDF检查器 - 独立窗口模式

## 概述

PDF检查器现在采用独立窗口模式，完全模仿**Enfocus PitStop Pro**的Inspector窗口行为。

## 设计理念

### 为什么使用独立窗口？

1. **符合PitStop Pro习惯** - 用户熟悉的工作方式
2. **灵活的窗口管理** - 可以自由移动和调整大小
3. **多显示器支持** - 可以放在第二个显示器上
4. **不占用主界面空间** - PDF预览区域更大
5. **独立的生命周期** - 关闭窗口不影响主程序

## 功能特性

### 1. 独立窗口

✅ **窗口特性**
- 独立的浮动窗口
- 可自由移动和调整大小
- 不在任务栏显示（ShowInTaskbar = false）
- 关闭时隐藏而不是销毁
- 记住窗口位置和尺寸

✅ **智能定位**
- 首次打开：自动定位到主窗口右侧
- 再次打开：恢复上次的位置和尺寸
- 多显示器：自动检测并保持在可见区域

### 2. 与主窗口同步

✅ **双向同步**
- PDF预览切换页面 → 检查器自动更新
- 检查器选择页面 → PDF预览自动跳转

✅ **实时更新**
- 打开新PDF → 检查器自动加载
- 切换页面 → 检查器立即同步

### 3. 窗口管理

✅ **位置记忆**
- 自动保存窗口位置
- 自动保存窗口尺寸
- 下次打开恢复到上次位置

✅ **智能行为**
- 关闭窗口 → 隐藏（不销毁）
- 再次点击"检查器"按钮 → 显示窗口
- 窗口始终在主窗口之上

## 使用方法

### 基本操作

#### 1. 打开检查器
```
PDF操作面板 → 打开PDF文件 → 点击"检查器"按钮
```

#### 2. 查看页面框信息
```
检查器窗口 → 当前页面/所有页面/问题 标签页
```

#### 3. 跳转到指定页面
```
检查器窗口 → 所有页面标签 → 点击页面行
或
检查器窗口 → 问题标签 → 点击问题行
```

#### 4. 关闭检查器
```
点击窗口关闭按钮 → 窗口隐藏（不销毁）
```

#### 5. 再次显示检查器
```
点击"检查器"按钮 → 窗口重新显示
```

### 高级操作

#### 多显示器使用
```
1. 将检查器窗口拖到第二个显示器
2. 调整到合适的大小
3. 位置会自动保存
4. 下次打开自动恢复到第二个显示器
```

#### 快速切换页面
```
方式1: 在PDF预览中切换 → 检查器自动同步
方式2: 在检查器中点击 → PDF预览自动跳转
```

## 界面布局

### 主窗口 + 检查器窗口

```
┌─────────────────────────────────────┐  ┌──────────────────┐
│ MainShellForm                       │  │ PDF 检查器       │
│ ┌─────────────────────────────────┐ │  │ ┌──────────────┐ │
│ │ PDF操作面板                      │ │  │ │ 当前页面     │ │
│ │ ┌─────────────────────────────┐ │ │  │ ├──────────────┤ │
│ │ │ [打开] [保存] │ [检查器] ◄──┼─┼──┼─┤ 页码: 1/10   │ │
│ │ ├─────────────────────────────┤ │ │  │ │ 旋转: 0°     │ │
│ │ │                             │ │ │  │ │              │ │
│ │ │                             │ │ │  │ │ MediaBox     │ │
│ │ │      PDF 预览区域            │ │ │  │ │ ✓ 已定义     │ │
│ │ │                             │ │ │  │ │ 210×297mm    │ │
│ │ │                             │ │ │  │ │              │ │
│ │ │                             │ │ │  │ │ CropBox      │ │
│ │ │                             │ │ │  │ │ ✓ 已定义     │ │
│ │ └─────────────────────────────┘ │ │  │ │ ...          │ │
│ │ 状态: 已加载 test.pdf           │ │  │ └──────────────┘ │
│ └─────────────────────────────────┘ │  └──────────────────┘
└─────────────────────────────────────┘
```

## 技术实现

### 核心类

#### PdfInspectorForm.cs
独立的检查器窗口类

```csharp
public class PdfInspectorForm : Form
{
    private PdfInspectorControl _inspectorControl;
    
    // 事件：页面选择
    public event EventHandler<int> PageSelected;
    
    // 方法
    public void LoadPdf(string filePath, int currentPage);
    public void SwitchToPage(int pageNumber);
    public void RefreshInspector();
    public void ToggleVisibility();
}
```

**关键特性**：
- `ShowInTaskbar = false` - 不在任务栏显示
- `FormClosing` - 关闭时隐藏而不是销毁
- 自动保存/恢复窗口位置
- 智能定位到主窗口右侧

### 窗口位置管理

#### 保存位置
```csharp
private void SaveWindowPosition()
{
    AppSettings.Set("PdfInspector_WindowX", this.Location.X);
    AppSettings.Set("PdfInspector_WindowY", this.Location.Y);
    AppSettings.Set("PdfInspector_WindowWidth", this.Width);
    AppSettings.Set("PdfInspector_WindowHeight", this.Height);
}
```

#### 恢复位置
```csharp
private void RestoreWindowPosition()
{
    var x = AppSettings.Get("PdfInspector_WindowX");
    var y = AppSettings.Get("PdfInspector_WindowY");
    
    if (x != null && y != null)
    {
        var location = new Point(Convert.ToInt32(x), Convert.ToInt32(y));
        
        // 确保窗口在屏幕范围内
        if (IsLocationVisible(location))
        {
            this.Location = location;
        }
        else
        {
            PositionNextToOwner(); // 默认位置
        }
    }
}
```

#### 智能定位
```csharp
private void PositionNextToOwner()
{
    if (this.Owner != null)
    {
        // 主窗口右侧，稍微偏移
        int x = this.Owner.Right + 10;
        int y = this.Owner.Top;
        
        // 确保不超出屏幕
        var screen = Screen.FromControl(this.Owner);
        if (x + this.Width > screen.WorkingArea.Right)
        {
            x = screen.WorkingArea.Right - this.Width - 10;
        }
        
        this.Location = new Point(x, y);
    }
}
```

### 与主窗口集成

#### PdfOperationsPanel修改

```csharp
// 字段
private PdfInspectorForm _inspectorForm;

// 显示检查器
private void ShowInspector()
{
    if (string.IsNullOrEmpty(_currentFilePath))
    {
        ShowWarning("请先打开PDF文件");
        return;
    }

    // 创建或显示检查器窗口
    if (_inspectorForm == null || _inspectorForm.IsDisposed)
    {
        _inspectorForm = new PdfInspectorForm
        {
            Owner = this.FindForm()
        };
        _inspectorForm.PageSelected += Inspector_PageSelected;
    }

    // 加载当前PDF
    _inspectorForm.LoadPdf(_currentFilePath, _currentPage);

    // 显示窗口
    _inspectorForm.Show();
}

// 同步：预览 → 检查器
private void PdfPreview_PageChanged(object sender, PageChangedEventArgs e)
{
    _currentPage = e.CurrentPage;
    
    if (_inspectorForm != null && _inspectorForm.Visible)
    {
        _inspectorForm.SwitchToPage(_currentPage);
    }
}

// 同步：检查器 → 预览
private void Inspector_PageSelected(object sender, int pageNumber)
{
    _currentPage = pageNumber;
    _ = _pdfPreview.GoToPageAsync(pageNumber);
}
```

## 与PitStop Pro对比

| 特性 | PitStop Pro | 本实现 | 说明 |
|------|-------------|--------|------|
| 独立窗口 | ✓ | ✓ | 完全一致 |
| 窗口位置记忆 | ✓ | ✓ | 完全一致 |
| 多显示器支持 | ✓ | ✓ | 完全一致 |
| 双向同步 | ✓ | ✓ | 完全一致 |
| 关闭时隐藏 | ✓ | ✓ | 完全一致 |
| 页面框参数 | ✓ | ✓ | 完全一致 |
| 问题检测 | ✓ | ✓ | 基础实现 |
| 页面框编辑 | ✓ | ✗ | 待实现 |

## 优势

### 相比侧边栏模式

1. **更符合专业软件习惯** - PitStop Pro用户熟悉
2. **更灵活的布局** - 可以放在任意位置
3. **更大的预览空间** - 不占用主窗口空间
4. **多显示器友好** - 可以放在第二个显示器
5. **独立的生命周期** - 不影响主窗口

### 用户体验

1. **直观** - 点击按钮打开窗口
2. **灵活** - 可以自由移动和调整
3. **持久** - 位置和尺寸自动保存
4. **智能** - 自动定位和同步

## 常见问题

### Q: 如何打开检查器？
A: 打开PDF文件后，点击工具栏的"检查器"按钮。

### Q: 检查器窗口关闭后如何再次打开？
A: 再次点击"检查器"按钮，窗口会重新显示。

### Q: 检查器窗口的位置会保存吗？
A: 是的，窗口位置和尺寸会自动保存，下次打开时恢复。

### Q: 可以把检查器放在第二个显示器吗？
A: 可以，直接拖动窗口到第二个显示器即可，位置会自动保存。

### Q: 检查器和PDF预览会同步吗？
A: 是的，双向同步。在任一侧切换页面，另一侧会自动更新。

### Q: 关闭检查器窗口会影响主程序吗？
A: 不会，检查器窗口只是隐藏，不会影响主程序。

## 快捷键（计划）

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+I` | 显示/隐藏检查器窗口 |
| `F5` | 刷新检查器 |
| `Esc` | 关闭检查器窗口 |

## 更新日志

### v1.2.0 (2026-01-19)
- ✅ 改为独立窗口模式
- ✅ 添加窗口位置记忆
- ✅ 添加智能定位
- ✅ 添加多显示器支持
- ✅ 移除侧边栏模式

### v1.1.0 (2026-01-19)
- ✅ 侧边栏模式（已废弃）

### v1.0.0 (2026-01-19)
- ✅ 基础检查器功能

## 下一步计划

1. **快捷键支持** - Ctrl+I显示/隐藏
2. **窗口停靠** - 支持停靠到主窗口边缘
3. **透明度调节** - 支持调整窗口透明度
4. **始终置顶** - 可选的始终置顶模式

---

**版本**: v1.2.0  
**更新日期**: 2026-01-19  
**状态**: ✅ 独立窗口模式已完成
