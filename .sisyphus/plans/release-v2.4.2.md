# 工作计划: 发布 v2.4.2

## 背景
用户希望按照项目的 "release-version" 流程发布版本 `v2.4.2`。当前仓库存在未提交的更改（文档重命名和重构），必须先进行提交。

## 工作目标
1. **清理**: 提交所有挂起的更改（重构和文档重命名）。
2. **版本更新**: 更新代码和安装包配置中的版本号为 `2.4.2`。
3. **构建**: 生成 Release 版本的二进制文件和 Inno Setup 安装包。
4. **发布**: 提交版本更新并打上标签。

## 验证策略
- **人工验证**:
  - 检查 `MainShellForm.cs` 包含 "V2.4.2"。
  - 检查 `AssemblyInfo.cs` 包含 "2.4.2.0"。
  - 检查 `Setup.iss` 包含 "2.4.2"。
  - 验证 `dotnet build` 返回 0 个错误。
  - 验证 `ISCC` 在 `installers/安装包/` 目录下成功生成 `.exe` 安装包。

## 任务流程
`清理提交` → `更新版本文件` → `构建与验证` → `发布提交与打标签`

---

## 待办事项 (TODOs)

- [ ] 1. **提交挂起的更改**
    **目标**: 在发布前清理工作区。
    **命令**:
    ```bash
    git add .
    git commit -m "refactor: 重构导航面板与文档结构整理"
    ```
    **验证**: `git status` 应显示 "nothing to commit"（无待提交更改）。

- [ ] 2. **更新 `AssemblyInfo.cs` 中的版本**
    **文件**: `src/WindowsFormsApp3/Properties/AssemblyInfo.cs`
    **操作**: 将 `2.4.1.8` 替换为 `2.4.2.0`
    - `[assembly: AssemblyVersion("2.4.2.0")]`
    - `[assembly: AssemblyFileVersion("2.4.2.0")]`

- [ ] 3. **更新 `MainShellForm.cs` 中的版本**
    **文件**: `src/WindowsFormsApp3/Forms/Main/MainShellForm.cs`
    **操作**: 找到版本字符串（例如 `V2.4.1` 或默认值）并更新为 `V2.4.2`。
    **搜索模式**: `string versionStr = .*? "V2\..*?";` -> 更新为字面量字符串。

- [ ] 4. **更新 `Setup.iss` 中的版本**
    **文件**: `installers/Setup.iss`
    **操作**: 更新以下字段:
    - `AppVersion=2.4.2`
    - `AppVerName=大诚重命名工具 v2.4.2`
    - `OutputBaseFilename=大诚重命名工具_v2.4.2_安装包`

- [ ] 5. **构建项目 (Release 配置)**
    **命令**:
    ```bash
    dotnet build WindowsFormsApp3.sln -c Release
    ```
    **验证**: 输出必须以 "Build Succeeded"（构建成功）结尾。

- [ ] 6. **构建安装包**
    **命令**:
    ```bash
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installers/Setup.iss
    ```
    **备选方案**: 如果 ISCC 路径无效，请使用 `where ISCC` 查找正确路径。
    **验证**: 文件 `installers/安装包/大诚重命名工具_v2.4.2_安装包.exe` 存在。

- [ ] 7. **提交发布与打标签**
    **命令**:
    ```bash
    git add src/WindowsFormsApp3/Properties/AssemblyInfo.cs src/WindowsFormsApp3/Forms/Main/MainShellForm.cs installers/Setup.iss
    git commit -m "chore(release): v2.4.2"
    git tag v2.4.2
    ```
    **更新日志 (参考/提交信息)**:
    - refactor: 重构导航面板并完善PDF检查器与文件重命名功能
    - feat: 完成Poppler字体检测集成与拼版功能扩展
    - docs: 整理文档结构

## 成功标准
- [ ] `git describe --tags` 返回 `v2.4.2`
- [ ] 存在文件名正确的安装包
- [ ] 应用程序启动后在"关于"或标题栏显示 "V2.4.2"
