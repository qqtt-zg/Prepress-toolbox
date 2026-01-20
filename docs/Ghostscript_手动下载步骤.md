# Ghostscript 手动下载步骤

由于自动下载脚本遇到网络问题，请按照以下步骤手动下载 Ghostscript。

---

## 📥 下载步骤

### 方法 1：从官网下载（推荐）

1. **打开浏览器**，访问 Ghostscript 官方下载页面：
   ```
   https://ghostscript.com/releases/gsdnld.html
   ```

2. **找到 AGPL Release 部分**，向下滚动找到：
   ```
   Ghostscript AGPL Release
   ```

3. **选择版本**，点击最新版本（推荐 10.04.0）：
   ```
   Ghostscript 10.04.0 for Windows (64 bit)
   ```

4. **下载文件**：
   - 文件名：`gs10.04.0-win64.exe`
   - 大小：约 40 MB
   - 保存到：`F:\编程项目\Prepress-toolbox\ghostscript\`

### 方法 2：从 GitHub 下载

1. **打开浏览器**，访问 GitHub Releases 页面：
   ```
   https://github.com/ArtifexSoftware/ghostpdl-downloads/releases
   ```

2. **找到最新版本**，点击 `gs10.04.0` 或更高版本

3. **下载对应文件**：
   - **64位系统**：`gs10.04.0-win64.exe`
   - **32位系统**：`gs10.04.0-win32.exe`

4. **保存位置**：
   ```
   F:\编程项目\Prepress-toolbox\ghostscript\gs10.04.0-win64.exe
   ```

### 方法 3：使用浏览器直接下载

**64位系统**，复制以下链接到浏览器地址栏：
```
https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10040/gs10.04.0-win64.exe
```

**32位系统**，复制以下链接到浏览器地址栏：
```
https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10040/gs10.04.0-win32.exe
```

---

## 📦 安装步骤

### 1. 找到下载的文件

下载完成后，文件应该在：
```
F:\编程项目\Prepress-toolbox\ghostscript\gs10.04.0-win64.exe
```

或者在浏览器的默认下载目录（通常是 `C:\Users\你的用户名\Downloads\`）

### 2. 运行安装程序

**方法 A：双击安装**
1. 双击 `gs10.04.0-win64.exe`
2. 如果出现安全警告，点击"运行"
3. 按照安装向导操作

**方法 B：使用 PowerShell 安装**
```powershell
# 进入下载目录
cd "F:\编程项目\Prepress-toolbox\ghostscript"

# 运行安装程序
.\gs10.04.0-win64.exe
```

### 3. 安装向导

1. **欢迎界面**
   - 点击 "Next"

2. **许可协议**
   - 阅读 AGPL 许可协议
   - 选择 "I accept the agreement"
   - 点击 "Next"

3. **选择安装路径**
   - 推荐使用默认路径：`C:\Program Files\gs\gs10.04.0\`
   - 或自定义路径
   - 点击 "Next"

4. **选择组件**
   - 保持默认选择（全部安装）
   - 点击 "Next"

5. **开始安装**
   - 点击 "Install"
   - 等待安装完成（约 1-2 分钟）

6. **完成安装**
   - 点击 "Finish"

---

## ✅ 验证安装

### 方法 1：命令行验证

打开 PowerShell 或命令提示符，输入：

```powershell
gswin64c --version
```

**预期输出**：
```
10.04.0
```

如果显示版本号，说明安装成功！

### 方法 2：检查安装目录

检查以下目录是否存在：

```
C:\Program Files\gs\gs10.04.0\bin\gswin64c.exe
```

如果文件存在，说明安装成功！

### 方法 3：在应用程序中测试

1. 运行 `大诚重命名工具.exe`
2. 加载一个 PDF 文件
3. 打开 PDF 检查器
4. 切换到"字体"标签页
5. 点击"转曲"按钮
6. 如果没有提示"未找到 Ghostscript"，说明安装成功！

---

## 🔧 故障排除

### 问题 1：下载链接无法访问

**解决方案**：
1. 检查网络连接
2. 尝试使用 VPN 或代理
3. 使用下载工具（如 IDM、迅雷）
4. 从其他镜像站下载

### 问题 2：下载速度很慢

**解决方案**：
1. 使用下载工具加速
2. 更换网络环境
3. 从国内镜像下载（如果有）

### 问题 3：安装后找不到 gswin64c.exe

**解决方案**：
1. 检查安装路径：
   ```
   C:\Program Files\gs\gs10.04.0\bin\
   ```
2. 如果不在默认路径，搜索文件：
   ```powershell
   Get-ChildItem -Path "C:\" -Filter "gswin64c.exe" -Recurse -ErrorAction SilentlyContinue
   ```
3. 将找到的路径添加到 PATH 环境变量

### 问题 4：应用程序仍提示"未找到 Ghostscript"

**解决方案 A：添加到 PATH**
1. 右键"此电脑" → "属性"
2. 点击"高级系统设置"
3. 点击"环境变量"
4. 在"系统变量"中找到"Path"
5. 点击"编辑"
6. 点击"新建"
7. 添加：`C:\Program Files\gs\gs10.04.0\bin`
8. 点击"确定"保存
9. 重启应用程序

**解决方案 B：复制到应用程序目录**
1. 找到 Ghostscript 安装目录：
   ```
   C:\Program Files\gs\gs10.04.0\bin\
   ```
2. 复制以下文件到应用程序目录：
   ```
   gswin64c.exe
   gsdll64.dll
   ```
3. 复制整个 `lib` 文件夹
4. 重启应用程序

---

## 📋 快速检查清单

安装完成后，请检查以下项目：

- [ ] 下载了正确的版本（64位/32位）
- [ ] 安装程序运行成功
- [ ] 可以在命令行运行 `gswin64c --version`
- [ ] 文件存在：`C:\Program Files\gs\gs10.04.0\bin\gswin64c.exe`
- [ ] 应用程序可以找到 Ghostscript
- [ ] 字体转曲功能正常工作

---

## 🆘 需要帮助？

如果遇到问题，请提供以下信息：

1. **系统信息**：
   ```powershell
   systeminfo | findstr /B /C:"OS Name" /C:"OS Version" /C:"System Type"
   ```

2. **Ghostscript 安装路径**：
   ```powershell
   Get-ChildItem -Path "C:\Program Files" -Filter "gs*" -Directory
   ```

3. **PATH 环境变量**：
   ```powershell
   $env:PATH -split ';' | Where-Object { $_ -like "*gs*" }
   ```

4. **错误日志**：
   查看应用程序日志文件：`app_2026-01-19.log`

---

## 📚 相关文档

- `docs/Ghostscript_下载安装指南.md` - 完整安装指南
- `docs/PDF字体转曲_使用说明.md` - 字体转曲功能使用说明
- `docs/PDF字体转曲_开源方案调研.md` - 技术方案说明

---

**提示**：如果您已经成功下载并安装了 Ghostscript，可以直接测试字体转曲功能了！

**下一步**：
1. ✅ 下载 Ghostscript
2. ✅ 安装 Ghostscript
3. ✅ 验证安装
4. 🎯 测试字体转曲功能

---

**文档版本**: v1.0  
**更新日期**: 2026-01-19  
**状态**: 等待用户手动下载
