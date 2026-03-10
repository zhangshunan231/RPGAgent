# Fine-tuning 快速开始 🚀

## 🎯 一句话总结
让 **SceneAgent** 和 **NarrativeAgent** 更智能，只需 3 个命令！

---

## ⚡ 超快速开始（3 步）

### 1️⃣ 运行系统，收集数据（1-2 周）
```bash
python server.py
```
数据会自动保存到 `training_data/` 目录。

---

### 2️⃣ 准备数据
```bash
# 合并数据
python -c "from data_collector import get_collector; get_collector().merge_all_sessions()"

# 分离数据
python split_data.py
```

---

### 3️⃣ 开始 Fine-tuning
```bash
# SceneAgent（推荐优先）
python simple_finetune.py SceneAgent training_data/scene_data.jsonl

# NarrativeAgent
python simple_finetune.py NarrativeAgent training_data/narrative_data.jsonl
```

**或者双击运行**：`quick_finetune.bat`（Windows）

---

## 📊 训练时间和成本

| 数据量 | 时间 | 成本（gpt-3.5-turbo） |
|--------|------|-----------------------|
| 50 条  | 30-60 分钟 | ￥5-15 |
| 100 条 | 1-2 小时 | ￥10-30 |
| 200 条 | 2-3 小时 | ￥20-60 |

---

## ✅ 使用 Fine-tuned 模型

训练完成后，修改 Agent 文件：

**scene_agent.py**：
```python
# from llm_config import LLM_CONFIG
from llm_config_sceneagent_finetuned import LLM_CONFIG
```

**narrative_agent.py**：
```python
# from llm_config import LLM_CONFIG
from llm_config_narrativeagent_finetuned import LLM_CONFIG
```

---

## 📚 详细文档

- **完整指南**：`SIMPLE_FINETUNE_GUIDE.md`
- **智增增配置**：`ZZZ_SETUP_GUIDE.md`

---

## 🆘 常见问题

**Q: 需要多少数据？**  
A: 最少 50 条，推荐 100-200 条。

**Q: 可以中途关闭终端吗？**  
A: 可以，训练会继续。用 `python check_finetune_status.py <job_id>` 检查。

**Q: 失败怎么办？**  
A: 检查 API Key、余额、数据格式。

---

## 🎉 完成后的效果

- ✅ **更准确**：资产选择和故事生成更精准
- ✅ **更快**：响应速度提升
- ✅ **更稳定**：减少错误和遗漏
- ✅ **更省钱**：缩短提示词，降低 token 成本

---

**开始你的 Fine-tuning 之旅吧！🚀**

