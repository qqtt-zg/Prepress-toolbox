# Ghostscript 便携版设置脚本
# 用于将 Ghostscript 复制到应用程序目录，实现便携式部署

param(
    [string]$GhostscriptPath = "C:\Program Files\gs\gs10.06.0",
    [string]$OutputDir = ".\src\WindowsFormsApp3\bin\Debug\net48\win-x64\ghostscript"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Ghostscript 便携版设置" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查 Ghostscript 是否已安装
if (-not (Test-Path $GhostscriptPath)) {
    Write-Host "错误: 未找到 Ghostscript 安装目录" -ForegroundColor Red
    Write-Host "路径: $GhostscriptPath" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "请先安装 Ghostscript 或指定正确的安装路径：" -ForegroundColor Yellow
    Write-Host "  .\scripts\setup-ghostscript-portable.ps1 -GhostscriptPath 'C:\Your\Path'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "下载地址: https://www.ghostscript.com/releases/gsdnld.html" -ForegroundColor Cyan
    exit 1
}

Write-Host "✓ 找到 Ghostscript 安装目录" -ForegroundColor Green
Write-Host "  路径: $GhostscriptPath" -ForegroundColor Gray
Write-Host ""

# 创建输出目录
Write-Host "创建输出目录..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Force -Path "$OutputDir\lib" | Out-Null
Write-Host "✓ 输出目录已创建" -ForegroundColor Green
Write-Host "  路径: $OutputDir" -ForegroundColor Gray
Write-Host ""

# 复制可执行文件
Write-Host "复制 Ghostscript 可执行文件..." -ForegroundColor Yellow
$exeFile = "$GhostscriptPath\bin\gswin64c.exe"
if (Test-Path $exeFile) {
    Copy-Item $exeFile -Destination $OutputDir -Force
    $exeSize = (Get-Item "$OutputDir\gswin64c.exe").Length / 1MB
    Write-Host "✓ gswin64c.exe ($([math]::Round($exeSize, 2)) MB)" -ForegroundColor Green
} else {
    Write-Host "✗ 未找到 gswin64c.exe" -ForegroundColor Red
}

# 复制 DLL
Write-Host "复制 Ghostscript DLL..." -ForegroundColor Yellow
$dllFile = "$GhostscriptPath\bin\gsdll64.dll"
if (Test-Path $dllFile) {
    Copy-Item $dllFile -Destination $OutputDir -Force
    $dllSize = (Get-Item "$OutputDir\gsdll64.dll").Length / 1MB
    Write-Host "✓ gsdll64.dll ($([math]::Round($dllSize, 2)) MB)" -ForegroundColor Green
} else {
    Write-Host "✗ 未找到 gsdll64.dll" -ForegroundColor Red
}

# 复制库文件
Write-Host "复制 Ghostscript 库文件..." -ForegroundColor Yellow
$libPath = "$GhostscriptPath\lib"
if (Test-Path $libPath) {
    Copy-Item "$libPath\*" -Destination "$OutputDir\lib" -Recurse -Force
    $libCount = (Get-ChildItem "$OutputDir\lib" -File).Count
    $libSize = (Get-ChildItem "$OutputDir\lib" -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
    Write-Host "✓ 库文件 ($libCount 个文件, $([math]::Round($libSize, 2)) MB)" -ForegroundColor Green
} else {
    Write-Host "✗ 未找到库文件目录" -ForegroundColor Red
}

Write-Host ""

# 计算总大小
$totalSize = (Get-ChildItem $OutputDir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✓ 设置完成！" -ForegroundColor Green
Write-Host "  总大小: $([math]::Round($totalSize, 2)) MB" -ForegroundColor Gray
Write-Host "  位置: $OutputDir" -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 验证文件
Write-Host "验证文件..." -ForegroundColor Yellow
$requiredFiles = @(
    "gswin64c.exe",
    "gsdll64.dll",
    "lib\gs_init.ps"
)

$allValid = $true
foreach ($file in $requiredFiles) {
    $fullPath = Join-Path $OutputDir $file
    if (Test-Path $fullPath) {
        Write-Host "  ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $file (缺失)" -ForegroundColor Red
        $allValid = $false
    }
}

Write-Host ""

if ($allValid) {
    Write-Host "✓ 所有必需文件已就绪" -ForegroundColor Green
    Write-Host ""
    Write-Host "下一步：" -ForegroundColor Cyan
    Write-Host "  1. 运行应用程序测试转曲功能" -ForegroundColor Gray
    Write-Host "  2. 检查日志确认使用便携版 Ghostscript" -ForegroundColor Gray
    Write-Host "  3. 将 ghostscript 文件夹包含到安装包中" -ForegroundColor Gray
} else {
    Write-Host "✗ 部分文件缺失，请检查 Ghostscript 安装" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "提示: 如需打包到安装程序，请参考文档：" -ForegroundColor Yellow
Write-Host "  docs/Ghostscript_打包指南.md" -ForegroundColor Gray
Write-Host ""
