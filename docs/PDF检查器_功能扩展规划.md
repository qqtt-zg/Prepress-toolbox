# PDF检查器功能扩展规划

## 概述

基于Enfocus PitStop Pro Inspector的功能，规划下一步要实现的功能模块。

**规划日期**: 2026-01-19  
**当前版本**: v1.2.0  
**目标版本**: v2.0.0

---

## 已实现功能 (v1.2.0)

✅ **页面框参数显示**
- MediaBox, CropBox, TrimBox, BleedBox, ArtBox
- 多单位支持 (mm/in/pt)
- 尺寸和位置信息

✅ **问题检测**
- 页面框尺寸检查
- 页面框关系检查
- 页面一致性检查

✅ **独立窗口模式**
- 浮动窗口
- 位置记忆
- 双向同步

✅ **三个视图标签页**
- 当前页面
- 所有页面
- 问题列表

---

## 待实现功能（按优先级）

### 🎯 优先级1: 对象信息检查器

#### 功能描述
类似PitStop Pro的Inspector面板，可以选择PDF中的任意对象（文本、图像、矢量图形），查看其详细属性。

#### 核心功能
1. **对象选择**
   - 在PDF预览中点击选择对象
   - 显示对象边界框
   - 支持多选

2. **文本对象信息**
   - 字体名称和类型
   - 字体大小
   - 字体嵌入状态
   - 文本内容
   - 颜色信息
   - 位置和尺寸

3. **图像对象信息**
   - 图像类型 (JPEG, PNG, TIFF等)
   - 分辨率 (DPI)
   - 色彩空间 (RGB, CMYK, Gray等)
   - 尺寸 (像素和物理尺寸)
   - 压缩方式
   - 透明度信息

4. **矢量图形信息**
   - 路径类型
   - 填充颜色
   - 描边颜色和宽度
   - 位置和尺寸
   - 透明度

5. **颜色信息**
   - 色彩空间
   - 颜色值
   - 专色/印刷色
   - 叠印设置

#### 技术实现
```csharp
// 使用iText 7解析PDF内容
PdfPage page = document.GetPage(pageNumber);
PdfDictionary resources = page.GetResources();

// 解析文本对象
// 解析图像对象
// 解析图形对象
```

#### 优先级理由
- 这是PitStop Pro Inspector的核心功能
- 对印前检查非常重要
- 可以发现字体、图像、颜色等问题

---

### 🎯 优先级2: 页面框可视化叠加

#### 功能描述
在PDF预览上叠加显示不同颜色的页面框边界，直观显示各个页面框的位置关系。

#### 核心功能
1. **页面框叠加显示**
   - MediaBox: 红色边框
   - CropBox: 蓝色边框
   - TrimBox: 绿色边框
   - BleedBox: 黄色边框
   - ArtBox: 灰色边框

2. **可视化控制**
   - 显示/隐藏各个页面框
   - 调整边框透明度
   - 调整边框粗细
   - 显示页面框标签

3. **交互功能**
   - 鼠标悬停显示页面框信息
   - 点击页面框高亮显示
   - 测量工具

#### 技术实现
```csharp
// 已有基础: PdfBoxOverlay.cs
// 需要增强:
// 1. 添加显示/隐藏控制
// 2. 添加交互功能
// 3. 优化渲染性能
```

#### 优先级理由
- 已有基础代码 (PdfBoxOverlay.cs)
- 可视化效果直观
- 帮助理解页面框关系

---

### 🎯 优先级3: 字体信息检查

#### 功能描述
检查PDF中使用的所有字体，显示字体详细信息和潜在问题。

#### 核心功能
1. **字体列表**
   - 字体名称
   - 字体类型 (TrueType, Type1, OpenType等)
   - 嵌入状态 (完全嵌入/子集嵌入/未嵌入)
   - 使用页面

2. **字体问题检测**
   - 未嵌入字体
   - 子集嵌入字体
   - 缺失字体
   - 字体编码问题

3. **字体详细信息**
   - 字体文件信息
   - 字符集
   - 字形数量
   - 许可信息

#### 数据模型
```csharp
public class FontInfo
{
    public string FontName { get; set; }
    public string FontType { get; set; }
    public FontEmbeddingStatus EmbeddingStatus { get; set; }
    public List<int> UsedPages { get; set; }
    public bool HasIssues { get; set; }
    public List<string> Issues { get; set; }
}

public enum FontEmbeddingStatus
{
    FullyEmbedded,
    SubsetEmbedded,
    NotEmbedded
}
```

---

### 🎯 优先级4: 图像信息检查

#### 功能描述
检查PDF中的所有图像，显示图像详细信息和质量问题。

#### 核心功能
1. **图像列表**
   - 图像缩略图
   - 图像类型
   - 分辨率
   - 色彩空间
   - 文件大小
   - 使用页面

2. **图像问题检测**
   - 低分辨率图像 (<300 DPI)
   - 高分辨率图像 (>600 DPI, 浪费)
   - RGB图像 (应该是CMYK)
   - 透明度问题
   - 压缩问题

3. **图像详细信息**
   - 原始尺寸
   - 显示尺寸
   - 缩放比例
   - 压缩方式
   - 色彩配置文件

#### 数据模型
```csharp
public class ImageInfo
{
    public byte[] Thumbnail { get; set; }
    public string ImageType { get; set; }
    public int WidthPixels { get; set; }
    public int HeightPixels { get; set; }
    public double WidthMm { get; set; }
    public double HeightMm { get; set; }
    public int DpiX { get; set; }
    public int DpiY { get; set; }
    public string ColorSpace { get; set; }
    public long FileSize { get; set; }
    public List<int> UsedPages { get; set; }
    public bool HasIssues { get; set; }
    public List<string> Issues { get; set; }
}
```

---

### 🎯 优先级5: 颜色信息检查

#### 功能描述
检查PDF中使用的颜色，显示色彩空间和专色信息。

#### 核心功能
1. **颜色列表**
   - 色彩空间 (RGB, CMYK, Gray, Spot)
   - 颜色值
   - 使用次数
   - 使用页面

2. **颜色问题检测**
   - RGB颜色 (印刷应该用CMYK)
   - 专色使用
   - 颜色配置文件缺失
   - 叠印设置问题

3. **专色信息**
   - 专色名称
   - 专色类型
   - 替代颜色
   - 使用位置

---

### 🎯 优先级6: 页面框编辑功能

#### 功能描述
允许用户直接修改页面框参数。

#### 核心功能
1. **数值编辑**
   - 输入新的尺寸值
   - 输入新的位置值
   - 单位转换

2. **可视化编辑**
   - 拖拽调整页面框
   - 等比缩放
   - 对齐工具

3. **批量操作**
   - 应用到所有页面
   - 应用到选定页面
   - 应用到相似页面

4. **预设模板**
   - 常用尺寸预设
   - 出血预设
   - 自定义预设

---

## 实现计划

### 第一阶段 (v1.3.0) - 2周
- ✅ 页面框可视化叠加增强
- ✅ 字体信息检查基础功能
- ✅ 新增"字体"标签页

### 第二阶段 (v1.4.0) - 2周
- ✅ 图像信息检查基础功能
- ✅ 新增"图像"标签页
- ✅ 图像缩略图显示

### 第三阶段 (v1.5.0) - 2周
- ✅ 颜色信息检查基础功能
- ✅ 新增"颜色"标签页
- ✅ 颜色样本显示

### 第四阶段 (v1.6.0) - 3周
- ✅ 对象信息检查器
- ✅ 对象选择和高亮
- ✅ 对象详细信息面板

### 第五阶段 (v2.0.0) - 3周
- ✅ 页面框编辑功能
- ✅ 批量操作
- ✅ 预设模板
- ✅ 完整测试和文档

---

## 技术挑战

### 1. PDF内容解析
**挑战**: iText 7的内容解析API比较底层  
**解决**: 使用LocationTextExtractionStrategy和ImageRenderListener

### 2. 对象选择
**挑战**: 在PDF预览中精确选择对象  
**解决**: 坐标映射和碰撞检测

### 3. 性能优化
**挑战**: 大型PDF文件的解析性能  
**解决**: 异步加载、缓存、分页处理

### 4. UI响应性
**挑战**: 复杂操作时保持UI响应  
**解决**: 后台线程、进度提示

---

## 参考资料

- [iText 7 Content Parsing](https://itextpdf.com/en/resources/books/itext-7-building-blocks/chapter-7-creating-annotations-and-fields)
- [PDF Reference - Content Streams](https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/PDF32000_2008.pdf)
- [Enfocus PitStop Pro Features](https://www.enfocus.com/en/pitstop-pro)

---

**规划日期**: 2026-01-19  
**预计完成**: 2026-03-31  
**总工作量**: 约12周
