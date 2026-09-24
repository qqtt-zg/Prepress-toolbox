param(
    [switch]$NoBuild,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $workspaceRoot "src\WindowsFormsApp3\WindowsFormsApp3.csproj"
$outputDirectory = Join-Path $workspaceRoot "src\WindowsFormsApp3\bin\$Configuration\net48\win-x64"
$executablePath = $null

try
{
    if (-not $NoBuild)
    {
        Write-Host "Building the V5 material selector preview..." -ForegroundColor Cyan
        & dotnet build $projectPath -c $Configuration --nologo
        if ($LASTEXITCODE -ne 0)
        {
            throw "Build failed with exit code $LASTEXITCODE"
        }
    }

    $executablePath = Get-ChildItem -LiteralPath $outputDirectory -Filter "*.exe" -File |
        Where-Object { $_.Name -notmatch "^(CefSharp|vshost)" } |
        Sort-Object Length -Descending |
        Select-Object -First 1 -ExpandProperty FullName

    if ([string]::IsNullOrWhiteSpace($executablePath) -or -not (Test-Path -LiteralPath $executablePath))
    {
        throw "Preview executable was not found in: $outputDirectory"
    }

    Write-Host "Opening the V5 material selector..." -ForegroundColor Green
    Start-Process `
        -FilePath $executablePath `
        -ArgumentList "--preview-material" `
        -WorkingDirectory $outputDirectory
}
catch
{
    Write-Host "Launch failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
