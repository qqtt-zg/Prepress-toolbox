@echo off
chcp 65001 >nul
echo ========================================
echo PDF 字体检测工具测试
echo ========================================
echo.

REM 检查 pdffonts 是否存在
set PDFFONTS_PATH=..\poppler\bin\pdffonts.exe

if exist "%PDFFONTS_PATH%" (
    echo [✓] 找到 pdffonts 工具: %PDFFONTS_PATH%
    echo.
    
    REM 显示版本信息
    echo 版本信息:
    "%PDFFONTS_PATH%" -v
    echo.
    
    REM 检查测试文件
    if exist "..\官方示例文件\*.pdf" (
        echo 测试文件:
        for %%f in (..\官方示例文件\*.pdf) do (
            echo.
            echo ----------------------------------------
            echo 文件: %%~nxf
            echo ----------------------------------------
            "%PDFFONTS_PATH%" "%%f"
            echo.
        )
    ) else (
        echo [!] 未找到测试 PDF 文件
        echo 请将 PDF 文件放在 官方示例文件 目录中
    )
) else (
    echo [✗] 未找到 pdffonts 工具
    echo.
    echo 请按以下步骤安装:
    echo 1. 访问 https://github.com/oschwartz10612/poppler-windows/releases
    echo 2. 下载 Release-24.08.0-0.zip
    echo 3. 解压到项目根目录，确保路径为: poppler\bin\pdffonts.exe
    echo.
)

echo.
echo ========================================
echo 测试完成
echo ========================================
pause
