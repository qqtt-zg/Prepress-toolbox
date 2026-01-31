---
description: 发布 Prepress Toolbox 应用程序的新版本
---

# 发布新版本

此工作流帮助你发布应用程序的新版本，包括更新 `AssemblyInfo.cs` 中的版本号并构建项目。

## 1. 请求版本号

询问用户新的版本号（例如 "2.4.3"）。
如果未提供，则请求用户输入。

## 2. 更新 AssemblyInfo.cs

使用 `replace_file_content` 工具更新 `src/WindowsFormsApp3/Properties/AssemblyInfo.cs` 中的 `AssemblyVersion` 和 `AssemblyFileVersion` 属性。

```csharp
[assembly: AssemblyVersion("YOUR_NEW_VERSION.0")]
[assembly: AssemblyFileVersion("YOUR_NEW_VERSION.0")]
```

## 3. 构建项目

// turbo
运行构建命令以在 Release 模式下编译项目。

```powershell
dotnet build src\WindowsFormsApp3\WindowsFormsApp3.csproj -c Release
```

## 4. 验证构建

检查构建输出是否包含 "Build succeeded"（或 "生成 成功"）。
如果需要独立验证，请检查 `src/WindowsFormsApp3/bin/Release/net48/win-x64/大诚重命名工具.exe` 的文件版本。

// turbo

```powershell
(Get-Item "src/WindowsFormsApp3/bin/Release/net48/win-x64/大诚重命名工具.exe").VersionInfo.ProductVersion
```

## 5. 生成安装包

Inno Setup 脚本位于 `installers/setup.iss`。运行以下命令生成安装包：

```powershell
$iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) { $iscc = "C:\Program Files\Inno Setup 6\ISCC.exe" }
& $iscc "installers\setup.iss"
```
