# 发布安装包完整流程 Skill

> **用途**: 使用 Inno Setup Compiler 发布 Prepress-toolbox 安装包的完整可复用流程  
> **版本**: 2.4.5  
> **最后更新**: 2026-02-02

## 前置条件

1. **Inno Setup Compiler** 已安装（通常位于 `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`）
2. **.NET SDK** 已安装并支持 .NET Framework 4.8
3. 项目代码已提交或已保存所有更改
4. **重要**: 确保 `Setup.iss` 文件已添加到版本控制（检查 `.gitignore` 是否排除了该文件）

## 完整发布流程

### 步骤 1: 更新版本号

需要更新以下三个文件中的版本号（假设新版本号为 `X.Y.Z`）：

#### 1.1 更新程序集版本号

**文件**: `src/WindowsFormsApp3/Properties/AssemblyInfo.cs`

```csharp
[assembly: AssemblyVersion("X.Y.Z.0")]
[assembly: AssemblyFileVersion("X.Y.Z.0")]
```

**说明**: 
- 程序集版本格式为 `Major.Minor.Build.Revision`（例如 `2.4.5.0`）
- 主界面显示的版本号会自动从程序集版本读取

#### 1.2 更新 Inno Setup 脚本版本号

**文件**: `installers/Setup.iss`

需要更新三处：

```ini
AppVersion=X.Y.Z
AppVerName=大诚重命名工具 vX.Y.Z
OutputBaseFilename=大诚重命名工具_vX.Y.Z_安装包
```

**说明**:
- `AppVersion`: 安装程序的版本号（格式：`Major.Minor.Build`）
- `AppVerName`: 安装程序中显示的版本名称
- `OutputBaseFilename`: 输出安装包的文件名（包含版本号，确保每次生成不同文件名，保留旧版本）

#### 1.3 更新主窗体回退版本号

**文件**: `src/WindowsFormsApp3/Forms/Main/MainShellForm.cs`

查找 `ShowAboutDialog` 方法中的回退版本号：

```csharp
string versionStr = version != null ? $"V{version.Major}.{version.Minor}.{version.Build}" : "VX.Y.Z";
```

**说明**: 这是当程序集版本读取失败时的回退版本号

### 步骤 2: 清理和编译项目

**推荐流程**（确保干净的编译环境）：

#### 2.1 清理项目

```powershell
cd src\WindowsFormsApp3
dotnet clean -c Release
```

**说明**: 清理之前的编译输出，确保重新编译时没有旧文件干扰。

#### 2.2 还原 NuGet 包

```powershell
cd src\WindowsFormsApp3
dotnet restore
```

**说明**: 确保所有 NuGet 依赖包已正确还原。

#### 2.3 编译项目（Release x64）

```powershell
cd src\WindowsFormsApp3
dotnet build -c Release -r win-x64
```

**验证**:
- 检查编译输出中是否有错误（警告可以忽略）
- 确认编译成功生成 `src/WindowsFormsApp3/bin/Release/net48/win-x64/大诚重命名工具.exe`
- 验证程序版本号：检查生成的 exe 文件的版本信息

**注意**: 
- 必须使用 `-r win-x64` 参数指定运行时标识符，确保生成 x64 版本
- 编译输出目录为 `bin\Release\net48\win-x64\`

### 步骤 3: 编译安装包

#### 3.1 查找 Inno Setup Compiler

Inno Setup 可能安装在以下位置之一：

```powershell
# 自动查找 Inno Setup Compiler
$isccPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 5\ISCC.exe",
    "C:\Program Files\Inno Setup 5\ISCC.exe"
)

$isccPath = $null
foreach ($path in $isccPaths) {
    if (Test-Path $path) {
        $isccPath = $path
        break
    }
}

if (-not $isccPath) {
    Write-Host "未找到 Inno Setup Compiler" -ForegroundColor Red
    exit 1
}
```

#### 3.2 编译安装包

**方法 1: 使用命令行（推荐）**

```powershell
# 切换到 installers 目录
Push-Location installers

# 运行 Inno Setup Compiler
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "Setup.iss"

# 返回原目录
Pop-Location
```

**方法 2: 使用 GUI**

1. 打开 Inno Setup Compiler
2. 文件 → 打开 → 选择 `installers\Setup.iss`
3. 生成 → 编译（或按 `Ctrl+F9`）

**重要验证步骤**:
1. **编译前验证**: 
   - 确认 `Setup.iss` 文件已保存（检查文件修改时间）
   - 再次确认 `OutputBaseFilename` 已更新为新版本号
   - 如果使用 Git，确认 `Setup.iss` 已提交到版本控制
2. **编译后验证**: 
   - 编译完成后，立即检查 `installers\安装包\` 目录
   - 确认生成了正确版本号的安装包文件
   - 验证文件大小（通常为 100-200 MB）
3. **如果文件名不正确**: 
   - 检查 `Setup.iss` 文件是否已保存
   - 重新打开 `Setup.iss` 确认 `OutputBaseFilename` 的值
   - 检查 `.gitignore` 是否排除了 `Setup.iss`（如果被排除，修改可能未保存）
   - 重新编译安装包

**说明**:
- 编译过程可能需要 1-2 分钟（取决于文件数量）
- 编译成功后，安装包会生成在 `installers\安装包\` 目录
- **注意**: Inno Setup 编译输出信息可能显示旧文件名，但实际生成的文件名应该与 `OutputBaseFilename` 一致
- 如果路径包含中文字符，确保使用正确的编码

### 步骤 4: 验证安装包

#### 4.1 检查文件存在和大小

```powershell
$installerFile = "installers\安装包\大诚重命名工具_vX.Y.Z_安装包.exe"
if (Test-Path $installerFile) {
    $fileSize = (Get-Item $installerFile).Length / 1MB
    Write-Host "✓ 安装包创建成功！" -ForegroundColor Green
    Write-Host "  文件路径: $installerFile" -ForegroundColor Cyan
    Write-Host "  文件大小: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Cyan
} else {
    Write-Host "✗ 安装包文件不存在" -ForegroundColor Red
}
```

**验证项**:
1. **文件存在**: 确认 `installers\安装包\大诚重命名工具_vX.Y.Z_安装包.exe` 已生成
2. **文件大小**: 安装包大小通常为 100-200 MB（包含所有依赖）
3. **版本号**: 
   - 右键安装包 → 属性 → 详细信息，查看版本信息
   - 或运行安装程序，查看安装向导中的版本号
4. **旧版本保留**: 确认旧版本的安装包未被覆盖（如果存在）

#### 4.2 验证安装包内容（可选）

可以解压或安装测试，确认包含以下关键文件：
- `大诚重命名工具.exe`
- `x64\pdfium.dll`
- `ghostscript\gswin64c.exe`
- `poppler\bin\pdffonts.exe`
- `Resources\` 目录下的所有资源文件

### 步骤 5: 测试安装包（可选但推荐）

1. 在测试环境中运行安装包
2. 验证安装过程正常
3. 验证程序启动后显示的版本号正确
4. 验证"关于"对话框显示的版本号正确

## 版本号更新检查清单

在每次发布前，确保以下位置版本号一致：

- [ ] `AssemblyInfo.cs` - `AssemblyVersion` 和 `AssemblyFileVersion`
- [ ] `Setup.iss` - `AppVersion`
- [ ] `Setup.iss` - `AppVerName`
- [ ] `Setup.iss` - `OutputBaseFilename`（**文件名中的版本号 - 最关键，防止覆盖旧安装包**）
- [ ] `MainShellForm.cs` - 关于对话框的回退版本号

**编译后验证**:
- [ ] 检查 `installers\安装包\` 目录中生成的安装包文件名是否包含正确的版本号
- [ ] 确认旧版本的安装包未被覆盖（如果存在）

## 常见问题

### Q1: 编译输出显示的文件名是旧版本，或实际生成的文件名不正确

**原因**: 
- Inno Setup 编译输出信息可能显示旧文件名
- `Setup.iss` 文件可能未正确保存
- Inno Setup 可能读取了缓存的配置

**解决**: 
1. **立即检查**: 编译后立即检查 `installers\安装包\` 目录中的实际文件名
2. **验证配置**: 重新打开 `Setup.iss` 文件，确认 `OutputBaseFilename` 的值是否正确
3. **如果文件名错误**: 
   - 确认 `Setup.iss` 文件已保存（检查文件修改时间）
   - 重新编译安装包
   - 如果问题仍然存在，检查文件编码或尝试用文本编辑器重新保存文件
4. **预防措施**: 在编译前，使用 `grep` 或文本搜索确认 `OutputBaseFilename` 的值

### Q2: 旧版本的安装包被删除了

**原因**: Inno Setup 默认会删除与 `OutputBaseFilename` 同名的旧文件。

**解决**: 
- 确保 `OutputBaseFilename` 包含版本号（例如 `大诚重命名工具_v2.4.5_安装包`）
- 每次版本更新时，文件名会不同，旧文件会被保留

### Q3: 程序显示的版本号不正确

**原因**: 可能某个位置的版本号未更新。

**解决**: 
1. 检查 `AssemblyInfo.cs` 中的版本号
2. 重新编译项目
3. 重新编译安装包

### Q4: 编译安装包时找不到文件

**原因**: 项目未编译或编译路径不正确。

**解决**: 
1. 确保已执行 `dotnet build -c Release -r win-x64` 且编译成功
2. 检查 `Setup.iss` 中的源文件路径是否正确（相对于 `installers` 目录）
3. 确认编译输出目录为 `src\WindowsFormsApp3\bin\Release\net48\win-x64\`

### Q5: Setup.iss 文件修改无法保存或被 Git 忽略

**原因**: 
- `Setup.iss` 可能被 `.gitignore` 排除
- 文件可能被其他程序锁定

**解决**: 
1. 检查 `.gitignore` 文件，确认 `installers/` 目录是否被排除
2. 如果被排除，添加例外规则：
   ```gitignore
   # Installers
   installers/
   # 但保留 Setup.iss 配置文件（需要版本控制）
   !installers/Setup.iss
   ```
3. 强制添加文件到 Git：
   ```bash
   git add -f installers/Setup.iss
   ```
4. 如果文件被锁定，关闭 Inno Setup Compiler GUI（如果打开）

### Q6: PowerShell 路径问题（包含中文字符）

**原因**: PowerShell 在处理包含中文字符的路径时可能出现编码问题。

**解决**: 
1. 使用相对路径而不是绝对路径
2. 使用 `Push-Location` 和 `Pop-Location` 切换目录
3. 如果必须使用绝对路径，确保路径正确编码
4. 使用 `Test-Path` 验证路径是否存在

## 自动化脚本

项目已提供自动化发布脚本，位于 `scripts\发布V2.4.5.ps1`（或对应版本号）。

### 使用发布脚本

```powershell
# 在项目根目录执行
powershell -ExecutionPolicy Bypass -File "scripts\发布V2.4.5.ps1" -SkipTests
```

**脚本参数**:
- `-SkipBuild`: 跳过编译步骤（如果已编译）
- `-SkipTests`: 跳过测试步骤（推荐，加快发布速度）
- `-OpenOutput`: 编译完成后自动打开输出目录（默认：true）

**脚本功能**:
1. 清理项目（`dotnet clean`）
2. 还原 NuGet 包（`dotnet restore`）
3. 编译项目 Release x64（`dotnet build -c Release -r win-x64`）
4. 运行测试（可选，默认跳过）
5. 创建安装包（使用 Inno Setup）
6. 验证安装包文件
7. 可选：创建 Git 标签

**为新版本创建脚本**:
1. 复制 `scripts\发布V2.4.5.ps1` 为新版本脚本
2. 修改脚本中的版本号（搜索 `2.4.5` 并替换为新版本号）
3. 修改安装包文件名验证路径

### 手动执行步骤（如果脚本不可用）

```powershell
# 1. 清理项目
cd src\WindowsFormsApp3
dotnet clean -c Release

# 2. 还原 NuGet 包
dotnet restore

# 3. 编译项目
dotnet build -c Release -r win-x64

# 4. 编译安装包
cd ..\..\installers
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "Setup.iss"

# 5. 验证
cd ..\..
$installerFile = "installers\安装包\大诚重命名工具_vX.Y.Z_安装包.exe"
if (Test-Path $installerFile) {
    Write-Host "安装包生成成功！" -ForegroundColor Green
}
```

## 发布后检查

- [ ] 安装包文件已生成
- [ ] 安装包文件名包含正确的版本号
- [ ] 旧版本的安装包已保留（如果存在）
- [ ] 安装包可以正常安装
- [ ] 程序启动后显示的版本号正确
- [ ] "关于"对话框显示的版本号正确

## 版本历史

- **v2.4.5** (2026-02-02): 完善版本，基于实际发布经验更新
  - 添加清理和还原步骤
  - 完善编译流程（必须使用 `-r win-x64`）
  - 添加 Setup.iss 版本控制问题解决方案
  - 完善自动化脚本说明
  - 添加路径问题处理方案
  - 完善验证步骤
  - 添加发布脚本使用说明
- **v2.4.5** (2026-02-01): 初始版本，包含完整发布流程
  - 修复了内置主题无法持久化问题
  - 修复了回收站图标需要还原默认图标问题

## 注意事项

1. **版本号格式**: 
   - 程序集版本: `X.Y.Z.0`（四位）
   - 安装包版本: `X.Y.Z`（三位）

2. **文件名保留**: 由于 `OutputBaseFilename` 包含版本号，每次发布都会生成不同文件名的安装包，旧版本会自动保留。

3. **编译顺序**: 
   - **必须**先清理、还原、编译项目，再编译安装包
   - 确保安装包包含最新的程序文件
   - 推荐使用 `-r win-x64` 参数指定运行时标识符

4. **Setup.iss 版本控制**: 
   - 确保 `Setup.iss` 文件已添加到 Git 版本控制
   - 如果被 `.gitignore` 排除，需要添加例外规则
   - 修改后必须保存并提交，避免版本号不同步

5. **测试建议**: 
   - 每次发布前建议在测试环境中验证安装包
   - 测试安装、运行、卸载流程
   - 验证版本号显示正确

6. **发布脚本**: 
   - 推荐使用自动化发布脚本（`scripts\发布VX.Y.Z.ps1`）
   - 为新版本创建对应的发布脚本
   - 脚本会自动处理清理、还原、编译、打包等步骤

7. **路径问题**: 
   - 如果项目路径包含中文字符，注意 PowerShell 编码问题
   - 使用相对路径和 `Push-Location`/`Pop-Location` 更可靠
