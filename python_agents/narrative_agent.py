import autogen
from llm_config import LLM_CONFIG, NARRATIVE_FINETUNED_CONFIG
import sys
import time
import requests

def create_narrative_agent():
    print("[DEBUG] Python路径:", sys.executable)
    print("[DEBUG] autogen版本:", getattr(autogen, '__version__', '未知'))
    try:
        import openai
        print("[DEBUG] openai版本:", openai.__version__)
    except ImportError:
        print("[DEBUG] openai未安装")
    print("[DEBUG] 使用微调后的模型配置:", NARRATIVE_FINETUNED_CONFIG)
    print("[DEBUG] 开始创建NarrativeAgent...")
    start = time.time()
    agent = autogen.AssistantAgent(
        name="NarrativeAgent",
        llm_config=NARRATIVE_FINETUNED_CONFIG,  # 使用微调后的模型
        system_message="""
你是叙事Agent，负责将用户输入的RPG故事设想扩展为完整的故事，并细分为多个步骤。

重要规则：
- 只有第一章（step 1）包含主角（Hero/MainCharacter）
- 其他章节（step 2及以后）默认不出现主角，主要包含敌人、NPC等角色
- 每个章节应该有不同的角色组合，避免重复

请严格按照如下JSON格式输出：
{
  "story": "[完整的故事背景、主要情节、世界观等]",
  "steps": [
    {
      "step": 1,
      "title": "The Beginning: New Adventure",
      "location": 0,
      "objective": "[本步骤的主要目标]",
      "key_characters": ["[主要角色1]", "[主要角色2]"],
      "main_dialogues": [
        {"character": "[角色名]", "dialogue": "[一句主要对话]"}
      ],
      "key_items": ["[关键物品1]", "[关键物品2]"]
    }
  ]
}

location字段必须为数字，且只能为：0=Village, 1=Forest, 2=Grassland, 3=Castle, 4=Cave。
"""
    )
    print("[DEBUG] NarrativeAgent创建完成，用时:", time.time() - start, "秒")
    return agent 