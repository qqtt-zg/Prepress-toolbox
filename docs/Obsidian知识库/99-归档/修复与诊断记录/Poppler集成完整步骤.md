# Poppler 集成完整步骤

## 当前状态

✅ Poppler 工具已下载并解压到 `poppler` 文件夹  
✅ 代码已修改为使用 `PdfFontInspectorService_Poppler`  
✅ 添加了详细的调试日志  
⏳ 需要复制 poppler 到编译输出目录并测试

## 步骤 1: 复制 Poppler 到编译输出目录

### 方法 A: 使用脚本自动复制（推荐）

```cmd
cd scripts
copy_poppler_to_bin.bat
```

### 方法 B: 手动复制

```cmd
xcopy /E /I /Y poppler src\WindowsFormsApp3\bin\Debug\poppler
xcopy /E /I /Y poppler src\WindowsFormsApp3\bin\Release\poppler
```

### 验证复制结果

```cmd
dir src\WindowsFormsApp3\bin\Debug\poppler\bin\pdffonts.exe
```

应该显示文件存在。

## 步骤 2: 编译项目

1. 打开 Visual Studio
2. 打开 `WindowsFormsApp3.sln`
3. 选择 **生成 → 重新生成解决方案** (Ctrl+Shift+B)
4. 确保编译成功，无错误

## 步骤 3: 运行程序并测试

1. 按 F5 运行程序（或点击"开始"按钮）
2. 打开一个 PDF 文件
3. 点击工具栏的"检查器"按钮
4. 切换到"字体"标签页
5. 查看字体列表

## 步骤 4: 查看日志验证 Poppler 是否工作

### 4.1 打开日志文件

日志文件位置：`app_2026-01-20.log`（或当天日期）

### 4.2 搜索关键日志

**成功使用 Poppler 的标志：**

```
[Poppler] ========== 开始字体检测 ==========
[Poppler] 文件路径: C:\...\test.pdf
[Poppler] 开始查找 pdffonts 工具
[Poppler] 应用程序目录: F:\...\src\WindowsFormsApp3\bin\Debug\
[Poppler] 检查路径 1: F:\...\bin\Debug\poppler\bin\pdffonts.exe
[Poppler] ✓ 找到便携版 pdffonts: F:\...\bin\Debug\poppler\bin\pdffonts.exe
[Poppler] ✓ 使用 pdffonts 工具: F:\...\bin\Debug\poppler\bin\pdffonts.exe
[Poppler] 执行命令: "F:\...\pdffonts.exe" "C:\...\test.pdf"
[Poppler] 退出代码: 0
[Poppler] 输出长度: 456 字符
[Poppler] 开始解析输出，长度: 456 字符
[Poppler] 总行数: 8
[Poppler] ✓ 解析字体: SimSun (类型: CID Type 0C, 嵌入: SubsetEmbedded)
[Poppler] ✓ 解析字体: Arial-Bold (类型: TrueType, 嵌入: FullyEmbedded)
[Poppler] 解析完成，共 5 个字体
[Poppler] ========== 字体检测完成 ==========
[Poppler] 总字体数: 5, 问题字体数: 1
```

**回退到 iText 的标志：**

```
[Poppler] 开始查找 pdffonts 工具
[Poppler] 应用程序目录: F:\...\bin\Debug\
[Poppler] 检查路径 1: F:\...\bin\Debug\poppler\bin\pdffonts.exe
[Poppler] ✗ 未找到 pdffonts 工具
[Poppler] 请确保 poppler\bin\pdffonts.exe 存在于应用程序目录
[Poppler] 未找到 pdffonts 工具，回退到 iText 解析
```

## 步骤 5: 对比测试

### 5.1 准备测试文件

选择一个包含多种字体的 PDF 文件。

### 5.2 Adobe Acrobat 检测

1. 用 Adobe Acrobat DC 打开 PDF
2. 点击 **文件 → 属性 → 字体**
3. 记录所有字体信息

### 5.3 pdffonts 命令行检测

```cmd
cd src\WindowsFormsApp3\bin\Debug\poppler\bin
pdffonts.exe "C:\path\to\your\test.pdf"
```

记录输出结果。

### 5.4 程序检测

在程序中打开同一个 PDF，查看字体列表。

### 5.5 填写对比表格

| 字体名称 | Adobe Acrobat | pdffonts 命令行 | 程序显示 | 一致性 |
|---------|--------------|----------------|---------|--------|
| 示例字体1 | 嵌入子集 | yes/yes | 嵌入子集 | ✓ |
| 示例字体2 | 未嵌入 | no/no | 未嵌入 | ✓ |
| ... | ... | ... | ... | ... |

## 常见问题排查

### 问题 1: 日志显示"未找到 pdffonts 工具"

**原因：** poppler 文件夹未复制到编译输出目录

**解决：**
```cmd
cd scripts
copy_poppler_to_bin.bat
```

然后重新运行程序。

### 问题 2: 日志显示"退出代码: 1"

**可能原因：**
- PDF 文件路径错误
- PDF 文件损坏
- DLL 依赖缺失

**检查：**
```cmd
cd src\WindowsFormsApp3\bin\Debug\poppler\bin
pdffonts.exe "C:\path\to\test.pdf"
```

查看命令行输出的错误信息。

### 问题 3: 解析字体数量为 0

**可能原因：**
- PDF 文件没有字体（已转曲或纯图片）
- pdffonts 输出格式不符合预期

**检查：**
查看日志中的"原始输出"部分，确认 pdffonts 是否真的返回了字体数据。

### 问题 4: 字体数量仍然与 Adobe 不一致

**可能原因：**
- pdffonts 版本问题
- PDF 文件特殊格式
- 解析逻辑问题

**调试：**
1. 对比 pdffonts 命令行输出和 Adobe Acrobat 显示
2. 如果命令行输出与 Adobe 一致，但程序显示不一致，则是解析逻辑问题
3. 如果命令行输出就与 Adobe 不一致，则是 pdffonts 工具本身的限制

## 步骤 6: 修改项目文件自动复制（可选）

为了避免每次编译后都要手动复制，可以修改项目文件：

### 6.1 打开项目文件

用文本编辑器打开 `src\WindowsFormsApp3\WindowsFormsApp3.csproj`

### 6.2 添加复制规则

在 `</Project>` 标签之前添加：

```xml
  <!-- 自动复制 Poppler 工具到输出目录 -->
  <ItemGroup>
    <None Include="..\..\poppler\bin\**\*.*">
      <Link>poppler\bin\%(RecursiveDir)%(Filename)%(Extension)</Link>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
```

### 6.3 保存并重新编译

这样每次编译时会自动复制 poppler 文件夹。

## 步骤 7: 更新安装包配置

如果要发布程序，需要在安装包中包含 Poppler 工具。

### 7.1 打开 Inno Setup 脚本

找到 `installers` 目录下的 `.iss` 文件。

### 7.2 添加文件复制规则

在 `[Files]` 部分添加：

```iss
; Poppler 工具
Source: "..\..\poppler\bin\*"; DestDir: "{app}\poppler\bin"; Flags: ignoreversion recursesubdirs
```

### 7.3 重新生成安装包

## 验证清单

完成以下所有项目后，Poppler 集成才算完成：

- [ ] poppler 文件夹已复制到 `src\WindowsFormsApp3\bin\Debug\`
- [ ] 编译成功，无错误
- [ ] 程序可以正常运行
- [ ] 日志显示"✓ 找到便携版 pdffonts"
- [ ] 日志显示"✓ 使用 pdffonts 工具"
- [ ] 日志显示"退出代码: 0"
- [ ] 日志显示"解析完成，共 X 个字体"
- [ ] 程序界面显示字体列表
- [ ] 字体数量与 Adobe Acrobat 一致
- [ ] 字体嵌入状态与 Adobe Acrobat 一致
- [ ] 项目文件已配置自动复制（可选）
- [ ] 安装包配置已更新（可选）

## 下一步

如果所有验证项都通过，说明 Poppler 集成成功！

如果仍然有问题，请提供：
1. 完整的日志文件（从"开始字体检测"到"字体检测完成"）
2. pdffonts 命令行输出
3. Adobe Acrobat 字体列表截图
4. 程序中显示的字体列表截图

这样我可以进一步分析问题。

---

**最后更新**: 2026-01-20  
**状态**: 等待测试验证
