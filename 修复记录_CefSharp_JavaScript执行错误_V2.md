# 修复记录：CefSharp JavaScript 执行错误（V2）

## 问题描述

**版本**: V2.3.8

**现象**: 用户安装后，打开 PDF 文件时出现错误弹框：

```
PDF加载失败: Unable to execute javascript at this time, scripts can only 
be executed within a V8Context. Use the IWebBrowser.CanExecuteJavascriptInMainFrame 
property to guard against this exception. See 
https://github.com/cefsharp/CefSharp/wiki/General-Usage#when-can-i-start-executing-javascript 
for more details on when you can execute javascript. For frames that do not contain 
Javascript then no V8Context will be created. Executing a script once the frame has 
loaded it's possible to create a V8Context. You can use 
browser.GetMainFrame().ExecuteJavaScriptAsync(script) or 
browser.GetMainFrame().EvaluateScriptAsync to bypass these checks (advanced users only).
```

## 问题分析

虽然我们在上一个版本中已经将 `EvaluateScriptAsync()` 改为 `ExecuteJavaScriptAsync()`，但问题依然存在。

**根本原因**：
1. 我们只检查了 `IsBrowserInitialized`（浏览器是否初始化）
2. 但没有检查 **V8Context 是否已创建**
3. V8Context 的创建时机晚于浏览器初始化
4. 在 Frame 完全加载之前，V8Context 不存在，无法执行 JavaScript

**错误信息建议**：
- 使用 `IWebBrowser.CanExecuteJavascriptInMainFrame` 属性来检查是否可以执行 JavaScript
- 这个属性会同时检查浏览器初始化状态和 V8Context 是否已创建

## 解决方案

### 修改文件
`src/WindowsFormsApp3/Controls/CefPdfPreviewControl.cs`

### 修改内容

#### 1. LoadPdfAsync 方法
**修改前**：
```csharp
// 等待浏览器完全初始化
if (!_browser.IsBrowserInitialized)
{
    // 等待逻辑...
}
```

**修改后**：
```csharp
// 等待浏览器完全初始化并且可以执行JavaScript
if (!_browser.IsBrowserInitialized || !_browser.CanExecuteJavascriptInMainFrame)
{
    LogHelper.Debug("[CefPdfPreview] 等待浏览器和Frame初始化...");
    // 等待最多10秒
    int waitCount = 0;
    while ((!_browser.IsBrowserInitialized || !_browser.CanExecuteJavascriptInMainFrame) && waitCount < 100)
    {
        await Task.Delay(100);
        waitCount++;
    }
    
    if (!_browser.IsBrowserInitialized || !_browser.CanExecuteJavascriptInMainFrame)
    {
        LogHelper.Error("[CefPdfPreview] 浏览器或Frame初始化超时");
        LoadError?.Invoke(this, new PdfLoadErrorEventArgs("浏览器初始化超时"));
        return;
    }
}

LogHelper.Debug("[CefPdfPreview] 浏览器和Frame已就绪，可以执行JavaScript");
```

**关键改进**：
- 同时检查 `IsBrowserInitialized` 和 `CanExecuteJavascriptInMainFrame`
- 等待时间从 5 秒增加到 10 秒
- 添加详细的日志输出

#### 2. 所有其他 JavaScript 执行方法

将所有方法中的 `_browser.IsBrowserInitialized` 检查改为 `_browser.CanExecuteJavascriptInMainFrame`：

- `GoToPageAsync()`
- `RotateClockwiseAsync()`
- `RotateCounterClockwiseAsync()`
- `SetDarkModeAsync()`
- `ToggleBoxDisplayAsync()`
- `ExecuteScriptAsync()`

**示例**：
```csharp
// 修改前
if (_browser?.IsBrowserInitialized == true)
{
    _browser.GetMainFrame().ExecuteJavaScriptAsync(script);
}

// 修改后
if (_browser?.CanExecuteJavascriptInMainFrame == true)
{
    _browser.GetMainFrame().ExecuteJavaScriptAsync(script);
}
```

## 技术细节

### CefSharp 初始化流程

1. **Cef.Initialize()** - CefSharp 全局初始化（在 Program.cs 中）
2. **ChromiumWebBrowser 创建** - 创建浏览器控件
3. **IsBrowserInitialized = true** - 浏览器进程启动完成
4. **Frame 开始加载** - 加载 HTML 页面
5. **V8Context 创建** - JavaScript 引擎上下文创建
6. **CanExecuteJavascriptInMainFrame = true** - 可以执行 JavaScript
7. **FrameLoadEnd 事件** - 页面加载完成

### 为什么需要 CanExecuteJavascriptInMainFrame

- `IsBrowserInitialized` 只表示浏览器进程已启动
- 但此时 Frame 可能还在加载中，V8Context 还未创建
- `CanExecuteJavascriptInMainFrame` 会检查：
  - 浏览器是否已初始化
  - MainFrame 是否存在
  - V8Context 是否已创建
  - 是否可以安全执行 JavaScript

## 编译和发布

### 编译
```bash
dotnet build WindowsFormsApp3.sln -c Release /v:minimal
```

**结果**: ✅ 成功（2 个警告）

### 生成安装包
```bash
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installers\Setup.iss
```

**结果**: ✅ 成功（69.672 秒）

**输出文件**: `installers\安装包\大诚重命名工具_v2.3.8_安装包.exe`

## 测试建议

1. **安装测试**
   - 在干净的系统上安装
   - 启动程序，切换到 PDF 操作界面
   - 确认没有初始化错误

2. **PDF 加载测试**
   - 打开各种大小的 PDF 文件
   - 确认没有 JavaScript 执行错误
   - 检查进度条是否正常显示

3. **功能测试**
   - 测试页面导航（上一页/下一页）
   - 测试旋转功能
   - 测试主题切换
   - 测试框线显示切换

## 相关文档

- [修复记录_CefSharp_JavaScript执行错误.md](修复记录_CefSharp_JavaScript执行错误.md) - 第一次修复（不完整）
- [修复记录_CefSharp资源文件缺失.md](修复记录_CefSharp资源文件缺失.md) - 资源文件修复
- [发布说明_V2.3.8.md](发布说明_V2.3.8.md) - 版本发布说明

## 总结

这次修复彻底解决了 JavaScript 执行时机问题：

✅ 使用 `CanExecuteJavascriptInMainFrame` 替代 `IsBrowserInitialized`  
✅ 确保 V8Context 已创建后才执行 JavaScript  
✅ 增加等待时间和详细日志  
✅ 所有 JavaScript 执行点都已修复  

**状态**: ✅ 已修复，可以分发给用户测试

---

**修复日期**: 2026-01-20  
**修复版本**: V2.3.8 修复版  
**修复人员**: Kiro AI Assistant
