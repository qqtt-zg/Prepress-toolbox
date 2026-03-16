# CLAUDE.md

本文件为 Claude Code (claude.ai/code) 在本项目中工作时提供指导。

## 项目概述

基于 .NET Framework 4.8 的 Windows Forms 桌面应用程序，专为印刷行业设计，提供文件批量重命名、Excel 数据导入、PDF 处理等印前工作流功能。

### 解决方案

**WindowsFormsApp3.sln**
- `WindowsFormsApp3` - 主程序（C# 9.0，net48，x64）
- `WindowsFormsApp3.Tests` - 测试项目（xUnit + MSTest）

### 核心功能

| 模块 | 功能 |
|------|------|
| 文件重命名 | 批量重命名、正则匹配、返单识别、事件分组 |
| Excel 导入 | 数据匹配、列组合、未分组处理 |
| PDF 处理 | 多引擎渲染（Pdfium/CefSharp+PDF.js/iText）、形状识别 |
| 拼版 | 连拼（ImpositionService）、骑马钉折手（SaddleStitchService） |
| PDF 检查 | 字体检测、出血检查、刀线检查 |
| 撤销重做 | 命令模式实现 |

### UI 架构

MainShellForm + 面板化设计，功能面板继承 `BasePanelControl`：
- FileRenamePanel、ExcelImportPanel、ImpositionWorkspacePanel、AeWorkspacePanel
- PdfInspectorPanel、PdfOperationsPanel、DatabasePanel、SettingsPanel

## 构建和运行命令

### 基本构建

```bash
# 还原 NuGet 包
dotnet restore WindowsFormsApp3.sln

# 构建 Debug 版本
dotnet build WindowsFormsApp3.sln --configuration Debug

# 构建 Release 版本
dotnet build WindowsFormsApp3.sln --configuration Release

# 清理解决方案
dotnet clean WindowsFormsApp3.sln
```

### 运行应用

```bash
# 运行主程序
dotnet run --project src/WindowsFormsApp3/WindowsFormsApp3.csproj

# 或直接运行编译后的 exe
src/WindowsFormsApp3/bin/Debug/net48/win-x64/大诚重命名工具.exe
```

### 测试命令

```bash
# 运行所有测试
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj

# 运行单个测试类
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj --filter "FullyQualifiedName~FileRenameServiceTests"

# 运行单个测试方法
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj --filter "FullyQualifiedName~TestMethodName"
```

### 发布和安装包

```bash
# 1. 构建 Release 版本
dotnet build src/WindowsFormsApp3/WindowsFormsApp3.csproj --configuration Release

# 2. 编译安装包（需要 Inno Setup 6）
"C:/Program Files (x86)/Inno Setup 6/ISCC.exe" installers/Setup.iss

# 安装包输出: installers/安装包/大诚重命名工具_v{版本号}_安装包.exe
# 版本号在 installers/Setup.iss 中定义
```

## 印前排版术语

> **重要**: 以下术语遵循国内印刷行业标准，开发时请使用正确术语。

### 连拼 (Step and Repeat)
将**相同**的设计按固定间距重复排列，形成网格布局。用于不干胶标签、名片等。
- **项目实现**: `ImpositionService` 中的布局计算功能

### 折手 (Signature Imposition)
将**相同尺寸**的页面按**特定顺序**排列，用于折叠/装订后顺序正确。用于骑马钉、折页、画册内页。
- **项目实现**: `SaddleStitchService`

### 合版 (Gang Run) - 术语预留
将**不同设计**或**不同尺寸**的内容拼在同一版面。项目未规划此功能。

### 术语对照表

| 中文 | 英文 | 定义 | 状态 |
|------|------|------|------|
| 连拼 | Step and Repeat | 相同设计重复排列 | ✅ 已实现 |
| 折手 | Signature Imposition | 相同尺寸按折叠顺序排列 | ✅ 已实现 |
| 合版 | Gang Run | 不同尺寸混合拼版 | 🔮 未规划 |
| 出血 | Bleed | 裁切线外的延伸区域 | ✅ 已实现 |
| 刀模/刀线 | Die-cut | 模切轮廓线 | ✅ 已实现 |

## 核心架构

### 分层架构

- **表示层**: Forms、Controls、Presenters - UI 展示和用户交互
- **应用层**: Services、Commands - 业务逻辑和服务协调
- **领域层**: Models、Interfaces - 核心数据模型和业务规则
- **基础设施层**: Utils、Helpers - 通用工具和外部依赖

### 关键设计模式

1. **服务定位器模式** - `ServiceLocator` 管理服务生命周期，基于 Microsoft.Extensions.DependencyInjection
2. **MVP 模式** - 面板继承 `BasePanelControl`，View 通过接口解耦，Presenter 协调交互
3. **命令模式** - `ICommand` + `UndoRedoManager` 实现撤销/重做，支持批量操作
4. **事件驱动** - `IEventBus` 发布-订阅模式，组件间解耦通信

### 核心服务

| 类别 | 服务 |
|------|------|
| 文件 | FileRenameService、BatchProcessingService、FileMonitor |
| PDF | PdfProcessingService、PdfInspectorService、PdfFontInspectorService、DimensionCalculationService |
| 拼版 | ImpositionService（连拼）、SaddleStitchService（折手） |
| 配置 | ApplicationSettingsService、EventBus、ThemeManager、UndoRedoService |

### PDF 多引擎架构

- **PdfiumViewer**: 轻量预览（本地 DLL + x64/pdfium.dll）
- **CefSharp + PDF.js**: Chromium 渲染
- **iText 7**: PDF 操作、转曲、字体处理

接口：`IPdfPreviewControl`、`IPdfDimensionService`、IPdfInfoProvider`

## 项目要点

### 平台和依赖
- Windows 专用，.NET Framework 4.8，x64
- **Spire.Pdf**: 本地 DLL（`Spire.Office Platinum v9.9.0/`）
- **PdfiumViewer**: 本地修改版（`lib/PdfiumViewer.dll`）
- **中文字体**: simhei.ttf、simsun.ttc、msyh.ttc、NotoSansSC-Regular.ttf

### 主要 NuGet
- UI: AntdUI、ReaLTaiizor、Krypton.Toolkit、Svg
- PDF: CefSharp、iText、PDFsharp、PdfiumViewer
- 数据: EPPlus、Newtonsoft.Json
- DI: Microsoft.Extensions.DependencyInjection

### 开发规范
- 新面板继承 `BasePanelControl`，通过 `ServiceLocator` 注册服务
- 可撤销操作继承 `CommandBase`，组件间通信使用 `IEventBus`
- 异步方法以 `Async` 结尾，新增功能面板时实现 Presenter + View 接口
- PDF 预览用 PdfiumViewer，操作用 iText

### 测试
- 框架: xUnit + MSTest + Moq + FlaUI（UI 自动化）
- 运行: `dotnet test --filter "FullyQualifiedName~类名"`

## 配置文件

| 文件 | 说明 |
|------|------|
| `configs.json` | 用户设置（导出路径、预设参数等），位于 `%APPDATA%\大诚重命名工具\` |
| `Properties/Config/App.config` | 应用程序配置 |
| `installers/Setup.iss` | Inno Setup 安装脚本 |

## 已知问题

### PDF 缩放轻微跳动
- **现象**: Ctrl+滚轮缩放 PDF 时画面轻微跳动（约 2-5px）
- **原因**: DOM 布局重排和滚动位置同步的时序问题
- **临时方案**: 使用"适配宽度"或"适配页面"按钮重置视图
- **参考**: `src/WindowsFormsApp3/Resources/pdfjs/bridge.js:260-285`

### 悬浮拖拽窗口提示文字不显示
- **现象**: 从应用内拖拽文件时，鼠标指针只显示小长方形，不显示"复制到..."提示
- **原因**: Windows Forms 的拖拽提示通常只在从资源管理器拖拽时显示
- **参考**: `src/WindowsFormsApp3/Forms/Utils/FloatingDropZoneForm.cs`

## 搁置功能

### 导出路径功能扩展 (2026-03-19)
**状态**: 📦 搁置
**已实现**:
- 导出路径表格宽度拓宽至 950px
- 子文件夹预设配置（材料、出血、颜色等）
- 子文件夹继承父路径预设，可单独修改
- 表格层次结构显示（缩进 + └─ 前缀）

**待优化**:
- [ ] 子路径删除功能（右键菜单）
- [ ] 子路径手动添加功能
- [ ] 路径验证和错误提示优化
- [ ] 预设参数批量应用功能

**相关文件**:
- `src/WindowsFormsApp3/Forms/Controls/Settings/SettingsPathControl.cs`
- `src/WindowsFormsApp3/Forms/Main/MaterialSelectFormModern.cs`
- `src/WindowsFormsApp3/Models/MaterialSelectionPreset.cs`

## 开发环境

Visual Studio 2019/2022 + .NET Framework 4.8 开发工具