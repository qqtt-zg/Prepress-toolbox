---
name: obsidian-doc-gen
description: 为 Prepress Toolbox 项目的 Obsidian 知识库生成标准化模块文档
---

# Obsidian 模块文档生成技能

## 概述

为 Prepress Toolbox (印前工具箱) 项目的 Obsidian 知识库生成标准化的模块文档。

## 目标目录

```
docs/Obsidian知识库/
├── 00-首页与导航/     # 项目概览、模块图、导航页
├── 02-模块知识库/     # 各功能模块详细文档
├── 03-架构与实现/     # 技术架构文档
├── 04-竞品与行业/     # 竞品分析
├── 05-用户手册与教程/  # 用户文档
├── 06-开发与维护/     # 开发指南、版本记录
├── 07-测试与验证/     # 测试文档
└── 99-归档/          # 历史文档
```

## 文档模板

### 模块知识库文档 (02-模块知识库/)

每个模块文档应包含以下结构：

```markdown
# {模块名称}

## 模块定位
{一句话描述模块的核心职责}

覆盖功能：
- {功能点1}
- {功能点2}
- ...

## 当前状态
{简述当前实现进度}

## 关键文件
- `{文件路径}` - {说明}

## 核心服务
| 服务名 | 职责 | 依赖 |
|--------|------|------|
| {ServiceName} | {职责} | {依赖} |

## UI 组件
| 组件名 | 类型 | 说明 |
|--------|------|------|
| {ComponentName} | Panel/Dialog/Control | {说明} |

## 数据流
```
用户操作 → View → Presenter → Service → 数据处理
```

## 已知限制
- {限制1}
- {限制2}

## 相关模块
- [[{相关模块1}]]
- [[{相关模块2}]]

## 后续优化方向
- {优化1}
- {优化2}
```

## 生成流程

### 1. 分析模块源码

```powershell
# 读取模块相关文件
Get-ChildItem -Path "src/WindowsFormsApp3/Services/*{模块关键词}*.cs"
Get-ChildItem -Path "src/WindowsFormsApp3/Forms/**/*{模块关键词}*.cs"
```

### 2. 提取关键信息

- **服务类**: 从 `Services/` 目录提取
- **视图组件**: 从 `Forms/Panels/` 和 `Forms/Controls/` 提取
- **接口定义**: 从 `Interfaces/` 提取
- **数据模型**: 从 `Models/` 提取

### 3. 生成文档

使用模板填充内容，确保：
- 所有 Wikilinks 使用 `[[模块名]]` 格式
- 文件路径使用相对路径
- 代码块使用正确的语言标记

### 4. 更新导航

生成文档后，更新相关导航页：
- `00-首页与导航/模块图.md`
- `00-首页与导航/目录.md`

## 核心模块清单

| 模块 | 文档路径 | 核心服务 |
|------|----------|----------|
| PDF拆分 | `02-模块知识库/PDF拆分模块.md` | PdfSplitService |
| 拼版工作台 | `02-模块知识库/拼版工作台.md` | ImpositionService, SaddleStitchService |
| 文件重命名 | `02-模块知识库/文件重命名模块.md` | FileRenameService |
| Excel数据导入 | `02-模块知识库/Excel导入模块.md` | ExcelImportService |
| PDF检查器 | `02-模块知识库/PDF检查器.md` | PdfInspectorService, PdfFontInspectorService |
| 色彩管理 | `02-模块知识库/色彩管理.md` | (待实现) |
| 字体管理 | `02-模块知识库/字体管理.md` | PdfFontInspectorService_Poppler |
| 批量处理 | `02-模块知识库/批量处理.md` | BatchProcessingService |
| 事件系统 | `02-模块知识库/事件系统.md` | EventBus |

## 注意事项

1. **中文注释**: 所有文档使用简体中文
2. **Wikilinks**: 使用 Obsidian 格式的 `[[链接]]`
3. **代码引用**: 使用反引号标记类名、方法名
4. **图片**: 如需图片，存放在 `docs/Obsidian知识库/assets/` 目录
5. **更新频率**: 模块有重大变更时同步更新文档
