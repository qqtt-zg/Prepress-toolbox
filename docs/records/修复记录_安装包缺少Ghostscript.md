# 修复记录：安装包缺少 Ghostscript

## 问题描述

**版本**: V2.3.8

**现象**: 
- PDF 预览功能正常
- **转曲功能失效**
- **字体检测功能失效**

## 问题分析

### 环境差异
- **开发环境**: 系统已安装 Ghostscript（C:\Program Files\gs\gs10.06.0）
- **安装包**: 缺少 Ghostscript 便携版文件

### 根本原因
1. 转曲和字体检测功能依赖 Ghostscript
2. 代码优先查找应用程序目录下的 `ghostscript` 子目录
3. 安装脚本中没有包含 Ghostscript 文件

### 代码依赖
`PdfFontOutlineService.cs` 中的 `FindGhostscript()` 方法查找顺序：
```csharp
// 1. 优先检查应用程序目录下的打包版本（便携版）
string portablePath = Path.Combine(appDir, "ghostscript", gsExe);

// 2. 检查应用程序根目录
string localPath = Path.Combine(appDir, gsExe);

// 3. 检查 PATH 环境变量

// 4. 检查系统安装位置
```

如果找不到 Ghostscript，转曲和字体检测功能将无法使用。

## 解决方案

### 步骤 1：复制 Ghostscript 到 Release 目录

从系统安装位置复制 Ghostscript 文件到编译输出目录：

```powershell
# 创建目录
New-Item -ItemType Directory -Force -Path ".\src\WindowsFormsApp3\bin\Release\net48\win-x64\ghostscript"
New-Item -ItemType Directory -Force -Path ".\src\WindowsFormsApp3\bin\Release\net48\win-x64\ghostscript\lib"

# 复制可执行文件
Copy-Item "C:\Program Files\gs\gs10.06.0\bin\gswin64c.exe" -Destination ".\src\WindowsFormsApp3\bin\Release\net48\win-x64\ghostscript\"

# 复制 DLL
Copy-Item "C:\Program Files\gs\gs10.06.0\bin\gsdll64.dll" -Destination ".\src\WindowsFormsApp3\bin\Release\net48\win-x64\ghostscript\"

# 复制库文件
Copy-Item "C:\Program Files\gs\gs10.06.0\lib\*" -Destination ".\src\WindowsFormsApp3\bin\Release\net48\win-x64\ghostscript\lib\" -Recurse
```

### 步骤 2：修改安装脚本

**文件**: `installers/Setup.iss`

**添加内容**：
```ini
; Ghostscript 便携版（必需 - 用于PDF转曲和字体检测）
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\ghostscript\gswin64c.exe"; DestDir: "{app}\ghostscript"; Flags: ignoreversion
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\ghostscript\gsdll64.dll"; DestDir: "{app}\ghostscript"; Flags: ignoreversion
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\ghostscript\lib\*"; DestDir: "{app}\ghostscript\lib"; Flags: ignoreversion recursesubdirs
```

## Ghostscript 文件清单

### 文件结构
```
ghostscript/
├── gswin64c.exe          # Ghostscript 命令行工具（~91 KB）
├── gsdll64.dll           # Ghostscript 核心库（~26 MB）
└── lib/                  # PostScript 库文件（~200+ 文件）
    ├── gs_init.ps        # 初始化脚本
    ├── pdf2ps.ps         # PDF 转 PS 脚本
    ├── ps2pdf.bat        # PS 转 PDF 批处理
    └── ...               # 其他库文件
```

### 文件大小
- `gswin64c.exe`: ~91 KB
- `gsdll64.dll`: ~26 MB
- `lib` 文件夹: ~200+ 文件
- **总大小**: ~27 MB

## 编译和发布

### 生成安装包
```bash
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installers\Setup.iss
```

**结果**: ✅ 成功（94.469 秒）

**输出文件**: `installers\安装包\大诚重命名工具_v2.3.8_安装包.exe`

### 安装包大小变化
- **修复前**: ~83 MB（缺少 Ghostscript）
- **修复后**: ~110 MB（包含 Ghostscript）

增加的文件：
- Ghostscript 文件：~27 MB

## 测试建议

### 1. 安装测试
- 在干净的系统上安装
- 检查安装目录是否包含 `ghostscript` 文件夹
- 验证文件结构：
  ```
  C:\Program Files\大诚重命名工具\
  ├── 大诚重命名工具.exe
  ├── ghostscript\
  │   ├── gswin64c.exe
  │   ├── gsdll64.dll
  │   └── lib\
  │       ├── gs_init.ps
  │       └── ...
  └── ...
  ```

### 2. 转曲功能测试
- 打开包含文字的 PDF 文件
- 点击"转曲"按钮
- 确认转曲成功，生成新的 PDF 文件
- 检查转曲后的 PDF 文字是否已转换为路径

### 3. 字体检测功能测试
- 打开 PDF 文件
- 点击"字体检测"按钮
- 确认能正确检测 PDF 中使用的字体
- 检查字体列表是否完整

### 4. 日志检查
查看日志文件 `logs\app_*.log`，确认 Ghostscript 被正确找到：
```
[字体转曲] 找到便携版 Ghostscript: C:\Program Files\大诚重命名工具\ghostscript\gswin64c.exe
```

## Ghostscript 查找逻辑

代码会按以下顺序查找 Ghostscript：

1. **便携版**（优先）: `{app}\ghostscript\gswin64c.exe`
2. **本地版本**: `{app}\gswin64c.exe`
3. **PATH 环境变量**: 在系统 PATH 中查找
4. **系统安装位置**: `C:\Program Files\gs\*\bin\gswin64c.exe`

安装包使用便携版方式，确保在没有系统安装 Ghostscript 的环境中也能正常工作。

## 相关文档

- [修复记录_安装包缺少PDF.js资源文件.md](修复记录_安装包缺少PDF.js资源文件.md) - PDF.js 资源文件修复
- [修复记录_CefSharp_JavaScript执行错误_V2.md](修复记录_CefSharp_JavaScript执行错误_V2.md) - JavaScript 执行修复
- [发布说明_V2.3.8.md](发布说明_V2.3.8.md) - 版本发布说明

## 总结

这次修复解决了转曲和字体检测功能失效的问题：

✅ 从系统安装位置复制 Ghostscript 文件  
✅ 添加 Ghostscript 到安装包  
✅ 使用便携版方式，无需系统安装  
✅ 包含完整的库文件（200+ 文件）  

**根本原因**: 安装脚本不完整，遗漏了 Ghostscript 依赖  
**解决方案**: 将 Ghostscript 作为便携版打包到安装包中  

**状态**: ✅ 已修复，可以分发给用户测试

---

**修复日期**: 2026-01-20  
**修复版本**: V2.3.8 完整版  
**Ghostscript 版本**: 10.06.0  
**修复人员**: Kiro AI Assistant
