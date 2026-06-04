# Excel导入与数据库视图

## 模块定位
负责 Excel 数据导入、字段匹配、结果共享，以及导入结果的浏览与校对。

## 相关代码
- `src/WindowsFormsApp3/Forms/Panels/ExcelImportPanel.cs`
- `src/WindowsFormsApp3/Forms/Dialogs/ExcelImportForm.cs`
- `src/WindowsFormsApp3/Forms/Panels/DatabasePanel.cs`

## 命名提醒
`DatabasePanel` 在产品语义上更接近“Excel 结果视图”，不是传统数据库系统。

## 与其他模块的关系
- 为 [[文件重命名模块]] 提供数据匹配输入
- 与正则系统、列组合、序号搜索、事件分组都有交叉

## 后续整理方向
- 统一“Excel 导入”“数据库”“数据视图”的术语
- 收口列组合、数据匹配正则、诊断报告类文档