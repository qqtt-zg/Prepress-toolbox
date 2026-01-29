@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo 字体检测对比验证工具
echo ========================================
echo.

set TEST_PDF=C:\Users\admin\Desktop\45 HYS000714 60张 HED-125 B270喇叭形吊灯（玫瑰金色）273876 铭牌.pdf

echo [1] 测试 pdffonts 命令行工具
echo ----------------------------------------
if exist "poppler\bin\pdffonts.exe" (
    echo ✓ pdffonts.exe 存在
    echo.
    echo 执行: pdffonts.exe "%TEST_PDF%"
    echo.
    poppler\bin\pdffonts.exe "%TEST_PDF%"
    echo.
    
    echo 列位置参考:
    echo 位置:  0         10        20        30        40        50        60        70        80
    echo        ^|         ^|         ^|         ^|         ^|         ^|         ^|         ^|         ^|
    echo        name(0-36)────────────┘└──type(37-54)──┘└─encoding(55-71)─┘└emb┘└sub┘└uni┘
    echo.
) else (
    echo ✗ pdffonts.exe 不存在
    echo 请确保 poppler\bin\pdffonts.exe 已安装
    pause
    exit /b 1
)

echo.
echo [2] 检查编译输出目录
echo ----------------------------------------
set BIN_DIR=src\WindowsFormsApp3\bin\Debug\net48\win-x64

if exist "%BIN_DIR%\poppler\bin\pdffonts.exe" (
    echo ✓ 编译输出目录包含 pdffonts.exe
    echo    路径: %BIN_DIR%\poppler\bin\pdffonts.exe
) else (
    echo ✗ 编译输出目录缺少 pdffonts.exe
    echo    目标路径: %BIN_DIR%\poppler\bin\
    echo.
    echo 正在复制 poppler 文件夹...
    if not exist "%BIN_DIR%" (
        echo ✗ 编译输出目录不存在: %BIN_DIR%
        echo    请先编译项目
    ) else (
        xcopy /E /I /Y poppler "%BIN_DIR%\poppler" >nul 2>&1
        if !errorlevel! equ 0 (
            echo ✓ 复制成功
        ) else (
            echo ✗ 复制失败，错误代码: !errorlevel!
        )
    )
)

echo.
echo [3] 查看最新日志
echo ----------------------------------------
if exist "app_2026-01-20.log" (
    echo 最近的字体检测日志（最后10条）:
    echo.
    powershell -Command "Get-Content 'app_2026-01-20.log' | Select-String '\[Poppler\].*解析字体' | Select-Object -Last 10"
    echo.
) else (
    echo ✗ 日志文件不存在
    echo    请运行程序后再查看日志
)

echo.
echo [4] 验证解析结果
echo ----------------------------------------
if exist "app_2026-01-20.log" (
    echo 检查嵌入状态判断:
    echo.
    powershell -Command "Get-Content 'app_2026-01-20.log' | Select-String '解析结果.*emb=' | Select-Object -Last 5"
    echo.
) else (
    echo ✗ 无法验证，日志文件不存在
)

echo.
echo ========================================
echo 测试完成
echo ========================================
echo.
echo 下一步操作:
echo 1. 对比上述 pdffonts 输出与 Adobe Acrobat 结果
echo 2. 运行程序，打开测试PDF，点击"检查器"
echo 3. 检查日志中是否显示 "嵌入: SubsetEmbedded"
echo 4. 验证字体名称是否已移除子集前缀（如 EPROCH+）
echo.
echo 预期结果:
echo - 字体数量: 2
echo - 字体名称: MicrosoftYaHei（不带前缀）
echo - 嵌入状态: SubsetEmbedded（子集嵌入）
echo.
pause
