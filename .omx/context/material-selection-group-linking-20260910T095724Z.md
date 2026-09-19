# 材料选择分组联动缺陷修复：上下文快照

- 任务：修复材料选择框的分组锁定持久性、联动分组间拖放，以及左侧列表右键菜单禁用状态。
- 目标结果：监控文件动态追加时保留既有锁定组，并将新文件放入联动组；文件可在联动组间拖动；右侧禁用配置继续约束左侧列表菜单。
- 已知代码事实：
  - `AppendPendingFiles` 追加后调用 `RebuildProcessGroups`。
  - `RebuildProcessGroups` 目前按文件名重新创建组，普通组使用新 `GroupId` 且 `IsLocked=false`，因此会丢失人工锁定状态。
  - 现有拖放只连接平铺表格 `dgvBatchFiles`；分组表格没有拖放事件。
  - `MoveSelectedFilesToGroup` 已提供跨组移动能力，但未连接到拖放。
  - 左侧分组表格菜单没有读取预设的 `DisabledOptions`/当前禁用状态。
- 约束：保持 WinForms/.NET Framework 4.8 与既有 MVP/AntdUI 约定；不引入新依赖；注释使用简体中文。
- 非目标：不重做材料选择界面、不改变 PDF/排版业务算法、不修改无关模块。
- 待确认决策：文件是否允许拖入已锁定的目标组；锁定语义决定拖放校验行为。
- 可能触点：
  - `src/WindowsFormsApp3/Forms/Main/MaterialSelectFormModern.BatchGroupWorkbench.cs`
  - `src/WindowsFormsApp3/Forms/Main/MaterialSelectFormModern.cs`
  - `src/WindowsFormsApp3/Models/MaterialSelectionResult.cs`
  - `src/WindowsFormsApp3.Tests/Forms/MaterialSelectionSchemeATests.cs`
- 已检查文档：项目 `AGENTS.md`、`README.md`、`docs/Obsidian知识库/05-操作手册与流程/材料选择对话框.md`。
- 初始上下文摘要：不需要额外压缩。
