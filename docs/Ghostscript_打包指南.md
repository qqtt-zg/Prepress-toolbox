# Ghostscript 打包指南

## 概述

本指南说明如何将 Ghostscript 打包到应用程序安装包中，让用户无需单独安装 Ghostscript 即可使用字体转曲功能。

## 许可证说明

### Ghostscript 许可证类型

Ghostscript 提供两种许可证：

1. **AGPL 许可证**（免费开源）
   - 可以免费使用和分发
   - 要求：如果你的应用是开源的，或者用户可以通过网络访问，你必须开源你的代码
   - 适用于：开源项目、内部使用的工具

2. **商业许可证**（付费）
   - 无需开源你的代码
   - 适用于：商业闭源软件
   - 联系：https://www.ghostscript.com/licensing/

### 建议

- 如果你的应用是**开源的**或**仅内部使用**，可以使用 AGPL 版本
- 如果你的应用是**商业闭源软件**，建议购买商业许可证
- 在应用程序的"关于"页面中注明使用了 Ghostscript 及其许可证

## 打包步骤

### 1. 下载 Ghostscript

从官方网站下载 Ghostscript：
- 官网：https://www.ghostscript.com/releases/gsdnld.html
- 选择：Ghostscript 10.06.0 for Windows (64-bit)
- 文件：`gs10.06.0w64.exe`

### 2. 提取必要文件

安装 Ghostscript 后，从安装目录提取以下文件：

```
C:\Program Files\gs\gs10.06.0\
├── bin\
│   ├── gswin64c.exe    (必需 - 命令行可执行文件)
│   ├── gsdll64.dll     (必需 - 核心库)
│   └── gsdll64.lib     (可选)
└── lib\                (必需 - Ghostscript 库文件)
    ├── gs_init.ps
    ├── pdf_main.ps
    ├── *.ps
    └── ... (所有 .ps 文件)
```

### 3. 创建打包目录结构

在你的项目中创建以下结构：

```
项目根目录/
├── ghostscript/
│   ├── gswin64c.exe
│   ├── gsdll64.dll
│   └── lib/
│       ├── gs_init.ps
│       ├── pdf_main.ps
│       └── ... (所有库文件)
```

### 4. 复制文件到项目

**方法 A：手动复制**

1. 在项目根目录创建 `ghostscript` 文件夹
2. 复制上述文件到该文件夹
3. 在 Visual Studio 中，将这些文件添加到项目
4. 设置文件属性：
   - 右键点击文件 → 属性
   - "复制到输出目录" → "如果较新则复制"

**方法 B：使用构建脚本**

创建 `scripts/copy-ghostscript.ps1`：

```powershell
# 复制 Ghostscript 文件到输出目录
$gsSource = "C:\Program Files\gs\gs10.06.0"
$outputDir = ".\src\WindowsFormsApp3\bin\Debug\net48\win-x64\ghostscript"

# 创建目标目录
New-Item -ItemType Directory -Force -Path $outputDir
New-Item -ItemType Directory -Force -Path "$outputDir\lib"

# 复制可执行文件和 DLL
Copy-Item "$gsSource\bin\gswin64c.exe" -Destination $outputDir
Copy-Item "$gsSource\bin\gsdll64.dll" -Destination $outputDir

# 复制库文件
Copy-Item "$gsSource\lib\*" -Destination "$outputDir\lib" -Recurse

Write-Host "Ghostscript 文件已复制到输出目录"
```

在项目文件 (`.csproj`) 中添加构建后事件：

```xml
<Target Name="CopyGhostscript" AfterTargets="Build">
  <Exec Command="powershell -ExecutionPolicy Bypass -File &quot;$(ProjectDir)..\..\scripts\copy-ghostscript.ps1&quot;" />
</Target>
```

### 5. 更新 .gitignore

如果使用 Git，添加到 `.gitignore`：

```
# Ghostscript 便携版（文件较大，不提交到仓库）
ghostscript/
```

### 6. 验证打包

运行应用程序，检查日志：

```
[DEBUG] [字体转曲] 找到便携版 Ghostscript: C:\...\ghostscript\gswin64c.exe
```

## 安装包制作

### 使用 Inno Setup

在 Inno Setup 脚本中添加：

```iss
[Files]
; 主程序
Source: "bin\Release\大诚重命名工具.exe"; DestDir: "{app}"; Flags: ignoreversion

; Ghostscript 便携版
Source: "ghostscript\gswin64c.exe"; DestDir: "{app}\ghostscript"; Flags: ignoreversion
Source: "ghostscript\gsdll64.dll"; DestDir: "{app}\ghostscript"; Flags: ignoreversion
Source: "ghostscript\lib\*"; DestDir: "{app}\ghostscript\lib"; Flags: ignoreversion recursesubdirs
```

### 使用 WiX Toolset

在 WiX 配置中添加：

```xml
<Directory Id="GHOSTSCRIPTDIR" Name="ghostscript">
  <Component Id="GhostscriptExe" Guid="YOUR-GUID-HERE">
    <File Source="ghostscript\gswin64c.exe" />
  </Component>
  <Component Id="GhostscriptDll" Guid="YOUR-GUID-HERE">
    <File Source="ghostscript\gsdll64.dll" />
  </Component>
  <Directory Id="GHOSTSCRIPTLIBDIR" Name="lib">
    <!-- 添加所有库文件 -->
  </Directory>
</Directory>
```

## 文件大小优化

### 完整版 vs 精简版

**完整版**（推荐）：
- 大小：约 50-60 MB
- 包含所有功能
- 兼容性最好

**精简版**（高级）：
- 大小：约 20-30 MB
- 仅包含 PDF 处理必需的文件
- 需要测试确保功能正常

### 精简方法

只保留以下库文件（需要测试）：

```
lib/
├── gs_init.ps
├── pdf_main.ps
├── pdf_ops.ps
├── pdf_base.ps
├── pdf_draw.ps
├── pdf_font.ps
└── ... (PDF 相关的 .ps 文件)
```

## 许可证文件

在应用程序中包含 Ghostscript 许可证：

1. 创建 `licenses/GHOSTSCRIPT_LICENSE.txt`
2. 从 Ghostscript 安装目录复制 `LICENSE` 文件
3. 在应用程序的"关于"对话框中显示：

```csharp
// 在关于对话框中
var licenseText = @"
本软件使用了 Ghostscript
版本：10.06.0
许可证：AGPL v3
网站：https://www.ghostscript.com/

Ghostscript 是一个开源的 PostScript 和 PDF 解释器。
详细许可证信息请参见 licenses/GHOSTSCRIPT_LICENSE.txt
";
```

## 用户体验优化

### 首次运行检测

在应用程序启动时检测 Ghostscript：

```csharp
public void CheckGhostscript()
{
    var service = new PdfFontOutlineService();
    
    if (!service.IsGhostscriptAvailable())
    {
        var result = MessageBox.Show(
            "未检测到 Ghostscript，字体转曲功能将不可用。\n\n" +
            "是否要下载并安装 Ghostscript？",
            "Ghostscript 未安装",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information
        );
        
        if (result == DialogResult.Yes)
        {
            // 打开下载页面
            Process.Start("https://www.ghostscript.com/releases/gsdnld.html");
        }
    }
}
```

### 功能降级

如果 Ghostscript 不可用，优雅地禁用功能：

```csharp
// 在 PdfInspectorControl 中
if (!_fontOutlineService.IsGhostscriptAvailable())
{
    _outlineButton.Enabled = false;
    _outlineButton.ToolTipText = "需要安装 Ghostscript 才能使用此功能";
}
```

## 测试清单

打包后测试：

- [ ] 在**全新的 Windows 系统**上测试（虚拟机）
- [ ] 确认不需要单独安装 Ghostscript
- [ ] 测试字体转曲功能正常工作
- [ ] 检查日志确认使用的是便携版 Ghostscript
- [ ] 测试不同类型的 PDF 文件
- [ ] 验证生成的 PDF 文件正确

## 常见问题

### Q: 打包后文件太大怎么办？

A: 
1. 使用压缩工具（如 UPX）压缩 `gswin64c.exe`
2. 精简 `lib` 目录，只保留必需文件
3. 使用在线安装器，首次运行时下载 Ghostscript

### Q: 是否需要 32 位版本？

A: 
- 如果你的应用只支持 64 位，只需打包 `gswin64c.exe`
- 如果需要支持 32 位系统，同时打包 `gswin32c.exe` 和 `gsdll32.dll`

### Q: 如何更新 Ghostscript 版本？

A:
1. 下载新版本
2. 替换 `ghostscript` 目录中的文件
3. 测试功能
4. 更新版本号说明

### Q: 用户已安装 Ghostscript 怎么办？

A: 代码会自动检测：
1. 优先使用应用程序目录的便携版
2. 如果没有，使用系统安装的版本
3. 两者都可以正常工作

## 分发策略

### 策略 A：完整打包（推荐）

**优点：**
- 用户体验最好，开箱即用
- 无需网络连接
- 版本一致，兼容性好

**缺点：**
- 安装包较大（增加 50-60 MB）

### 策略 B：可选组件

在安装程序中将 Ghostscript 设为可选组件：

```iss
[Components]
Name: "main"; Description: "主程序"; Types: full compact custom; Flags: fixed
Name: "ghostscript"; Description: "Ghostscript (字体转曲功能)"; Types: full
```

### 策略 C：在线下载

首次使用转曲功能时提示下载：

```csharp
if (!service.IsGhostscriptAvailable())
{
    var downloader = new GhostscriptDownloader();
    await downloader.DownloadAndInstallAsync();
}
```

## 总结

推荐使用**策略 A（完整打包）**：
- 将 Ghostscript 打包到 `ghostscript` 子目录
- 在安装包中包含所有必需文件
- 提供最佳的用户体验
- 文件大小增加可接受（50-60 MB）

---

**更新日期：** 2026-01-19
**Ghostscript 版本：** 10.06.0
