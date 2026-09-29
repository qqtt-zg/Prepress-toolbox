# Release Rules
<!-- last-analyzed: 2026-09-29T08:29:16Z -->

## Version Sources
- `src/WindowsFormsApp3/Properties/AssemblyInfo.cs`: `AssemblyVersion`, `AssemblyFileVersion`.
- `installers/Setup.iss`: `AppVersion`, `AppVerName`, `AppComments`, `OutputBaseFilename`.
- `README.md`: version history heading and the version summary near the end.

## Release Trigger
- Manual Windows release. No CI release workflow is present.

## Test Gate
- `dotnet build WindowsFormsApp3.sln -c Release`.
- Run affected Release test classes independently because settings tests share global state.
- The full suite currently contains `TestITextSharpPageSize`, which references a developer desktop PDF and intentionally calls `Assert.True(false)`; it is not a usable automated release gate.
- For v2.7.0, the relevant Release classes are `FileRenameServiceTests`, `MaterialSelectFormV5CardsTests`, `MaterialSelectFormSizeContractTests`, `ApplicationSettingsBackupTests`, `OrderNumberDropdownTests`, `CancelledFileServiceTests`, and `TemporaryImpositionParametersFormTests`.

## Registry / Distribution
- Compile `installers/Setup.iss` with Inno Setup 6 `ISCC.exe`.
- Installer is written to `installers/安装包/` and is ignored by Git.
- Publish the executable as a GitHub Release asset when distributing publicly.

## Release Notes Strategy
- User-facing history is in `README.md`; GitHub Release notes are manual.

## CI Workflow Files
- None found.

## First-Time Setup Gaps
- Release is manual; no tag-triggered test or publishing workflow.
- Build and installer executables are ignored. Existing version tags are present.
