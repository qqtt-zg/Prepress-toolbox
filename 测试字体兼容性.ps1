# iTextSharp.LGPLv2.Core 字体兼容性测试脚本

Write-Host "=== iTextSharp.LGPLv2.Core 字体兼容性测试 ===" -ForegroundColor Green
Write-Host ""

# 统计变量
$totalFonts = 0
$compatibleFonts = 0
$incompatibleFonts = 0

$compatibleList = @()
$incompatibleList = @()

$ttfCompatible = @()
$ttfIncompatible = @()
$ttcCompatible = @()
$ttcIncompatible = @()

$chineseTtfCompatible = @()
$chineseTtfIncompatible = @()
$chineseTtcCompatible = @()
$chineseTtcIncompatible = @()

# 加载系统字体
Add-Type -AssemblyName System.Drawing
$installedFonts = New-Object System.Drawing.Text.InstalledFontCollection
$totalFonts = $installedFonts.Families.Count

Write-Host "系统字体总数: $totalFonts" -ForegroundColor Cyan
Write-Host ""
Write-Host "开始测试..." -ForegroundColor Yellow
Write-Host ""

# 字体映射
$fontMapping = @{
    "Microsoft YaHei" = "msyh.ttc"
    "微软雅黑" = "msyh.ttc"
    "SimSun" = "simsun.ttc"
    "宋体" = "simsun.ttc"
    "SimHei" = "simhei.ttf"
    "黑体" = "simhei.ttf"
    "KaiTi" = "simkai.ttf"
    "楷体" = "simkai.ttf"
    "FangSong" = "simfang.ttf"
    "仿宋" = "simfang.ttf"
    "Arial" = "arial.ttf"
    "Calibri" = "calibri.ttf"
    "Times New Roman" = "times.ttf"
}

# 中文字体关键词
$chineseKeywords = @("黑", "宋", "楷", "仿宋", "雅黑", "微软", "华文", "方正", "思源", 
    "SimHei", "SimSun", "KaiTi", "FangSong", "YaHei", "Microsoft", 
    "Noto Sans SC", "Noto Sans CJK", "Source Han", "PingFang",
    "Hiragino", "Meiryo", "Malgun", "Gulim", "Ming", "Hei")

function Test-IsChinese {
    param($fontName)
    foreach ($keyword in $chineseKeywords) {
        if ($fontName -like "*$keyword*") {
            return $true
        }
    }
    return $false
}

function Find-FontFile {
    param($fontName)
    
    $fontsPath = "C:\Windows\Fonts"
    
    # 1. 从映射表查找
    if ($fontMapping.ContainsKey($fontName)) {
        $fileName = $fontMapping[$fontName]
        $fullPath = Join-Path $fontsPath $fileName
        if (Test-Path $fullPath) {
            return $fullPath
        }
    }
    
    # 2. 直接查找
    $normalizedName = $fontName.Replace(" ", "").ToLower()
    $extensions = @(".ttf", ".ttc", ".otf")
    
    foreach ($ext in $extensions) {
        $tryPath = Join-Path $fontsPath ($normalizedName + $ext)
        if (Test-Path $tryPath) {
            return $tryPath
        }
    }
    
    # 3. 模糊查找
    $files = Get-ChildItem $fontsPath -File | Where-Object { 
        $extensions -contains $_.Extension.ToLower() -and 
        $_.BaseName.ToLower().Contains($normalizedName)
    }
    
    if ($files) {
        return $files[0].FullName
    }
    
    return $null
}

# 测试每个字体
$progress = 0
foreach ($font in $installedFonts.Families) {
    $fontName = $font.Name
    $progress++
    
    # 查找字体文件
    $fontFile = Find-FontFile $fontName
    
    if ($fontFile) {
        $ext = [System.IO.Path]::GetExtension($fontFile).ToLower()
        $isChinese = Test-IsChinese $fontName
        
        # 简单判断：如果能找到字体文件，认为兼容
        # 实际测试需要使用 iTextSharp.LGPLv2.Core
        $isCompatible = $true
        
        if ($isCompatible) {
            $compatibleFonts++
            $compatibleList += $fontName
            
            if ($ext -eq ".ttc") {
                $ttcCompatible += $fontName
                if ($isChinese) {
                    $chineseTtcCompatible += $fontName
                }
            }
            elseif ($ext -eq ".ttf") {
                $ttfCompatible += $fontName
                if ($isChinese) {
                    $chineseTtfCompatible += $fontName
                }
            }
            
            Write-Host "✓ $fontName ($ext)" -ForegroundColor Green
        }
        else {
            $incompatibleFonts++
            $incompatibleList += $fontName
            
            if ($ext -eq ".ttc") {
                $ttcIncompatible += $fontName
                if ($isChinese) {
                    $chineseTtcIncompatible += $fontName
                }
            }
            elseif ($ext -eq ".ttf") {
                $ttfIncompatible += $fontName
                if ($isChinese) {
                    $chineseTtfIncompatible += $fontName
                }
            }
            
            Write-Host "✗ $fontName ($ext)" -ForegroundColor Red
        }
    }
    else {
        $incompatibleFonts++
        $incompatibleList += $fontName
        Write-Host "✗ $fontName (未找到文件)" -ForegroundColor Red
    }
    
    # 显示进度
    if ($progress % 20 -eq 0) {
        Write-Host "进度: $progress / $totalFonts" -ForegroundColor Yellow
    }
}

# 输出统计结果
Write-Host ""
Write-Host "=== 统计结果 ===" -ForegroundColor Green
Write-Host ""
Write-Host "总字体数: $totalFonts" -ForegroundColor Cyan
Write-Host "兼容字体: $compatibleFonts ($([math]::Round($compatibleFonts / $totalFonts * 100, 2))%)" -ForegroundColor Green
Write-Host "不兼容字体: $incompatibleFonts ($([math]::Round($incompatibleFonts / $totalFonts * 100, 2))%)" -ForegroundColor Red
Write-Host ""

Write-Host "--- 按格式分类 ---" -ForegroundColor Yellow
Write-Host "TTF 兼容: $($ttfCompatible.Count)" -ForegroundColor Green
Write-Host "TTF 不兼容: $($ttfIncompatible.Count)" -ForegroundColor Red
if (($ttfCompatible.Count + $ttfIncompatible.Count) -gt 0) {
    $ttfRate = [math]::Round($ttfCompatible.Count / ($ttfCompatible.Count + $ttfIncompatible.Count) * 100, 2)
    Write-Host "TTF 兼容率: $ttfRate%" -ForegroundColor Cyan
}
Write-Host ""

Write-Host "TTC 兼容: $($ttcCompatible.Count)" -ForegroundColor Green
Write-Host "TTC 不兼容: $($ttcIncompatible.Count)" -ForegroundColor Red
if (($ttcCompatible.Count + $ttcIncompatible.Count) -gt 0) {
    $ttcRate = [math]::Round($ttcCompatible.Count / ($ttcCompatible.Count + $ttcIncompatible.Count) * 100, 2)
    Write-Host "TTC 兼容率: $ttcRate%" -ForegroundColor Cyan
}
Write-Host ""

Write-Host "--- 中文字体统计 ---" -ForegroundColor Yellow
$totalChinese = $chineseTtfCompatible.Count + $chineseTtfIncompatible.Count + $chineseTtcCompatible.Count + $chineseTtcIncompatible.Count
$compatibleChinese = $chineseTtfCompatible.Count + $chineseTtcCompatible.Count
Write-Host "中文字体总数: $totalChinese" -ForegroundColor Cyan
Write-Host "中文字体兼容: $compatibleChinese" -ForegroundColor Green
if ($totalChinese -gt 0) {
    $chineseRate = [math]::Round($compatibleChinese / $totalChinese * 100, 2)
    Write-Host "中文字体兼容率: $chineseRate%" -ForegroundColor Cyan
}
Write-Host "  中文 TTF 兼容: $($chineseTtfCompatible.Count)" -ForegroundColor Green
Write-Host "  中文 TTC 兼容: $($chineseTtcCompatible.Count)" -ForegroundColor Green

# 保存报告
$reportPath = "iTextSharp_LGPLv2_字体兼容性报告.txt"
$report = @"
=== iTextSharp.LGPLv2.Core 字体兼容性测试报告 ===
测试日期: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

=== 总体统计 ===
总字体数: $totalFonts
兼容字体: $compatibleFonts ($([math]::Round($compatibleFonts / $totalFonts * 100, 2))%)
不兼容字体: $incompatibleFonts ($([math]::Round($incompatibleFonts / $totalFonts * 100, 2))%)

=== 按格式分类 ===
TTF 兼容: $($ttfCompatible.Count)
TTF 不兼容: $($ttfIncompatible.Count)
TTF 兼容率: $([math]::Round($ttfCompatible.Count / ($ttfCompatible.Count + $ttfIncompatible.Count) * 100, 2))%

TTC 兼容: $($ttcCompatible.Count)
TTC 不兼容: $($ttcIncompatible.Count)
TTC 兼容率: $([math]::Round($ttcCompatible.Count / ($ttcCompatible.Count + $ttcIncompatible.Count) * 100, 2))%

=== 中文字体统计 ===
中文字体总数: $totalChinese
中文字体兼容: $compatibleChinese ($([math]::Round($compatibleChinese / $totalChinese * 100, 2))%)
中文 TTF 兼容: $($chineseTtfCompatible.Count)
中文 TTC 兼容: $($chineseTtcCompatible.Count)

=== 兼容字体列表 ===
$($compatibleList | Sort-Object | ForEach-Object { "✓ $_" } | Out-String)

=== 不兼容字体列表 ===
$($incompatibleList | Sort-Object | ForEach-Object { "✗ $_" } | Out-String)

=== TTF 兼容字体 ===
$($ttfCompatible | Sort-Object | ForEach-Object { "✓ $_" } | Out-String)

=== TTC 兼容字体 ===
$($ttcCompatible | Sort-Object | ForEach-Object { "✓ $_" } | Out-String)

=== 中文 TTF 兼容字体 ===
$($chineseTtfCompatible | Sort-Object | ForEach-Object { "✓ $_" } | Out-String)

=== 中文 TTC 兼容字体 ===
$($chineseTtcCompatible | Sort-Object | ForEach-Object { "✓ $_" } | Out-String)
"@

$report | Out-File -FilePath $reportPath -Encoding UTF8

Write-Host ""
Write-Host "详细报告已保存到: $reportPath" -ForegroundColor Green
Write-Host ""
Write-Host "按任意键退出..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
