# PDF拆分与动作列表模块化设计文档

## 目录

1. [PDF拆分标签页功能汇总](#1-pdf拆分标签页功能汇总)
2. [布局计算与空白页添加](#2-布局计算与空白页添加)
3. [动作列表模块化设计](#3-动作列表模块化设计)
4. [使用指南](#4-使用指南)

---

## 1. PDF拆分标签页功能汇总

### 1.1 功能概述

PDF拆分标签页是一个根据Excel数据按页数范围拆分PDF文件的功能模块，属于印前工具箱的一部分。

### 1.2 核心文件结构

| 文件 | 说明 |
|------|------|
| `PdfSplitPanel.cs` | 面板主逻辑（UI交互、拖拽、预览、执行） |
| `PdfSplitPanel.Designer.cs` | 界面设计文件（控件布局） |
| `PdfSplitService.cs` | PDF拆分服务（核心算法、iText7实现） |
| `PdfSplitExcelHelper.cs` | Excel/CSV读取帮助类 |

### 1.3 界面布局

```
┌─────────────────────────────────────────┐
│  📊 Excel: [___________] [选择文件]      │  ← 顶部控制区
│  📄 PDF:   [___________] [选择文件] 共X页 │
├─────────────────────────────────────────┤
│  序号 │ 文件名 │ 页数 │ 起始页 │ 结束页  │  ← 预览表格区
│   1   │ A.pdf  │  2   │   1    │   2    │
│   2   │ B.pdf  │  3   │   3    │   5    │
├─────────────────────────────────────────┤
│  [执行拆分] [取消] [========进度条] 状态  │  ← 底部操作区
└─────────────────────────────────────────┘
```

### 1.4 核心功能

#### 1.4.1 文件选择
- **PDF文件选择**：支持 `.pdf` 格式，显示源PDF总页数
- **Excel/CSV文件选择**：支持 `.xlsx`、`.xls`、`.csv` 格式

#### 1.4.2 拖拽支持
- 支持将PDF文件直接拖拽到PDF路径输入框
- 支持将Excel/CSV文件直接拖拽到Excel路径输入框
- 双击Excel路径可打开文件

#### 1.4.3 Excel数据格式
支持读取以下列数据：
- **文件名**（第1列）：输出PDF的文件名
- **页数**（第2列）：该文件包含的页数
- **订单号**（第3列，可选）：订单编号
- **数量**（第4列，可选）：数量信息

#### 1.4.4 预览功能
- 自动计算每个输出文件的起始页和结束页
- 实时显示拆分预览表格
- 检查总页数是否超出源PDF范围并给出警告

#### 1.4.5 拆分执行
- 支持选择输出目录
- 显示实时进度条和状态信息
- 支持**取消操作**
- 自动避免文件名冲突（添加序号后缀）

### 1.5 核心算法

#### 页数范围计算
```
// 根据文件列表自动累计计算页数范围
// 文件A(2页) → 1-2
// 文件B(3页) → 3-5
// 文件C(1页) → 6-6
```

#### 页面复制模式
支持两种页面排列模式：
- **连续重复模式** (`111222333`)：每页重复N次后再复制下一页
- **交替循环模式** (`123123123`)：按顺序循环复制所有页面N次

### 1.6 技术实现

| 技术点 | 实现方式 |
|--------|----------|
| PDF处理 | iText7 (iText.Kernel.Pdf) |
| Excel读取 | EPPlus (OfficeOpenXml) |
| CSV解析 | 自定义解析器（支持引号） |
| UI框架 | AntdUI + Krypton.Toolkit |
| 异步操作 | async/await + CancellationToken |

### 1.7 使用流程

1. 选择/拖拽 **Excel文件**（包含文件名和页数）
2. 选择/拖拽 **PDF文件**（源文件）
3. 系统自动计算并显示 **拆分预览**
4. 点击 **执行拆分**，选择输出目录
5. 等待处理完成，查看输出文件

---

## 2. 布局计算与空白页添加

### 2.1 功能概述

在PDF拆分过程中，结合布局计算和空白页添加功能，实现印前拼版需求。

### 2.2 新增文件

| 文件路径 | 说明 |
|---------|------|
| `Services/PdfSplitLayoutService.cs` | 核心服务 - 结合PDF拆分、布局计算和空白页添加 |
| `Forms/Panels/PdfSplitPanelExtended.cs` | 扩展面板 - 带布局配置的UI界面 |

### 2.3 布局计算类型

| 类型 | 说明 | 使用场景 |
|------|------|----------|
| **None** (不计算) | 仅按Excel拆分，不添加空白页 | 普通拆分 |
| **FixedLayoutQuantity** (固定布局) | 用户指定每页容纳的页数 | 已知布局数量 |
| **BasedOnFlatSheet** (平张材料) | 基于纸张尺寸自动计算布局 | 平张印刷 |
| **BasedOnRollMaterial** (卷装材料) | 基于卷装宽度自动计算布局 | 数码印刷 |

### 2.4 布局模式

| 模式 | 说明 | 空白页处理 |
|------|------|-----------|
| **连拼模式** (Continuous) | 直接按布局数量拼接 | 不添加空白页 |
| **折手模式** (Folding) | 需要完整纸张 | 自动添加空白页补齐 |

### 2.5 空白页计算逻辑

```csharp
// 折手模式
if (layoutMode == LayoutMode.Folding)
{
    // 计算余数
    int remainder = pageCount % layoutQuantity;
    if (remainder != 0)
    {
        blankPagesNeeded = layoutQuantity - remainder;
    }
}

// 连拼模式 - 不需要空白页
```

### 2.6 扩展界面功能

```
┌──────────────────────────────────────────────────────────────┐
│  📊 Excel: [___________] [选择文件]                           │
│  📄 PDF:   [___________] [选择文件] 共X页                      │
├──────────────────────────────────────────────────────────────┤
│  布局计算: [不计算布局▼] 排版模式: [折手模式▼] 每页页数: [2]   │
│  自动补空白页: [●] [计算布局] 总页数: 10 | 空白页: 2 | 纸张数: 6│
├──────────────────────────────────────────────────────────────┤
│  序号 │ 文件名 │ 页数      │ 起始页 │ 结束页 │ 布局 │ 纸张数  │
│   1   │ A.pdf  │ 2 (+0空白)│   1    │   2    │  2   │   1    │
│   2   │ B.pdf  │ 3 (+1空白)│   3    │   5    │  2   │   2    │
├──────────────────────────────────────────────────────────────┤
│  [执行拆分] [取消] [========进度条] 状态                       │
└──────────────────────────────────────────────────────────────┘
```

### 2.7 使用流程

1. 选择 **Excel文件**（包含文件名和页数）
2. 选择 **PDF文件**（源文件）
3. 配置 **布局选项**：
   - 选择计算类型（固定布局/平张/卷装）
   - 选择排版模式（连拼/折手）
   - 设置每页页数（固定布局时）
   - 开启/关闭自动补空白页
4. 点击 **"计算布局"** 预览布局信息
5. 点击 **"执行拆分"** 完成拆分和空白页添加

---

## 3. 动作列表模块化设计

### 3.1 设计目标

参考 **PitStop Pro 的动作列表（Action List）**，将材料选择框的所有功能拆分为**细粒度的独立模块**，可以在任何窗口中通过模块组合的方式调用。

### 3.2 核心概念

#### 3.2.1 动作（Action）
每个动作是一个独立的功能模块，例如：
- `设置材料类型`
- `计算平张布局`
- `添加空白页`

#### 3.2.2 动作列表（Action List）
多个动作按顺序组合成一个动作列表，形成完整的处理流程。

#### 3.2.3 上下文（Context）
动作之间通过上下文传递数据，前一个动作的输出可以作为后一个动作的输入。

### 3.3 文件结构

```
src/WindowsFormsApp3/Actions/
├── IPrepressAction.cs           # 动作接口定义
├── ActionListManager.cs         # 动作列表管理器（核心引擎）
├── BasicSettingsActions.cs      # 基础设置模块
├── LayoutActions.cs             # 排版布局模块
├── CalculationActions.cs        # 布局计算模块
└── BlankPageActions.cs          # 空白页处理模块
```

### 3.4 动作模块清单

#### 基础设置类

| 动作ID | 显示名称 | 说明 |
|--------|----------|------|
| `set_material_type` | 设置材料类型 | 设置PET/PP/PVC等材料 |
| `set_process_params` | 设置工艺参数 | 设置颜色、覆膜等 |
| `set_dimensions` | 设置尺寸 | 设置成品宽度和高度 |
| `set_shape` | 设置形状 | 设置直角/圆角/圆形等 |

#### 排版布局类

| 动作ID | 显示名称 | 说明 |
|--------|----------|------|
| `set_material_spec` | 设置材料规格 | 设置平张/卷装尺寸 |
| `set_margins` | 设置边距 | 设置上下左右边距 |
| `set_rows_columns` | 设置行列 | 设置固定行列数 |
| `set_copy_count` | 设置联数 | 设置一式几联 |

#### 布局计算类

| 动作ID | 显示名称 | 说明 |
|--------|----------|------|
| `calculate_flat_sheet` | 计算平张布局 | 基于平张材料计算布局 |
| `calculate_roll_material` | 计算卷装布局 | 基于卷装材料计算布局 |
| `apply_rotation` | 应用旋转 | 旋转PDF页面 |

#### 空白页处理类

| 动作ID | 显示名称 | 说明 |
|--------|----------|------|
| `calculate_blank_pages` | 计算空白页 | 计算需要添加的空白页 |
| `add_blank_pages` | 添加空白页 | 向PDF添加空白页 |

### 3.5 架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    印前处理动作列表 (Action List)              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  【基础设置模块】                                            │
│  ├─ 设置材料类型    → SetMaterialTypeAction                 │
│  ├─ 设置工艺参数    → SetProcessParamsAction                │
│  ├─ 设置尺寸        → SetDimensionsAction                   │
│  └─ 设置形状        → SetShapeAction                        │
│                                                             │
│  【排版布局模块】                                            │
│  ├─ 设置材料规格    → SetMaterialSpecAction                 │
│  ├─ 设置边距        → SetMarginsAction                      │
│  ├─ 设置行列        → SetRowsColumnsAction                  │
│  └─ 设置联数        → SetCopyCountAction                    │
│                                                             │
│  【布局计算模块】                                            │
│  ├─ 计算平张布局    → CalculateFlatSheetAction              │
│  ├─ 计算卷装布局    → CalculateRollMaterialAction           │
│  └─ 应用旋转        → ApplyRotationAction                   │
│                                                             │
│  【空白页处理模块】                                          │
│  ├─ 计算空白页      → CalculateBlankPagesAction             │
│  └─ 添加空白页      → AddBlankPagesAction                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 3.6 数据流示意图

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  set_material   │────▶│  set_margins    │────▶│ calculate_flat  │
│     _spec       │     │                 │     │    _sheet       │
└─────────────────┘     └─────────────────┘     └────────┬────────┘
                                                         │
                              ┌──────────────────────────┘
                              ▼
                    ┌─────────────────┐     ┌─────────────────┐
                    │ calculate_blank │────▶│  add_blank      │
                    │    _pages       │     │    _pages       │
                    └─────────────────┘     └─────────────────┘
                              │                      │
                              ▼                      ▼
                    ┌─────────────────────────────────────┐
                    │           ActionContext             │
                    │  • LayoutQuantity = 6               │
                    │  • BlankPagesNeeded = 2             │
                    │  • RequiredSheets = 3               │
                    └─────────────────────────────────────┘
```

### 3.7 上下文数据传递

| 数据键 | 类型 | 说明 | 设置者 | 使用者 |
|--------|------|------|--------|--------|
| `MaterialType` | MaterialType | 材料类型 | set_material_spec | calculate_flat_sheet |
| `MaterialWidth` | float | 材料宽度 | set_material_spec | calculate_flat_sheet |
| `MaterialHeight` | float | 材料高度 | set_material_spec | calculate_flat_sheet |
| `MarginTop` | float | 上边距 | set_margins | calculate_flat_sheet |
| `MarginBottom` | float | 下边距 | set_margins | calculate_flat_sheet |
| `MarginLeft` | float | 左边距 | set_margins | calculate_flat_sheet |
| `MarginRight` | float | 右边距 | set_margins | calculate_flat_sheet |
| `Rows` | int | 行数 | set_rows_columns / calculate_flat_sheet | - |
| `Columns` | int | 列数 | set_rows_columns / calculate_flat_sheet | - |
| `CopyCount` | int | 联数 | set_copy_count | calculate_flat_sheet |
| `LayoutQuantity` | int | 布局数量 | calculate_flat_sheet | calculate_blank_pages |
| `BlankPagesNeeded` | int | 空白页数量 | calculate_blank_pages | add_blank_pages |
| `TotalPageCount` | int | 总页数 | calculate_blank_pages | - |
| `RequiredSheets` | int | 所需纸张数 | calculate_blank_pages | - |

---

## 4. 使用指南

### 4.1 基本使用

```csharp
// 创建动作列表管理器
var actionManager = new ActionListManager();

// 创建动作列表（平张材料+空白页计算）
// 1. 设置材料规格
var setMaterialSpec = actionManager.CreateAction("set_material_spec");
((Layout.SetMaterialSpecAction)setMaterialSpec).MaterialType = MaterialType.FlatSheet;
((Layout.SetMaterialSpecAction)setMaterialSpec).Width = 450f;
((Layout.SetMaterialSpecAction)setMaterialSpec).Height = 320f;
actionManager.AddAction(setMaterialSpec);

// 2. 设置边距
var setMargins = actionManager.CreateAction("set_margins");
((Layout.SetMarginsAction)setMargins).UniformMargin = 10f;
actionManager.AddAction(setMargins);

// 3. 设置联数
var setCopyCount = actionManager.CreateAction("set_copy_count");
((Layout.SetCopyCountAction)setCopyCount).CopyCount = 2;
actionManager.AddAction(setCopyCount);

// 4. 计算平张布局
var calculateLayout = actionManager.CreateAction("calculate_flat_sheet");
actionManager.AddAction(calculateLayout);

// 5. 计算空白页
var calculateBlank = actionManager.CreateAction("calculate_blank_pages");
((BlankPage.CalculateBlankPagesAction)calculateBlank).LayoutMode = LayoutMode.Folding;
actionManager.AddAction(calculateBlank);

// 执行动作列表
var context = new ActionContext
{
    InputFilePath = "C:\\input.pdf",
    OutputDirectory = "C:\\output"
};

var result = await actionManager.ExecuteAllAsync(context);

// 获取计算结果
int layoutQuantity = context.GetData<int>("LayoutQuantity");
int blankPagesNeeded = context.GetData<int>("BlankPagesNeeded");
int requiredSheets = context.GetData<int>("RequiredSheets");

Console.WriteLine($"布局数量: {layoutQuantity}页/纸");
Console.WriteLine($"空白页: {blankPagesNeeded}页");
Console.WriteLine($"所需纸张: {requiredSheets}张");
```

### 4.2 批量处理任务

```csharp
// 创建一个可复用的动作列表配置
void CreateBatchProcessingActionList(ActionListManager manager)
{
    // 清空现有动作
    manager.Clear();
    
    // 添加标准处理流程
    manager.AddAction(manager.CreateAction("set_material_spec"));
    manager.AddAction(manager.CreateAction("set_margins"));
    manager.AddAction(manager.CreateAction("calculate_flat_sheet"));
    manager.AddAction(manager.CreateAction("calculate_blank_pages"));
    manager.AddAction(manager.CreateAction("add_blank_pages"));
}

// 应用到多个文件
var pdfFiles = Directory.GetFiles("C:\\input", "*.pdf");
foreach (var pdfFile in pdfFiles)
{
    var manager = new ActionListManager();
    CreateBatchProcessingActionList(manager);
    
    var context = new ActionContext
    {
        InputFilePath = pdfFile,
        OutputDirectory = "C:\\output"
    };
    
    var result = await manager.ExecuteAllAsync(context);
    
    if (result.Success)
    {
        Console.WriteLine($"处理成功: {Path.GetFileName(pdfFile)}");
    }
}
```

### 4.3 保存和加载配置

```csharp
// 保存配置到文件
var manager = new ActionListManager();

// 添加动作...
manager.AddAction(manager.CreateAction("set_material_spec"));
manager.AddAction(manager.CreateAction("set_margins"));
manager.AddAction(manager.CreateAction("calculate_flat_sheet"));

// 保存到JSON文件
manager.SaveToFile("C:\\configs\\standard_layout.json");

// 在其他地方加载配置
var newManager = new ActionListManager();
newManager.LoadFromFile("C:\\configs\\standard_layout.json");

// 执行
var result = await newManager.ExecuteAllAsync(context);
```

### 4.4 扩展自定义动作

```csharp
public class MyCustomAction : IPrepressAction
{
    public string ActionId => "my_custom_action";
    public string DisplayName => "我的自定义动作";
    public string Description => "这是一个示例自定义动作";
    public ActionCategory Category => ActionCategory.Other;
    public string IconName => "StarOutlined";
    
    // 参数
    public string MyParameter { get; set; } = "默认值";
    
    public async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken = default)
    {
        // 实现您的逻辑
        context.SetData("MyResult", "处理结果");
        return ActionResult.CreateSuccess("执行成功");
    }
    
    public ValidationResult Validate()
    {
        if (string.IsNullOrEmpty(MyParameter))
            return ValidationResult.CreateInvalid("参数不能为空");
        return ValidationResult.CreateValid();
    }
    
    public List<ActionParameterDefinition> GetParameterDefinitions()
    {
        return new List<ActionParameterDefinition>
        {
            new ActionParameterDefinition
            {
                Name = nameof(MyParameter),
                DisplayName = "我的参数",
                Type = ParameterType.String,
                IsRequired = true,
                DefaultValue = "默认值"
            }
        };
    }
    
    public IPrepressAction Clone()
    {
        return new MyCustomAction { MyParameter = this.MyParameter };
    }
}

// 注册自定义动作
var manager = new ActionListManager();
manager.Register<MyCustomAction>();
```

---

## 5. 核心特性总结

| 特性 | 说明 |
|------|------|
| **细粒度** | 每个功能点都是独立模块，可单独调用 |
| **可组合** | 像搭积木一样自由组合动作 |
| **数据传递** | 通过上下文自动传递数据 |
| **可序列化** | 配置可保存为JSON文件 |
| **可扩展** | 轻松添加新的动作模块 |
| **参数验证** | 每个动作都有参数验证 |
| **进度报告** | 支持执行进度回调 |

---

## 6. 下一步建议

1. **创建UI编辑器** - 可视化拖拽编辑动作列表
2. **添加更多动作** - 角线、套准标记、标识页等
3. **条件判断** - 支持if/else条件分支
4. **循环处理** - 支持批量文件循环处理
5. **预设模板** - 提供常用配置模板

---

## 7. Agent集成方案

### 7.1 Agent可完成的工作

#### 布局计算类任务

| 任务 | 说明 | 涉及动作 |
|------|------|----------|
| 平张排版计算 | 根据纸张尺寸自动计算最优布局 | `set_material_spec` → `calculate_flat_sheet` |
| 卷装排版计算 | 根据卷装宽度计算布局 | `set_material_spec` → `calculate_roll_material` |
| 一式N联排版 | 计算多联排版方案 | `set_copy_count` → `calculate_flat_sheet` |

#### 空白页处理任务

| 任务 | 说明 | 涉及动作 |
|------|------|----------|
| 空白页计算 | 计算需要添加的空白页数量 | `calculate_blank_pages` |
| 空白页添加 | 向PDF添加空白页 | `add_blank_pages` |

#### PDF处理任务

| 任务 | 说明 | 涉及动作 |
|------|------|----------|
| 页面旋转 | 旋转PDF页面 | `apply_rotation` |
| PDF拆分+布局 | 拆分PDF并应用布局计算 | `PdfSplitLayoutService` |

### 7.2 Agent架构

```
┌─────────────────────────────────────────────────────────────┐
│                    LLM Agent 架构                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────┐                                            │
│  │   用户输入   │  "帮我把这个PDF按A4纸排版，一式2联"        │
│  └──────┬──────┘                                            │
│         │                                                   │
│         ▼                                                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              LLM (大语言模型)                        │   │
│  │  • 理解用户意图                                       │   │
│  │  • 匹配可用动作                                       │   │
│  │  • 生成JSON配置                                       │   │
│  └──────────────────────┬──────────────────────────────┘   │
│                         │                                   │
│                         ▼                                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │           AgentActionAdapter                        │   │
│  │  • 解析JSON配置                                       │   │
│  │  • 执行动作列表                                       │   │
│  │  • 返回结果                                          │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 7.3 Agent使用示例

```csharp
// 创建Agent
var llmClient = new OpenAIClient("your-api-key");
var agent = new PrepressAgent(llmClient);

// 方式1：自然语言处理
var response = await agent.ProcessAsync(
    "帮我把这个PDF按A4纸排版，一式2联，自动补空白页",
    "C:\\input\\test.pdf"
);

Console.WriteLine(response.Message);

// 方式2：快捷方法
var result = await agent.QuickFlatSheetLayoutAsync(
    "C:\\input\\test.pdf",
    width: 210,
    height: 297,
    copyCount: 2,
    addBlankPages: true
);

Console.WriteLine($"布局数量: {result.OutputData["LayoutQuantity"]}");
Console.WriteLine($"空白页: {result.OutputData["BlankPagesNeeded"]}");
```

### 7.4 支持的LLM后端

| 后端 | 说明 | 实现类 |
|------|------|--------|
| OpenAI | GPT-4/GPT-3.5 | `OpenAIClient` |
| Azure OpenAI | 企业级部署 | `AzureOpenAIClient` |
| Ollama | 本地部署 | `OllamaClient` |
| 自定义 | 自行实现 `ILLMClient` 接口 | - |

### 7.5 Agent工作流程

```
用户输入 → LLM解析 → 生成JSON配置 → 执行动作列表 → 返回结果
    │           │           │              │           │
    │           │           │              │           │
    ▼           ▼           ▼              ▼           ▼
"按A4排版"  理解意图   {actions:[...]}  计算布局   "布局数量:6"
```

### 7.6 Agent相关文件

| 文件 | 说明 |
|------|------|
| `Actions/PrepressAgent.cs` | Agent核心实现 |
| `Actions/AgentActionAdapter.cs` | Agent适配器 |
| `Actions/IPrepressAction.cs` | 动作接口定义 |
| `Actions/ActionListManager.cs` | 动作列表管理器 |

---

## 附录：相关文件路径

| 文件类型 | 路径 |
|----------|------|
| PDF拆分服务 | `src/WindowsFormsApp3/Services/PdfSplitService.cs` |
| PDF拆分布局服务 | `src/WindowsFormsApp3/Services/PdfSplitLayoutService.cs` |
| 动作接口 | `src/WindowsFormsApp3/Actions/IPrepressAction.cs` |
| 动作列表管理器 | `src/WindowsFormsApp3/Actions/ActionListManager.cs` |
| 基础设置动作 | `src/WindowsFormsApp3/Actions/BasicSettingsActions.cs` |
| 布局动作 | `src/WindowsFormsApp3/Actions/LayoutActions.cs` |
| 计算动作 | `src/WindowsFormsApp3/Actions/CalculationActions.cs` |
| 空白页动作 | `src/WindowsFormsApp3/Actions/BlankPageActions.cs` |
| 使用指南 | `docs/ActionList使用指南.md` |
