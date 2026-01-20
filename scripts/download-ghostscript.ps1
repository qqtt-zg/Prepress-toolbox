# Ghostscript 下载脚本
# 用于自动下载并安装 Ghostscript

param(
    [string]$Version = "10.04.0",
    [string]$OutputDir = ".\ghostscript",
    [switch]$Install = $false
)

$ErrorActionPreference = "Stop"

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Ghostscript 下载脚本" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# 检测系统架构
$is64bit = [Environment]::Is64BitOperatingSystem
$arch = if ($is64bit) { "win64" } else { "win32" }

Write-Host "检测到系统架构: $arch" -ForegroundColor Green

# 构建下载 URL
$filename = "gs$Version-$arch.exe"
$url = "https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs${Version//.}/$filename"

Write-Host "下载 URL: $url" -ForegroundColor Yellow
Write-Host ""

# 创建输出目录
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
    Write-Host "创建目录: $OutputDir" -ForegroundColor Green
}

$outputFile = Join-Path $OutputDir $filename

# 下载文件
Write-Host "开始下载 Ghostscript $Version ($arch)..." -ForegroundColor Cyan
Write-Host "保存到: $outputFile" -ForegroundColor Yellow
Write-Host ""

try {
    # 使用 Invoke-WebRequest 下载
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $url -OutFile $outputFile -UseBasicParsing
    
    Write-Host "✓ 下载完成!" -ForegroundColor Green
    Write-Host ""
    
    # 显示文件信息
    $fileInfo = Get-Item $outputFile
    Write-Host "文件大小: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Yellow
    Write-Host "文件路径: $($fileInfo.FullName)" -ForegroundColor Yellow
    Write-Host ""
    
    # 询问是否安装
    if ($Install) {
        Write-Host "开始安装 Ghostscript..." -ForegroundColor Cyan
        Start-Process -FilePath $outputFile -ArgumentList "/S" -Wait
        Write-Host "✓ 安装完成!" -ForegroundColor Green
    } else {
        Write-Host "提示: 使用 -Install 参数可以自动安装" -ForegroundColor Yellow
        Write-Host "或者手动运行: $outputFile" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "==================================" -ForegroundColor Cyan
    Write-Host "下载成功!" -ForegroundColor Green
    Write-Host "==================================" -ForegroundColor Cyan
}
catch {
    Write-Host "✗ 下载失败: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "请尝试手动下载:" -ForegroundColor Yellow
    Write-Host "1. 访问: https://ghostscript.com/releases/gsdnld.html" -ForegroundColor Yellow
    Write-Host "2. 下载对应版本: $filename" -ForegroundColor Yellow
    Write-Host "3. 保存到: $OutputDir" -ForegroundColor Yellow
    exit 1
}
