# PDF检查器 v1.3.5 - 转曲工作流优化

## 更新日期
2026-01-19

## 概述
优化了字体转曲功能的工作流程，转曲后不再立即保存文件，而是由用户手动保存或另存为。这样可以让用户在保存前预览转曲效果，并选择保存位置。

## 主要变更

### 1. 转曲按钮行为修改
**文件**: `src/WindowsFormsApp3/Forms/Controls/PdfInspectorControl.cs`

**变更内容**:
- 点击"转曲"按钮后，只在内存中执行转曲操作
- 不再弹出保存对话框
- 转曲完成后显示提示："字体已转换为路径，请使用工具栏的保存或另存为按钮保存文件"
- 触发 `FontOutlineCompleted` 事件，通知父窗口转曲完成

**新增事件**:
```csharp
public event EventHandler<FontOutlineCompletedEventArgs> FontOutlineCompleted;
```

**事件参数**:
```csharp
public class FontOutlineCompletedEventArgs : EventArgs
{
    public byte[] PdfBytes { get; set; }          // 转曲后的PDF字节数组
    public string OriginalFilePath { get; set; }  // 原始文件路径
}
```

### 2. 新增"另存为"按钮
**文件**: `src/WindowsFormsApp3/Forms/Panels/PdfOperationsPanel.Designer.cs`

**变更内容**:
- 在工具栏添加"另存为"按钮（位于"保存"按钮之后）
- 按钮布局调整，所有后续按钮位置相应偏移

**按钮顺序**:
```
[打开PDF] [保存] [另存为] | 印刷标记: [添加裁切标记] [添加套准标记] | [检查器]
```

### 3. 完善保存功能
**文件**: `src/WindowsFormsApp3/Forms/Panels/PdfOperationsPanel.cs`

#### 3.1 新增状态跟踪
```csharp
private byte[] _modifiedPdfBytes = null;      // 存储转曲后的PDF字节数组
private bool _hasUnsavedChanges = false;      // 是否有未保存的更改
```

#### 3.2 保存按钮 (BtnSave_Click)
**功能**:
- 保存到当前文件（覆盖原文件）
- 只在有未保存更改时启用
- 保存后重新加载文件以显示最新内容

**实现逻辑**:
1. 检查是否有打开的文件
2. 检查是否有未保存的更改
3. 将 `_modifiedPdfBytes` 写入当前文件路径
4. 重置状态标志
5. 重新加载PDF预览

#### 3.3 另存为按钮 (BtnSaveAs_Click)
**功能**:
- 保存到新文件（不覆盖原文件）
- 如果有转曲数据，保存转曲后的；否则保存原文件副本
- 默认文件名：原文件名 + "_转曲.pdf" 或 "_副本.pdf"
- 保存后询问是否打开新文件

**实现逻辑**:
1. 检查是否有打开的文件
2. 选择要保存的数据（转曲后的或原文件）
3. 弹出保存对话框
4. 写入文件
5. 询问是否打开新文件
6. 如果打开，重置状态并加载新文件

#### 3.4 转曲完成事件处理 (Inspector_FontOutlineCompleted)
**功能**:
- 接收检查器的转曲完成事件
- 保存转曲后的PDF字节数组
- 启用保存按钮
- 更新状态栏显示"已转曲 (未保存)"
- 创建临时文件并重新加载预览

**实现逻辑**:
1. 接收转曲后的PDF字节数组
2. 设置 `_modifiedPdfBytes` 和 `_hasUnsavedChanges`
3. 启用保存和另存为按钮
4. 创建临时文件用于预览
5. 重新加载PDF预览以显示转曲后的效果

### 4. 服务层增强
**文件**: `src/WindowsFormsApp3/Services/PdfFontOutlineService.cs`

**新增方法**:
```csharp
public byte[] ConvertTextToOutlinesBytes(string inputPath)
```

**功能**:
- 将PDF转曲后返回字节数组（而不是直接保存到文件）
- 内部使用临时文件，完成后自动清理
- 失败时返回 null

**实现逻辑**:
1. 创建临时输出文件
2. 调用现有的 `ConvertTextToOutlines` 方法
3. 读取临时文件为字节数组
4. 删除临时文件
5. 返回字节数组

### 5. 事件转发
**文件**: `src/WindowsFormsApp3/Forms/Utils/PdfInspectorForm.cs`

**变更内容**:
- 新增 `FontOutlineCompleted` 事件
- 订阅内部 `_inspectorControl` 的 `FontOutlineCompleted` 事件
- 转发事件到外部订阅者

**代码**:
```csharp
public event EventHandler<WindowsFormsApp3.Forms.Controls.FontOutlineCompletedEventArgs> FontOutlineCompleted;

private void InspectorControl_FontOutlineCompleted(object sender, FontOutlineCompletedEventArgs e)
{
    FontOutlineCompleted?.Invoke(this, e);
}
```

## 用户工作流

### 转曲并保存工作流
1. 打开PDF文件
2. 点击"检查器"按钮打开检查器窗口
3. 在检查器的"字体"标签页中点击"转曲"按钮
4. 确认转曲操作
5. 等待转曲完成（会显示成功提示）
6. 预览窗口自动显示转曲后的效果
7. 选择保存方式：
   - **保存**: 覆盖原文件
   - **另存为**: 保存到新文件

### 转曲并另存为工作流
1. 打开PDF文件
2. 点击"检查器"按钮
3. 在检查器中点击"转曲"按钮
4. 确认转曲操作
5. 转曲完成后，点击工具栏的"另存为"按钮
6. 选择保存位置和文件名
7. 选择是否打开新文件

## 状态管理

### 按钮启用状态
- **保存按钮**: 只在有未保存更改时启用
- **另存为按钮**: 加载PDF后始终启用
- **其他按钮**: 加载PDF后启用

### 状态栏显示
- 加载文件: "已加载: 文件名.pdf"
- 转曲完成: "已转曲 (未保存) - 文件名.pdf"
- 保存完成: "已保存: 文件名.pdf"

## 技术细节

### 内存管理
- 转曲后的PDF存储在 `_modifiedPdfBytes` 字节数组中
- 预览时创建临时文件（自动清理）
- 加载新文件时重置状态

### 事件流
```
PdfInspectorControl (转曲按钮)
    ↓ FontOutlineCompleted 事件
PdfInspectorForm (转发事件)
    ↓ FontOutlineCompleted 事件
PdfOperationsPanel (处理事件)
    ↓ 保存字节数组、启用按钮、更新预览
```

### 临时文件处理
- 预览转曲后的PDF时创建临时文件
- 文件名格式: `temp_preview_{GUID}.pdf`
- 位置: 系统临时文件夹
- 加载新文件时不会重置状态（通过文件名判断）

## 测试建议

### 基本功能测试
1. 打开PDF → 转曲 → 保存 → 验证文件已更新
2. 打开PDF → 转曲 → 另存为 → 验证新文件已创建
3. 打开PDF → 转曲 → 另存为 → 打开新文件 → 验证预览正确

### 边界情况测试
1. 转曲后不保存，直接打开新文件 → 验证状态重置
2. 转曲后保存，再次转曲 → 验证可以重复操作
3. 转曲失败 → 验证错误提示和状态恢复

### UI状态测试
1. 验证保存按钮只在有更改时启用
2. 验证另存为按钮始终启用（加载PDF后）
3. 验证状态栏显示正确

## 兼容性
- 不影响现有功能
- 向后兼容
- 所有现有的PDF操作功能正常工作

## 已知限制
1. 转曲后的PDF存储在内存中，大文件可能占用较多内存
2. 预览转曲效果需要创建临时文件
3. 关闭应用程序前需要保存，否则转曲结果会丢失

## 后续优化建议
1. 添加"是否保存更改"提示（关闭文件或应用程序时）
2. 支持撤销转曲操作
3. 添加转曲进度显示（大文件）
4. 支持批量转曲多个PDF文件

## 相关文件
- `src/WindowsFormsApp3/Forms/Controls/PdfInspectorControl.cs`
- `src/WindowsFormsApp3/Forms/Panels/PdfOperationsPanel.cs`
- `src/WindowsFormsApp3/Forms/Panels/PdfOperationsPanel.Designer.cs`
- `src/WindowsFormsApp3/Services/PdfFontOutlineService.cs`
- `src/WindowsFormsApp3/Forms/Utils/PdfInspectorForm.cs`
