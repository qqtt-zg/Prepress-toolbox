@echo off
chcp 65001 >nul
echo ========================================
echo 复制 Poppler 工具到编译输出目录
echo ========================================
echo.

set SOURCE=..\poppler
set TARGET_DEBUG=..\src\WindowsFormsApp3\bin\Debug\poppler
set TARGET_RELEASE=..\src\WindowsFormsApp3\bin\Release\poppler

REM 检查源目录
if not exist "%SOURCE%\bin\pdffonts.exe" (
    echo [✗] 源目录不存在或缺少 pdffonts.exe
    echo 路径: %SOURCE%\bin\pdffonts.exe
    echo.
    echo 请先确保 poppler 文件夹在项目根目录
    pause
    exit /b 1
)

echo [✓] 找到源目录: %SOURCE%
echo.

REM 复制到 Debug 目录
echo [1] 复制到 Debug 目录...
if not exist "..\src\WindowsFormsApp3\bin\Debug" (
    echo [!] Debug 目录不存在，跳过
) else (
    xcopy /E /I /Y "%SOURCE%" "%TARGET_DEBUG%" >nul
    if exist "%TARGET_DEBUG%\bin\pdffonts.exe" (
        echo [✓] 复制成功: %TARGET_DEBUG%
    ) else (
        echo [✗] 复制失败
    )
)

echo.

REM 复制到 Release 目录
echo [2] 复制到 Release 目录...
if not exist "..\src\WindowsFormsApp3\bin\Release" (
    echo [!] Release 目录不存在，跳过
) else (
    xcopy /E /I /Y "%SOURCE%" "%TARGET_RELEASE%" >nul
    if exist "%TARGET_RELEASE%\bin\pdffonts.exe" (
        echo [✓] 复制成功: %TARGET_RELEASE%
    ) else (
        echo [✗] 复制失败
    )
)

echo.
echo ========================================
echo 复制完成
echo ========================================
echo.
echo 现在可以运行程序测试 Poppler 字体检测功能
echo.
pause
