---
description: Update version numbers across the project and build the Inno Setup installer.
---

# Release Version Skill

This skill helps you update the application version number in all relevant files (C# code, AssemblyInfo, Inno Setup script) and build the installer.

## Usage

When you ask to "release version X.X.X" or "build installer for version X.X.X", this skill will guide the process.

## Steps

### 1. Update Version in C# Code

Update the version number in `src/WindowsFormsApp3/Forms/Main/MainShellForm.cs`.
Look for the version string definition (usually inside `ShowAboutDialog` or `InitializeComponent`):

**Pattern:**

```csharp
string versionStr = version != null ? $"V{version.Major}.{version.Minor}.{version.Build}" : "V2.3.8";
```

**Action:**
Update the default/fallback version string (e.g., "V2.3.8") to the new version (e.g., "V2.3.9").

### 2. Update Assembly Info

Update the `AssemblyVersion` and `AssemblyFileVersion` in `src/WindowsFormsApp3/Properties/AssemblyInfo.cs`.

**Action:**
Replace:

```csharp
[assembly: AssemblyVersion("2.3.8.0")]
[assembly: AssemblyFileVersion("2.3.8.0")]
```

With the new version (e.g., "2.3.9.0").

### 3. Update Inno Setup Script

Update the version definitions in `installers/Setup.iss`.

**Action:**
Update the following keys in the `[Setup]` section:

- `AppVersion`
- `AppVerName`
- `OutputBaseFilename`

**Example:**

```ini
AppVersion=2.3.9
AppVerName=大诚重命名工具 v2.3.9
OutputBaseFilename=大诚重命名工具_v2.3.9_安装包
```

### 4. Build the Project

Run the build command to ensure the binaries are up-to-date with the new version info.

```bash
dotnet build WindowsFormsApp3.sln --configuration Release
```

### 5. Build Installer

Compile the Inno Setup script using ISCC.
**Note:** Use the Bash-style path for robustness in this environment.

```bash
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installers/Setup.iss
```

## Verification

1. Check `MainShellForm.cs` for the updated version string.
2. Check `AssemblyInfo.cs` for the updated attributes.
3. Check `Setup.iss` for the updated version keys.
4. Verify the build command succeeded (0 errors).
5. Verify the installer was generated in `installers/安装包/`.
