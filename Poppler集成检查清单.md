# Poppler 集成检查清单

## ✅ 已完成的工作

- [x] 下载 Poppler 工具包 (Release-25.12.0-0.zip)
- [x] 解压到项目根目录 `poppler` 文件夹
- [x] 创建 `PdfFontInspectorService_Poppler.cs` 服务类
- [x] 修改 `PdfInspectorControl.cs` 使用 Poppler 服务
- [x] 修复类型声明错误
- [x] 添加详细的调试日志
- [x] 创建辅助脚本和文档

## 📋 待完成的步骤

### 步骤 1: 运行准备脚本

```cmd
cd scripts
build_and_test.bat
```

**预期结果：**
```
[✓] Poppler 工具已复制到 Debug 目录
[✓] pdffonts.exe
[✓] poppler.dll
[✓] cairo.dll
[✓] freetype.dll
[✓] pdffonts 工具可以正常运行
```

- [ ] 脚本运行成功
- [ ] 所有文件检查通过

### 步骤 2: 编译项目

在 Visual Studio 中：
1. 打开 `WindowsFormsApp3.sln`
2. 选择 **生成 → 重新生成解决方案** (Ctrl+Shift+B)

**预期结果：**
```
生成成功
0 个错误
```

- [ ] 编译成功，无错误
- [ ] 编译成功，无警告（或只有已知的警告）

### 步骤 3: 运行程序

1. 按 F5 运行程序
2. 程序正常启动

- [ ] 程序启动成功
- [ ] 界面显示正常

### 步骤 4: 测试字体检测

1. 点击"打开 PDF"按钮
2. 选择一个测试 PDF 文件
3. 点击工具栏的"检查器"按钮
4. 切换到"字体"标签页

**预期结果：**
- 显示字体列表
- 字体信息包含：名称、类型、编码、嵌入状态

- [ ] 字体列表显示正常
- [ ] 字体信息完整

### 步骤 5: 验证 Poppler 是否工作

打开日志文件：`app_2026-01-20.log`（或当天日期）

搜索 `[Poppler]`，应该看到：

**✅ 成功使用 Poppler：**
```
[Poppler] ========== 开始字体检测 ==========
[Poppler] 文件路径: C:\...\test.pdf
[Poppler] 开始查找 pdffonts 工具
[Poppler] 应用程序目录: F:\...\bin\Debug\
[Poppler] 检查路径 1: F:\...\bin\Debug\poppler\bin\pdffonts.exe
[Poppler] ✓ 找到便携版 pdffonts: F:\...\bin\Debug\poppler\bin\pdffonts.exe
[Poppler] ✓ 使用 pdffonts 工具: F:\...\bin\Debug\poppler\bin\pdffonts.exe
[Poppler] 执行命令: "F:\...\pdffonts.exe" "C:\...\test.pdf"
[Poppler] 退出代码: 0
[Poppler] 输出长度: XXX 字符
[Poppler] 开始解析输出，长度: XXX 字符
[Poppler] 总行数: X
[Poppler] ✓ 解析字体: XXX (类型: XXX, 嵌入: XXX)
[Poppler] 解析完成，共 X 个字体
[Poppler] ========== 字体检测完成 ==========
[Poppler] 总字体数: X, 问题字体数: X
```

**❌ 回退到 iText：**
```
[Poppler] 开始查找 pdffonts 工具
[Poppler] 检查路径 1: F:\...\bin\Debug\poppler\bin\pdffonts.exe
[Poppler] ✗ 未找到 pdffonts 工具
[Poppler] 未找到 pdffonts 工具，回退到 iText 解析
```

- [ ] 日志显示"✓ 找到便携版 pdffonts"
- [ ] 日志显示"✓ 使用 pdffonts 工具"
- [ ] 日志显示"退出代码: 0"
- [ ] 日志显示"解析完成，共 X 个字体"
- [ ] 没有"回退到 iText"的日志

### 步骤 6: 对比测试

#### 6.1 Adobe Acrobat 检测

1. 用 Adobe Acrobat DC 打开同一个 PDF
2. 点击 **文件 → 属性 → 字体**
3. 记录字体数量和名称

- [ ] 已记录 Adobe Acrobat 的检测结果

#### 6.2 pdffonts 命令行检测

```cmd
cd src\WindowsFormsApp3\bin\Debug\poppler\bin
pdffonts.exe "C:\path\to\test.pdf"
```

- [ ] 已记录 pdffonts 命令行输出

#### 6.3 填写对比表格

| 检测方式 | 字体总数 | 字体名称示例 | 嵌入状态 |
|---------|---------|-------------|---------|
| Adobe Acrobat | ? | ? | ? |
| pdffonts 命令行 | ? | ? | ? |
| 程序显示 | ? | ? | ? |

- [ ] 字体总数一致
- [ ] 字体名称一致
- [ ] 嵌入状态一致

## 🔍 问题排查

### 如果日志显示"未找到 pdffonts 工具"

**检查：**
```cmd
dir src\WindowsFormsApp3\bin\Debug\poppler\bin\pdffonts.exe
```

**解决：**
```cmd
cd scripts
build_and_test.bat
```

### 如果日志显示"退出代码: 1"

**检查：**
```cmd
cd src\WindowsFormsApp3\bin\Debug\poppler\bin
pdffonts.exe "C:\path\to\test.pdf"
```

查看错误信息。

### 如果字体数量仍然不一致

1. 对比 pdffonts 命令行输出和 Adobe Acrobat
2. 如果命令行输出与 Adobe 一致，但程序不一致 → 解析逻辑问题
3. 如果命令行输出与 Adobe 不一致 → pdffonts 工具限制

## 📊 测试结果

### 测试环境
- 操作系统: Windows __
- Visual Studio 版本: ____
- .NET Framework: 4.8
- Poppler 版本: 25.12.0

### 测试文件
- 文件名: ____________
- 文件大小: _______ KB
- 页数: _______

### 测试结果

| 项目 | 结果 | 备注 |
|------|------|------|
| Poppler 工具安装 | ✓ / ✗ | |
| 编译成功 | ✓ / ✗ | |
| 程序运行 | ✓ / ✗ | |
| 字体检测功能 | ✓ / ✗ | |
| Poppler 正常工作 | ✓ / ✗ | |
| 与 Adobe 一致性 | ✓ / ✗ | |

### 一致性评分

- Adobe Acrobat 字体数: _____
- pdffonts 命令行字体数: _____
- 程序显示字体数: _____
- 一致性: ____%

## ✅ 完成标志

当以下所有项目都打勾时，Poppler 集成完成：

- [ ] 所有准备步骤完成
- [ ] 编译无错误
- [ ] 程序正常运行
- [ ] 日志显示 Poppler 正常工作
- [ ] 字体检测结果与 Adobe Acrobat 一致
- [ ] 对比测试完成并记录

## 📝 备注

记录任何问题或特殊情况：

```
（在此记录）
```

---

**测试人员**: ___________  
**测试日期**: ___________  
**测试状态**: ⏳ 进行中 / ✅ 完成 / ❌ 失败
