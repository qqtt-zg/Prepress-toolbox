# TTC 字体兼容性问题解决方案

## 问题描述

### 症状
切换到某些常用字体（如微软雅黑、新宋体）时，程序抛出未处理的异常：

```
System.NullReferenceException
HResult=0x80004003
Message=未将对象引用设置到对象的实例。
Source=PdfSharp
StackTrace:
在 PdfSharp.Fonts.OpenType.OpenTypeFontFace.CetOrCreateFrom(XFontSource fontSource)
```

### 受影响的字体
- 微软雅黑 (msyh.ttc)
- 新宋体 (simsun.ttc)
- 其他 TrueType Collection (.ttc) 格式的字体

### 根本原因

1. **TTC 字体格式问题**
   - TrueType Collection (.ttc) 是一个包含多个字体的容器文件
   - PDFsharp 6.2.0 在解析某些 TTC 文件时存在兼容性问题
   - `OpenTypeFontFace.CetOrCreateFrom` 方法中的空引用异常

2. **自定义字体解析器的限制**
   - 我们的 `SystemFontResolver` 直接提供字体文件字节数据
   - PDFsharp 在解析 TTC 文件时需要额外的索引信息
   - 缺少索引信息导致解析失败

## 解决方案

### 策略：混合字体解析

使用 PDFsharp 的平台字体解析器（Platform Font Resolver）作为主解析器，自定义解析器作为回退。

#### 1. 启用 Windows 字体支持

```csharp
// 在 PdfSharpFontInitializer.Initialize() 中
GlobalFontSettings.UseWindowsFontsUnderWindows = true;
```

**效果：**
- PDFsharp 可以直接使用 Windows GDI 访问系统字体
- 支持 TTC 字体（微软雅黑、新宋体等）
- 不需要手动加载字体文件

**支持的字体：**
- Arial
- Times New Roman
- Courier New
- Verdana
- Lucida Console
- Symbol
- **以及所有 Windows 系统字体**

#### 2. 自定义解析器作为回退

```csharp
// 设置为回退解析器，而不是主解析器
GlobalFontSettings.FallbackFontResolver = fontResolver;
```

**解析顺序：**
```
1. 平台字体解析器（Platform Font Resolver）
   ├─ 使用 Windows GDI
   ├─ 支持所有系统字体（包括 TTC）
   └─ 如果失败 → 调用回退解析器

2. 自定义字体解析器（SystemFontResolver）
   ├─ 直接加载字体文件
   ├─ 支持 TTF/OTF 格式
   └─ 跳过 TTC 格式（返回 null）
```

#### 3. TTC 字体检测和跳过

```csharp
public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic)
{
    // ... 查找字体文件 ...
    
    // 检查是否为 TTC 文件
    string extension = Path.GetExtension(fontFilePath).ToLower();
    if (extension == ".ttc")
    {
        LogHelper.Debug($"检测到 TTC 字体文件，可能存在兼容性问题: {fontFilePath}");
        // 返回 null，让 PDFsharp 使用平台字体解析器
        return null;
    }
    
    // TTF/OTF 字体正常处理
    return new FontResolverInfo(faceName, false, false);
}
```

**逻辑：**
- 检测到 TTC 文件 → 返回 null
- PDFsharp 收到 null → 使用平台解析器
- 平台解析器 → 通过 Windows GDI 访问字体
- 成功加载 TTC 字体

### 4. 增强异常处理

#### A. 任务异常观察

```csharp
var task = System.Threading.Tasks.Task.Run(() =>
{
    // 测试代码
});

// 添加异常观察，防止未观察到的任务异常
task.ContinueWith(t =>
{
    if (t.IsFaulted && t.Exception != null)
    {
        LogHelper.Error($"字体测试任务异常: {t.Exception.GetBaseException().Message}", 
                       t.Exception.GetBaseException());
    }
}, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
```

#### B. 全局未观察任务异常处理

```csharp
// 在 Program.Main() 中
System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    LogHelper.Error($"未观察到的任务异常: {e.Exception.GetBaseException().Message}", 
                   e.Exception.GetBaseException());
    e.SetObserved(); // 标记异常已被观察，防止程序崩溃
};
```

#### C. UI 控件状态检查

```csharp
if (lblPreviewHint != null && !lblPreviewHint.IsDisposed)
{
    // 安全更新 UI
}
```

## 技术细节

### TrueType Collection (TTC) 格式

#### 文件结构
```
偏移  大小  说明
0     4     TTC Tag ("ttcf")
4     4     Version (1.0 或 2.0)
8     4     numFonts (字体数量)
12    4*n   OffsetTable[numFonts] (每个字体的偏移)
```

#### 特点
- 一个文件包含多个字体
- 共享字形数据，节省空间
- 需要索引信息才能访问特定字体

#### 示例
- `msyh.ttc` 包含：
  - Microsoft YaHei (Regular)
  - Microsoft YaHei Bold
  - Microsoft YaHei Light
  - 等等

### PDFsharp 字体解析流程

#### 使用平台解析器
```
1. 应用程序请求字体 "Microsoft YaHei"
   ↓
2. PDFsharp 调用 Platform Font Resolver
   ↓
3. Platform Font Resolver 使用 Windows GDI
   ↓
4. Windows GDI 访问 C:\Windows\Fonts\msyh.ttc
   ↓
5. 返回字体对象（包含正确的索引）
   ↓
6. 成功创建 XFont
```

#### 使用自定义解析器（TTC 失败）
```
1. 应用程序请求字体 "Microsoft YaHei"
   ↓
2. PDFsharp 调用 Custom Font Resolver
   ↓
3. Custom Font Resolver 读取 msyh.ttc 文件
   ↓
4. 返回字节数据（缺少索引信息）
   ↓
5. PDFsharp 尝试解析 TTC
   ↓
6. OpenTypeFontFace.CetOrCreateFrom 失败
   ↓
7. NullReferenceException ❌
```

### 混合解析策略

```
字体请求 "Microsoft YaHei"
    ↓
┌─────────────────────────────────┐
│ 1. Platform Font Resolver       │
│    (UseWindowsFontsUnderWindows) │
├─────────────────────────────────┤
│ 尝试通过 Windows GDI 访问        │
│ ✓ 成功 → 返回字体               │
│ ✗ 失败 → 调用回退解析器         │
└─────────────────────────────────┘
    ↓ (如果失败)
┌─────────────────────────────────┐
│ 2. Custom Font Resolver          │
│    (FallbackFontResolver)        │
├─────────────────────────────────┤
│ 检查字体文件类型                 │
│ • TTC → 返回 null (跳过)        │
│ • TTF/OTF → 加载并返回          │
└─────────────────────────────────┘
    ↓
如果都失败 → 抛出异常
```

## 改进效果

### 改进前

**TTC 字体（如微软雅黑）：**
- 自定义解析器尝试加载 TTC 文件
- PDFsharp 解析失败
- 抛出 NullReferenceException
- 程序崩溃 ❌

**TTF 字体（如 Arial）：**
- 自定义解析器成功加载
- 正常工作 ✅

### 改进后

**TTC 字体（如微软雅黑）：**
- 平台解析器通过 Windows GDI 访问
- 成功加载
- 正常工作 ✅

**TTF 字体（如 Arial）：**
- 平台解析器或自定义解析器加载
- 正常工作 ✅

**所有字体：**
- 异常被正确捕获和处理
- 程序不会崩溃 ✅
- 详细的日志记录 ✅

## 兼容性矩阵

| 字体 | 格式 | 平台解析器 | 自定义解析器 | 最终结果 |
|------|------|------------|--------------|----------|
| 微软雅黑 | TTC | ✅ 成功 | ⏭️ 跳过 | ✅ 可用 |
| 新宋体 | TTC | ✅ 成功 | ⏭️ 跳过 | ✅ 可用 |
| 宋体 | TTC | ✅ 成功 | ⏭️ 跳过 | ✅ 可用 |
| Arial | TTF | ✅ 成功 | ✅ 成功 | ✅ 可用 |
| Times New Roman | TTF | ✅ 成功 | ✅ 成功 | ✅ 可用 |
| Calibri | TTF | ✅ 成功 | ✅ 成功 | ✅ 可用 |

## 日志示例

### 成功加载 TTC 字体

```
[INFO] 开始初始化 PDFsharp 字体解析器
[DEBUG] ✓ 已启用 Windows 字体支持
[INFO] ✓ PDFsharp 字体解析器初始化成功
[DEBUG] 选择字体: Microsoft YaHei
[DEBUG] 解析字体: Microsoft YaHei, Bold=False, Italic=False
[DEBUG] 字体映射: Microsoft YaHei -> msyh
[DEBUG] 检测到 TTC 字体文件，可能存在兼容性问题: C:\Windows\Fonts\msyh.ttc
[DEBUG] ✗ 字体解析失败: Microsoft YaHei，返回 null 让 PDFsharp 使用平台解析器
[DEBUG] 字体 Microsoft YaHei 测试结果: 兼容, 文本宽度: 123.45
[DEBUG] 字体 Microsoft YaHei 验证通过：PDF兼容
```

### 成功加载 TTF 字体

```
[DEBUG] 选择字体: Arial
[DEBUG] 解析字体: Arial, Bold=False, Italic=False
[DEBUG] 字体映射: Arial -> arial
[DEBUG] ✓ 字体解析成功: Arial -> C:\Windows\Fonts\arial.ttf
[DEBUG] ✓ 加载字体文件: C:\Windows\Fonts\arial.ttf (765432 字节)
[DEBUG] 字体 Arial 测试结果: 兼容, 文本宽度: 98.76
[DEBUG] 字体 Arial 验证通过：PDF兼容
```

## 使用建议

### 1. 优先使用常用字体

**推荐字体（TTC 格式，使用平台解析器）：**
- 微软雅黑 (Microsoft YaHei)
- 宋体 (SimSun)
- 新宋体 (NSimSun)
- 黑体 (SimHei)

**推荐字体（TTF 格式，两种解析器都支持）：**
- Arial
- Times New Roman
- Calibri
- Consolas

### 2. 测试字体兼容性

1. 打开字体设置
2. 选择字体
3. 等待验证（1-2 秒）
4. 查看状态：
   - ✅ 绿色 "PDF兼容" = 可以使用
   - ❌ 红色 "PDF不兼容" = 选择其他字体

### 3. 查看日志

如果遇到问题，查看日志文件：
```
%AppData%\Roaming\大诚重命名工具\Logs\log_YYYYMMDD.txt
```

关键日志：
- `已启用 Windows 字体支持` - 平台解析器已启用
- `检测到 TTC 字体文件` - TTC 字体使用平台解析器
- `字体解析成功` - TTF 字体使用自定义解析器
- `PDF兼容` - 字体测试通过

## 故障排除

### Q1: 微软雅黑仍然显示不兼容？

**可能原因：**
- Windows 字体支持未启用
- 字体文件损坏

**解决方法：**
1. 查看日志，确认 "已启用 Windows 字体支持"
2. 重启应用程序
3. 重新安装字体

### Q2: 所有字体都不兼容？

**可能原因：**
- PDFsharp 初始化失败
- 字体解析器未正确设置

**解决方法：**
1. 查看启动日志
2. 确认 "PDFsharp 字体解析器初始化成功"
3. 重启应用程序

### Q3: 程序仍然崩溃？

**可能原因：**
- 其他未捕获的异常
- 内存问题

**解决方法：**
1. 查看完整的异常堆栈
2. 提供日志文件
3. 联系技术支持

## 总结

### 核心改进

1. ✅ **启用 Windows 字体支持**
   - 使用平台字体解析器
   - 支持 TTC 字体
   - 兼容性最好

2. ✅ **混合解析策略**
   - 平台解析器优先
   - 自定义解析器回退
   - TTC 字体跳过

3. ✅ **增强异常处理**
   - 任务异常观察
   - 全局异常处理
   - UI 控件状态检查

4. ✅ **详细日志记录**
   - 解析过程日志
   - 异常详细信息
   - 便于故障排除

### 技术优势

- **稳定性**：TTC 字体不再导致崩溃
- **兼容性**：支持所有 Windows 系统字体
- **可靠性**：多层异常保护
- **可维护性**：清晰的日志和错误信息

### 用户体验

- 微软雅黑等常用字体可以正常使用
- 程序不会因字体问题崩溃
- 清晰的兼容性状态提示
- 详细的日志便于问题诊断

---

**修复日期：** 2026-01-12  
**版本：** 2.0  
**状态：** 已修复并测试
