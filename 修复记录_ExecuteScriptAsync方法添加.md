# 修复记录：添加 ExecuteScriptAsync 方法

## 问题描述

**编译错误1**:
```
CS1061: "CefPdfPreviewControl"未包含"EvaluateScriptAsync"的定义
```

**编译错误2**:
```
CS0117: "LogHelper"未包含"Warning"的定义
```

---

## 解决方案

### 错误1: EvaluateScriptAsync 方法缺失
**原因**: `CefPdfPreviewControl` 没有直接暴露 `EvaluateScriptAsync` 方法，该方法是内部 `_browser` 对象的方法。

**解决**: 在 `CefPdfPreviewControl` 中添加公共方法 `ExecuteScriptAsync`，封装对内部浏览器的脚本执行。

### 错误2: LogHelper.Warning 方法名错误
**原因**: `LogHelper` 类的警告日志方法名是 `Warn`，而不是 `Warning`。

**解决**: 将 `LogHelper.Warning` 改为 `LogHelper.Warn`。

---

## 修改内容

### 1. CefPdfPreviewControl.cs

**添加公共方法**:
```csharp
/// <summary>
/// 执行JavaScript脚本
/// </summary>
/// <param name="script">要执行的JavaScript代码</param>
public async Task ExecuteScriptAsync(string script)
{
    try
    {
        if (_browser?.IsBrowserInitialized == true)
        {
            await _browser.EvaluateScriptAsync(script);
        }
        else
        {
            LogHelper.Warn("[CefPdfPreview] 浏览器未初始化，无法执行脚本");  // ✅ 使用 Warn
        }
    }
    catch (Exception ex)
    {
        LogHelper.Error($"[CefPdfPreview] 执行脚本失败: {ex.Message}", ex);
        throw;
    }
}
```

**位置**: 在 `ToggleBoxDisplayAsync` 方法之后，`#endregion` 之前

---

### 2. PdfOperationsPanel.cs

**使用新方法**:
```csharp
private async void ShowPdfPreview(byte[] pdfBytes, bool quickMode = false)
{
    // ...
    if (quickMode)
    {
        // 快速模式：使用 loadPdfQuick
        string fileUrl = $"file:///{tempPath.Replace("\\", "/")}";
        await _pdfPreview.ExecuteScriptAsync($"loadPdfQuick('{fileUrl}');");
    }
    else
    {
        // 标准模式：使用现有的 LoadPdfAsync 方法
        await _pdfPreview.LoadPdfAsync(tempPath);
    }
    // ...
}
```

---

## LogHelper 可用方法

| 方法 | 用途 | 参数 |
|------|------|------|
| `Debug(string)` | 调试信息 | 消息 |
| `Info(string)` | 一般信息 | 消息 |
| `Warn(string)` | ⚠️ 警告信息 | 消息 |
| `Error(string)` | 错误信息 | 消息 |
| `Error(string, Exception)` | 错误信息+异常 | 消息, 异常 |
| `Critical(string)` | 严重错误 | 消息 |
| `Critical(string, Exception)` | 严重错误+异常 | 消息, 异常 |

**注意**: 方法名是 `Warn`，不是 `Warning`！

---

## 技术说明

### 为什么需要 ExecuteScriptAsync 方法？

1. **封装性**: 内部 `_browser` 字段是私有的，不应直接暴露
2. **安全性**: 通过公共方法可以添加检查和错误处理
3. **可维护性**: 如果将来更换浏览器实现，只需修改这一个方法

### 方法特点

- ✅ 检查浏览器是否已初始化
- ✅ 异步执行，不阻塞UI
- ✅ 完整的错误处理和日志记录
- ✅ 抛出异常供调用者处理

---

## 其他可用方法

`CefPdfPreviewControl` 现在提供以下脚本执行相关方法：

| 方法 | 用途 | 参数 |
|------|------|------|
| `LoadPdfAsync(string)` | 加载PDF文件（标准模式） | 文件路径 |
| `ExecuteScriptAsync(string)` | 执行任意JavaScript脚本 | JS代码 |
| `GoToPageAsync(int)` | 跳转到指定页 | 页码 |
| `RotateClockwiseAsync()` | 顺时针旋转 | 无 |
| `RotateCounterClockwiseAsync()` | 逆时针旋转 | 无 |
| `ToggleBoxDisplayAsync(bool)` | 切换框线显示 | 是否显示 |

---

## 使用示例

### 快速加载PDF
```csharp
await _pdfPreview.ExecuteScriptAsync($"loadPdfQuick('file:///C:/temp/test.pdf');");
```

### 执行自定义脚本
```csharp
await _pdfPreview.ExecuteScriptAsync("console.log('Hello from C#');");
```

### 调用PDF.js函数
```csharp
await _pdfPreview.ExecuteScriptAsync("fitToWidth();");
await _pdfPreview.ExecuteScriptAsync("state.scale *= 1.5; renderPage(state.currentPage);");
```

---

## 测试验证

### 编译测试
```
✅ 编译通过，无错误
✅ 无警告
```

### 功能测试
```
1. 打开PDF文件（标准模式）
2. 执行转曲操作
3. 点击"撤回"按钮（快速模式）
4. 验证底部进度条显示
5. 验证内容快速切换
```

---

## 总结

通过添加 `ExecuteScriptAsync` 公共方法并修正日志方法名，解决了所有编译错误，同时保持了良好的封装性和可维护性。

**修改文件**:
- `src/WindowsFormsApp3/Controls/CefPdfPreviewControl.cs` (+18行, 修正日志方法名)
- `src/WindowsFormsApp3/Forms/Panels/PdfOperationsPanel.cs` (修改调用方式)

**状态**: ✅ 已修复，可以编译

