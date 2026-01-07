# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个基于 .NET Framework 4.8 的 Windows Forms 桌面应用程序，名为"大诚重命名工具"。该工具专为生产环境设计，提供文件批量重命名、Excel数据导入、PDF处理等高级功能。

### 解决方案结构

项目包含两个解决方案：

1. **WindowsFormsApp3.sln** (主解决方案)
   - `WindowsFormsApp3` - 主应用程序
   - `WindowsFormsApp3.Tests` - 测试项目

2. **PdfiumViewer.sln** (PDF渲染库子项目)
   - `PdfiumViewer` - 核心 PDF 渲染库
   - `PdfiumViewer.Demo` - WinForms 演示程序
   - `PdfiumViewer.WPFDemo` - WPF 演示程序
   - `PdfiumViewer.Test` - 测试项目

### 核心功能

- 批量文件重命名（支持正则表达式、智能返单识别）
- Excel 数据导入（自动匹配、列组合）
- 事件分组管理（TreeView 层级化、拖拽排序）
- PDF 处理（Pdfium 渲染引擎）
- 拼版功能（ImpositionService）
- 撤销/重做系统（命令模式实现）

### UI 架构状态

项目正在进行 UI 界面重构（当前分支：`重构ui界面`）：

- **新架构**: MainShellForm + 面板化设计
  - MainShellForm 作为主窗体框架，提供侧边导航和内容区域
  - 各功能模块独立为 Panel（FileRenamePanel、ExcelImportPanel、SettingsPanel 等）
  - 面板继承自 `BasePanelControl`，支持 MVP 模式

- **旧架构**: Form1（单体窗体）
  - 逐步迁移中，部分功能已迁移到新架构
  - 建议新功能使用 MainShellForm + Panel 架构实现

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
src/WindowsFormsApp3/bin/Debug/net48/大诚重命名工具.exe
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
# 发布为依赖框架的可执行文件
dotnet publish src/WindowsFormsApp3/WindowsFormsApp3.csproj --configuration Release --output ./publish

# 发布为自包含应用（包含 .NET 运行时）
dotnet publish src/WindowsFormsApp3/WindowsFormsApp3.csproj --configuration Release --self-contained true --runtime win-x64 --output ./publish-sc
```

## 核心架构

### 目录结构

```
src/WindowsFormsApp3/
├── Forms/                          # 表示层 - UI组件
│   ├── Main/                       # 主窗体
│   │   ├── MainShellForm.cs       # 主界面框架（带侧边导航和内容区域）
│   │   └── MaterialSelectFormModern.cs  # 材料选择窗体
│   ├── Panels/                     # 功能面板（模块化设计，继承自 BasePanelControl）
│   │   ├── FileRenamePanel        # 文件重命名面板
│   │   ├── ExcelImportPanel       # Excel导入面板
│   │   ├── DatabasePanel          # 数据库面板
│   │   └── SettingsPanel          # 设置面板
│   └── Controls/                   # 自定义控件
│       └── Settings/              # 设置相关控件
├── Presenters/                     # MVP模式 - 展示器
│   ├── FileRenamePanelPresenter.cs  # 文件重命名面板展示器
│   ├── PdfProcessingPresenter.cs    # PDF处理展示器
│   └── IFileRenamePanelView.cs      # 面板视图接口定义
├── Services/                       # 应用层 - 业务逻辑
│   ├── ServiceLocator.cs          # 依赖注入容器
│   ├── FileRenameService.cs       # 文件重命名核心服务
│   ├── PdfProcessingService.cs    # PDF处理服务
│   ├── DimensionCalculationService.cs  # PDF尺寸计算服务
│   ├── EventGroupConfigurationService.cs  # 事件分组配置服务
│   ├── BatchProcessingService.cs  # 批量处理服务
│   ├── ImpositionService.cs       # 拼版服务
│   ├── ExcelImportHelper.cs       # Excel导入助手
│   ├── EventBus.cs                # 事件总线
│   ├── UndoRedoService.cs         # 撤销重做服务
│   ├── ConfigService.cs           # 配置管理服务
│   └── FileMonitor.cs             # 文件监控服务
├── Commands/                       # 命令模式实现
│   ├── ICommand.cs                # 命令接口
│   ├── CommandBase.cs             # 命令基类
│   └── UndoRedoManager.cs         # 撤销重做管理器
├── Models/                         # 领域层 - 数据模型
│   └── FileRenameInfo.cs          # 核心数据模型
├── Interfaces/                     # 业务接口定义
│   ├── IFileRenameService.cs      # 文件重命名服务接口
│   ├── IPdfPreviewControl.cs      # PDF预览控件接口（双引擎抽象）
│   └── IEventBus.cs               # 事件总线接口
├── Utils/                          # 基础设施层 - 通用工具
│   ├── FontManager.cs             # 字体管理器
│   ├── LogHelper.cs               # 日志助手
│   ├── PdfTools.cs                # PDF工具（大型工具类）
│   └── AppSettings.cs             # 应用设置管理
└── Helpers/                        # 业务助手类
    ├── FileOperationHelper.cs     # 文件操作助手
    └── ValidationHelper.cs        # 验证助手
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
   - 各功能面板采用 MVP 模式（FileRenamePanel、ExcelImportPanel、SettingsPanel 等）
   - View 通过接口定义实现解耦（如 IFileRenamePanelView）
   - Presenter 协调 View 和 Model 之间的交互
   - 常见 Presenter: FileRenamePanelPresenter、PdfProcessingPresenter

3. **命令模式 (Command Pattern)**
   - 实现完整的撤销/重做功能
   - 支持批量操作和复合命令
   - 命令历史可持久化

4. **事件驱动架构**
   - 基于 `IEventBus` 的发布-订阅模式
   - 支持同步和异步事件处理
   - 用于组件间的解耦通信

### 核心服务组件

- **FileRenameService**: 文件重命名核心服务
- **ExcelImportService**: Excel 数据导入服务
- **PdfProcessingService**: PDF 处理服务（支持 Pdfium 渲染引擎）
- **DimensionCalculationService**: PDF 尺寸识别和计算服务
- **EventGroupConfigurationService**: 事件分组配置服务
- **ImpositionService**: 拼版服务
- **BatchProcessingService**: 批量处理服务
- **UndoRedoService**: 撤销重做服务
- **EventBus**: 事件总线
- **ConfigService**: 配置管理服务
- **FileMonitor**: 文件监控服务

### 重要技术细节

#### PDF 渲染引擎
- **PdfiumViewer**: 轻量级 PDF 渲染引擎（本地修改版本，支持单页滚动）
- 通过 `IPdfPreviewControl` 和 `IPdfDimensionService` 接口实现统一抽象
- 本地 DLL 引用位于 `lib/PdfiumViewer.dll`

#### 命令系统
- 所有可撤销操作都实现 `ICommand` 接口
- 支持批量操作的事务性
- 命令历史记录可导出和恢复

#### 事件系统
- 使用强类型事件
- 支持事件过滤和优先级
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
- AntdUI 2.2.4 - Material Design 风格 UI 库
- ReaLTaiizor 3.8.1.3 - WinForms 增强控件
- Krypton.Toolkit 100.25.11.328 - 高级 WinForms UI 组件套件
- Svg 3.4.7 - SVG 图标支持
- Ookii.Dialogs.WinForms 4.0.0 - 增强对话框

**PDF处理**
- iText 9.3.0 - PDF 处理库
- PdfiumViewer.Native.x86_64.v8-xfa 2018.4.8.256 - PDF 渲染引擎
- Spire.Pdf 9.9.0 - 本地 DLL 引用

**数据处理**
- EPPlus 5.0.4 - Excel 文件处理
- Newtonsoft.Json 13.0.3 - JSON 序列化

**日志和配置**
- Microsoft.Extensions.Logging 8.0.1 - 日志抽象
- Microsoft.Extensions.Options 8.0.2 - 选项模式
- Microsoft.Extensions.DependencyInjection 8.0.0 - DI 容器

**测试框架**
- xUnit 2.9.3 - 单元测试框架
- MSTest 3.6.3 - 测试框架
- Moq 4.20.72 - 模拟框架
- FlaUI.UIA3 4.0.0 - UI 自动化测试

### 开发建议
1. **UI 架构迁移**: 优先使用 MainShellForm + Panel 架构，避免直接修改旧的 Form1
2. **服务开发**: 修改服务时优先实现接口，并通过 ServiceLocator 注册
3. **命令模式**: 新增可撤销操作时继承 `CommandBase`
4. **事件通信**: 使用事件总线进行组件间通信，避免直接依赖
5. **代码规范**: 遵循现有的命名约定和代码风格
6. **异步编程**: 所有异步方法都应以 Async 后缀结尾
7. **Panel 开发**: 新增功能面板时继承 `BasePanelControl` 并实现对应的 Presenter

### 测试策略

**测试框架组合**
- 单元测试: xUnit + MSTest
- 模拟框架: Moq
- UI 自动化: FlaUI.UIA3

**测试分类**
- **Services/** - 服务层单元测试（FileRenameService, EventBus, ConfigService, DimensionCalculationService 等）
- **Presenters/** - 展示器测试
- **Commands/** - 命令模式测试（UndoRedoManagerTests）
- **Integration/** - 集成测试（EndToEndTests 等）
- **UIAutomation/** - UI 自动化测试（FlaUI）
- **Performance/** - 性能基准测试

**测试覆盖重点**
- 核心业务逻辑和服务层
- 命令模式的撤销/重做功能
- 事件总线的发布-订阅机制
- 文件重命名的各种场景

## 开发环境配置

推荐使用 Visual Studio 2019/2022 进行开发：
1. 打开 `WindowsFormsApp3.sln` 解决方案
2. 确保安装了 .NET Framework 4.8 开发工具
3. 还原 NuGet 包后即可编译运行

## 关键配置文件

### 应用配置
- **App.config** - 应用程序主配置文件
- **App.Debug.config** - 调试环境配置
- **LogConfig.json** - 日志配置（运行时生成）

### 安装配置
- **installers/Setup.iss** - Inno Setup 安装脚本
- **update.json** - 自动更新配置

### 项目配置
- **WindowsFormsApp3.csproj** - 主项目配置
- **WindowsFormsApp3.Tests.csproj** - 测试项目配置