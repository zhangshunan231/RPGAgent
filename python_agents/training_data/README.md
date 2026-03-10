# 训练数据说明

## 📁 数据存储位置

```
python_agents/training_data/
├── scene_data.jsonl          # SceneAgent 训练数据（3 条样本）
└── narrative_data.jsonl      # NarrativeAgent 训练数据（3 条样本）
```

---

## 📊 当前数据统计

| Agent | 样本数量 | 文件 |
|-------|---------|------|
| SceneAgent | 3 条 | `scene_data.jsonl` |
| NarrativeAgent | 3 条 | `narrative_data.jsonl` |

**注意**：这些是**示例数据**，用于测试 fine-tuning 流程。

---

## ⚠️ 重要提示

### 最少数据要求
- **测试 fine-tuning**：至少 3-10 条（当前已满足）
- **实际使用**：推荐 **50-100 条**
- **最佳效果**：推荐 **200+ 条**

### 数据质量
当前提供的 3 条样本是**高质量手工标注**的示例：
- ✅ 覆盖不同场景（森林、城堡、洞穴）
- ✅ 正确的输入输出格式
- ✅ 符合 Agent 的规则要求

---

## 🚀 立即开始 Fine-tuning

即使只有 3 条数据，你也可以**立即测试** fine-tuning 流程：

```bash
# SceneAgent（3 条样本）
python simple_finetune.py SceneAgent training_data/scene_data.jsonl

# NarrativeAgent（3 条样本）
python simple_finetune.py NarrativeAgent training_data/narrative_data.jsonl
```

**预计时间**：10-30 分钟  
**预计成本**：约 ￥1-5

---

## 📈 如何收集更多数据

### 方法 1：自动收集（推荐）
1. 正常使用系统 1-2 周
2. 数据会自动保存到 `training_data/` 目录
3. 合并数据：
```bash
python -c "from data_collector import get_collector; get_collector().merge_all_sessions()"
```
4. 分离数据：
```bash
python split_data.py
```

### 方法 2：手动添加
直接编辑 `generate_training_data.py`，添加更多样本到：
- `scene_training_samples` 列表
- `narrative_training_samples` 列表

然后重新运行：
```bash
python generate_training_data.py
```

---

## 📝 数据格式说明

### JSONL 格式
每行一条 JSON 数据，格式：
```json
{
  "messages": [
    {"role": "system", "content": "系统提示词"},
    {"role": "user", "content": "用户输入"},
    {"role": "assistant", "content": "Agent 输出"}
  ]
}
```

### SceneAgent 示例
```json
{
  "messages": [
    {"role": "system", "content": "你是场景Agent..."},
    {"role": "user", "content": "{\"story\": \"...\", \"steps\": [...], \"assets\": [...]}"},
    {"role": "assistant", "content": "{\"scene_params\": {...}, \"selected_assets\": [...]}"}
  ]
}
```

### NarrativeAgent 示例
```json
{
  "messages": [
    {"role": "system", "content": "你是叙事Agent..."},
    {"role": "user", "content": "一个年轻的战士踏上寻找失落宝藏的旅程"},
    {"role": "assistant", "content": "{\"story\": \"...\", \"steps\": [...]}"}
  ]
}
```

---

## 🎯 下一步

1. **立即测试**（当前 3 条样本）：
   ```bash
   python simple_finetune.py SceneAgent training_data/scene_data.jsonl
   ```

2. **收集更多数据**（1-2 周）：
   - 正常使用系统
   - 数据自动收集

3. **重新 Fine-tune**（50-100 条样本）：
   - 效果会更好
   - 更符合你的实际需求

---

**当前数据足够测试流程，建议先尝试！** 🚀

