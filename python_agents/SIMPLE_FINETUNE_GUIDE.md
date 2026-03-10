# 超简单 Fine-tuning 指南

## 🎯 目标
Fine-tune **SceneAgent** 和 **NarrativeAgent**，让它们更智能、更准确。

---

## 📋 准备工作

### 1. 确认已安装 openai 包
```bash
pip install openai
```

### 2. 确认配置正确
检查 `llm_config.py` 中的 `ZZZ_API_KEY` 是否已设置。

---

## 🚀 使用步骤（3 步）

### 第 1 步：收集训练数据

正常使用系统，数据会自动收集到 `training_data/` 目录。

**建议收集量**：
- SceneAgent: 50-100 条
- NarrativeAgent: 50-100 条

**合并数据**：
```bash
cd python_agents
python -c "from data_collector import get_collector; get_collector().merge_all_sessions()"
```

会生成：`training_data/merged_training_data.jsonl`

---

### 第 2 步：分离数据（按 Agent）

数据收集器会把所有 Agent 的数据混在一起，我们需要分离出来。

**手动分离**（使用文本编辑器）：

1. 打开 `training_data/merged_training_data.jsonl`
2. 查看每条数据的 `system` 字段：
   - 包含 "场景Agent" → 复制到 `scene_data.jsonl`
   - 包含 "叙事Agent" → 复制到 `narrative_data.jsonl`

**或者使用这个简单脚本**（创建后运行）：

```python
# split_data.py
import json

with open('training_data/merged_training_data.jsonl', 'r', encoding='utf-8') as f:
    lines = f.readlines()

scene_data = []
narrative_data = []

for line in lines:
    data = json.loads(line)
    system_msg = data['messages'][0]['content']
    
    if '场景Agent' in system_msg or 'SceneAgent' in system_msg:
        scene_data.append(line)
    elif '叙事Agent' in system_msg or 'NarrativeAgent' in system_msg:
        narrative_data.append(line)

# 保存
with open('training_data/scene_data.jsonl', 'w', encoding='utf-8') as f:
    f.writelines(scene_data)

with open('training_data/narrative_data.jsonl', 'w', encoding='utf-8') as f:
    f.writelines(narrative_data)

print(f"✅ 分离完成：SceneAgent {len(scene_data)} 条, NarrativeAgent {len(narrative_data)} 条")
```

运行：
```bash
python split_data.py
```

---

### 第 3 步：开始 Fine-tuning

**Fine-tune SceneAgent**：
```bash
python simple_finetune.py SceneAgent training_data/scene_data.jsonl
```

**Fine-tune NarrativeAgent**：
```bash
python simple_finetune.py NarrativeAgent training_data/narrative_data.jsonl
```

**可选：使用 GPT-4o**（更贵，但效果更好）：
```bash
python simple_finetune.py SceneAgent training_data/scene_data.jsonl gpt-4o
```

---

## ⏰ 等待时间

- **上传文件**：1-2 分钟
- **训练时间**：30 分钟 - 2 小时
- 脚本会自动等待，每 60 秒检查一次

**你可以关闭终端，稍后手动检查状态**（见下方）。

---

## 🔧 使用 Fine-tuned 模型

训练完成后，会生成配置文件：
- `llm_config_sceneagent_finetuned.py`
- `llm_config_narrativeagent_finetuned.py`

### 方法 1：修改 import（推荐）

**在 `scene_agent.py` 中**：
```python
# 原来
from llm_config import LLM_CONFIG

# 改为
from llm_config_sceneagent_finetuned import LLM_CONFIG
```

**在 `narrative_agent.py` 中**：
```python
# 原来
from llm_config import LLM_CONFIG

# 改为
from llm_config_narrativeagent_finetuned import LLM_CONFIG
```

### 方法 2：直接替换模型名称

打开 `llm_config.py`，将 `model` 改为 fine-tuned 模型名称：
```python
LLM_CONFIG = {
    "config_list": [
        {
            "model": "ft:gpt-3.5-turbo-0613:xxxxxxxx",  # 替换为你的模型名称
            "api_key": ZZZ_API_KEY,
            "base_url": "https://api.zhizengzeng.com/v1"
        }
    ],
    "temperature": 0.7,
    "seed": None,
}
```

---

## 💰 成本估算

使用 **gpt-3.5-turbo** fine-tuning：
- **100 条样本**：约 ￥10-30
- **SceneAgent + NarrativeAgent**：约 ￥20-60

使用 **gpt-4o** fine-tuning：
- **100 条样本**：约 ￥30-80
- **SceneAgent + NarrativeAgent**：约 ￥60-160

---

## 🆘 手动检查训练状态

如果中途关闭了终端，可以手动检查：

```python
# check_status.py
from simple_finetune import check_status

job_id = "ftjob-xxxxxxxxxxxx"  # 替换为你的任务 ID
status, model = check_status(job_id)

print(f"状态: {status}")
if model:
    print(f"模型: {model}")
```

---

## 📝 完整流程总结

```bash
# 1. 收集数据（使用系统 1-2 周）
python server.py  # 正常运行

# 2. 合并数据
python -c "from data_collector import get_collector; get_collector().merge_all_sessions()"

# 3. 分离数据
python split_data.py

# 4. Fine-tune SceneAgent
python simple_finetune.py SceneAgent training_data/scene_data.jsonl

# 5. Fine-tune NarrativeAgent
python simple_finetune.py NarrativeAgent training_data/narrative_data.jsonl

# 6. 修改 import，使用新模型
# 编辑 scene_agent.py 和 narrative_agent.py
```

---

## ❓ 常见问题

### Q1: 训练需要多久？
**A:** 通常 30 分钟 - 2 小时，取决于数据量。

### Q2: 可以中途停止吗？
**A:** 可以关闭终端，训练会继续。稍后用 `check_status.py` 检查。

### Q3: 如何知道训练完成？
**A:** 脚本会自动检查，完成后会显示模型名称。

### Q4: 训练失败怎么办？
**A:** 
1. 检查数据格式是否正确（JSONL）
2. 检查 API Key 和余额
3. 联系智增增客服

### Q5: 可以重新训练吗？
**A:** 可以！收集更多数据后再次运行即可。

---

## 🎉 完成！

训练完成后，你的 SceneAgent 和 NarrativeAgent 就会更智能了！

**预期改善**：
- ✅ 资产选择更准确
- ✅ 故事生成更符合你的风格
- ✅ 减少错误和遗漏
- ✅ 响应更快

---

**祝 Fine-tuning 成功！🚀**

