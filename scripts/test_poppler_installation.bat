@echo off
chcp 65001 >nul
echo ========================================
echo Poppler 安装验证
echo ========================================
echo.

echo [1] 检查 pdffonts.exe 是否存在...
if exist "poppler\bin\pdffonts.exe" (
    echo ✓ pdffonts.exe 已找到
) else (
    echo ✗ pdffonts.exe 未找到
    echo 路径: poppler\bin\pdffonts.exe
    goto :end
)

echo.
echo [2] 检查版本信息...
poppler\bin\pdffonts.exe -v
if %ERRORLEVEL% EQU 0 (
    echo ✓ pdffonts 可以正常运行
) else (
    echo ✗ pdffonts 运行失败
    goto :end
)

echo.
echo [3] 检查依赖 DLL...
set MISSING=0
for %%f in (poppler.dll cairo.dll freetype.dll) do (
    if exist "poppler\bin\%%f" (
        echo ✓ %%f
    ) else (
        echo ✗ %%f 缺失
        set MISSING=1
    )
)

if %MISSING% EQU 1 (
    echo.
    echo ⚠ 部分依赖文件缺失
    goto :end
)

echo.
echo ========================================
echo ✓ Poppler 安装成功！
echo ========================================
echo.
echo 下一步：
echo 1. 编译项目
echo 2. 测试字体检测功能
echo 3. 对比 iText 和 Poppler 的检测结果
echo.

:end
pause
