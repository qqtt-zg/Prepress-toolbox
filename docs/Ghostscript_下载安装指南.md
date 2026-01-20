# Ghostscript 下载安装指南

## 方法一：使用自动化脚本（推荐）

### 1. 运行下载脚本

在项目根目录打开 PowerShell，运行：

```powershell
# 仅下载
.\scripts\download-ghostscript.ps1

# 下载并自动安装
.\scripts\download-ghostscript.ps1 -Install
```

### 2. 脚本说明

**参数**：
- `-Version`: Ghostscript 版本（默认：10.04.0）
- `-OutputDir`: 下载目录（默认：.\ghostscript）
- `-Install`: 自动安装（静默安装）

**示例**：
```powershell
# 下载特定版本
.\scripts\download-ghostscript.ps1 -Version "10.03.1"

# 下载到指定目录
.\scripts\download-ghostscript.ps1 -OutputDir "C:\Downloads"

# 下载并安装
.\scripts\download-ghostscript.ps1 -Install
```

---

## 方法二：手动下载

### 1. 访问官方下载页面

打开浏览器，访问：
```
https://ghostscript.com/releases/gsdnld.html
```

### 2. 选择版本

找到 **Ghostscript AGPL Release** 部分，选择最新版本（推荐 10.04.0 或更高）。

### 3. 下载安装包

根据您的系统选择：

**64位 Windows**：
```
Ghostscript 10.04.0 for Windows (64 bit)
文件名: gs10.04.0-win64.exe
大小: 约 40 MB
```

**32位 Windows**：
```
Ghostscript 10.04.0 for Windows (32 bit)
文件名: gs10.04.0-win32.exe
大小: 约 35 MB
```

### 4. 直接下载链接

如果上述页面无法访问，可以尝试直接下载链接：

**64位**：
```
https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10040/gs10.04.0-win64.exe
```

**32位**：
```
https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10040/gs10.04.0-win32.exe
```

---

## 方法三：使用 PowerShell 直接下载

### 64位系统

```powershell
# 创建下载目录
New-Item -ItemType Directory -Force -Path ".\ghostscript"

# 下载 Ghostscript
Invoke-WebRequest -Uri "https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10040/gs10.04.0-win64.exe" -OutFile ".\ghostscript\gs10.04.0-win64.exe"

Write-Host "下载完成: .\ghostscript\gs10.04.0-win64.exe"
```

### 32位系统

```powershell
# 创建下载目录
New-Item -ItemType Directory -Force -Path ".\ghostscript"

# 下载 Ghostscript
Invoke-WebRequest -Uri "https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10040/gs10.04.0-win32.exe" -OutFile ".\ghostscript\gs10.04.0-win32.exe"

Write-Host "下载完成: .\ghostscript\gs10.04.0-win32.exe"
```

---

## 安装步骤

### 标准安装（推荐）

1. **运行安装程序**
   - 双击下载的 `.exe` 文件
   - 或在 PowerShell 中运行：
     ```powershell
     .\ghostscript\gs10.04.0-win64.exe
     ```

2. **安装向导**
   - 点击 "Next"
   - 接受许可协议
   - 选择安装路径（推荐使用默认路径）
   - 点击 "Install"
   - 等待安装完成
   - 点击 "Finish"

3. **验证安装**
   ```powershell
   # 检查版本
   gswin64c --version
   
   # 或
   gswin32c --version
   ```
   
   如果显示版本号（如 `10.04.0`），说明安装成功。

### 静默安装

如果需要自动化安装（无需用户交互）：

```powershell
# 64位
.\ghostscript\gs10.04.0-win64.exe /S

# 32位
.\ghostscript\gs10.04.0-win32.exe /S
```

---

## 便携式部署（无需安装）

如果不想在系统中安装 Ghostscript，可以使用便携式部署：

### 1. 提取文件

使用 7-Zip 或其他解压工具提取安装包：

```powershell
# 使用 7-Zip 提取（需要先安装 7-Zip）
7z x .\ghostscript\gs10.04.0-win64.exe -o.\ghostscript\portable
```

### 2. 复制到应用程序目录

将以下文件复制到应用程序目录：

```
应用程序目录/
├── 大诚重命名工具.exe
├── gswin64c.exe          ← 从 bin 目录复制
├── gsdll64.dll           ← 从 bin 目录复制
└── lib/                  ← 整个 lib 目录
    ├── gs_init.ps
    ├── pdf_main.ps
    └── ...
```

### 3. 测试

```powershell
# 进入应用程序目录
cd "F:\编程项目\Prepress-toolbox\src\WindowsFormsApp3\bin\Debug\net48\win-x64"

# 测试 Ghostscript
.\gswin64c.exe --version
```

---

## 验证安装

### 方法 1：命令行验证

```powershell
# 检查版本
gswin64c --version

# 或
gswin32c --version
```

**预期输出**：
```
10.04.0
```

### 方法 2：测试转曲功能

```powershell
# 创建测试 PDF（如果有）
$testPdf = "test.pdf"
$outputPdf = "test_outlined.pdf"

# 执行转曲
gswin64c -o $outputPdf -dNoOutputFonts -sDEVICE=pdfwrite $testPdf

# 检查输出文件
if (Test-Path $outputPdf) {
    Write-Host "✓ 转曲成功!" -ForegroundColor Green
} else {
    Write-Host "✗ 转曲失败" -ForegroundColor Red
}
```

### 方法 3：在应用程序中验证

1. 运行应用程序
2. 打开 PDF 检查器
3. 切换到字体标签页
4. 点击"转曲"按钮
5. 查看是否提示"未找到 Ghostscript"

---

## 常见问题

### Q: 下载速度很慢怎么办？

**A**: 可以尝试以下方法：
1. 使用下载工具（如 IDM、迅雷）
2. 从 GitHub 镜像下载
3. 使用国内镜像（如果有）

### Q: 安装后找不到 gswin64c.exe？

**A**: 检查以下位置：
```
C:\Program Files\gs\gs10.04.0\bin\gswin64c.exe
C:\Program Files (x86)\gs\gs10.04.0\bin\gswin32c.exe
```

如果找不到，重新安装并确保选择了正确的安装路径。

### Q: 提示"无法运行此应用"？

**A**: 可能原因：
1. 下载的版本与系统不匹配（32位/64位）
2. 文件损坏，重新下载
3. 缺少运行时库，安装 Visual C++ Redistributable

### Q: 应用程序提示"未找到 Ghostscript"？

**A**: 解决方法：
1. 确认 Ghostscript 已正确安装
2. 将 Ghostscript 的 bin 目录添加到 PATH 环境变量
3. 或将 gswin64c.exe 复制到应用程序目录

### Q: 需要购买许可证吗？

**A**: 
- **个人使用**：免费（AGPL 许可证）
- **开源项目**：免费（AGPL 许可证）
- **商业闭源项目**：需要购买商业许可证

---

## 卸载 Ghostscript

### Windows 控制面板

1. 打开"控制面板"
2. 选择"程序和功能"
3. 找到"GPL Ghostscript"
4. 点击"卸载"

### PowerShell

```powershell
# 查找 Ghostscript
Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -like "*Ghostscript*" }

# 卸载（替换为实际的 IdentifyingNumber）
$app = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -like "*Ghostscript*" }
$app.Uninstall()
```

---

## 其他资源

### 官方资源
- **官网**: https://ghostscript.com
- **文档**: https://ghostscript.com/docs/
- **GitHub**: https://github.com/ArtifexSoftware/ghostpdl-downloads

### 社区资源
- **Stack Overflow**: https://stackoverflow.com/questions/tagged/ghostscript
- **Reddit**: https://www.reddit.com/r/ghostscript/

---

## 快速命令参考

```powershell
# 下载（64位）
Invoke-WebRequest -Uri "https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10040/gs10.04.0-win64.exe" -OutFile "gs-installer.exe"

# 安装
.\gs-installer.exe

# 验证
gswin64c --version

# 测试转曲
gswin64c -o output.pdf -dNoOutputFonts -sDEVICE=pdfwrite input.pdf
```

---

**文档版本**: v1.0  
**更新日期**: 2026-01-19  
**适用版本**: Ghostscript 10.04.0
