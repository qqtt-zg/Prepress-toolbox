# 方案 A 原生实现与验证

日期：2026-10-09。用户已选择方案 A，HTML 基准保持不变，本轮实现 Windows Forms／AntdUI 界面。

## 已实现

- 设置页和材料选择框共用 `ImpositionParametersEditor`，左参数、右预览，按窗口可用尺寸切换单列／双列参数布局，无参数滚动条。
- 默认设置保存两种材料的尺寸、四边距、独立方向、共用行列和默认启用状态；切回自动显式保存行列为 0。
- 本次参数使用独立草稿，仅提交选中的材料。三种策略互斥，隐藏的原始输入缓存不参与校验或计算；另一材料的无效草稿不阻塞当前应用。
- 平张校验宽高；卷装只校验横向容量，手动行数保持精确，实际长度为最小长度与实际网格长度的较大值。不足部分留白，并在可印刷区域居中。
- 指定布局数提供可行组合选择；卷装长卷可延展，移除原有 8 行／96 页限制，以因子枚举控制计算量。
- 预览使用生产计算服务，支持适应窗口、局部查看、放大缩小和横纵位置导航；只绘制可见范围的有限线条，不创建逐页控件。
- 设置中的 PDF 纸张导入与成品预览参考分别处理；读取异步执行，失败保留原尺寸。参考成品尺寸不写入默认纸张参数。
- 最新预览覆盖旧预览；取消和关闭清理异步操作。本次应用需要可用文件布局，停用排版可独立提交。
- 材料框看板和批量配置读取各自材料的有效布局状态；临时倍数方向不回写页序复制设置。

## 实现边界

| 职责 | 文件 |
|---|---|
| 共用编辑视图 | [ImpositionParametersEditor.cs](../src/WindowsFormsApp3/Forms/Controls/ImpositionParametersEditor.cs) |
| 有界图形预览 | [ImpositionPreviewControl.cs](../src/WindowsFormsApp3/Forms/Controls/ImpositionPreviewControl.cs) |
| 草稿、校验、来源、提交与居中几何 | [ImpositionEditorPresenter.cs](../src/WindowsFormsApp3/Presenters/ImpositionEditorPresenter.cs) |
| 默认设置读写 | [ImpositionSettingsService.cs](../src/WindowsFormsApp3/Services/ImpositionSettingsService.cs) |
| 原始输入与有效参数 | [ImpositionEditorDraft.cs](../src/WindowsFormsApp3/Models/ImpositionEditorDraft.cs)、[TemporaryImpositionParameters.cs](../src/WindowsFormsApp3/Models/TemporaryImpositionParameters.cs) |
| 生产算法 | [ImpositionService.cs](../src/WindowsFormsApp3/Services/ImpositionService.cs) |

既有尺寸和边距键继续使用；新增 `Imposition_FlatRotationMode` 与 `Imposition_RollRotationMode`，未设置时兼容自动方向。原有临时尺寸／方向快照继续兼容；布局操作不写入跨任务快照，默认布局仍由默认设置提供。

没有修改方案 B，没有引入依赖，没有制作或发布安装包。预览为真实比例的矩形网格，不渲染 PDF 内容，不新增出血、间距或刀线模拟。

## 验证

- Debug 编译通过，0 警告、0 错误。
- 76 项定向测试通过，覆盖共享编辑器、草稿隔离、默认保存、材料框看板、页面框选择与批量布局；结果见 [TRX](../.build-verify/TestResults/scheme-a-native.trx) 与[测试日志](../.build-verify/scheme-a-native-test.log)。测试项目另有 3 项既有编译／分析器警告。
- 原生界面边界检查：1160×720、1040×620、941×529、800×520、880×445，覆盖两种材料、三种排版方式及数量不可用状态。
- 验证 2000 行／4000 个页面的长卷局部放大和横纵导航；验证 997 个页面的卷装数量组合、非对称边距居中、少行数满足长度下限。
- 验证过期异步预览不覆盖新结果，PDF 读取失败保留尺寸；验证默认预览样本放不下时仍可保存合法参数。
- 验证本次提交不修改默认尺寸，也不改变页序复制方向与次数。

当前证据为定向测试与原生截图，未宣称全量测试套件、全部显示器／DPI 或系统文件选择对话框的完整自动化验收；最终结果以所链接 TRX 为准。

## 原生截图

| 状态 | 证据 |
|---|---|
| 桌面参数与居中预览 | [1160×720](../output/native-scheme-a/native-1160x720.png) |
| 实际设置页可用尺寸 | [941×529](../output/native-scheme-a/native-941x529.png) |
| 紧凑窗口 | [880×445](../output/native-scheme-a/native-880x445.png) |
| 默认预览警告仍可保存 | [设置页警告](../output/native-scheme-a/settings-941x529-warning.png) |
| 长卷局部查看及导航 | [长卷预览](../output/native-scheme-a/long-roll-detail.png) |

## 置顶遮挡修复（2026-10-09）

用户反馈材料框开启置顶后遮挡临时排版窗口。本次没有检测到正在运行的产品窗口，因此没有直接读取用户出问题时的窗口状态；以下为使用实际 Win32 句柄的受控复现和修复。

修改前，首次 `ShowDialog(owner)` 的所有者关系和前后顺序正常；材料框重建句柄后，临时窗口仍属于材料框，双方原生置顶标志仍为真，但窗口顺序检查发现临时窗口落到材料框后方。托管 `Enabled` 与原生窗口禁用状态也不能混用，重建后原生父窗可能恢复为可操作状态。

修复内容：

- 临时窗口显示、激活、自身或材料框句柄变化后，显式同步置顶层级并恢复在材料框前方。
- 重新排序使用 `SWP_NOACTIVATE` 与 `SWP_NOOWNERZORDER`，不同时激活或重新排序父窗；标志语义见 [Microsoft SetWindowPos 文档](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos)。
- 模态期间维护材料框的托管和原生禁用状态，关闭后恢复原状态，保持图钉偏好不变。
- 材料框延迟执行的首次置顶逻辑不再抢到已打开的临时窗口前面；重复打开请求复用当前模态上下文。
- 使用原生 `IsWindowEnabled` 检测下一级模态对话框，避免恢复临时窗口时遮挡文件选择一类对话框。

15 项相关回归测试通过，覆盖置顶开启／关闭、父子句柄重建、父窗重新提升、嵌套模态窗口、关闭恢复、重复打开、原图钉开关和参数编辑。Debug 编译通过，0 警告、0 错误。报告见 [窗口层级回归 TRX](../.build-verify/TestResults/window-order-regression.trx)。实际用户场景的具体触发动作仍需在新编译版本中确认；修复前后的受控窗口层级差异已验证。
