# C# 项目分析：对接 C++ Adobe Acrobat 插件（摘要）

> 项目：Prepress-toolbox / WindowsFormsApp3（.NET Framework 4.8，WinForms）

本文档基于对当前 C# 工程代码的静态分析，提取与“热文件夹工作流 + PDF 微操作 + 对外任务分发”相关的关键实现点，便于与你的 C++ Acrobat 插件架构对接。

---

## 1. 文件处理逻辑

### 1.1 PDF 文件识别与重命名：入口与核心服务

- **核心服务**：`src/WindowsFormsApp3/Services/FileRenameService.cs`
- **文件系统操作**：
  - **移动/重命名**：`File.Move(source, target)`
  - **复制模式**：`File.Copy(source, target, false)`
- **文件名冲突处理**：`HandleFileNameConflict(...)`
  - 若目标已存在，将自动生成：`xxx(1).pdf`、`xxx(2).pdf`... 直到不冲突。

### 1.2 命名规则（是否存在命名规则引擎）

该项目并非单一“规则引擎”，而是并行提供两种生成方式：

#### A) 字段拼接式命名（业务字段顺序拼接）
- 方法：`GenerateNewFileName(FileRenameInfo fileInfo, string separator)`
- 拼接字段顺序（按代码顺序）：
  - `SerialNumber`（序号）
  - `OrderNumber`（订单号）
  - `Material`（材料）
  - `Quantity`（数量）
  - `Dimensions`（尺寸）
  - `CompositeColumn`（列组合）
  - 最后追加原扩展名

典型结果：
- `序号_订单号_材料_数量_尺寸_列组合.pdf`（分隔符可配置）

#### B) 模板占位符式命名（类规则引擎）
- 方法：`GenerateNewFileName(string baseName, string extension, string pattern, int sequenceNumber = 1)`
- 支持占位符：
  - `{序号}`
  - `{原文件名}`
  - `{扩展名}`
  - `{日期}`（`yyyyMMdd`）
  - `{时间}`（`HHmmss`）
- 额外处理：
  - 清理非法文件名字符（`Path.GetInvalidFileNameChars()`）
  - 必要时自动补扩展名

### 1.3 热文件夹监控机制（FileSystemWatcher / 轮询）

- **明确使用 `FileSystemWatcher`（非轮询）**
- **核心实现**：`src/WindowsFormsApp3/Services/FileMonitor.cs`
- 关键配置：
  - 监听事件：`Created`、`Renamed`、`Changed`、`Error`
  - `NotifyFilter = LastWrite | FileName | DirectoryName`
  - **`Filter = "*.pdf"`（只监控 PDF）**
  - `IncludeSubdirectories = false`（可由 `StartMonitoring(path, includeSubdirectories)` 覆盖）
  - `Changed` 中 `Thread.Sleep(500)`：用于等待落盘写入完成（简化的文件稳定性处理）

---

## 2. PDF 操作能力

### 2.1 已集成的 PDF 库（NuGet / 本地引用）

来自 `src/WindowsFormsApp3/WindowsFormsApp3.csproj`：

- **iText 7（itext 9.3.0）**：
  - `itext`
  - `itext.bouncy-castle-adapter`
  - `itext.font-asian`
  - `itext7.pdfcalligraph`
- **iTextSharp LGPL Core**：`iTextSharp.LGPLv2.Core (3.4.22)`
- **PDFsharp**：`PDFsharp (6.2.0)`
- **Spire.Pdf**：本地引用 `Spire.Pdf.dll (9.9.0.0)`
- **PdfiumViewer**：
  - `PdfiumViewer.Native.x86_64.v8-xfa`
  - 本地 `PdfiumViewer.dll`（自定义版，注释显示“实现单页滚动”）
- **Poppler 工具链**：项目根目录 `poppler/bin` 并被复制到输出目录（常用于字体/信息提取）

> 结论：C# 侧 PDF 处理能力强且“多栈混用”，可按模块逐步迁移到 C++ 插件侧，或保留在 C# 侧做预处理。

### 2.2 已实现/调用的 PDF 微操作（从重命名流程可见）

在 `FileRenameService` 的“重命名 + PDF处理”管线中，已集成以下能力（通过 `PdfTools`/相关服务调用）：

- **页面重排/统一页面盒（坐标系统统一）**
  - `PdfTools.AdvancedPageReorganizer.ExecuteAdvancedReorganization(...)`
- **插入标识页（通常插入到第一页）**
  - `PdfTools.InsertIdentifierPage(...)`
- **折手（Folding）模式补空白页**
  - 按 `LayoutQuantity` 补齐页数到倍数（通过 `InsertIdentifierPage` 插入空白页）
- **添加图层（点号/计数器/出血线等）**
  - 处理前检查目标图层是否已存在：`_pdfDimensionService.CheckPdfLayersExist(...)`
  - 添加处理：`PdfTools.AddDotsAddCounterLayer(...)`
- **全页旋转**
  - `PdfTools.RotateAllPages(path, angle)`

---

## 3. 对外交互接口

### 3.1 外部 exe 调用（Process.Start）

本次分析已发现工程内存在 `Process.Start` 关键词命中，但**尚未逐文件确认具体调用点、参数与用途**。

建议下一步聚焦打开并确认以下文件中的实际调用逻辑：
- `src/WindowsFormsApp3/Program.cs`
- `src/WindowsFormsApp3/Forms/Main/MainShellForm.cs`
- `src/WindowsFormsApp3/Forms/Panels/PdfOperationsPanel.cs`
- `src/WindowsFormsApp3/Forms/Panels/AeWorkspacePanel.cs`

### 3.2 COM / Acrobat IAC（互操作）

同样已检索到 `AcroApp/Interop/COM` 等关键词命中，但**尚未实锤定位具体实现**（是否真实接入 Acrobat IAC、是否仅为预留/残留引用）。

### 3.3 配置管理方式（JSON/XML）

- **配置服务**：`src/WindowsFormsApp3/Services/ConfigService.cs`
- **格式**：JSON（`Newtonsoft.Json`）
- **存储模型**：
  - 主配置文件为 `Dictionary<string, AppConfig>`
  - 路径由 `AppDataPathManager.ConfigFilePath` 提供（AppData 下）
- **支持能力**：
  - Load / Save
  - Import / Export
  - 缓存（`ICacheService`，过期 5 分钟）

> 结论：建议与你的 C++ 插件/外部 Worker 统一采用 JSON 作为协议与配置格式，便于跨语言对接。

---

## 4. 工作流状态机（典型文件生命周期）

结合 `FileMonitor` + `FileRenameService`，可抽象出典型生命周期：

1. **文件进入热文件夹**
   - `FileSystemWatcher.Created/Changed` 捕获新 PDF。
   - `Changed` 中等待 `500ms` 以提升“写入完成”概率。

2. **解析/生成新文件名（命名规则）**
   - 字段拼接模式或模板占位符模式。
   - 可选：对原文件名做正则提取（`ProcessRegexMatch`）。

3. **（可选）PDF 预处理管线**
   - 图层存在性检查（存在则跳过大量处理）。
   - 页面重排（统一页面盒/坐标）。
   - 插入标识页 / 折手补空白页。
   - 添加图层（点号/计数器/出血线）。
   - 页面旋转。

4. **生成最终输出文件（落地）**
   - 先在临时文件上完成 PDF 处理，最后再复制/移动到目标目录。
   - 处理同名冲突，确保输出可落地。

5. **事件发布/可扩展挂点**
   - 重命名成功后会发布事件（用于 UI 更新、日志、以及后续扩展分发）：
     - `FileRenamed` / `FileRenamedSuccessfully` 事件
     - `_eventBus.Publish(new FileRenamedEvent {...})`

---

## 5. 任务分发建议：重命名结束后输出 JSON 指令给外部程序

### 5.1 推荐触发点

建议以 `FileRenameService` 中“重命名成功”后的事件为唯一权威挂点：
- `FileRenamed` / `FileRenamedSuccessfully`
- 或订阅事件总线中的 `FileRenamedEvent`

### 5.2 推荐分发方式（更稳）——写入 Job JSON 到队列目录

相比直接 `Process.Start`：
- 可重试（worker 崩溃也不会丢任务）
- 可审计（每个 JSON 即记录）
- 解耦（C# UI/服务不阻塞）

**建议协议（最小可用字段）**：

```json
{
  "jobId": "c2f0b9c9-0c84-4b3c-9bd0-4c2f1f3a6d7e",
  "event": "rename_completed",
  "sourcePath": "D:\\Hotfolder\\in\\a.pdf",
  "targetPath": "D:\\Hotfolder\\out\\01_ORD123_PET_100_100x200.pdf",
  "timestamp": "2026-02-07T15:01:52+08:00",
  "payload": {
    "workflow": "imposition",
    "engine": "acrobat_cpp_plugin",
    "params": {
      "layout": "N-up",
      "rows": 2,
      "cols": 3
    }
  }
}
```

### 5.3 外部程序/插件侧消费模型

- 方案 A：外部 Worker（C++/Rust/Go）监控 `job_queue` 目录，消费 JSON，调用 Acrobat + C++ 插件执行拼版/处理。
- 方案 B：若 Acrobat 插件宿主进程可常驻，也可以由宿主直接消费队列 JSON。

---

## 6. 待进一步确认项（如需“实锤代码片段”）

- **`Process.Start` 的真实使用点**：是否已有稳定的外部 exe 分发器。
- **Acrobat IAC / COM 互操作**：是否已集成、调用边界是什么。

如你希望我继续补齐这两项，我会逐一打开命中最集中的文件并整理出可直接对接的接口与参数。
