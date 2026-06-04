# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

基于 .NET Framework 4.8 的 Windows Forms 桌面应用程序，专为印刷行业设计，提供文件批量重命名、Excel 数据导入、PDF 处理、拼版等印前工作流功能。

## 构建和运行命令

```bash
# 还原 NuGet 包
dotnet restore WindowsFormsApp3.sln

# Debug/Release 构建
dotnet build WindowsFormsApp3.sln --configuration Debug|Release

# 发布安装包（使用 Inno Setup 6）
"/c/Program Files (x86)/Inno Setup 6/ISCC.exe" installers/Setup.iss
```

## 架构特点

### 双正则表达式系统
项目使用两个独立的正则表达式系统，理解这一点对开发至关重要：

1. **主界面正则 (_cmbRegex)**: `FileRenamePanelPresenter._cmbRegex`，用于**文件名生成**
2. **Excel匹配正则 (cmbRegex2)**: `ExcelImportService.SelectedRegexPattern`，用于**Excel数据匹配**

关键方法：
- `GetRegexResultForMatching()`: 获取用于Excel数据匹配的正则结果，优先使用 cmbRegex2
- `ExtractRegexResultForMatching()`: 从文件名提取匹配结果用于Excel查找

### 数据匹配完整返回链
```
ExcelImportForm → AppSettings → ExcelImportHelper → FileRenamePanelPresenter
    → MaterialSelectFormModern → FileRenamePanel → FileRenamePanelPresenter → GenerateNewFileName
```

### 序号搜索二级搜索机制
当 `MaterialSelectFormModern` 中的序号搜索复选框启用时：
1. 在 MatchedRows 或 Excel 序号列中搜索匹配的序号
2. 如果启用了"序号搜索结果更新正则"功能（`EnableSerialSearchResultToRegex`），从匹配行指定列提取值更新正则结果
3. 同时从匹配行提取"列组合"值（如果有）

### 列组合功能
- 配置通过 `ICompositeColumnService` 管理
- 设置持久化到 AppSettings
- 在 MaterialSelectFormModern 中，列组合值在序号搜索成功后会同步更新

### UI 架构
- **新架构**: MainShellForm + Panel（FileRenamePanel、ExcelImportPanel 等）
- **旧架构**: Form1（单体窗体，逐步迁移）
- 新功能开发应使用 MainShellForm + Panel 架构

## 核心设计模式

1. **服务定位器 (ServiceLocator)**: 基于 DI 容器管理服务生命周期
2. **MVP 模式**: Panel + Presenter + View接口
3. **命令模式**: 撤销/重做支持
4. **事件总线 (IEventBus)**: 组件间解耦通信

## 开发规范

### 复选框逻辑
- `checked = true` 表示功能**启用**
- 控件状态变更时需要持久化的，使用 AppSettings 保存

### 版本发布
版本号在以下位置同步更新：
- `src/WindowsFormsApp3/Properties/AssemblyInfo.cs` → `AssemblyVersion`
- `installers/Setup.iss` → `AppVersion`, `AppVerName`, `OutputBaseFilename`

### 平台要求
- 仅支持 Windows
- 目标框架: .NET Framework 4.8
- 构建平台: x64

### 关键配置文件
- **installers/Setup.iss** - Inno Setup 安装脚本
- **src/WindowsFormsApp3/Properties/Config/App.config** - 应用程序配置
- **src/WindowsFormsApp3/Properties/Config/LogConfig.json** - 日志配置（级别、文件大小、保留数量）
- **src/WindowsFormsApp3/App.Debug.config** - Debug 构建专用配置（禁用 iText CJK 字体加载、禁用 FIPS 策略）
- **src/WindowsFormsApp3/Properties/AssemblyInfo.cs** - 版本信息

### 测试

```bash
# 运行所有测试
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj

# 运行特定测试类
dotnet test --filter "ClassName=BasicTests"

# 运行特定测试方法
dotnet test --filter "FullyQualifiedName~ServiceLocatorTests"
```

测试框架: xUnit 2.9.3 + MSTest 3.6.3，使用 Moq 4.20.72 做 mock，FlaUI.UIA3 4.0.0 做 UI 自动化测试。

测试项目结构:
- `Services/` - 服务层测试
- `Integration/` - 端到端集成测试
- `Performance/` - 性能测试
- `UIAutomation/` - UI 自动化测试
- `Models/` - 模型测试
- `Utils/` - 工具类测试

### 关键依赖

| 依赖 | 用途 |
|------|------|
| CefSharp | Chromium 内核 PDF 预览 |
| iText 9.3.0 | PDF 处理和分析 |
| EPPlus 5.0.4 | Excel 读写 |
| AntdUI 2.1.12 | 现代 UI 组件库 |
| Newtonsoft.Json 13.0.3 | JSON 序列化 |

### 外部原生工具（打包在项目中）

- `ghostscript/` - Ghostscript 便携版，用于 PDF 格式转换
- `poppler/` - Poppler 工具集，用于字体检测

### 用户数据

运行时配置存储在 `{userappdata}\大诚重命名工具`，网格布局保存在 `SavedGrids/` 目录。
# CLAUDE.md

用于减少大语言模型常见编程错误的行为准则。可根据需要与项目特定说明合并使用。

**权衡说明：** 本准则偏向谨慎而非速度。对于简单任务，请自行判断。

## 1. 思考先行

**不要假设。不要隐藏困惑。主动呈现权衡方案。**

在实现之前：
- 明确陈述你的假设。如果不确定，请提问。
- 如果存在多种解读，请逐一呈现——不要默默选择其中一种。
- 如果存在更简单的方案，请明确指出。在必要时提出反对意见。
- 如果遇到不清楚的地方，请停下来。明确指出困惑所在。主动提问。

## 2. 简洁至上

**用最少的代码解决问题。不添加任何推测性内容。**

- 不添加超出需求范围的功能。
- 不为仅使用一次的代码创建抽象层。
- 不添加未被要求的"灵活性"或"可配置性"。
- 不为不可能发生的场景添加错误处理。
- 如果你写了 200 行代码而实际上 50 行就能完成，请重写。

自问："资深工程师会觉得这过于复杂吗？"如果答案是肯定的，请简化。

## 3. 精准修改

**只触碰必须修改的部分。只清理你自己造成的遗留问题。**

在编辑现有代码时：
- 不要"改进"相邻的代码、注释或格式。
- 不要重构没有问题的部分。
- 匹配现有代码风格，即使你更倾向于不同的写法。
- 如果你注意到无关的死代码，请提及它——但不要删除它。

当你的修改产生了孤立代码时：
- 删除因你的修改而变得不再使用的导入、变量或函数。
- 除非被明确要求，否则不要删除之前就存在的死代码。

检验标准：每一行被修改的代码都应能直接追溯到用户的请求。

## 4. 目标驱动执行

**定义成功标准。循环迭代直至验证通过。**

将任务转化为可验证的目标：
- "添加验证" → "为无效输入编写测试，然后使其通过"
- "修复这个 bug" → "编写一个能复现该 bug 的测试，然后使其通过"
- "重构 X" → "确保重构前后测试均通过"

对于多步骤任务，陈述一个简要计划：
```
1. [步骤] → 验证：[检查项]
2. [步骤] → 验证：[检查项]
3. [步骤] → 验证：[检查项]
```

明确的成功标准让你能够独立循环迭代。模糊的标准（"让它能工作"）则需要不断澄清。

---

**本准则生效的标志：** diff 中不必要的修改减少、因过度复杂化而导致的返工减少、澄清性问题在实现之前而非出错之后提出。