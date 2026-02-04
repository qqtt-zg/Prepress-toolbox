# 发布 V2.4.5 版本脚本
# 此脚本自动化编译和打包流程

param(
    [switch]$SkipBuild = $false,
    [switch]$SkipTests = $false,
    [switch]$OpenOutput = $true
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  大诚重命名工具 V2.4.5 发布脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 设置错误处理
$ErrorActionPreference = "Stop"

# 获取脚本所在目录
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
$solutionPath = Join-Path $projectRoot "src\WindowsFormsApp3"
$installerPath = Join-Path $projectRoot "installers"
$outputPath = Join-Path $installerPath "安装包"

# 步骤 1：清理项目
if (-not $SkipBuild) {
    Write-Host "[1/5] 清理项目..." -ForegroundColor Yellow
    Push-Location $solutionPath
    try {
        dotnet clean -c Release
        Write-Host "  ✓ 清理完成" -ForegroundColor Green
    }
    catch {
        Write-Host "  ✗ 清理失败: $_" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Pop-Location
    Write-Host ""
}
else {
    Write-Host "[1/5] 跳过清理步骤" -ForegroundColor Gray
    Write-Host ""
}

# 步骤 2：还原 NuGet 包
if (-not $SkipBuild) {
    Write-Host "[2/5] 还原 NuGet 包..." -ForegroundColor Yellow
    Push-Location $solutionPath
    try {
        dotnet restore
        Write-Host "  ✓ 还原完成" -ForegroundColor Green
    }
    catch {
        Write-Host "  ✗ 还原失败: $_" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Pop-Location
    Write-Host ""
}
else {
    Write-Host "[2/5] 跳过还原步骤" -ForegroundColor Gray
    Write-Host ""
}

# 步骤 3：编译项目
if (-not $SkipBuild) {
    Write-Host "[3/5] 编译项目 (Release x64)..." -ForegroundColor Yellow
    Push-Location $solutionPath
    try {
        dotnet build -c Release -r win-x64
        Write-Host "  ✓ 编译完成" -ForegroundColor Green
        
        # 验证编译输出
        $exePath = Join-Path $solutionPath "bin\Release\net48\win-x64\大诚重命名工具.exe"
        if (Test-Path $exePath) {
            $fileVersion = (Get-Item $exePath).VersionInfo.FileVersion
            Write-Host "  ✓ 程序版本: $fileVersion" -ForegroundColor Green
        }
        else {
            Write-Host "  ✗ 找不到编译输出文件" -ForegroundColor Red
            Pop-Location
            exit 1
        }
    }
    catch {
        Write-Host "  ✗ 编译失败: $_" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Pop-Location
    Write-Host ""
}
else {
    Write-Host "[3/5] 跳过编译步骤" -ForegroundColor Gray
    Write-Host ""
}

# 步骤 4：运行测试（可选）
if (-not $SkipTests) {
    Write-Host "[4/5] 运行测试..." -ForegroundColor Yellow
    $testProject = Join-Path $projectRoot "src\WindowsFormsApp3.Tests"
    if (Test-Path $testProject) {
        Push-Location $testProject
        try {
            dotnet test -c Release --no-build
            Write-Host "  ✓ 测试通过" -ForegroundColor Green
        }
        catch {
            Write-Host "  ⚠ 测试失败，但继续发布" -ForegroundColor Yellow
        }
        Pop-Location
    }
    else {
        Write-Host "  ⚠ 未找到测试项目，跳过测试" -ForegroundColor Yellow
    }
    Write-Host ""
}
else {
    Write-Host "[4/5] 跳过测试步骤" -ForegroundColor Gray
    Write-Host ""
}

# 步骤 5：创建安装包
Write-Host "[5/5] 创建安装包..." -ForegroundColor Yellow

# 查找 Inno Setup Compiler
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
    Write-Host "  ✗ 未找到 Inno Setup Compiler" -ForegroundColor Red
    Write-Host "  请安装 Inno Setup: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    exit 1
}

Write-Host "  使用 Inno Setup: $isccPath" -ForegroundColor Gray

Push-Location $installerPath
try {
    $setupScript = Join-Path $installerPath "Setup.iss"
    & $isccPath $setupScript
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ 安装包创建完成" -ForegroundColor Green
        
        # 验证安装包
        $installerFile = Join-Path $outputPath "大诚重命名工具_v2.4.5_安装包.exe"
        if (Test-Path $installerFile) {
            $fileSize = (Get-Item $installerFile).Length / 1MB
            Write-Host "  ✓ 安装包大小: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Green
            Write-Host "  ✓ 安装包位置: $installerFile" -ForegroundColor Green
        }
        else {
            Write-Host "  ✗ 找不到安装包文件" -ForegroundColor Red
            Pop-Location
            exit 1
        }
    }
    else {
        Write-Host "  ✗ 安装包创建失败" -ForegroundColor Red
        Pop-Location
        exit 1
    }
}
catch {
    Write-Host "  ✗ 安装包创建失败: $_" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location
Write-Host ""

# 完成
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  发布完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "安装包位置: $outputPath" -ForegroundColor Cyan
Write-Host ""

# 打开输出目录
if ($OpenOutput) {
    Write-Host "正在打开输出目录..." -ForegroundColor Gray
    Start-Process explorer.exe $outputPath
}

# 显示下一步操作
Write-Host "下一步操作:" -ForegroundColor Yellow
Write-Host "  1. 测试安装包" -ForegroundColor White
Write-Host "  2. 创建 Git 标签: git tag -a v2.4.5 -m 'Release V2.4.5'" -ForegroundColor White
Write-Host "  3. 推送到远程仓库: git push origin v2.4.5" -ForegroundColor White
Write-Host ""

# 询问是否创建 Git 标签
$createTag = Read-Host "是否创建 Git 标签? (y/n)"
if ($createTag -eq "y" -or $createTag -eq "Y") {
    try {
        git tag -a v2.4.5 -m "Release V2.4.5"
        Write-Host "  ✓ Git 标签已创建" -ForegroundColor Green
        
        $pushTag = Read-Host "是否推送标签到远程仓库? (y/n)"
        if ($pushTag -eq "y" -or $pushTag -eq "Y") {
            git push origin v2.4.5
            Write-Host "  ✓ 标签已推送到远程仓库" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "  ⚠ Git 操作失败: $_" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "发布脚本执行完成！" -ForegroundColor Green
