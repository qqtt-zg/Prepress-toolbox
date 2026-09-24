# 架构与落地计划：材料选择框一体化工作台 V5 终版重构

## 一、 系统架构决策 (ADR)
1. **容器分层架构**：
   - 双联体由左右两大顶层容器构成：左侧 `pnlFileList`（520px，可折叠至 0px）与右侧工位 `pnlRightStation`（360px，固定宽）；
   - 右侧工位内部垂直自上而下严格按白底细线框卡片组织，各卡片高度与边距精准满足 `BASE_CLIENT_HEIGHT = 508px`；
2. **预览展开机制**：
   - 预览展开方向为窗体高度向下延展 `+245px`（达 `753px`）；
   - 折叠条（16px）固定在 `Top = 492px`，底边对齐 508px；
   - 预览面板位于 `Top = 508px`，展开高度 245px，底边对齐 753px，绝不挤占上方控件；
3. **UI 库标准**：
   - 全面使用 AntdUI (v2.1.12) 控件：按钮统一 3px 微圆角，预设分组采用 StyleLine 标签页；
   - 尺寸只读不可修改，纯白无边框。

## 二、 任务拆解与执行序列 (Tasks & Sequencing)
- **Task 1: 阶段 1 窗体尺寸系统固化（已就绪并通过测试）**
  - 确认 `MaterialSelectFormModern.cs`、`Designer.cs` 与 `MaterialSelectFormSizeContractTests.cs` 契约生效。
- **Task 2: 常用材料卡片化与 4×4 网格重构 (pnlMaterialsCard)**
  - 封装为 `pnlMaterialsCard`，白底细框，移除标题与清除文本，按钮增高至 26px，3px 微圆角。
- **Task 3: 独立全宽输出路径卡片重构 (pnlOutputPathCard)**
  - 白底细框，只读 Input 路径展示 + 切换目录按钮 + TreeView 目录树浮层。
- **Task 4: 工艺开关矩阵卡片重构 (pnlCraftMatrixCard)**
  - 覆膜分段栏 (AntdUI.Segmented) + 模切形状分段栏 + 复合圆角原地 R 半径输入面板。
- **Task 5: 2×2 工单参数舱卡片重构 (tlpOrderParameters)**
  - 订单正则模式 + 序号 Excel 反查 + 数量步长 + 一式N联联数微调器。
- **Task 6: 纯净 6 宫格排版看板卡片重构 (pnlImpositionDashboard)**
  - 网格与布局数（计算只读），尺寸（只读不可修改，无底色无边框，光标 default），角度/出血/颜色（点击微调）。
- **Task 7: 快捷预设方案卡片重构 (presetBrowserCard)**
  - Tabs.StyleLine 分组标签页 + 3px 微圆角预设按钮 + 完整的右键管理菜单。
- **Task 8: 底部沉底操作栏重构 (pnlActionBar)**
  - 位于预设卡片下方、折叠条上方，左侧范围切换按钮，右侧取消与蓝色主确认按钮。
- **Task 9: 编译构建验证与全量回归测试**
  - 验证 0 错误、0 警告，运行全部单元测试确保无回归。

## 三、 测试与验收策略
- 单元测试：运行 `MaterialSelectFormSizeContractTests`、`MaterialSelectFormThemeTests`；
- 构建验收：Debug/Release 构建 0 错误；
- 状态验证：单工位折叠/展开、批量折叠/展开 4 种基准状态下的尺寸与布局。
