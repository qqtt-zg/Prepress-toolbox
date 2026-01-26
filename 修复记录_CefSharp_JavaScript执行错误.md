# 修复记录 - CefSharp JavaScript 执行错误

## 问题描述
用户安装修复版后，切换到 PDF 操作界面不再出错，但打开 PDF 文件时出现错误弹框：

```
PDF加载失败: Unable to execute javascript at this time, scripts can only be executed within a V8Context.
```

## 问题原因
在 `CefPdfPreviewControl.cs` 中，使用了 `_browser.EvaluateScriptAsync()` 方法执行 JavaScript，但该方法在 V8 上下文创建之前就被调用了，导致错误。

### 技术背景
- **V8Context**: Chromium 的 JavaScript 引擎上下文
- **EvaluateScriptAsync**: 需要在 V8Context 中执行，如果上下文未创建会抛出异常
- **ExecuteJavaScriptAsync**: 更底层的方法，直接在主框架中执行，不需要等待 V8Context

## 解决方案

### 1. 使用正确的 JavaScript 执行方法
将所有 `_browser.EvaluateScriptAsync()` 改为 `_browser.GetMainFrame().ExecuteJavaScriptAsync()`

### 2. 添加浏览器初始化检查
在执行 JavaScript 之前，确保浏览器已完全初始化：

```csharp
// 等待浏览器完全初始化
if (!_browser.IsBrowserInitialized)
{
    LogHelper.Debug("[CefPdfPreview] 等待浏览器初始化...");
    // 等待最多5秒
    int waitCount = 0;
    while (!_browser.IsBrowserInitialized && waitCount < 50)
    {
        await Task.Delay(100);
        waitCount++;
    }
    
    if (!_browser.IsBrowserInitialized)
    {
        LogHelper.Error("[CefPdfPreview] 浏览器初始化超时");
        LoadError?.Invoke(this, new PdfLoadErrorEventArgs("浏览器初始化超时"));
        return;
    }
}
```

### 3. 修改的方法

#### LoadPdfAsync
```csharp
// 修改前
await _browser.EvaluateScriptAsync(script);

// 修改后
_browser.GetMainFrame().ExecuteJavaScriptAsync(script);
await Task.Delay(100); // 等待脚本执行
```

#### GoToPageAsync
```csharp
// 修改前
await _browser.EvaluateScriptAsync($"goToPage({page});");

// 修改后
_browser.GetMainFrame().ExecuteJavaScriptAsync($"goToPage({page});");
await Task.Delay(50);
```

#### RotateClockwiseAsync / RotateCounterClockwiseAsync
```csharp
// 修改前
await _browser.EvaluateScriptAsync("state.rotation = ...");

// 修改后
_browser.GetMainFrame().ExecuteJavaScriptAsync("state.rotation = ...");
await Task.Delay(50);
```

#### SetDarkModeAsync
```csharp
// 修改前
await _browser.EvaluateScriptAsync(script);

// 修改后
_browser.GetMainFrame().ExecuteJavaScriptAsync(script);
await Task.Delay(50);
```

#### ToggleBoxDisplayAsync
```csharp
// 修改前
await _browser.EvaluateScriptAsync(script);

// 修改后
_browser.GetMainFrame().ExecuteJavaScriptAsync(script);
await Task.Delay(50);
```

#### ExecuteScriptAsync
```csharp
// 修改前
await _browser.EvaluateScriptAsync(script);

// 修改后
_browser.GetMainFrame().ExecuteJavaScriptAsync(script);
await Task.Delay(50);
```

## 修改文件
- `src/WindowsFormsApp3/Controls/CefPdfPreviewControl.cs` - 修复所有 JavaScript 执行方法

## 技术说明

### EvaluateScriptAsync vs ExecuteJavaScriptAsync

| 方法 | 特点 | 使用场景 |
|------|------|----------|
| `EvaluateScriptAsync` | 需要 V8Context，可以返回结果 | 需要获取 JS 执行结果时 |
| `ExecuteJavaScriptAsync` | 不需要 V8Context，无返回值 | 只需执行 JS，不需要结果时 |

### 为什么添加 Task.Delay？
`ExecuteJavaScriptAsync` 是异步执行但不返回 Task，所以我们添加一个小延迟确保脚本有时间执行。这是一个权衡方案：
- 优点：简单可靠，不会出现 V8Context 错误
- 缺点：无法确认脚本是否执行成功
- 替代方案：使用 JavaScript 回调通知 C# 执行完成

### IsBrowserInitialized 检查
确保在执行任何 JavaScript 之前，浏览器已经完全初始化：
```csharp
if (_browser?.IsBrowserInitialized == true)
{
    _browser.GetMainFrame().ExecuteJavaScriptAsync(script);
}
```

## 测试验证

### 1. PDF 加载测试
- [x] 打开小型 PDF（< 1MB）
- [x] 打开大型 PDF（> 10MB）
- [x] 连续打开多个 PDF
- [x] 快速切换 PDF 文件

### 2. 功能测试
- [x] 页面导航（上一页/下一页）
- [x] 旋转功能（顺时针/逆时针）
- [x] 主题切换（深色/浅色）
- [x] 框线显示切换

### 3. 错误处理测试
- [x] 浏览器未初始化时加载 PDF
- [x] 加载损坏的 PDF 文件
- [x] 加载超大 PDF 文件

## 编译信息
- **编译配置**: Release
- **编译状态**: ✅ 成功（2 个警告）
- **安装包生成**: ✅ 成功（67.844 秒）

## 新安装包信息
- **文件名**: `大诚重命名工具_v2.3.8_安装包.exe`
- **版本**: 2.3.8
- **位置**: `installers/安装包/`
- **状态**: ✅ 已修复 JavaScript 执行错误

## 预防措施

### 代码规范
1. **始终检查浏览器初始化状态**
   ```csharp
   if (_browser?.IsBrowserInitialized == true)
   {
       // 执行 JavaScript
   }
   ```

2. **使用正确的执行方法**
   - 需要返回值 → `EvaluateScriptAsync`（确保 V8Context 已创建）
   - 不需要返回值 → `ExecuteJavaScriptAsync`（更可靠）

3. **添加超时机制**
   ```csharp
   int waitCount = 0;
   while (!_browser.IsBrowserInitialized && waitCount < 50)
   {
       await Task.Delay(100);
       waitCount++;
   }
   ```

### 错误处理
1. **捕获特定异常**
   ```csharp
   try
   {
       _browser.GetMainFrame().ExecuteJavaScriptAsync(script);
   }
   catch (Exception ex)
   {
       LogHelper.Error($"执行脚本失败: {ex.Message}", ex);
       // 通知用户或采取补救措施
   }
   ```

2. **提供友好的错误消息**
   ```csharp
   LoadError?.Invoke(this, new PdfLoadErrorEventArgs("浏览器初始化超时"));
   ```

## 完成时间
2026-01-20

## 状态
✅ 已修复并重新生成安装包  
✅ JavaScript 执行错误已解决  
✅ PDF 加载功能正常  
✅ 所有功能测试通过  

## 建议
**请使用此最新版本的安装包分发给用户，替换之前的所有版本。**
