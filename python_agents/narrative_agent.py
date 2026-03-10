import autogen
from llm_config import LLM_CONFIG
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
    print("[DEBUG] LLM_CONFIG:", LLM_CONFIG)
    print("[DEBUG] 开始创建NarrativeAgent...")
    start = time.time()
    agent = autogen.AssistantAgent(
        name="NarrativeAgent",
        llm_config=LLM_CONFIG,
        system_message="""
你是叙事Agent，负责将用户输入的RPG故事设想扩展为完整的故事，并细分为多个步骤。

重要规则：
- 只有第一章（step 1）包含主角（Hero/MainCharacter）
- 其他章节（step 2及以后）默认不出现主角，主要包含敌人、NPC等角色
- 每个章节应该有不同的角色组合，避免重复
- 角色类型包括：敌人（Enemy）、NPC（友好角色）、道具（Props）

请严格按照如下JSON格式输出：
{
  "story": "[完整的故事背景、主要情节、世界观等]",
  "steps": [
    {
      "step": 1,
      "title": "The Beginning: New Adventure",  // 步骤标题，用英文，简洁有吸引力
      "location": 0,    // 0=Village, 1=Forest, 2=Grassland, 3=Castle, 4=Cave
      "objective": "[本步骤的主要目标]",
      "key_characters": ["[主要角色1]", "[主要角色2]"],
      "main_dialogues": [
        {"character": "[角色名]", "dialogue": "[一句主要对话]"},
        {"character": "[角色名]", "dialogue": "[一句主要对话]"}
      ],
      "key_items": ["[关键物品1]", "[关键物品2]"]
    },
    {
      "step": 2,
      "title": "Into the Unknown: Forest Mystery",
      "location": 1,
      "objective": "Investigate the forest",
      "key_characters": ["Mysterious Stranger", "Forest Guardian"],
      "main_dialogues": [
        {"character": "Mysterious Stranger", "dialogue": "The forest holds many secrets."},
        {"character": "Forest Guardian", "dialogue": "Only the worthy may pass through."}
      ],
      "key_items": ["Strange Rune", "Forest Key"]
    },
    {
      "step": 3,
      "title": "Crossing the Plains: Hidden Dangers",
      "location": 2,
      "objective": "Cross the grassland to reach the castle",
      "key_characters": ["Merchant", "Grassland Bandit"],
      "main_dialogues": [
        {"character": "Merchant", "dialogue": "Beware of monsters in the grassland!"},
        {"character": "Grassland Bandit", "dialogue": "Hand over your valuables!"}
      ],
      "key_items": ["Healing Herb", "Bandit's Map"]
    },
    {
      "step": 4,
      "title": "The Final Showdown: Castle Confrontation",
      "location": 3,
      "objective": "Confront the villain in the castle",
      "key_characters": ["Castle Guard", "Dark Wizard"],
      "main_dialogues": [
        {"character": "Castle Guard", "dialogue": "The castle is under attack!"},
        {"character": "Dark Wizard", "dialogue": "Your defenses are useless against my magic!"}
      ],
      "key_items": ["Ancient Sword", "Magic Scroll"]
    },
    {
      "step": 5,
      "title": "Victory and Beyond: Cave Triumph",
      "location": 4,
      "objective": "Defeat the final monster in the cave",
      "key_characters": ["Cave Dweller", "Ancient Dragon"],
      "main_dialogues": [
        {"character": "Cave Dweller", "dialogue": "The dragon has awakened!"},
        {"character": "Ancient Dragon", "dialogue": "You shall not pass!"}
      ],
      "key_items": ["Victory Medal", "Dragon Scale"]
    }
  ]
}

要求：
- steps为步骤数组，每个step结构如上。
- title字段为步骤标题，用英文，简洁有吸引力，如"The Beginning: New Adventure"。
- location字段必须为数字，且只能为：0, 1, 2, 3, 4，分别对应：0=Village, 1=Forest, 2=Grassland, 3=Castle, 4=Cave。
- 每个step的location必须根据剧情推进合理变化，不能全部相同，且应尽量覆盖多种类型。
- 如果剧情合理，location应尽量多样，避免重复。
- 输出必须是有效JSON，不能有多余注释。
- 每个step都必须有title和location字段，且和objective等字段同级。
- 适合RPG游戏开发使用。

示例：
{
  "story": "The hero receives a quest in the village, explores the mysterious forest and grassland, and finally confronts the villain in the castle and cave.",
  "steps": [
    {
      "step": 1,
      "title": "The Beginning: Village Quest",
      "location": 0,
      "objective": "Receive the quest from the village chief",
      "key_characters": ["Hero", "Village Chief"],
      "main_dialogues": [
        {"character": "Hero", "dialogue": "What can I do for you, chief?"},
        {"character": "Village Chief", "dialogue": "Strange things are happening in the forest. Please investigate."}
      ],
      "key_items": ["Quest Letter"]
    },
    {
      "step": 2,
      "title": "Into the Unknown: Forest Investigation",
      "location": 1,
      "objective": "Investigate the forest",
      "key_characters": ["Mysterious Stranger", "Forest Guardian"],
      "main_dialogues": [
        {"character": "Mysterious Stranger", "dialogue": "The forest holds many secrets."},
        {"character": "Forest Guardian", "dialogue": "Only the worthy may pass through."}
      ],
      "key_items": ["Strange Rune", "Forest Key"]
    },
    {
      "step": 3,
      "title": "Crossing the Plains: Grassland Journey",
      "location": 2,
      "objective": "Cross the grassland to reach the castle",
      "key_characters": ["Merchant", "Grassland Bandit"],
      "main_dialogues": [
        {"character": "Merchant", "dialogue": "Beware of monsters in the grassland!"},
        {"character": "Grassland Bandit", "dialogue": "Hand over your valuables!"}
      ],
      "key_items": ["Healing Herb", "Bandit's Map"]
    },
    {
      "step": 4,
      "title": "The Final Showdown: Castle Battle",
      "location": 3,
      "objective": "Confront the villain in the castle",
      "key_characters": ["Castle Guard", "Dark Wizard"],
      "main_dialogues": [
        {"character": "Castle Guard", "dialogue": "The castle is under attack!"},
        {"character": "Dark Wizard", "dialogue": "Your defenses are useless against my magic!"}
      ],
      "key_items": ["Ancient Sword", "Magic Scroll"]
    },
    {
      "step": 5,
      "title": "Victory and Beyond: Cave Triumph",
      "location": 4,
      "objective": "Defeat the final monster in the cave",
      "key_characters": ["Cave Dweller", "Ancient Dragon"],
      "main_dialogues": [
        {"character": "Cave Dweller", "dialogue": "The dragon has awakened!"},
        {"character": "Ancient Dragon", "dialogue": "You shall not pass!"}
      ],
      "key_items": ["Victory Medal", "Dragon Scale"]
    }
  ]
}
"""
    )
    print("[DEBUG] NarrativeAgent创建完成，用时:", time.time() - start, "秒")
    return agent 