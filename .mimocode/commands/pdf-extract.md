---
description: 从 PDF 文件中提取结构化数据（尺寸、名称、页数等）
---

# PDF 数据提取命令

## 使用方式

```
/pdf-extract <pdf_path_or_directory> [--format json|csv|table] [--fields dimensions,name,pages]
```

## 参数

- `<pdf_path_or_directory>`: PDF 文件路径或包含 PDF 的目录
- `--format`: 输出格式 (json, csv, table)，默认 table
- `--fields`: 提取的字段，逗号分隔，默认全部

## 可提取字段

| 字段名 | 说明 | 示例 |
|--------|------|------|
| `name` | 文件名 | "45_M02922 TXM.ARIE.0065.pdf" |
| `pages` | 页数 | 4 |
| `dimensions` | 页面尺寸 (mm) | "210 x 297" |
| `width` | 宽度 (mm) | 210 |
| `height` | 高度 (mm) | 297 |
| `size_mb` | 文件大小 (MB) | 2.5 |
| `title` | PDF 标题 | "Document Title" |
| `author` | 作者 | "Author Name" |
| `created` | 创建时间 | "2024-01-15" |

## 实现方式

### 使用 pdfplumber (推荐)

```python
import pdfplumber
from pathlib import Path

def extract_pdf_info(pdf_path):
    """提取 PDF 基本信息"""
    info = {'name': Path(pdf_path).name}
    
    with pdfplumber.open(pdf_path) as pdf:
        info['pages'] = len(pdf.pages)
        
        # 提取第一页尺寸
        if pdf.pages:
            page = pdf.pages[0]
            width_pt = page.width
            height_pt = page.height
            # 转换为毫米 (1 pt = 0.3528 mm)
            info['width'] = round(width_pt * 0.3528, 1)
            info['height'] = round(height_pt * 0.3528, 1)
            info['dimensions'] = f"{info['width']} x {info['height']}"
        
        # 提取元数据
        if pdf.metadata:
            info['title'] = pdf.metadata.get('Title', '')
            info['author'] = pdf.metadata.get('Author', '')
            info['created'] = pdf.metadata.get('CreationDate', '')[:10]
    
    # 文件大小
    info['size_mb'] = round(Path(pdf_path).stat().st_size / (1024*1024), 2)
    
    return info

def extract_from_directory(dir_path, fields=None):
    """从目录中提取所有 PDF 信息"""
    results = []
    for pdf_file in sorted(Path(dir_path).glob("*.pdf")):
        try:
            info = extract_pdf_info(pdf_file)
            if fields:
                info = {k: v for k, v in info.items() if k in fields}
            results.append(info)
        except Exception as e:
            results.append({'name': pdf_file.name, 'error': str(e)})
    
    return results
```

### 输出格式示例

**Table 格式:**
```
名称                                    页数    尺寸 (mm)      大小 (MB)
45_M02922 TXM.ARIE.0065.pdf           4      210 x 297      2.5
46_M02923 TXM.ARIE.0066.pdf           2      210 x 297      1.8
```

**JSON 格式:**
```json
[
  {
    "name": "45_M02922 TXM.ARIE.0065.pdf",
    "pages": 4,
    "dimensions": "210 x 297",
    "width": 210,
    "height": 297,
    "size_mb": 2.5
  }
]
```

**CSV 格式:**
```csv
name,pages,dimensions,width,height,size_mb
45_M02922 TXM.ARIE.0065.pdf,4,210 x 297,210,297,2.5
```

## 典型使用场景

### 1. 按尺寸分类 PDF

```powershell
# 提取所有 PDF 尺寸并按尺寸分组
$pdfInfo = python pdf_extract.py "C:\Users\admin\Desktop\60x55" --format json
$pdfInfo | Group-Object dimensions
```

### 2. 查找异常尺寸

```powershell
# 查找尺寸不是标准 A4 的 PDF
$pdfInfo = python pdf_extract.py "C:\Users\admin\Desktop\60x55" --format json
$pdfInfo | Where-Object { $_.width -ne 210 -or $_.height -ne 297 }
```

### 3. 生成报告

```powershell
# 导出为 CSV 用于 Excel 分析
python pdf_extract.py "C:\Users\admin\Desktop\60x55" --format csv --fields name,pages,dimensions > report.csv
```

## 依赖

- Python 3.x
- pdfplumber: `pip install pdfplumber`

## 相关文件

- 技能: `.mimocode/skills/obsidian-doc-gen/SKILL.md`
- 示例脚本: `C:\Users\admin\Desktop\60x55\.claude\skills\pdf-size-classify\classify.py`
