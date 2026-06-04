# PDF检查器功能说明

## 概述

PDF检查器是一个类似于 **Enfocus PitStop Pro Inspector** 的功能模块，用于检查和显示PDF文件的页面框参数。该功能可以帮助印前工作人员快速了解PDF文件的页面框设置，发现潜在问题。

## 功能特性

### 1. 页面框参数显示

支持显示PDF标准定义的五种页面框：

- **MediaBox（媒体框）**：PDF页面的物理尺寸，定义了输出介质的边界
- **CropBox（裁剪框）**：页面显示和打印的区域，默认等于MediaBox
- **TrimBox（裁切框）**：成品的最终尺寸，印刷后的裁切线
- **BleedBox（出血框）**：包含出血的区域，通常比TrimBox大3-5mm
- **ArtBox（艺术框）**：有意义内容的区域

### 2. 多单位支持

支持三种测量单位的切换：

- **毫米 (mm)**：印刷行业常用单位
- **英寸 (in)**：北美印刷常用单位
- **点 (pt)**：PDF原生单位（1点 = 1/72英寸）

### 3. 页面框信息

对于每个页面框，显示以下信息：

- **定义状态**：是否已定义该页面框
- **尺寸**：宽度 × 高度
- **位置**：左下角坐标（PDF坐标系）

### 4. 出血检测

自动计算并显示出血尺寸：

- 四边出血值（上、下、左、右）
- 是否为统一出血
- 出血值是否符合印刷标准（通常3mm）

### 5. 问题检测

自动检测以下问题：

#### 错误级别
- MediaBox尺寸无效（0或负数）
- 页面框尺寸为0

#### 警告级别
- CropBox超出MediaBox范围
- TrimBox超出CropBox范围
- BleedBox小于TrimBox
- 页面框顺序不正确

#### 信息级别
- 文档包含不同尺寸的页面
- 文档包含不同方向的页面（横向/纵向混合）

### 6. 三个视图标签页

#### 当前页面
- 显示当前页面的详细页面框信息
- 包含页码、旋转角度
- 显示所有页面框的尺寸和位置
- 显示出血信息
- 显示该页面的问题提示

#### 所有页面
- 以表格形式显示所有页面的概览
- 包含页码、尺寸、旋转、状态
- 可点击跳转到指定页面
- 支持多选（预留功能）

#### 问题
- 列出所有检测到的问题
- 按严重程度分类（错误/警告/信息）
- 显示问题所在页码和描述
- 可点击跳转到问题页面
- 显示问题数量徽章

## 使用方法

### 1. 独立使用检查器控件

```csharp
using WindowsFormsApp3.Forms.Controls;

// 创建检查器控件
var inspector = new PdfInspectorControl();
inspector.Dock = DockStyle.Fill;

// 加载PDF文件
inspector.LoadPdf("path/to/file.pdf", currentPage: 1);

// 监听页面选择事件
inspector.PageSelected += (sender, pageNumber) =>
{
    Console.WriteLine($"用户选择了第 {pageNumber} 页");
};

// 切换到指定页面
inspector.SwitchToPage(5);
```

### 2. 使用完整的检查器面板

```csharp
using WindowsFormsApp3.Forms.Panels;

// 创建检查器面板（包含预览和检查器）
var panel = new PdfInspectorPanel();
panel.Dock = DockStyle.Fill;

// 面板会自动处理PDF加载和页面同步
```

### 3. 使用检查器服务

```csharp
using WindowsFormsApp3.Services;

// 创建服务实例
var service = new PdfInspectorService();

// 检查PDF文件
var info = service.InspectPdf("path/to/file.pdf", currentPage: 1);

// 访问检查结果
Console.WriteLine($"总页数: {info.TotalPages}");
Console.WriteLine($"问题数: {info.Issues.Count}");

// 获取当前页面的页面框信息
var currentPage = info.CurrentPageBoxes;
Console.WriteLine($"MediaBox: {currentPage.MediaBox.WidthMm} × {currentPage.MediaBox.HeightMm} mm");
Console.WriteLine($"TrimBox: {currentPage.TrimBox.WidthMm} × {currentPage.TrimBox.HeightMm} mm");

// 获取出血信息
var bleedInfo = service.GetBleedInfo(currentPage);
Console.WriteLine($"出血: {bleedInfo}");

// 遍历所有问题
foreach (var issue in info.Issues)
{
    Console.WriteLine($"[{issue.Severity}] 第{issue.PageNumber}页 {issue.BoxType}: {issue.Description}");
}
```

## 数据模型

### PdfInspectorInfo
主要的检查器信息容器，包含：
- 文件信息（路径、文件名）
- 页面信息（总页数、当前页）
- 当前页面的页面框信息
- 所有页面的页面框信息列表
- 问题列表

### PageBoxInfo
单个页面的页面框信息，包含：
- 页码
- 旋转角度
- 五种页面框的尺寸信息
- 问题标记和描述

### BoxDimension
页面框的尺寸信息，包含：
- 定义状态
- 坐标（左、下、右、上）
- 尺寸（宽、高）
- 多单位转换方法

### PageBoxIssue
页面框问题信息，包含：
- 页码
- 问题类型
- 严重程度
- 描述
- 涉及的页面框类型

## 与PitStop Pro的对比

| 功能 | PitStop Pro | 本实现 | 说明 |
|------|-------------|--------|------|
| 页面框显示 | ✓ | ✓ | 完全支持 |
| 多单位切换 | ✓ | ✓ | mm/in/pt |
| 出血检测 | ✓ | ✓ | 自动计算 |
| 问题检测 | ✓ | ✓ | 基础检测 |
| 页面框编辑 | ✓ | ✗ | 待实现 |
| 页面框可视化 | ✓ | ✗ | 待实现 |
| 批量修改 | ✓ | ✗ | 待实现 |
| 预检配置 | ✓ | ✗ | 待实现 |

## 技术实现

### 核心技术栈
- **iText 7**：PDF页面框读取和解析
- **AntdUI**：现代化UI组件
- **WinForms**：窗体框架

### 页面框读取逻辑
```csharp
// 使用iText 7读取页面框
PdfPage page = document.GetPage(pageNumber);
Rectangle mediaBox = page.GetMediaBox();
Rectangle cropBox = page.GetCropBox();
Rectangle trimBox = page.GetTrimBox();
Rectangle bleedBox = page.GetBleedBox();
Rectangle artBox = page.GetArtBox();

// 处理未定义的页面框（使用默认值）
cropBox = cropBox ?? mediaBox;
trimBox = trimBox ?? cropBox ?? mediaBox;
bleedBox = bleedBox ?? cropBox ?? mediaBox;
artBox = artBox ?? cropBox ?? mediaBox;
```

### 单位转换
```csharp
// PDF使用点（pt）作为原生单位
// 1 点 = 1/72 英寸
// 1 英寸 = 25.4 毫米

// 点转毫米
double mm = points / 72 * 25.4;

// 点转英寸
double inch = points / 72;
```

## 扩展功能（待实现）

### 1. 页面框可视化
在PDF预览上叠加显示不同颜色的页面框边界：
- MediaBox：红色
- CropBox：蓝色
- TrimBox：绿色
- BleedBox：黄色
- ArtBox：灰色

### 2. 页面框编辑
支持直接修改页面框参数：
- 输入新的尺寸值
- 拖拽调整页面框
- 批量应用到多个页面

### 3. 预检规则
自定义检查规则：
- 出血值范围检查
- 页面尺寸标准检查
- 页面框关系检查

### 4. 导出报告
生成检查报告：
- PDF格式报告
- Excel格式报告
- HTML格式报告

## 常见问题

### Q: 为什么有些页面框显示"未定义"？
A: PDF规范中，只有MediaBox是必须的，其他页面框都是可选的。如果未定义，PDF阅读器会使用默认值（通常是上一级页面框的值）。

### Q: CropBox和TrimBox有什么区别？
A: CropBox是PDF显示和打印的区域，TrimBox是印刷后的裁切线。在印刷流程中，TrimBox代表成品尺寸，CropBox通常包含TrimBox和出血区域。

### Q: 出血值应该设置多少？
A: 标准出血值通常是3mm，但根据印刷厂要求可能是2-5mm。本检查器会自动计算并显示实际的出血值。

### Q: 如何修复检测到的问题？
A: 当前版本只提供检测功能。修复功能需要使用页面框编辑功能（待实现），或使用其他PDF编辑工具如Adobe Acrobat或PitStop Pro。

## 参考资料

- [PDF Reference 1.7 - Page Boundaries](https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/PDF32000_2008.pdf)
- [Enfocus PitStop Pro Documentation](https://www.enfocus.com/manuals/)
- [iText 7 Documentation](https://itextpdf.com/en/resources/api-documentation)

## 更新日志

### v1.0.0 (2026-01-19)
- ✓ 实现页面框参数显示
- ✓ 支持多单位切换
- ✓ 实现出血检测
- ✓ 实现基础问题检测
- ✓ 创建三个视图标签页
- ✓ 集成PDF预览和检查器
