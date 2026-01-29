# PDF操作面板 - PDF加载逻辑说明

## 概述

PDF操作面板使用了两种不同的PDF加载方式：
1. **直接文件加载** - 用户打开原始PDF文件
2. **临时文件加载** - 显示转曲/撤回/重做后的PDF状态

## 核心数据结构

```csharp
// 文件路径
private string _currentFilePath;      // 当前显示的PDF文件路径（可能是临时文件）
private string _originalFilePath;     // 原始PDF文件路径（用户打开的文件）

// PDF字节数组
private byte[] _originalPdfBytes;     // 原始PDF的字节数组
private byte[] _currentPdfBytes;      // 当前显示的PDF字节数组

// 历史栈
private Stack<byte[]> _undoStack;     // 撤回栈（保存历史状态）
private Stack<byte[]> _redoStack;     // 重做栈（保存被撤回的状态）
```

## 加载场景

### 场景1: 用户打开PDF文件

#### 触发方式
- 点击"打开PDF"按钮
- 选择PDF文件

#### 执行流程

```
用户点击"打开PDF"
    ↓
BtnOpen_Click()
    ↓
显示文件选择对话框
    ↓
用户选择文件
    ↓
LoadPdfFileAsync(filePath)
    ↓
┌─────────────────────────────────┐
│ 1. 保存文件路径                  │
│    _currentFilePath = filePath   │
│    _originalFilePath = filePath  │
├─────────────────────────────────┤
│ 2. 读取PDF字节到内存             │
│    _originalPdfBytes = 读取文件  │
│    _currentPdfBytes = 原始字节   │
├─────────────────────────────────┤
│ 3. 清除历史栈                    │
│    _undoStack.Clear()           │
│    _redoStack.Clear()           │
│    UpdateHistoryButtons()       │
├─────────────────────────────────┤
│ 4. 加载到预览控件                │
│    _pdfPreview.LoadPdfAsync()   │
└─────────────────────────────────┘
    ↓
PdfPreview_PdfLoaded 事件触发
    ↓
┌─────────────────────────────────┐
│ 1. 保存页面信息                  │
│    _totalPages = e.TotalPages   │
│    _currentPage = e.CurrentPage │
├─────────────────────────────────┤
│ 2. 启用控件                      │
│    EnableControls(true)         │
├─────────────────────────────────┤
│ 3. 更新检查器（如果打开）         │
│    _inspectorForm.LoadPdf()     │
├─────────────────────────────────┤
│ 4. 更新状态栏                    │
│    显示文件名和页面尺寸          │
└─────────────────────────────────┘
```

#### 代码实现

```csharp
private void BtnOpen_Click(object sender, EventArgs e)
{
    using (var openDialog = new OpenFileDialog())
    {
        openDialog.Filter = "PDF文件|*.pdf|所有文件|*.*";
        openDialog.Title = "选择PDF文件";

        if (openDialog.ShowDialog() == DialogResult.OK)
        {
            _ = LoadPdfFileAsync(openDialog.FileName);
        }
    }
}

private async Task LoadPdfFileAsync(string filePath)
{
    try
    {
        // 1. 保存文件路径
        _currentFilePath = filePath;
        _originalFilePath = filePath;
        
        // 2. 读取PDF字节到内存
        _originalPdfBytes = File.ReadAllBytes(filePath);
        _currentPdfBytes = _originalPdfBytes;
        
        // 3. 清除历史栈
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateHistoryButtons();
        
        // 4. 更新状态
        UpdateStatus($"正在加载: {Path.GetFileName(filePath)}");

        // 5. 加载到预览控件
        await _pdfPreview.LoadPdfAsync(filePath);
        
        LogHelper.Info($"[PdfOperationsPanel] PDF已加载: {filePath}");
    }
    catch (Exception ex)
    {
        ShowError($"加载PDF失败: {ex.Message}");
        UpdateStatus("加载失败");
    }
}
```

#### 状态变化

| 变量 | 加载前 | 加载后 |
|------|--------|--------|
| `_currentFilePath` | null | "C:\path\to\file.pdf" |
| `_originalFilePath` | null | "C:\path\to\file.pdf" |
| `_originalPdfBytes` | null | [PDF字节数组] |
| `_currentPdfBytes` | null | [PDF字节数组] |
| `_undoStack` | - | 清空 |
| `_redoStack` | - | 清空 |
| 撤回按钮 | 禁用 | 禁用 |
| 重做按钮 | 禁用 | 禁用 |
| 保存按钮 | 禁用 | 禁用 |

---

### 场景2: 字体转曲后加载

#### 触发方式
- 在检查器中点击"转曲"按钮
- 转曲完成后触发事件

#### 执行流程

```
用户点击"转曲"
    ↓
检查器执行转曲
    ↓
Inspector_FontOutlineCompleted 事件
    ↓
┌─────────────────────────────────┐
│ 1. 保存当前状态到撤回栈          │
│    _undoStack.Push(当前字节)     │
├─────────────────────────────────┤
│ 2. 更新当前状态                  │
│    _currentPdfBytes = 转曲字节   │
├─────────────────────────────────┤
│ 3. 清除重做栈                    │
│    _redoStack.Clear()           │
├─────────────────────────────────┤
│ 4. 限制历史栈大小                │
│    LimitHistorySize()           │
├─────────────────────────────────┤
│ 5. 更新按钮状态                  │
│    UpdateHistoryButtons()       │
└─────────────────────────────────┘
    ↓
ShowPdfPreview(转曲字节)
    ↓
┌─────────────────────────────────┐
│ 1. 创建临时文件                  │
│    tempPath = Temp\guid.pdf     │
│    写入转曲后的字节              │
├─────────────────────────────────┤
│ 2. 更新当前文件路径              │
│    _currentFilePath = tempPath  │
├─────────────────────────────────┤
│ 3. 加载到预览控件                │
│    _pdfPreview.LoadPdfAsync()   │
├─────────────────────────────────┤
│ 4. 更新检查器（如果打开）         │
│    _inspectorForm.LoadPdf()     │
└─────────────────────────────────┘
    ↓
PdfPreview_PdfLoaded 事件触发
    ↓
显示转曲后的PDF
```

#### 代码实现

```csharp
private void Inspector_FontOutlineCompleted(object sender, FontOutlineCompletedEventArgs e)
{
    try
    {
        LogHelper.Info("[PdfOperationsPanel] 收到字体转曲完成事件");
        
        // 1. 保存当前状态到撤回栈
        _undoStack.Push(_currentPdfBytes);
        
        // 2. 限制历史栈大小
        LimitHistorySize();
        
        // 3. 更新当前状态
        _currentPdfBytes = e.PdfBytes;
        
        // 4. 清除重做栈（新操作清除重做历史）
        _redoStack.Clear();
        
        // 5. 更新按钮状态
        UpdateHistoryButtons();
        
        // 6. 更新状态栏
        UpdateStatus($"已转曲 (未保存) - {Path.GetFileName(_originalFilePath)}");
        
        // 7. 显示转曲后的PDF
        ShowPdfPreview(_currentPdfBytes);
        
        LogHelper.Info($"[PdfOperationsPanel] 转曲完成，历史栈深度: {_undoStack.Count}");
    }
    catch (Exception ex)
    {
        LogHelper.Error($"[PdfOperationsPanel] 处理转曲完成事件失败: {ex.Message}", ex);
        ShowError($"加载转曲后的PDF失败: {ex.Message}");
    }
}

private void ShowPdfPreview(byte[] pdfBytes)
{
    try
    {
        // 1. 创建临时文件
        string tempPath = Path.Combine(Path.GetTempPath(), 
            $"temp_preview_{Guid.NewGuid()}.pdf");
        File.WriteAllBytes(tempPath, pdfBytes);
        
        // 2. 更新当前文件路径为临时文件
        _currentFilePath = tempPath;
        
        // 3. 加载到预览
        _ = _pdfPreview.LoadPdfAsync(tempPath);
        
        // 4. 如果检查器窗口打开，重新加载检查器
        if (_inspectorForm != null && _inspectorForm.Visible)
        {
            _inspectorForm.LoadPdf(tempPath, _currentPage);
            LogHelper.Debug("[PdfOperationsPanel] 检查器已更新为转曲后的PDF");
        }
    }
    catch (Exception ex)
    {
        LogHelper.Error($"[PdfOperationsPanel] 显示预览失败: {ex.Message}", ex);
        ShowError($"显示预览失败: {ex.Message}");
    }
}
```

#### 状态变化

| 变量 | 转曲前 | 转曲后 |
|------|--------|--------|
| `_currentFilePath` | "C:\path\to\file.pdf" | "C:\Temp\temp_preview_guid.pdf" |
| `_originalFilePath` | "C:\path\to\file.pdf" | "C:\path\to\file.pdf" (不变) |
| `_originalPdfBytes` | [原始字节] | [原始字节] (不变) |
| `_currentPdfBytes` | [原始字节] | [转曲字节] |
| `_undoStack` | 空 | [原始字节] |
| `_redoStack` | - | 清空 |
| 撤回按钮 | 禁用 | "撤回 (1)" |
| 重做按钮 | 禁用 | 禁用 |
| 保存按钮 | 禁用 | 启用 |

---

### 场景3: 撤回操作后加载

#### 触发方式
- 点击"撤回"按钮
- 或按 Ctrl+Z

#### 执行流程

```
用户点击"撤回"
    ↓
BtnUndo_Click()
    ↓
┌─────────────────────────────────┐
│ 1. 保存当前状态到重做栈          │
│    _redoStack.Push(当前字节)     │
├─────────────────────────────────┤
│ 2. 从撤回栈弹出上一状态          │
│    _currentPdfBytes = 弹出字节   │
├─────────────────────────────────┤
│ 3. 更新按钮状态                  │
│    UpdateHistoryButtons()       │
└─────────────────────────────────┘
    ↓
ShowPdfPreview(上一状态字节)
    ↓
显示上一个状态的PDF
```

#### 代码实现

```csharp
private void BtnUndo_Click(object sender, EventArgs e)
{
    if (_undoStack.Count == 0)
    {
        return; // 按钮应该是禁用的
    }

    try
    {
        // 1. 将当前状态压入重做栈
        _redoStack.Push(_currentPdfBytes);
        
        // 2. 从撤回栈弹出上一个状态
        _currentPdfBytes = _undoStack.Pop();
        
        // 3. 更新按钮状态
        UpdateHistoryButtons();
        
        // 4. 显示上一个状态
        ShowPdfPreview(_currentPdfBytes);
        
        // 5. 更新状态栏
        bool isOriginal = _undoStack.Count == 0;
        UpdateStatus(isOriginal 
            ? $"已加载: {Path.GetFileName(_originalFilePath)}"
            : $"已撤回 (未保存) - {Path.GetFileName(_originalFilePath)}");
        
        LogHelper.Info($"[PdfOperationsPanel] 撤回操作，剩余历史: {_undoStack.Count}, 可重做: {_redoStack.Count}");
    }
    catch (Exception ex)
    {
        LogHelper.Error($"[PdfOperationsPanel] 撤回操作失败: {ex.Message}", ex);
        ShowError($"撤回失败: {ex.Message}");
    }
}
```

#### 状态变化示例

假设执行了2次转曲，然后撤回1次：

| 变量 | 转曲2次后 | 撤回1次后 |
|------|----------|----------|
| `_currentFilePath` | "C:\Temp\temp2.pdf" | "C:\Temp\temp3.pdf" |
| `_originalFilePath` | "C:\path\to\file.pdf" | "C:\path\to\file.pdf" |
| `_currentPdfBytes` | [转曲2字节] | [转曲1字节] |
| `_undoStack` | [原始, 转曲1] | [原始] |
| `_redoStack` | 空 | [转曲2] |
| 撤回按钮 | "撤回 (2)" | "撤回 (1)" |
| 重做按钮 | 禁用 | "重做 (1)" ⭐ |
| 保存按钮 | 启用 | 启用 |

---

### 场景4: 重做操作后加载

#### 触发方式
- 点击"重做"按钮
- 或按 Ctrl+Y

#### 执行流程

```
用户点击"重做"
    ↓
BtnRedo_Click()
    ↓
┌─────────────────────────────────┐
│ 1. 保存当前状态到撤回栈          │
│    _undoStack.Push(当前字节)     │
├─────────────────────────────────┤
│ 2. 从重做栈弹出                  │
│    _currentPdfBytes = 弹出字节   │
├─────────────────────────────────┤
│ 3. 更新按钮状态                  │
│    UpdateHistoryButtons()       │
└─────────────────────────────────┘
    ↓
ShowPdfPreview(重做状态字节)
    ↓
显示重做后的PDF
```

#### 代码实现

```csharp
private void BtnRedo_Click(object sender, EventArgs e)
{
    if (_redoStack.Count == 0)
    {
        return; // 按钮应该是禁用的
    }

    try
    {
        // 1. 将当前状态压入撤回栈
        _undoStack.Push(_currentPdfBytes);
        
        // 2. 从重做栈弹出
        _currentPdfBytes = _redoStack.Pop();
        
        // 3. 更新按钮状态
        UpdateHistoryButtons();
        
        // 4. 显示重做后的状态
        ShowPdfPreview(_currentPdfBytes);
        
        // 5. 更新状态栏
        UpdateStatus($"已重做 (未保存) - {Path.GetFileName(_originalFilePath)}");
        
        LogHelper.Info($"[PdfOperationsPanel] 重做操作，历史: {_undoStack.Count}, 剩余重做: {_redoStack.Count}");
    }
    catch (Exception ex)
    {
        LogHelper.Error($"[PdfOperationsPanel] 重做操作失败: {ex.Message}", ex);
        ShowError($"重做失败: {ex.Message}");
    }
}
```

---

### 场景5: 保存文件后

#### 触发方式
- 点击"保存"按钮

#### 执行流程

```
用户点击"保存"
    ↓
BtnSave_Click()
    ↓
┌─────────────────────────────────┐
│ 1. 保存当前字节到原文件          │
│    File.WriteAllBytes()         │
├─────────────────────────────────┤
│ 2. 更新原始字节                  │
│    _originalPdfBytes = 当前字节  │
├─────────────────────────────────┤
│ 3. 清除历史栈                    │
│    _undoStack.Clear()           │
│    _redoStack.Clear()           │
├─────────────────────────────────┤
│ 4. 更新按钮状态                  │
│    UpdateHistoryButtons()       │
└─────────────────────────────────┘
```

#### 代码实现

```csharp
private void BtnSave_Click(object sender, EventArgs e)
{
    if (string.IsNullOrEmpty(_currentFilePath))
    {
        ShowWarning("没有打开的文件");
        return;
    }

    if (_currentPdfBytes == null || _currentPdfBytes == _originalPdfBytes)
    {
        ShowWarning("没有需要保存的更改");
        return;
    }

    try
    {
        // 1. 保存当前状态到原文件
        File.WriteAllBytes(_originalFilePath, _currentPdfBytes);
        
        // 2. 保存后更新原始字节并清除历史
        _originalPdfBytes = _currentPdfBytes;
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateHistoryButtons();
        
        ShowSuccess("文件已保存");
        UpdateStatus($"已保存: {Path.GetFileName(_originalFilePath)}");
        
        LogHelper.Info("[PdfOperationsPanel] 文件已保存，历史已清除");
    }
    catch (Exception ex)
    {
        LogHelper.Error($"[PdfOperationsPanel] 保存文件失败: {ex.Message}", ex);
        ShowError($"保存失败: {ex.Message}");
    }
}
```

#### 状态变化

| 变量 | 保存前 | 保存后 |
|------|--------|--------|
| `_originalFilePath` | "C:\path\to\file.pdf" | "C:\path\to\file.pdf" |
| `_originalPdfBytes` | [原始字节] | [当前字节] ⭐ |
| `_currentPdfBytes` | [转曲字节] | [转曲字节] |
| `_undoStack` | [历史状态] | 清空 ⭐ |
| `_redoStack` | [重做状态] | 清空 ⭐ |
| 撤回按钮 | "撤回 (X)" | 禁用 |
| 重做按钮 | "重做 (Y)" | 禁用 |
| 保存按钮 | 启用 | 禁用 |

---

## 关键设计决策

### 1. 为什么使用两个文件路径？

```csharp
private string _currentFilePath;   // 当前显示的文件（可能是临时文件）
private string _originalFilePath;  // 原始文件（用户打开的文件）
```

**原因**:
- `_currentFilePath`: 用于预览和检查器加载，可能指向临时文件
- `_originalFilePath`: 用于保存操作，始终指向用户打开的原始文件

**好处**:
- 保存时不会覆盖临时文件
- 状态栏可以显示原始文件名
- 检查器可以正确加载当前显示的PDF

### 2. 为什么使用字节数组而不是文件路径？

```csharp
private byte[] _originalPdfBytes;  // 原始PDF字节
private byte[] _currentPdfBytes;   // 当前PDF字节
private Stack<byte[]> _undoStack;  // 历史栈
```

**原因**:
- 转曲操作返回的是字节数组，不是文件
- 避免频繁的磁盘I/O操作
- 撤回/重做速度更快

**权衡**:
- 优点: 速度快，响应迅速
- 缺点: 占用内存（通过限制历史栈大小缓解）

### 3. 为什么使用临时文件？

```csharp
string tempPath = Path.Combine(Path.GetTempPath(), 
    $"temp_preview_{Guid.NewGuid()}.pdf");
File.WriteAllBytes(tempPath, pdfBytes);
```

**原因**:
- CefSharp预览控件需要文件路径，不能直接加载字节数组
- 检查器也需要文件路径来分析PDF

**临时文件命名**:
- 使用GUID确保唯一性
- 避免文件冲突
- 便于调试和追踪

### 4. 为什么保存后清除历史？

```csharp
_originalPdfBytes = _currentPdfBytes;
_undoStack.Clear();
_redoStack.Clear();
```

**原因**:
- 保存后文件已持久化，历史不再需要
- 释放内存
- 符合专业软件的标准行为（Adobe Acrobat DC）

---

## 完整操作流程示例

### 示例: 打开 → 转曲 → 撤回 → 重做 → 保存

```
1. 打开PDF
   _originalFilePath = "C:\test.pdf"
   _currentFilePath = "C:\test.pdf"
   _originalPdfBytes = [原始]
   _currentPdfBytes = [原始]
   _undoStack = []
   _redoStack = []

2. 转曲
   _originalFilePath = "C:\test.pdf" (不变)
   _currentFilePath = "C:\Temp\temp1.pdf"
   _originalPdfBytes = [原始] (不变)
   _currentPdfBytes = [转曲1]
   _undoStack = [[原始]]
   _redoStack = []

3. 撤回
   _originalFilePath = "C:\test.pdf" (不变)
   _currentFilePath = "C:\Temp\temp2.pdf"
   _originalPdfBytes = [原始] (不变)
   _currentPdfBytes = [原始]
   _undoStack = []
   _redoStack = [[转曲1]]

4. 重做
   _originalFilePath = "C:\test.pdf" (不变)
   _currentFilePath = "C:\Temp\temp3.pdf"
   _originalPdfBytes = [原始] (不变)
   _currentPdfBytes = [转曲1]
   _undoStack = [[原始]]
   _redoStack = []

5. 保存
   _originalFilePath = "C:\test.pdf" (不变)
   _currentFilePath = "C:\Temp\temp3.pdf" (不变)
   _originalPdfBytes = [转曲1] ⭐ 更新
   _currentPdfBytes = [转曲1] (不变)
   _undoStack = [] ⭐ 清空
   _redoStack = [] ⭐ 清空
```

---

## 性能考虑

### 内存使用

```
总内存 ≈ PDF大小 × (2 + 撤回栈大小 + 重做栈大小)

示例（5MB PDF，3次撤回，2次重做）:
5MB × (2 + 3 + 2) = 35MB
```

### 磁盘I/O

- **打开文件**: 1次读取（加载到内存）
- **转曲/撤回/重做**: 1次写入临时文件
- **保存**: 1次写入原文件

### 优化策略

1. **限制历史栈大小**: 最多10个状态
2. **使用临时文件**: 避免重复读写原文件
3. **延迟加载**: 只在需要时加载检查器
4. **异步操作**: 使用async/await避免阻塞UI

---

## 常见问题

### Q1: 为什么转曲后_currentFilePath变成临时文件？
**A**: 因为转曲后的PDF存储在内存中（字节数组），需要写入临时文件才能被预览控件和检查器加载。

### Q2: 临时文件会不会越来越多？
**A**: 会的。每次转曲/撤回/重做都会创建新的临时文件。Windows会定期清理临时文件夹，或者可以在应用关闭时手动清理。

### Q3: 保存后为什么要清除历史？
**A**: 因为保存后文件已经持久化，不需要再保留历史。这也是Adobe Acrobat DC等专业软件的标准行为。

### Q4: 如果用户不保存就关闭应用会怎样？
**A**: 所有更改会丢失，因为只有保存操作才会写入原文件。可以考虑添加关闭前的提示。

### Q5: 为什么不直接修改原文件？
**A**: 为了安全性。只有用户明确点击"保存"时才修改原文件，避免意外覆盖。

---

## 相关文档

- `撤回重做系统_专业版设计.md` - 历史栈系统设计
- `修复记录_转曲后检查器实时更新.md` - 检查器更新逻辑
- `撤回重做系统_快速参考.md` - 开发者参考

---

## 总结

PDF加载逻辑的核心设计：

1. **双路径系统**: 原始文件路径 + 当前显示路径
2. **内存优先**: 使用字节数组存储PDF状态
3. **临时文件**: 用于预览和检查器加载
4. **历史栈**: 支持多级撤回/重做
5. **保存清除**: 保存后清除历史释放内存

这个设计在性能、内存使用和用户体验之间取得了良好的平衡。
