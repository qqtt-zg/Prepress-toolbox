---
description: 快速恢复项目工作上下文，显示上次工作状态和待办事项
---

# 项目上下文恢复命令

## 使用方式

```
/continue-project
```

## 功能

快速恢复 Prepress Toolbox 项目的工作上下文，帮助用户：
1. 了解上次会话的工作状态
2. 查看待办任务列表
3. 快速定位到上次编辑的文件
4. 获取项目健康状态概览

## 执行流程

### 1. 读取会话检查点

```powershell
# 读取最近的 checkpoint
checkpoint_path = "C:\Users\admin\.local\share\mimocode\memory\sessions\ses_1469c18c7ffepV8gD8sYp1tLdz\checkpoint.md"
```

### 2. 检查项目状态

```powershell
# 检查 Git 状态
git status --short
git log --oneline -5

# 检查编译状态
dotnet build WindowsFormsApp3.sln -c Debug --no-restore --verbosity quiet
```

### 3. 读取项目内存

```powershell
# 读取项目级内存
memory_path = "C:\Users\admin\.local\share\mimocode\memory\projects\382b6ce2-cbe5-4f1a-95a2-49118820f1cd\MEMORY.md"
```

### 4. 生成上下文摘要

输出格式：

```
=== 项目上下文恢复 ===

📋 上次会话状态:
- 会话ID: {session_id}
- 最后活动: {timestamp}
- 工作内容: {summary}

🔧 Git 状态:
- 分支: {branch}
- 未提交更改: {changed_files} 个文件
- 最近提交: {last_commit}

📊 项目健康:
- 编译状态: {build_status}
- 测试状态: {test_status}

📝 待办任务:
1. {task_1}
2. {task_2}
...

💡 建议操作:
- {suggestion_1}
- {suggestion_2}
```

## 输出变量

- `$LAST_SESSION_ID`: 上次会话ID
- `$LAST_WORK_SUMMARY`: 上次工作总结
- `$PENDING_TASKS`: 待办任务列表
- `$PROJECT_HEALTH`: 项目健康状态

## 相关文件

- 会话检查点: `C:\Users\admin\.local\share\mimocode\memory\sessions\{session_id}\checkpoint.md`
- 项目内存: `C:\Users\admin\.local\share\mimocode\memory\projects\{project_id}\MEMORY.md`
- 任务进度: `C:\Users\admin\.local\share\mimocode\memory\sessions\{session_id}\tasks\{task_id}\progress.md`
