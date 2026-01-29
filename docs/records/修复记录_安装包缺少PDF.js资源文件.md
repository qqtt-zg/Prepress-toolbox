# 修复记录：安装包缺少 PDF.js 资源文件

## 问题描述

**版本**: V2.3.8

**现象**: 
- 开发环境运行正常
- 安装包用户打开 PDF 文件时出现错误："PDF加载失败: 浏览器初始化超时"

## 问题分析

### 环境差异
- **开发环境**: 所有资源文件都在 `bin\Release\net48\win-x64\Resources` 目录下
- **安装包**: 缺少 `Resources\pdfjs` 文件夹及其内容

### 根本原因
Inno Setup 安装脚本中只包含了 `Resources\dc.ico`，没有包含其他资源文件：
- `Resources\pdfjs\*` - PDF.js 查看器（必需）
- `Resources\Fonts\*` - 字体文件（可选）
- `Resources\Icons\*` - 图标文件（可选）

### 代码依赖
`CefPdfPreviewControl.cs` 中的 `GetViewerPath()` 方法：
```csharp
private string GetViewerPath()
{
    return Path.Combine(Application.StartupPath, "Resources", "pdfjs", "viewer.html");
}
```

如果 `viewer.html` 不存在，浏览器无法加载 PDF.js，导致初始化超时。

## 解决方案

### 修改文件
`installers/Setup.iss`

### 修改内容

#### 1. 添加 PDF.js 资源文件（必需）

**修改前**：
```ini
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\Resources\dc.ico"; DestDir: "{app}"; Flags: ignoreversion
```

**修改后**：
```ini
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\Resources\dc.ico"; DestDir: "{app}\Resources"; Flags: ignoreversion

; PDF.js 资源文件（必需）
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\Resources\pdfjs\*"; DestDir: "{app}\Resources\pdfjs"; Flags: ignoreversion recursesubdirs

; 字体资源文件（可选）
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\Resources\Fonts\*"; DestDir: "{app}\Resources\Fonts"; Flags: ignoreversion

; 图标资源文件（可选）
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\Resources\Icons\*"; DestDir: "{app}\Resources\Icons"; Flags: ignoreversion
```

**关键点**：
- 使用 `recursesubdirs` 标志递归复制子目录
- `pdfjs` 文件夹包含：
  - `viewer.html` - PDF 查看器主页面
  - `bridge.js` - C# 与 JavaScript 桥接
  - `bridge_boxes.js` - 框线显示功能
  - `thumbnail-worker.js` - 缩略图生成
  - `build/pdf.min.js` - PDF.js 核心库
  - `build/pdf.worker.min.js` - PDF.js Worker

#### 2. 修复图标路径

**修改前**：
```ini
Name: "{group}\大诚重命名工具"; Filename: "{app}\大诚重命名工具.exe"; IconFilename: "{app}\dc.ico"
Name: "{commondesktop}\大诚重命名工具"; Filename: "{app}\大诚重命名工具.exe"; Tasks: desktopicon; IconFilename: "{app}\dc.ico"
```

**修改后**：
```ini
Name: "{group}\大诚重命名工具"; Filename: "{app}\大诚重命名工具.exe"; IconFilename: "{app}\Resources\dc.ico"
Name: "{commondesktop}\大诚重命名工具"; Filename: "{app}\大诚重命名工具.exe"; Tasks: desktopicon; IconFilename: "{app}\Resources\dc.ico"
```

## 资源文件清单

### PDF.js 文件夹结构
```
Resources/
├── pdfjs/
│   ├── viewer.html              # PDF 查看器主页面
│   ├── bridge.js                # C# 桥接脚本
│   ├── bridge_boxes.js          # 框线显示脚本
│   ├── thumbnail-worker.js      # 缩略图生成脚本
│   └── build/
│       ├── pdf.min.js           # PDF.js 核心库
│       └── pdf.worker.min.js    # PDF.js Worker
├── Fonts/                       # 字体文件（7个）
├── Icons/                       # SVG 图标（2个）
└── dc.ico                       # 应用程序图标
```

## 编译和发布

### 生成安装包
```bash
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installers\Setup.iss
```

**结果**: ✅ 成功（83.344 秒）

**输出文件**: `installers\安装包\大诚重命名工具_v2.3.8_安装包.exe`

### 安装包大小变化
- **修复前**: ~63 MB（缺少资源文件）
- **修复后**: ~83 MB（包含所有资源文件）

增加的文件：
- PDF.js 文件：~500 KB
- 字体文件：~20 MB
- 图标文件：~10 KB

## 测试建议

### 1. 安装测试
- 在干净的系统上安装
- 检查安装目录是否包含 `Resources` 文件夹
- 验证文件结构：
  ```
  C:\Program Files\大诚重命名工具\
  ├── 大诚重命名工具.exe
  ├── Resources\
  │   ├── pdfjs\
  │   │   ├── viewer.html
  │   │   ├── bridge.js
  │   │   └── build\
  │   ├── Fonts\
  │   ├── Icons\
  │   └── dc.ico
  └── ...
  ```

### 2. PDF 功能测试
- 启动程序，切换到 PDF 操作界面
- 打开 PDF 文件，确认能正常加载
- 测试页面导航、旋转、主题切换等功能
- 检查进度条是否正常显示

### 3. 错误排查
如果仍然出现问题，检查：
- 安装目录权限是否正确
- 防病毒软件是否阻止文件访问
- 查看日志文件 `logs\app_*.log`

## 相关文档

- [修复记录_CefSharp_JavaScript执行错误_V2.md](修复记录_CefSharp_JavaScript执行错误_V2.md) - JavaScript 执行修复
- [修复记录_CefSharp资源文件缺失.md](修复记录_CefSharp资源文件缺失.md) - CefSharp 资源文件修复
- [发布说明_V2.3.8.md](发布说明_V2.3.8.md) - 版本发布说明

## 总结

这次修复解决了安装包与开发环境的差异问题：

✅ 添加 PDF.js 资源文件到安装包  
✅ 添加字体和图标资源文件  
✅ 修复图标路径引用  
✅ 使用 `recursesubdirs` 递归复制子目录  

**根本原因**: 安装脚本不完整，遗漏了关键资源文件  
**解决方案**: 完善 Inno Setup 脚本，包含所有必需的资源文件  

**状态**: ✅ 已修复，可以分发给用户测试

---

**修复日期**: 2026-01-20  
**修复版本**: V2.3.8 最终版  
**修复人员**: Kiro AI Assistant
