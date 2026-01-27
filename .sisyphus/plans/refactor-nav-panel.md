# 工作计划：重构 MainShellForm 导航面板折叠逻辑

**目标**: 重构 MainShellForm.cs 中的导航面板折叠/展开逻辑，修复无动画、布局裁剪、硬编码问题。

**创建时间**: 2026-01-26T19:26:00Z
**预计时间**: 1-2 小时
**优先级**: 高

---

## 任务清单

### 阶段 1：准备和分析
- [x] 1. 分析现有折叠逻辑问题点
- [x] 2. 确认动画和布局需求
- [x] 3. 设计重构方案

### 阶段 2：核心重构
- [x] 4. 添加常量和状态字段
- [x] 5. 实现平滑动画逻辑 (Timer)
- [x] 6. 实现按钮自适应布局 (展开/折叠状态)
- [x] 7. 更新按钮点击事件处理

### 阶段 3：测试和验证
- [x] 8. 手动验证动画流畅性
- [x] 9. 验证按钮布局正确性
- [x] 10. 测试边缘情况（快速点击）

---

## 详细方案

### 技术要求
- **折叠宽度**: 70px（仅显示图标）
- **展开宽度**: 170px（保持原状）
- **动画步长**: 20px/帧
- **防抖**: 动画期间禁用点击
- **按钮行为**: 折叠时隐藏文字，展开时显示

### 关键文件
- `src/WindowsFormsApp3/Forms/Main/MainShellForm.cs` - 主要修改
- `src/WindowsFormsApp3/Forms/Main/MainShellForm.Designer.cs` - 可能需要添加Timer

### 实现细节
1. **常量定义**:
   ```csharp
   private const int ExpandedWidth = 170;
   private const int CollapsedWidth = 70;
   private const int AnimationStep = 20;
   ```

2. **状态管理**:
   ```csharp
   private bool isCollapsed = false;
   private Timer collapseTimer;
   private bool isAnimating = false;
   ```

3. **按钮处理**:
   - 使用现有的 `navButtons` 字典
   - 折叠时：设置 `Text = ""`，调整按钮宽度
   - 展开时：恢复原始文字和宽度

### 验收标准
- [ ] 折叠动画流畅，无卡顿
- [ ] 折叠状态下仅显示图标，布局整齐
- [ ] 展开状态下恢复完整显示
- [ ] 快速点击不会导致状态混乱
- [ ] 标题和版本标签正确显示/隐藏

### 风险控制
- 保留原始按钮文本，避免丢失
- 动画期间禁用交互，防止状态错乱
- 测试不同主题下的显示效果