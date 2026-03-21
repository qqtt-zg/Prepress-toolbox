# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

基于 .NET Framework 4.8 的 Windows Forms 桌面应用程序，专为印刷行业设计，提供文件批量重命名、Excel 数据导入、PDF 处理、拼版等印前工作流功能。

### 解决方案

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

## 构建和运行命令

```bash
# 还原 NuGet 包
dotnet restore WindowsFormsApp3.sln

# Debug 构建
dotnet build WindowsFormsApp3.sln --configuration Debug

# Release 构建
dotnet build WindowsFormsApp3.sln --configuration Release

# 运行主程序
dotnet run --project src/WindowsFormsApp3/WindowsFormsApp3.csproj

# 运行测试
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj

# 运行单个测试类
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj --filter "FullyQualifiedName~FileRenameServiceTests"
```

## 发布安装包

```bash
# 1. 构建 Release 版本
dotnet build src/WindowsFormsApp3/WindowsFormsApp3.csproj --configuration Release

# 2. 编译安装包（需要 Inno Setup 6）
"C:/Program Files (x86)/Inno Setup 6/ISCC.exe" installers/Setup.iss

# 输出: installers/安装包/大诚重命名工具_v{版本号}_安装包.exe
```

## 核心架构

### 分层架构
- **表示层**: Forms、Controls、Presenters - UI 展示和用户交互
- **应用层**: Services、Commands - 业务逻辑和服务协调
- **领域层**: Models、Interfaces - 核心数据模型和业务规则
- **基础设施层**: Utils、Helpers - 通用工具和外部依赖

### 设计模式
1. **服务定位器** - `ServiceLocator` 管理服务生命周期，基于 Microsoft.Extensions.DependencyInjection
2. **MVP 模式** - 面板继承 `BasePanelControl`，View 通过接口解耦，Presenter 协调交互
3. **命令模式** - `ICommand` + `UndoRedoManager` 实现撤销/重做
4. **事件驱动** - `IEventBus` 发布-订阅模式，组件间解耦通信

### PDF 多引擎架构
- **PdfiumViewer**: 轻量预览（`x64/pdfium.dll`）
- **CefSharp + PDF.js**: Chromium 渲染
- **iText 7**: PDF 操作、转曲、字体处理

## 开发规范

- 新面板继承 `BasePanelControl`，通过 `ServiceLocator` 注册服务
- 可撤销操作继承 `CommandBase`，组件间通信使用 `IEventBus`
- 异步方法以 `Async` 结尾
- PDF 预览用 PdfiumViewer，操作用 iText
- 预设参数模型: `MaterialSelectionPreset`，预设禁用选项: `PresetIgnoreOptions`

## 印前排版术语

| 中文 | 英文 | 定义 |
|------|------|------|
| 连拼 | Step and Repeat | 相同设计重复排列 |
| 折手 | Signature Imposition | 相同尺寸按折叠顺序排列 |
| 出血 | Bleed | 裁切线外的延伸区域 |
| 刀模/刀线 | Die-cut | 模切轮廓线 |

## 平台和依赖

- Windows 专用，.NET Framework 4.8，x64
- **Spire.Pdf**: 本地 DLL（`Spire.Office Platinum v9.9.0/`）
- **PdfiumViewer**: 本地修改版（`lib/PdfiumViewer.dll`）
- **Ghostscript**: PDF 转曲和字体检测
- **Poppler**: 精确字体检测（pdftotext）

## 配置文件

- `configs.json` - 用户设置，位于 `%APPDATA%\大诚重命名工具\`
- `installers/Setup.iss` - Inno Setup 安装脚本

## 已知问题

### PDF 缩放轻微跳动
- **原因**: DOM 布局重排和滚动位置同步时序问题
- **参考**: `src/WindowsFormsApp3/Resources/pdfjs/bridge.js:260-285`

### 悬浮拖拽窗口提示文字不显示
- **原因**: Windows Forms 拖拽提示只在从资源管理器拖拽时显示
- **参考**: `src/WindowsFormsApp3/Forms/Utils/FloatingDropZoneForm.cs`
