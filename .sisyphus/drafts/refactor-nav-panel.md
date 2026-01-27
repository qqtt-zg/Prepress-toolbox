# Draft: 重构 MainShellForm 导航面板折叠逻辑

## 需求 (Confirmed)
- 目标：重构 `MainShellForm.cs` 中的导航面板折叠/展开逻辑。
- 现状：用户反馈当前逻辑“很不合理”（无动画、布局裁剪、硬编码）。

## 决策记录 (Decisions)
- **折叠模式**：仅图标模式 (Icon-only)。
  - 折叠宽度：`70px` (足以容纳图标)。
  - 展开宽度：`170px` (保持原状)。
  - 行为：折叠时隐藏按钮文字，仅居中显示图标。
- **动画效果**：启用平滑过渡动画 (Timer)。
- **测试策略**：人工验证 (Manual Verification)。
  - 现有 UI 测试被跳过 (Skipped)，修复成本较高，本次侧重交互体验。

## 技术方案 (Technical Approach)
1. **引入常量**：
   - `private const int ExpandedWidth = 170;`
   - `private const int CollapsedWidth = 70;`
   - `private const int AnimationStep = 20;` (每帧变化量)
2. **状态管理**：
   - `private bool isCollapsed = false;` (明确状态)
   - `private Timer collapseTimer;` (动画定时器)
   - `private bool isAnimating = false;` (防抖)
3. **按钮适配**：
   - 修改 `InitializeMenuItems`，确保按钮引用可用（已有 `navButtons` 字典）。
   - 在折叠/展开过程中，遍历 `navButtons.Keys`：
     - 折叠时：`btn.Text = ""` (或保存原文本到 Tag), `btn.Width = CollapsedWidth - Padding`.
     - 展开时：`btn.Text = originalText`, `btn.Width = 100`.
   - **优化**：AntdUI Button 可能支持自动布局，若设置 `Text = ""` 且 `IconPosition = Top`，它应该会自动居中图标。
4. **事件处理**：
   - 重写 `BtnCollapse_Click` 启动 Timer。
   - `Timer_Tick` 更新 `SplitterDistance`。
   - 动画结束后更新 UI 状态 (Title/Version label visibility).

## 范围 (Scope)
- **IN**: `MainShellForm.cs` (逻辑), `MainShellForm.Designer.cs` (如果需要添加 Timer 控件).
- **OUT**: 其他面板的内部逻辑.
