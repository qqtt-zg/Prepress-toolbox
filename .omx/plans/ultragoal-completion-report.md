# Ultragoal 交付与验证报告：材料选择框一体化工作台 V5 终版重构

## 1. 目标与实现概述
按照 `docs/材料选择框终版一体化工作台.html` 的定稿规范，全面完成了 `MaterialSelectFormModern`（材料选择框）的外壳尺寸系统与右侧流水线工位的白底细线框卡片化重构。

## 2. 核心落地项
1. **尺寸契约体系固化**：
   - 常态折叠客户区：360×508px（单工位）/ 880×508px（双联体）；
   - 预览展开客户区：360×753px（单工位）/ 880×753px（双联体），向下展开增加 245px；
   - 折叠条（16px）与预览面板（245px）严丝合缝贴合底边，零多余空白；
2. **7 大白底细线框卡片架构**（`MaterialSelectFormModern.V5Workbench.cs`）：
   - 卡片 1 (`pnlMaterialsCard`)：纯净 4×4 材料网格，按钮 26px 高，3px 微圆角，去除冗余标题与清除文本；
   - 卡片 2 (`pnlOutputPathCard`)：独立全宽输出路径栏（25px 高），只读 Consolas 路径显示 + 目录树切换；
   - 卡片 3 (`pnlCraftMatrixCard`)：工艺开关矩阵（55px 高），覆膜分段 + 模切形状分段 + 复合圆角原地 R 输入；
   - 卡片 4 (`tlpOrderParametersCard`)：2×2 工单参数舱（58px 高），订单/序号/数量/一式N联均等分配；
   - 卡片 5 (`pnlImpositionDashboardCard`)：纯净 6 宫格排版看板（76px 高），尺寸纯白无边框只读，由文件自动识别解析，不可修改；
   - 卡片 6 (`presetBrowserCard`)：快捷预设方案卡片（55px 高），StyleLine 标签页 + 3px 微圆角白底按钮 + 完整右键管理菜单；
   - 卡片 7 (`pnlActionBar`)：底部沉底操作栏（28px 高），左侧范围切换，右侧取消与主确认。
3. **遗留旧控件隔离清理**：
   - 实现了 `HideLegacyControls()`，安全隐藏设计器历史遗留的零散占位控件。

## 3. 验证证据
- **Debug 构建**：0 错误、0 新警告，耗时 2.5 秒；
- **自动化测试**：
  - `MaterialSelectFormSizeContractTests`：5 个测试全部通过；
  - `MaterialSelectFormV5CardsTests`：7 大卡片结构与容器宽度断言全部通过；
  - `MaterialSelectFormThemeTests`：4 个主题测试全部通过；
  - `MaterialSelectionPresetTests`：2 个预设测试全部通过；
  - `BatchFileItemSortServiceTests`：5 个排序服务测试全部通过。
