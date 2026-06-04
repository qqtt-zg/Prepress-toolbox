# PDF拆分模块

## 当前状态
已实现基础拆分能力，Action List 扩展属于长期架构方向。

## 模块定位
负责按页码范围、Excel/CSV 数据等规则拆分 PDF，并提供拖放、预览、进度与取消能力。

## 当前有效入口
- [[PDF拆分操作]]
- [[PDF拆分与Action List的关系]]

## 操作手册
- [[PDF拆分操作]] — 源 PDF、Excel/CSV 数据、页数范围预览、执行拆分与输出规则

## 代码入口
- `src/WindowsFormsApp3/Forms/Panels/PdfSplitPanel.cs`
- `src/WindowsFormsApp3/Services/PdfSplitService.cs`

## 测试验证
- [[测试与验证索引]]
- 重点验证：页码范围解析、Excel/CSV 数据匹配、输出文件命名、取消与进度显示。

## 相关架构
- [[ActionList架构方向]] — 长期自动化架构参考
- `docs/PDF拆分与动作列表模块化设计.md` — PDF 拆分与动作列表关系的旧源文档

## 与其他主题的关系
- 与 [[工作台与排版工作区]] 在 PDF 处理链路上有交叉
- 与 [[PDF检查器]] 共享 PDF 文件输入、预览和处理语义

## 相关归档
- [[归档总索引]]
- `99-归档/实现完成记录/` — PDF 处理链路、撤回重做、进度条等实现记录

## 回链
- [[模块地图]]
- [[项目总览]]
