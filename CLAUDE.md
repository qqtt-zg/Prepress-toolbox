# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个基于 .NET Framework 4.8 的 Windows Forms 桌面应用程序，名为"大诚重命名工具"。该工具专为生产环境设计，提供文件批量重命名、Excel数据导入、PDF处理等高级功能。

### 解决方案结构

项目包含一个解决方案：

**WindowsFormsApp3.sln** (主解决方案)
- `WindowsFormsApp3` - 主应用程序（C# 9.0，目标框架 net48，平台 x64）
- `WindowsFormsApp3.Tests` - 测试项目（C# 10.0，使用 xUnit + MSTest）

### 核心功能

- 批量文件重命名（支持正则表达式、智能返单识别）
- Excel 数据导入（自动匹配、列组合）
- 事件分组管理（TreeView 层级化、拖拽排序）
- PDF 处理（Pdfium 渲染引擎 + CefSharp/PDF.js + iText）
- 拼版功能（ImpositionService、SaddleStitchService）
- PDF 检查器（字体检测、出血检查、刀线检查）
- 撤销/重做系统（命令模式实现）

### UI 架构状态

项目采用 MainShellForm + 面板化设计（当前分支：`排版扩展`）：

- **MainShellForm**: 主窗体框架，提供侧边导航和内容区域
- **功能面板**: 各模块独立为 Panel（继承自 `BasePanelControl`），支持 MVP 模式
  - `FileRenamePanel` - 文件重命名面板
  - `ExcelImportPanel` - Excel导入面板
  - `ImpositionWorkspacePanel` - 拼版工作区面板
  - `AeWorkspacePanel` - Acrobat 插件对接面板
  - `PdfInspectorPanel` - PDF 检查器面板
  - `PdfOperationsPanel` - PDF 操作面板
  - `DatabasePanel` - 数据库面板
  - `SettingsPanel` - 设置面板

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

# 运行测试并生成覆盖率报告
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj --collect:"XPlat Code Coverage"

# 运行测试并输出详细日志
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj --logger "console;verbosity=detailed"
```

### 发布命令

```bash
# 发布为依赖框架的可执行文件（生成在 bin/Release/net48/win-x64/）
dotnet build src/WindowsFormsApp3/WindowsFormsApp3.csproj --configuration Release --runtime win-x64

# 创建安装包（需要 Inno Setup）
# 1. 确保已发布 Release 版本
# 2. 运行 installers/Setup.iss 生成安装包
# 安装包输出到: ./安装包/大诚重命名工具_v{版本号}_安装包.exe
```

## 核心架构

### 目录结构

```
src/WindowsFormsApp3/
├── Forms/                          # 表示层 - UI组件
│   ├── Main/                       # 主窗体
│   │   └── MainShellForm.cs       # 主界面框架（带侧边导航和内容区域）
│   ├── Panels/                     # 功能面板（模块化设计，继承自 BasePanelControl）
│   │   ├── BasePanelControl.cs    # 面板基类
│   │   ├── FileRenamePanel        # 文件重命名面板
│   │   ├── ExcelImportPanel       # Excel导入面板
│   │   ├── ImpositionWorkspacePanel  # 拼版工作区面板
│   │   ├── AeWorkspacePanel       # Acrobat插件对接面板
│   │   ├── PdfInspectorPanel      # PDF检查器面板
│   │   ├── PdfOperationsPanel     # PDF操作面板
│   │   ├── DatabasePanel          # 数据库面板
│   │   └── SettingsPanel          # 设置面板
│   └── Dialogs/                   # 对话框
├── Presenters/                     # MVP模式 - 展示器
│   ├── FileRenamePanelPresenter.cs  # 文件重命名面板展示器
│   ├── PdfProcessingPresenter.cs    # PDF处理展示器
│   └── IFileRenamePanelView.cs      # 面板视图接口定义
├── Services/                       # 应用层 - 业务逻辑
│   ├── ServiceLocator.cs          # 依赖注入容器
│   ├── FileRenameService.cs       # 文件重命名核心服务
│   ├── ImpositionService.cs       # 拼版服务
│   ├── SaddleStitchService.cs     # 骑马钉拼版服务
│   ├── DimensionCalculationService.cs  # PDF尺寸计算服务
│   ├── EventGroupConfigurationService.cs  # 事件分组配置服务
│   ├── BatchProcessingService.cs  # 批量处理服务
│   ├── PdfInspectorService.cs     # PDF检查服务
│   ├── PdfFontInspectorService.cs # PDF字体检查服务
│   ├── PdfProcessingService.cs    # PDF处理服务
│   ├── EventBus.cs                # 事件总线（根目录）
│   ├── Events/                    # 事件定义
│   │   ├── IEventBus.cs
│   │   ├── FileOperationEvent.cs
│   │   └── ...
│   ├── ConfigService.cs           # 配置管理服务
│   ├── UndoRedoService.cs         # 撤销重做服务
│   ├── FileMonitor.cs             # 文件监控服务
│   └── ThemeManager.cs            # 主题管理器
├── Commands/                       # 命令模式实现
│   ├── ICommand.cs                # 命令接口
│   ├── CommandBase.cs             # 命令基类
│   ├── UndoRedoManager.cs         # 撤销重做管理器
│   ├── FileCommands.cs            # 文件操作命令
│   └── UICommands.cs              # UI操作命令
├── Interfaces/                     # 业务接口定义
│   ├── IFileRenameService.cs
│   ├── IPdfPreviewControl.cs      # PDF预览控件接口（双引擎抽象）
│   ├── IPdfDimensionService.cs
│   └── IConfigService.cs
├── Utils/                          # 基础设施层 - 通用工具
│   ├── LogHelper.cs               # 日志助手
│   ├── AppSettings.cs             # 应用设置管理
│   ├── PdfTools.cs                # PDF工具（大型工具类）
│   ├── IText7PdfTools.cs          # iText7 PDF工具
│   ├── ITextSharpLGPLPdfTools.cs  # iTextSharp PDF工具
│   ├── CefSharpInitializer.cs     # CefSharp初始化
│   └── ThemeHelper.cs             # 主题助手
├── Resources/                      # 资源文件
│   ├── pdfjs/                     # PDF.js 库文件
│   ├── Icons/                     # SVG图标
│   └── Fonts/                     # 嵌入字体
└── Properties/
    └── Config/
        └── App.config             # 应用配置文件
```

### 分层架构设计

项目采用清晰的分层架构：

- **表示层 (Presentation Layer)**: Forms、Controls、Presenters - 负责 UI 展示和用户交互
- **应用层 (Application Layer)**: Services、Commands - 包含业务逻辑和服务协调
- **领域层 (Domain Layer)**: Models、Interfaces - 核心数据模型和业务规则
- **基础设施层 (Infrastructure Layer)**: Utils、Helpers - 通用工具和外部依赖

### 关键设计模式

1. **服务定位器模式 (Service Locator)**
   - 使用 `ServiceLocator` 管理所有服务的生命周期
   - 基于 Microsoft.Extensions.DependencyInjection 容器
   - 支持单例和瞬态服务注册

2. **MVP 模式 (Model-View-Presenter)**
   - 各功能面板采用 MVP 模式（继承 `BasePanelControl`）
   - View 通过接口定义实现解耦（如 `IFileRenamePanelView`）
   - Presenter 协调 View 和 Model 之间的交互
   - 常见 Presenter: `FileRenamePanelPresenter`、`PdfProcessingPresenter`

3. **命令模式 (Command Pattern)**
   - 实现完整的撤销/重做功能
   - 支持批量操作和复合命令
   - 命令历史可持久化

4. **事件驱动架构**
   - 基于 `IEventBus` 的发布-订阅模式
   - 支持同步和异步事件处理
   - 用于组件间的解耦通信

### 核心服务组件

**文件处理**
- **FileRenameService**: 文件重命名核心服务
- **BatchProcessingService**: 批量处理服务
- **FileMonitor**: 文件监控服务

**PDF 处理**
- **PdfProcessingService**: PDF 处理服务
- **PdfInspectorService**: PDF 检查服务（出血、刀线等）
- **PdfFontInspectorService**: PDF 字体检查服务（支持 Poppler）
- **DimensionCalculationService**: PDF 尺寸识别和计算服务

**拼版服务**
- **ImpositionService**: 基础拼版服务（连拼）
- **SaddleStitchService**: 骑马钉拼版服务（折手）

**配置和事件**
- **ConfigService**: 配置管理服务
- **EventBus** / **IEventBus**: 事件总线（发布-订阅模式）
- **ThemeManager**: 主题管理器
- **UndoRedoService**: 撤销重做服务

**其他**
- **CompositeColumnService**: 列组合服务
- **ApplicationSettingsService**: 应用设置服务
- **UpdateManager**: 更新管理器

### 重要技术细节

#### PDF 渲染引擎（多引擎架构）
项目使用多种 PDF 技术栈，通过接口实现统一抽象：

1. **PdfiumViewer** - 轻量级 PDF 渲染引擎
   - 本地修改版本（支持单页滚动）
   - 本地 DLL 引用：`lib/PdfiumViewer.dll`
   - 原生库：`x64/pdfium.dll`

2. **CefSharp + PDF.js** - 现代化 PDF 渲染
   - 基于 Chromium 的 PDF.js 渲染
   - 资源文件：`Resources/pdfjs/`
   - 需要 CefSharp.BrowserSubprocess.exe

3. **iText 7** - PDF 处理和生成
   - 用于 PDF 操作、转曲、字体处理等

4. **iTextSharp LGPL** - 兼容旧代码
   - 保持向后兼容性

5. **PDFsharp** - 辅助 PDF 操作

接口抽象：
- `IPdfPreviewControl` - PDF 预览控件接口
- `IPdfDimensionService` - PDF 尺寸服务接口
- `IPdfInfoProvider` - PDF 信息提供者接口

#### 外部依赖
- **Ghostscript** - PDF 转曲和字体检测（`ghostscript/` 目录）
- **Poppler** - 精确字体检测（`poppler/` 目录）

#### 命令系统
- 所有可撤销操作都实现 `ICommand` 接口
- 支持批量操作的事务性
- 命令历史记录可导出和恢复
- `UndoRedoManager` 管理命令栈

#### 事件系统
- 使用强类型事件（`FileOperationEvent`、`AppStateEvent` 等）
- 支持同步和异步事件处理
- 统一的异常处理机制

## 项目注意事项

### 平台要求
- 必须在 Windows 平台上运行
- 目标框架为 .NET Framework 4.8
- 构建平台为 x64

### 特殊依赖
- **Spire.Pdf**: 使用本地 DLL 引用（位于 `Spire.Office Platinum v9.9.0` 目录）
- **PdfiumViewer**: 使用本地修改版本的 DLL（位于 `lib/PdfiumViewer.dll`）
- **字体资源**: 项目嵌入了多个中文字体文件（simhei.ttf、simsun.ttc、msyh.ttc、NotoSansSC-Regular.ttf）

#### 完整 NuGet 依赖

**UI组件**
- AntdUI 2.2.14 - Material Design 风格 UI 库
- ReaLTaiizor 3.8.1.3 - WinForms 增强控件
- Krypton.Toolkit 100.25.11.328 - 高级 WinForms UI 组件套件
- Svg 3.4.7 - SVG 图标支持
- Ookii.Dialogs.WinForms 4.0.0 - 增强对话框

**PDF处理**
- CefSharp.WinForms 109.1.110 - Chromium 浏览器组件（PDF.js）
- iText 9.3.0 - PDF 处理库
- itext.bouncy-castle-adapter 9.3.0 - 加密支持
- itext.font-asian 9.3.0 - 亚洲字体支持
- iTextSharp.LGPLv2.Core 3.4.22 - 旧版兼容
- PDFsharp 6.2.0 - PDF 辅助操作
- PdfiumViewer.Native.x86_64.v8-xfa 2018.4.8.256 - PDF 渲染引擎
- Spire.Pdf 9.9.0 - 本地 DLL 引用（`Spire.Office Platinum v9.9.0/`）

**数据处理**
- EPPlus 5.0.4 - Excel 文件处理
- Newtonsoft.Json 13.0.3 - JSON 序列化

**日志和配置**
- Microsoft.Extensions.Logging 8.0.1 - 日志抽象
- Microsoft.Extensions.Logging.Abstractions 8.0.2
- Microsoft.Extensions.Options 8.0.2 - 选项模式
- Microsoft.Extensions.DependencyInjection 8.0.1 - DI 容器

**测试框架**
- Microsoft.NET.Test.Sdk 17.14.1
- xUnit 2.9.3 - 单元测试框架
- xunit.runner.visualstudio 3.1.4
- MSTest.TestAdapter 3.6.3 - 测试框架
- MSTest.TestFramework 3.6.3
- Moq 4.20.72 - 模拟框架
- coverlet.collector 6.0.4 - 代码覆盖率
- FlaUI.UIA3 4.0.0 - UI 自动化测试

### 开发建议
1. **UI 架构**: 使用 MainShellForm + Panel 架构，新面板继承 `BasePanelControl`
2. **服务开发**: 优先实现接口，通过 `ServiceLocator` 注册服务
3. **命令模式**: 新增可撤销操作时继承 `CommandBase`
4. **事件通信**: 使用 `IEventBus` 进行组件间通信，避免直接依赖
5. **代码规范**: 遵循现有的命名约定和代码风格
6. **异步编程**: 所有异步方法都应以 `Async` 后缀结尾
7. **MVP 模式**: 新增功能面板时实现对应的 Presenter 和 View 接口
8. **PDF 处理**: 根据需求选择合适的 PDF 引擎（PdfiumViewer 用于预览，iText 用于操作）

### 测试策略

**测试框架组合**
- 单元测试: xUnit 2.9.3 + MSTest 3.6.3
- 模拟框架: Moq 4.20.72
- UI 自动化: FlaUI.UIA3 4.0.0
- 覆盖率: coverlet.collector 6.0.4

**测试分类**
- **Services/** - 服务层单元测试
  - `FileRenameServiceTests` - 文件重命名测试
  - `EventBusTests` - 事件总线测试
  - `ConfigServiceTests` - 配置服务测试
  - `DimensionCalculationServiceTests` - 尺寸计算测试
  - `PdfProcessingServiceTests` - PDF 处理测试
  - `BatchProcessingServiceTests` - 批量处理测试
  - `PdfInspectorServiceTests` - PDF 检查器测试
- **Commands/** - 命令模式测试
  - `UndoRedoManagerTests` - 撤销重做测试
- **Integration/** - 集成测试
  - `EndToEndTests` - 端到端测试
  - `ExcelImportTests` - Excel 导入测试
- **UIAutomation/** - UI 自动化测试
  - `MainWindowTests` - 主窗口 UI 测试
- **Utils/** - 工具类测试
  - `PdfToolsTests` - PDF 工具测试
  - `IOHelperTests` - IO 助手测试

## 开发环境配置

推荐使用 Visual Studio 2019/2022 进行开发：
1. 打开 `WindowsFormsApp3.sln` 解决方案
2. 确保安装了 .NET Framework 4.8 开发工具
3. 还原 NuGet 包后即可编译运行

## 关键配置文件

### 应用配置
- **Properties/Config/App.config** - 应用程序主配置文件（包含用户设置、程序集绑定重定向）
- **App.Debug.config** - 调试环境配置
- **LogConfig.json** - 日志配置（运行时生成）

### 安装配置
- **installers/Setup.iss** - Inno Setup 安装脚本（包含 Ghostscript、Poppler、CefSharp 等依赖打包）

### 项目配置
- **WindowsFormsApp3.csproj** - 主项目配置（C# 9.0，net48，x64）
- **WindowsFormsApp3.Tests.csproj** - 测试项目配置（C# 10.0，net48，x64）