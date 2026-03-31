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
- **App.config** - 应用程序配置
- **src/WindowsFormsApp3/Properties/AssemblyInfo.cs** - 版本信息