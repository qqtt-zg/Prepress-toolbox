---
description: 生成 Obsidian 知识库的交互式可视化图谱 (HTML)
---

# Obsidian 知识库可视化命令

## 使用方式

```
/obsidian-visualize [--output docs/项目知识图谱可视化/知识图谱.html] [--theme dark|light]
```

## 参数

- `--output`: 输出 HTML 文件路径，默认 `docs/项目知识图谱可视化/Obsidian知识图谱图谱.html`
- `--theme`: 主题 (dark, light)，默认 dark

## 功能

生成一个交互式 HTML 文件，可视化展示：
1. 模块之间的依赖关系
2. 文档之间的 Wikilinks 连接
3. 知识库的层级结构

## 实现方式

### 1. 扫描知识库结构

```powershell
# 扫描所有 Markdown 文件
$files = Get-ChildItem -Path "docs/Obsidian知识库" -Recurse -Filter "*.md"

# 提取 Wikilinks
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $links = [regex]::Matches($content, '\[\[([^\]]+)\]\]') | 
             ForEach-Object { $_.Groups[1].Value }
}
```

### 2. 生成图谱数据

```json
{
  "nodes": [
    {"id": "PDF拆分模块", "group": "02-模块知识库", "size": 10},
    {"id": "拼版工作台", "group": "02-模块知识库", "size": 12}
  ],
  "links": [
    {"source": "PDF拆分模块", "target": "PDF检查器", "value": 3},
    {"source": "拼版工作台", "target": "文件重命名模块", "value": 2}
  ]
}
```

### 3. HTML 模板

使用 D3.js 或 vis.js 生成交互式图谱：

```html
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="UTF-8">
    <title>Obsidian 知识图谱</title>
    <script src="https://d3js.org/d3.v7.min.js"></script>
    <style>
        body { margin: 0; background: #1a1a1a; color: #fff; }
        #graph { width: 100vw; height: 100vh; }
        .node { cursor: pointer; }
        .link { stroke: #666; stroke-opacity: 0.6; }
    </style>
</head>
<body>
    <div id="graph"></div>
    <script>
        // D3.js 力导向图实现
        const data = /* 图谱数据 */;
        
        const svg = d3.select("#graph")
            .append("svg")
            .attr("width", "100%")
            .attr("height", "100%");
        
        const simulation = d3.forceSimulation(data.nodes)
            .force("link", d3.forceLink(data.links).id(d => d.id))
            .force("charge", d3.forceManyBody())
            .force("center", d3.forceCenter());
        
        // 绘制节点和连线...
    </script>
</body>
</html>
```

## 输出文件

- 主文件: `docs/项目知识图谱可视化/Obsidian知识图谱图谱.html`
- 备用位置: `docs/Obsidian知识图谱图谱.html`

## 相关文件

- 技能: `.mimocode/skills/obsidian-doc-gen/SKILL.md`
- 现有可视化: `docs/项目知识图谱可视化/Obsidian知识图谱图谱.html`

## 使用场景

1. **项目概览**: 快速了解知识库结构
2. **文档导航**: 点击节点跳转到对应文档
3. **发现孤立文档**: 找出没有链接的文档
4. **识别核心模块**: 节点大小反映文档重要性

## 依赖

- D3.js v7 (CDN 引入)
- 或 vis.js (备选方案)
