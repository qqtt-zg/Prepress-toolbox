# Prepress-toolbox Development Guide

> **Context**: Windows Forms application (.NET Framework 4.8) for print shop automation.
> **Key Libraries**: AntdUI (UI), iText 9/Spire (PDF), EPPlus (Excel), Poppler (Analysis).

## 1. Build & Test Commands

### Build
**Environment**: .NET SDK (supporting .NET 4.8) or Visual Studio 2022.
```bash
# Restore dependencies
dotnet restore WindowsFormsApp3.sln

# Build (Debug/Release) - Safe to run frequently
dotnet build WindowsFormsApp3.sln -c Debug
dotnet build WindowsFormsApp3.sln -c Release
```

### Test
**Frameworks**: `xUnit` (Primary), `MSTest` (Legacy/Specific), `FlaUI` (UI Automation), `Moq`.
**Location**: `src/WindowsFormsApp3.Tests/`

```bash
# Run ALL tests (Essential before PR)
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj

# Run a SPECIFIC test file (Fast feedback loop)
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj --filter "FullyQualifiedName~FileRenameServiceTests"

# Run a SINGLE test method
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj --filter "FullyQualifiedName~TestSpecificMethodName"

# Run with detailed output (for debugging failures)
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj -v n
```

## 2. Code Style & Conventions

### C# Standards
- **Version**: C# 9.0 (Main App), C# 10.0 (Tests).
- **Naming**:
  - `PascalCase`: Classes, Methods, Properties, Events, Constants.
  - `_camelCase`: Private fields (e.g., `_pdfService`).
  - `camelCase`: Local variables, parameters.
  - `IInterface`: Interface prefix.
- **Formatting**:
  - **Indentation**: 4 spaces.
  - **Braces**: Allman style (New line).
  - **Usings**: System first, then alphabetical. Inside namespace? No, **outside**.

### Language Rules
- **Code Identifiers**: English (Classes, Methods, Variables).
- **Comments & Documentation**: **Simplified Chinese (简体中文)**. Mandatory for explaining complex logic.
- **Todo Lists**: Simplified Chinese (e.g., `1. 完成PDF拆分功能`).
- **Git Commit Messages**: Chinese (e.g., `fix: 修复打印预览崩溃问题`).

### Architecture: MVP (Model-View-Presenter)
- **Views (`Forms/`)**: Dumb UI. Inherit `Form` or `BasePanelControl`. Implement interfaces (e.g., `IMainView`).
  - *Rule*: NO business logic in Views. Only UI event forwarding.
- **Presenters (`Presenters/`)**: Orchestrators. Bind View to Services.
  - *Rule*: Handle exceptions here, not in Views.
- **Services (`Services/`)**: Business logic. Stateless where possible.
  - *Rule*: Use `LogHelper` for logging, not `Console.WriteLine`.
- **Models (`Models/`)**: Data structures (Anemic domain model).

### Critical: The "PDF Library Chaos"
This project uses multiple PDF libraries. **Use the right one for the job**:
1.  **iText 9.3.0** (`itext.*`): **PRIMARY** for PDF manipulation (Merge, Split, Watermark).
2.  **PdfiumViewer**: **PRIMARY** for PDF **Viewing/Rendering** in UI only.
3.  **Poppler**: **PRIMARY** for deep analysis (Font inspection, ink coverage). Use `PdfFontInspectorService_Poppler`.
4.  **Spire.Pdf**: Legacy/Fallback. Avoid new usage unless iText fails.
5.  **PDFsharp**: Secondary tool. Avoid mixing with iText logic.

### UI Development (AntdUI)
- We use **AntdUI** for modern components (Buttons, Inputs, Tables).
- **Do NOT** use standard WinForms controls if an AntdUI equivalent exists.
- **SVGs**: Use `Svg` library for icons. Store in `Resources/Icons`.

## 3. Operational Rules for Agents

1.  **No Blind Commits**: Always run `dotnet build` before confirming a task is done.
2.  **Atomic Changes**: Don't mix refactoring with bug fixes.
3.  **Error Handling**:
    - **UI Layer**: Show user-friendly messages (via `MessageBox` or Toast).
    - **Service Layer**: Throw exceptions or return `Result<T>` pattern. Log errors via `LogHelper`.
4.  **Async/Await**:
    - UI runs on Main Thread. Use `async/await` for ALL I/O (File, DB, PDF processing).
    - **Never** use `.Result` or `.Wait()` (Deadlock risk). Use `await`.
5.  **Path Handling**:
    - Windows paths are messy. Always use `Path.Combine()`.
    - Assume paths can contain spaces and Chinese characters.
6.  **Dependency Injection**:
    - Prefer Constructor Injection.
    - If impossible (Legacy Forms), use `ServiceLocator.Instance.GetService<T>()`.
    - Register new services in `Program.cs`.

## 4. Git Workflow
- **Commit Messages**: `type: subject`
  - `feat: add PDF split function`
  - `fix: resolve crash on large files`
  - `refactor: optimize import logic`
  - `docs: update README`
- **Scope**: Keep changes focused. If you see unrelated messy code, note it but don't fix it unless asked (or use a separate PR).

## 5. Directory Map
- `src/WindowsFormsApp3/`
  - `Forms/` -> UI Windows & Panels
  - `Presenters/` -> Logic connecting UI & Data
  - `Services/` -> Core Business Logic (The "Brains")
  - `Models/` -> Data Objects
  - `Resources/` -> Assets (Icons, Fonts, PDF.js)
- `src/WindowsFormsApp3.Tests/` -> Unit & Integration Tests
- `packages/` -> Local Nuget cache (Legacy style)

## 6. Troubleshooting
- **"Ghostscript missing"**: Check `docs/Ghostscript_下载安装指南.md`.
- **"Font not found"**: Ensure fonts are in `Resources/Fonts` and build action is "Embedded Resource".
- **"PDF Preview Jitter"**: Known issue in `PdfiumViewer`. See `README.md` for status.
