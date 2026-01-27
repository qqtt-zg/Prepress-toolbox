# Prepress-toolbox Development Guide

## 1. Build and Test Commands

This project is a Windows Forms application targeting .NET Framework 4.8. Use the `dotnet` CLI or Visual Studio 2019/2022.

### Build
```bash
# Restore NuGet packages
dotnet restore WindowsFormsApp3.sln

# Build Debug configuration
dotnet build WindowsFormsApp3.sln --configuration Debug

# Build Release configuration
dotnet build WindowsFormsApp3.sln --configuration Release
```

### Test
**Frameworks**: xUnit, MSTest, Moq, FlaUI.

```bash
# Run all tests
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj

# Run a specific test class
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj --filter "FullyQualifiedName~FileRenameServiceTests"

# Run a single test method
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj --filter "FullyQualifiedName~TestSpecificMethodName"

# Run with detailed output
dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj --logger "console;verbosity=detailed"
```

### Run
```bash
# Execute the built binary
src/WindowsFormsApp3/bin/Debug/net48/大诚重命名工具.exe
```

## 2. Code Style & Conventions

### General
- **Language Version**: C# 7.3 (default for .NET Framework 4.8).
- **Indentation**: 4 spaces.
- **Braces**: Allman style (new line for opening brace).
- **Encoding**: UTF-8.

### Naming Conventions
- **Classes/Methods/Properties**: `PascalCase`.
- **Private Fields**: `_camelCase` (e.g., `_eventBus`).
- **Local Variables/Parameters**: `camelCase`.
- **Interfaces**: Prefix with `I` (e.g., `IFileRenameService`).
- **Events**: `PascalCase` usually ending in `Changed`, `Completed`, etc. (e.g., `FileRenamed`).
- **Constants**: `PascalCase` or `UPPER_CASE` (prefer PascalCase for public consts).

### Imports (Usings)
- Place `using` directives at the very top of the file, outside the `namespace`.
- Sort system directives first, then alphabetical.

### Documentation
- Use XML documentation (`///`) for all public classes, methods, and properties.
- Explain *why* complex logic exists, not just *what* it does.
- Language: Chinese (Simplified) is preferred for comments in this codebase.

### Error Handling
- Use `try-catch` blocks in Service methods where I/O or external calls occur.
- **Do NOT** use `MessageBox.Show` in Services or Models. Raise events or throw exceptions.
- Use `LogHelper` for logging errors.

## 3. Architecture & Patterns

### UI Architecture
- **Pattern**: MVP (Model-View-Presenter).
- **Views**: Inherit from `Form` or `BasePanelControl`. Implement an Interface (e.g., `IFileRenamePanelView`).
- **Presenters**: Handle UI logic. Coordinate between View and Services.
- **Forms**: Main shell is `MainShellForm`. Functionality is split into Panels (e.g., `FileRenamePanel`).

### Dependency Injection
- **Container**: Microsoft.Extensions.DependencyInjection.
- **Access**: Use `ServiceLocator.Instance` to resolve services when constructor injection is not possible (legacy code).
- **Registration**: Register new services in `Program.cs` or `ServiceLocator.cs`.

### Event System
- Use `IEventBus` for cross-component communication.
- Define strong-typed event args in `Services/Events` or `Models`.

### PDF Processing
- **Engines**: `PdfiumViewer` (rendering/viewing) and `iText`/`Spire.Pdf` (manipulation).
- Use `IPdfPreviewControl` abstraction for UI elements displaying PDFs.

## 4. Agent Operational Rules

1.  **Safety First**: Never commit code that breaks the build. Run `dotnet build` before finishing a task.
2.  **Testing**: When modifying logic in Services, run the relevant unit tests. If no test exists, consider adding one in `WindowsFormsApp3.Tests`.
3.  **Refactoring**: Prefer modifying existing files over creating new ones unless the class is growing too large (> 500 lines).
4.  **UI Changes**:
    - When modifying UI, check `MainShellForm.Designer.cs` carefully.
    - Prefer editing logic in Presenters over code-behind (`.cs` files of Forms).
5.  **Dependencies**: Do not add new NuGet packages without explicit user permission.
6.  **Path Handling**: Always use `Path.Combine` and absolute paths. Be aware of Windows file system limitations.

## 5. Directory Structure Overview
- `src/WindowsFormsApp3/Forms`: UI Forms and Panels.
- `src/WindowsFormsApp3/Presenters`: Logic for UI.
- `src/WindowsFormsApp3/Services`: Business logic (Rename, PDF, Excel).
- `src/WindowsFormsApp3/Models`: Data objects.
- `src/WindowsFormsApp3/Commands`: Undo/Redo command implementations.
- `src/WindowsFormsApp3/Utils`: Helper classes and extensions.
