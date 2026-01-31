# FileRenamePanel 功能增强开发提示词

## 任务背景

这是一个 Windows Forms 应用程序（.NET Framework 4.8），使用 C# 9.0 开发。你需要修改 `FileRenamePanel.cs` 文件，增强 JSON 配置文件的自动管理功能。

**当前已有实现**：
- `_fileTable` 是核心数据表格控件
- `_cmbJsonFiles` 是下拉选择框，用于加载/保存 JSON 配置文件
- JSON 文件用于保存 `_fileTable` 的数据，并可供 `_fileTable` 加载
- 项目使用 iText 9 处理 PDF，使用 AntdUI 作为 UI 框架

---

## 核心需求

### 需求 1：修改 JSON 文件加载路径
**目标**：将 `_cmbJsonFiles` 的文件加载路径改为 Windows 系统的标准应用数据目录。

**具体要求**：
- 路径格式：`%AppData%\Roaming\大诚重命名工具\SavedGrids\`
- 即：`C:\Users\[用户名]\AppData\Roaming\大诚重命名工具\SavedGrids\`
- 如果目录不存在，需要在程序启动时自动创建
- `_cmbJsonFiles` 的下拉选项应该只显示该目录下的 `.json` 文件

**代码实现位置**：
- 修改文件路径相关的初始化代码
- 确保 `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)` 用于获取 AppData 路径

---

### 需求 2：每日首次打开自动生成当日 JSON 文件
**目标**：软件启动时，检查当日是否已存在对应的 JSON 文件。如果不存在，自动创建一个新的。

**具体要求**：
- 文件命名规则：`YYYY-MM-DD.json`（标准 ISO 8601 日期格式）
- 例如：`2026-01-31.json`、`2026-02-01.json`
- 触发时机：软件启动/面板初始化时
- 判断逻辑：
  ```csharp
  // 伪代码
  string todayFileName = $"{DateTime.Now:yyyy-MM-dd}.json";
  string todayFilePath = Path.Combine(appDataPath, todayFileName);
  if (!File.Exists(todayFilePath))
  {
      // 自动创建新的 JSON 文件
      // 保存当前 _fileTable 的数据到该文件
  }
  ```
- 新创建的文件应该包含当前 `_fileTable` 的所有数据

---

### 需求 3：再次打开时自动加载当日 JSON 文件
**目标**：软件启动时，如果当日 JSON 文件已存在，自动加载该文件到 `_fileTable`。

**具体要求**：
- 触发时机：软件启动/面板初始化时
- 优先级：自动加载当日文件优先级最高
- 如果当日文件不存在，按照需求 2 创建新文件
- 加载逻辑：
  ```csharp
  // 伪代码
  string todayFileName = $"{DateTime.Now:yyyy-MM-dd}.json";
  string todayFilePath = Path.Combine(appDataPath, todayFileName);
  if (File.Exists(todayFilePath))
  {
      // 自动调用加载逻辑，将文件数据加载到 _fileTable
      // 同时在 _cmbJsonFiles 中选中该文件
  }
  else
  {
      // 创建新文件（需求 2）
  }
  ```

---

### 需求 4：异常/手动关闭时自动保存
**目标**：无论软件是正常关闭还是异常关闭，都需要按照当前选择的 JSON 文件保存当前 `_fileTable` 数据。

**技术难点**：需要你设计异常关闭的检测机制。

**具体要求**：
- **正常关闭保存**：
  - 用户手动点击关闭按钮/退出菜单时触发
  - 保存到当前 `_cmbJsonFiles` 选中的 JSON 文件
  - 如果没有选中文件，保存到当日的 JSON 文件

- **异常关闭保存**：
  - 需要你设计并实现异常关闭的检测机制
  - 建议的检测方案（供你参考，可自行选择）：
    1. **定期自动保存方案**：设置定时器（如每 30 秒），自动保存当前状态到临时文件
    2. **标记文件方案**：启动时创建标记文件，正常关闭时删除标记文件；异常重启时检测标记文件存在则恢复
    3. **Application.ApplicationExit 事件**：监听应用程序退出事件
  - 恢复逻辑：
    - 软件再次启动时，检查是否存在未保存的临时数据
    - 如果存在，提示用户是否恢复上次的数据
    - 用户确认后加载备份数据到 `_fileTable`

**注意**：异常关闭的检测和恢复需要你根据项目实际情况设计方案，确保数据不会丢失。

---

## 技术约束与要求

### 代码规范（严格遵守）

1. **命名规范**：
   - 类、方法、属性：`PascalCase`
   - 私有字段：`_camelCase`（例如：`_jsonFilePath`）
   - 局部变量、参数：`camelCase`

2. **注释规范**：
   - **所有注释使用简体中文**
   - 复杂逻辑必须添加注释说明
   - 公共方法需要 XML 文档注释（`///`）

3. **代码风格**：
   - 缩进：4 个空格
   - 大括号：Allman 风格（新行）
   - using 语句：放在 namespace 外部

4. **异步操作**：
   - 文件 I/O 操作必须使用 `async/await`
   - 禁止使用 `.Result` 或 `.Wait()`（防止死锁）
   - UI 操作必须在主线程执行

5. **路径处理**：
   - 所有路径拼接使用 `Path.Combine()`
   - 路径可能包含空格和中文字符，需要正确处理

6. **日志记录**：
   - 使用 `LogHelper` 记录日志（不要使用 `Console.WriteLine`）
   - 关键操作（文件创建、加载、保存）需要记录日志
   - 异常情况必须记录错误日志

---

## 架构要求

### MVP 架构原则

项目采用 MVP（Model-View-Presenter）架构，你需要遵循以下原则：

1. **View 层（FileRenamePanel.cs）**：
   - **职责**：UI 事件转发，不包含业务逻辑
   - **禁止**：在 View 中实现文件操作、数据保存等业务逻辑
   - **正确做法**：通过调用 Presenter 或 Service 层的方法实现业务逻辑

2. **Service 层（如果需要新增）**：
   - **职责**：业务逻辑实现（如文件管理、JSON 序列化）
   - **状态**：尽可能保持无状态
   - **错误处理**：使用 `LogHelper` 记录错误，抛出异常或返回 `Result<T>`

3. **依赖注入**：
   - 优先使用构造函数注入
   - 如果不可行（如遗留代码），使用 `ServiceLocator.Instance.GetService<T>()`

---

## 开发步骤建议

### 阶段 1：路径迁移（需求 1）
1. 在 `FileRenamePanel.cs` 中定位 JSON 文件路径相关的代码
2. 修改路径为：`Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "大诚重命名工具", "SavedGrids")`
3. 在构造函数或初始化方法中添加目录创建逻辑（如果不存在）
4. 测试：确保 `_cmbJsonFiles` 能正确加载新路径下的文件

### 阶段 2：自动创建当日文件（需求 2）
1. 在面板初始化时添加当日文件检查逻辑
2. 使用 `DateTime.Now.ToString("yyyy-MM-dd")` 生成文件名
3. 如果文件不存在，调用保存逻辑创建新文件
4. 测试：启动软件，检查是否自动创建了当日文件

### 阶段 3：自动加载当日文件（需求 3）
1. 在面板初始化时添加当日文件加载逻辑
2. 如果文件存在，自动加载并选中
3. 确保 `_cmbJsonFiles` 的下拉选项也显示当日文件
4. 测试：
   - 当日文件存在时，启动后自动加载
   - 当日文件不存在时，自动创建并加载

### 阶段 4：异常/手动关闭保存（需求 4）
1. 实现 Application.ApplicationExit 事件监听
2. 实现正常关闭保存逻辑（保存到当前选中文件）
3. 设计并实现异常关闭检测机制（你自行决定方案）
4. 实现异常恢复逻辑（启动时检查并提示恢复）
5. 测试：
   - 正常关闭后数据是否保存
   - 强制关闭进程后数据是否恢复

---

## 验收标准

### 功能验收
- [ ] JSON 文件路径正确指向 `%AppData%\Roaming\大诚重命名工具\SavedGrids\`
- [ ] 首次启动时，自动创建当日 JSON 文件（文件名格式：`YYYY-MM-DD.json`）
- [ ] 再次启动时，自动加载当日 JSON 文件到 `_fileTable`
- [ ] 正常关闭时，数据正确保存到当前选中的 JSON 文件
- [ ] 异常关闭后，再次启动时能恢复数据（方案由你设计）
- [ ] `_cmbJsonFiles` 下拉框正确显示新路径下的所有 JSON 文件

### 代码质量验收
- [ ] 所有文件 I/O 操作使用 `async/await`
- [ ] 所有路径拼接使用 `Path.Combine()`
- [ ] 所有注释使用简体中文
- [ ] 关键操作使用 `LogHelper` 记录日志
- [ ] 异常处理完善，不会导致程序崩溃
- [ ] 符合 MVP 架构原则（业务逻辑在 Service/Presenter 层）

### 测试验收
- [ ] `dotnet build` 编译成功，无警告
- [ ] 运行软件，各项功能正常工作
- [ ] 多次启动/关闭循环测试，数据持久化稳定

---

## 注意事项

1. **不要修改无关代码**：只修改与这 4 个需求相关的代码，不要重构其他部分（除非有明显的错误）

2. **异常关闭检测方案**：这是本任务的核心难点，你需要自行设计并实现合理的方案。建议考虑：
   - 临时文件 + ApplicationExit 事件
   - 定时自动保存
   - 标记文件方案
   - 或者其他你认为合适的方案

3. **用户体验**：
   - 异常恢复时，应该给用户明确的提示（弹窗或 Toast）
   - 让用户可以选择是否恢复上次的数据
   - 不要静默覆盖用户的数据

4. **线程安全**：
   - 如果使用定时器自动保存，注意线程安全
   - 文件 I/O 操作应该在后台线程，UI 更新在主线程

5. **日志记录**：
   - 关键操作都要记录日志（文件创建、加载、保存、异常等）
   - 日志使用 `LogHelper`，不要用 `Console.WriteLine`

6. **路径安全**：
   - 路径可能包含空格和中文字符，必须使用 `Path.Combine()`
   - 文件名使用标准日期格式，避免特殊字符

---

## 参考代码片段

### 获取应用数据路径
```csharp
string appDataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "大诚重命名工具",
    "SavedGrids"
);

// 确保目录存在
if (!Directory.Exists(appDataPath))
{
    Directory.CreateDirectory(appDataPath);
}
```

### 生成当日文件名
```csharp
string todayFileName = $"{DateTime.Now:yyyy-MM-dd}.json";
string todayFilePath = Path.Combine(appDataPath, todayFileName);
```

### 异步保存文件示例
```csharp
private async Task SaveToFileAsync(string filePath, FileTableData data)
{
    try
    {
        LogHelper.Info($"开始保存文件: {filePath}");

        string json = JsonSerializer.Serialize(data);
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);

        LogHelper.Info($"文件保存成功: {filePath}");
    }
    catch (Exception ex)
    {
        LogHelper.Error($"文件保存失败: {filePath}", ex);
        throw;
    }
}
```

### ApplicationExit 事件监听
```csharp
public FileRenamePanel()
{
    InitializeComponent();
    Application.ApplicationExit += OnApplicationExit;
    // 其他初始化...
}

private void OnApplicationExit(object sender, EventArgs e)
{
    // 保存当前数据
    SaveCurrentData();
}
```

---

## 提交说明

完成开发后，请提交以下内容：

1. **修改的文件**：`src/WindowsFormsApp3/Forms/FileRenamePanel.cs`（如果新增 Service，也需要提交）

2. **Git 提交信息**：
   ```
   feat: 增强 FileRenamePanel 的 JSON 文件自动管理功能

   - 修改 JSON 文件路径为 %AppData%\Roaming\大诚重命名工具\SavedGrids\
   - 每日首次启动自动创建当日 JSON 文件（YYYY-MM-DD.json）
   - 再次启动时自动加载当日 JSON 文件
   - 实现异常/手动关闭时的自动保存机制
   - 添加完整的日志记录和错误处理
   ```

3. **测试说明**：简要描述你测试了哪些场景

---

## 开始开发

请按照以上提示词开始实现功能。如有任何疑问，请先询问，不要自行假设需求。

**特别提醒**：
- 遵循项目的代码规范和架构原则
- 异常关闭检测机制需要你自行设计方案
- 所有注释使用简体中文
- 使用 `LogHelper` 记录日志
