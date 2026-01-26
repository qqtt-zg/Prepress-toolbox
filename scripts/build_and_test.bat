@echo off
chcp 65001 >nul
echo ========================================
echo 编译和测试 Poppler 集成
echo ========================================
echo.

echo [步骤 1/3] 复制 Poppler 工具到编译输出目录...
echo.

set SOURCE=..\poppler
set TARGET_DEBUG=..\src\WindowsFormsApp3\bin\Debug\poppler

if not exist "%SOURCE%\bin\pdffonts.exe" (
    echo [✗] 错误: 源目录不存在
    echo 路径: %SOURCE%\bin\pdffonts.exe
    pause
    exit /b 1
)

if not exist "..\src\WindowsFormsApp3\bin\Debug" (
    mkdir "..\src\WindowsFormsApp3\bin\Debug"
)

xcopy /E /I /Y "%SOURCE%" "%TARGET_DEBUG%" >nul 2>&1

if exist "%TARGET_DEBUG%\bin\pdffonts.exe" (
    echo [✓] Poppler 工具已复制到 Debug 目录
) else (
    echo [✗] 复制失败
    pause
    exit /b 1
)

echo.
echo [步骤 2/3] 验证文件...
echo.

echo 检查关键文件:
if exist "%TARGET_DEBUG%\bin\pdffonts.exe" (
    echo [✓] pdffonts.exe
) else (
    echo [✗] pdffonts.exe 缺失
)

if exist "%TARGET_DEBUG%\bin\poppler.dll" (
    echo [✓] poppler.dll
) else (
    echo [✗] poppler.dll 缺失
)

if exist "%TARGET_DEBUG%\bin\cairo.dll" (
    echo [✓] cairo.dll
) else (
    echo [✗] cairo.dll 缺失
)

if exist "%TARGET_DEBUG%\bin\freetype.dll" (
    echo [✓] freetype.dll
) else (
    echo [✗] freetype.dll 缺失
)

echo.
echo [步骤 3/3] 测试 pdffonts 工具...
echo.

cd "%TARGET_DEBUG%\bin"
pdffonts.exe -v
if %ERRORLEVEL% EQU 0 (
    echo.
    echo [✓] pdffonts 工具可以正常运行
) else (
    echo.
    echo [✗] pdffonts 工具运行失败
    cd ..\..\..\..\..\scripts
    pause
    exit /b 1
)

cd ..\..\..\..\..\scripts

echo.
echo ========================================
echo ✓ 准备工作完成！
echo ========================================
echo.
echo 下一步:
echo 1. 在 Visual Studio 中打开项目
echo 2. 按 Ctrl+Shift+B 编译项目
echo 3. 按 F5 运行程序
echo 4. 打开 PDF 文件测试字体检测
echo 5. 查看日志文件验证 Poppler 是否工作
echo.
echo 日志文件位置: ..\app_2026-01-20.log
echo 搜索关键字: [Poppler]
echo.
pause
