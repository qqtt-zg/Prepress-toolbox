# 测试规格：材料选择分组联动一致性

## 单元/窗体状态测试

1. `AppendPendingFiles_PreservesLockedGroupAndAddsToUnlockedLinkedGroup`
   - 创建至少两个组，锁定其中一个。
   - 动态追加监控文件。
   - 断言锁定组身份、状态、成员与参数不变；新文件属于未锁定联动组并继承其参数。
2. `AppendPendingFiles_WithPreserveMarkers_DoesNotMutateLockedGroup`
   - 动态追加带匹配材料/工艺保留标记的文件。
   - 断言文件仍加入未锁定联动组，匹配的锁定保留组身份、成员和锁定状态不变。
3. `AppendPendingFiles_UsesFirstEligibleGroupOrCreatesFallback`
   - 两个未锁定、非保留组同时存在时，断言按 `_processGroups` 显示顺序选择第一个。
   - 没有合格组时，断言在末尾创建一个继承当前右侧选择参数的未锁定联动组。
4. `TryMoveFilesBetweenGroups_MovesOnlyBetweenUnlockedGroups`
   - 从未锁定组 A 移动文件到未锁定组 B。
   - 断言两个组成员、文件分组字段与全局批次项一致。
5. `TryMoveFilesBetweenGroups_RejectsLockedSourceOrTargetAtomically`
   - 分别覆盖锁定源组和锁定目标组。
   - 断言返回拒绝且移动前后快照一致。
6. `PresetContextMenu_IsSuppressedForLeftPanelDescendants`
   - 分别模拟鼠标位于动态分组表格与键盘焦点位于左侧子控件。
   - 断言全局预设菜单不显示；左侧专属菜单构造保持不变。
7. `SortingAndPanelReopen_PreserveGroupIdentityAndLockState`
   - 锁定分组后执行文件名/数量排序并折叠再展开。
   - 分别覆盖文件名、数量和尺寸排序；断言每个组内顺序符合排序键，`_batchItems` 顺序/Index 与组内投影一致，分组顺序、`GroupId`、锁定状态和成员归属不变。

## 回归验证

- 保留并运行已有动态追加去重、平铺列表排序/移动、主题可读性测试。
- 运行 `dotnet test src/WindowsFormsApp3.Tests/WindowsFormsApp3.Tests.csproj`。
- 运行 `dotnet build WindowsFormsApp3.sln -c Debug`。

## 手工烟雾场景

- 打开材料选择框，建立两个联动组并锁定其中一个；监控目录新增文件后确认锁定徽标及成员不变。
- 在两个未锁定组之间拖动；再尝试从锁定组拖出及拖入锁定组，确认光标拒绝且列表不变化。
- 在左侧列表及动态分组子控件处右键，确认全局预设菜单不显示，同时左侧专属数量菜单仍可用。
