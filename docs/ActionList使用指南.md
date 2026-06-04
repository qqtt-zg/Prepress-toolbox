# 印前处理动作列表使用指南

## 概述

本系统采用**PitStop Pro风格的动作列表（Action List）**设计，将材料选择框的所有功能拆分为独立的、可组合的功能模块。您可以在任何窗口中通过模块组合的方式调用这些功能。

---

## 核心概念

### 1. 动作（Action）
每个动作是一个独立的功能模块，例如：
- `设置材料类型`
- `计算平张布局`
- `添加空白页`

### 2. 动作列表（Action List）
多个动作按顺序组合成一个动作列表，形成完整的处理流程。

### 3. 上下文（Context）
动作之间通过上下文传递数据，前一个动作的输出可以作为后一个动作的输入。

---

## 动作分类

### 基础设置类
| 动作ID | 显示名称 | 说明 |
|--------|----------|------|
| `set_material_type` | 设置材料类型 | 设置PET/PP/PVC等材料 |
| `set_process_params` | 设置工艺参数 | 设置颜色、覆膜等 |
| `set_dimensions` | 设置尺寸 | 设置成品宽度和高度 |
| `set_shape` | 设置形状 | 设置直角/圆角/圆形等 |

### 排版布局类
| 动作ID | 显示名称 | 说明 |
|--------|----------|------|
| `set_material_spec` | 设置材料规格 | 设置平张/卷装尺寸 |
| `set_margins` | 设置边距 | 设置上下左右边距 |
| `set_rows_columns` | 设置行列 | 设置固定行列数 |
| `set_copy_count` | 设置联数 | 设置一式几联 |

### 布局计算类
| 动作ID | 显示名称 | 说明 |
|--------|----------|------|
| `calculate_flat_sheet` | 计算平张布局 | 基于平张材料计算布局 |
| `calculate_roll_material` | 计算卷装布局 | 基于卷装材料计算布局 |
| `apply_rotation` | 应用旋转 | 旋转PDF页面 |

### 空白页处理类
| 动作ID | 显示名称 | 说明 |
|--------|----------|------|
| `calculate_blank_pages` | 计算空白页 | 计算需要添加的空白页 |
| `add_blank_pages` | 添加空白页 | 向PDF添加空白页 |

---

## 使用示例

### 示例1：在PDF拆分流程中使用

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

### 示例2：批量处理任务

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

### 示例3：保存和加载动作列表配置

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

---

## 在UI中使用

### 动作列表编辑器

```csharp
// 创建动作列表编辑器窗口
public class ActionListEditor : Form
{
    private ActionListManager _manager;
    private ListBox _actionListBox;
    
    public ActionListEditor()
    {
        _manager = new ActionListManager();
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        // 左侧：可用动作列表
        var availableActions = new ListBox();
        foreach (var actionType in _manager.GetRegisteredActionTypes())
        {
            availableActions.Items.Add($"[{actionType.Category}] {actionType.DisplayName}");
        }
        
        // 右侧：当前动作列表
        _actionListBox = new ListBox();
        
        // 按钮：添加、删除、上移、下移
        var btnAdd = new Button { Text = "添加 →" };
        btnAdd.Click += (s, e) => {
            var selectedAction = availableActions.SelectedItem?.ToString();
            if (selectedAction != null)
            {
                // 解析动作ID并添加
                var actionId = ParseActionId(selectedAction);
                var action = _manager.CreateAction(actionId);
                _manager.AddAction(action);
                RefreshActionList();
            }
        };
        
        // ... 其他按钮
    }
    
    private void RefreshActionList()
    {
        _actionListBox.Items.Clear();
        foreach (var action in _manager.GetActions())
        {
            _actionListBox.Items.Add(action.DisplayName);
        }
    }
}
```

---

## 动作参数说明

### 设置材料规格 (set_material_spec)

```csharp
var action = new Layout.SetMaterialSpecAction
{
    MaterialType = MaterialType.FlatSheet,  // 平张/卷装
    Width = 450f,                           // 宽度(mm)
    Height = 320f,                          // 高度(mm，仅平张)
    MinLength = 297f                        // 最小长度(mm，仅卷装)
};
```

### 设置边距 (set_margins)

```csharp
var action = new Layout.SetMarginsAction
{
    MarginTop = 10f,
    MarginBottom = 10f,
    MarginLeft = 10f,
    MarginRight = 10f
    // 或使用 UniformMargin = 10f 统一设置
};
```

### 计算空白页 (calculate_blank_pages)

```csharp
var action = new BlankPage.CalculateBlankPagesAction
{
    LayoutMode = LayoutMode.Folding,  // 折手模式需要空白页
    PageCount = 0,                    // 0表示自动获取
    LayoutQuantity = 0                // 0表示从上下文获取
};
```

### 添加空白页 (add_blank_pages)

```csharp
var action = new BlankPage.AddBlankPagesAction
{
    BlankPageCount = 0,      // 0表示从上下文获取计算结果
    InsertPosition = -1      // -1=末尾，0=开头
};
```

---

## 上下文数据传递

动作之间通过上下文传递数据：

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

## 扩展自定义动作

您可以创建自己的动作模块：

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

## 总结

这种模块化的设计让您可以：

1. **灵活组合** - 根据需求自由组合不同的功能模块
2. **复用配置** - 保存常用的动作列表配置，重复使用
3. **独立调用** - 在任何窗口中单独调用某个功能模块
4. **扩展功能** - 轻松添加新的功能模块
5. **可视化编辑** - 通过UI拖拽方式编辑动作列表

就像PitStop Pro的动作列表一样，您可以创建复杂的印前处理流程，并将其应用到不同的文件和任务中。
