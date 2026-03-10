"""
生成训练数据样本
为 SceneAgent 和 NarrativeAgent 生成高质量的训练样本
"""

import json
import os

# SceneAgent 的 system message（简化版）
SCENE_AGENT_SYSTEM = """你是场景Agent，负责从RPG叙事步骤中提取Unity地图生成参数和选择合适资产。

输入格式：{"steps": [...], "assets": [...]}
steps包含：step(步骤号)、location(地点)、objective(目标)、key_characters(角色)、key_items(物品)
assets包含：name(名称)、type(类型)、aliases(别名)、description(描述)

你的任务：
1. 从输入的整体'story'描述中提取 world setting，并据此设置地图参数：noiseScale(10-50)、landThreshold(0.1-0.25)、mountainThreshold(0.6-0.9)。
2. 从assets中选择合适的角色和物品，根据steps中的key_characters和key_items进行智能匹配。
3. MainCharacter只能在step 1中选择，其他step只能选择NPC、Enemy、Props。
4. 每个step都必须包含至少1个Props类型的资产。

输出格式必须严格为JSON：
{
  "scene_params": {
    "noiseScale": 20,
    "landThreshold": 0.5,
    "mountainThreshold": 0.75
  },
  "selected_assets": [
    {"step": 1, "assets": ["角色名1", "物品名1"]},
    {"step": 2, "assets": ["角色名2", "物品名2"]}
  ]
}"""

# NarrativeAgent 的 system message（简化版）
NARRATIVE_AGENT_SYSTEM = """你是叙事Agent，负责将用户输入的RPG故事设想扩展为完整的故事，并细分为多个步骤。

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

location字段必须为数字，且只能为：0=Village, 1=Forest, 2=Grassland, 3=Castle, 4=Cave。"""


# SceneAgent 训练样本
scene_training_samples = [
    # 样本 1: 森林主题
    {
        "user": json.dumps({
            "story": "A hero ventures into the mysterious forest to find the lost treasure.",
            "steps": [
                {
                    "step": 1,
                    "location": 1,
                    "objective": "Meet the village elder",
                    "key_characters": ["Hero", "Village Elder"],
                    "key_items": ["Map", "Sword"]
                },
                {
                    "step": 2,
                    "location": 1,
                    "objective": "Explore the forest",
                    "key_characters": ["Forest Guardian", "Wild Wolf"],
                    "key_items": ["Magic Stone", "Health Potion"]
                }
            ],
            "assets": [
                {"name": "Hero", "type": "MainCharacter", "aliases": ["主角", "战士"], "description": "勇敢的主角"},
                {"name": "Elder", "type": "NPC", "aliases": ["长老", "村长"], "description": "智慧的村庄长老"},
                {"name": "Guardian", "type": "NPC", "aliases": ["守护者"], "description": "森林守护者"},
                {"name": "Wolf", "type": "Enemy", "aliases": ["狼", "野兽"], "description": "凶猛的野狼"},
                {"name": "Map", "type": "Props", "aliases": ["地图"], "description": "古老的地图"},
                {"name": "IronSword", "type": "Props", "aliases": ["剑", "武器"], "description": "锋利的铁剑"},
                {"name": "MagicStone", "type": "Props", "aliases": ["石头", "宝石"], "description": "神秘的魔法石"},
                {"name": "Potion", "type": "Props", "aliases": ["药水", "治疗"], "description": "恢复生命的药水"}
            ]
        }, ensure_ascii=False),
        "assistant": json.dumps({
            "scene_params": {
                "noiseScale": 30,
                "landThreshold": 0.2,
                "mountainThreshold": 0.7
            },
            "selected_assets": [
                {"step": 1, "assets": ["Hero", "Elder", "Map", "IronSword"]},
                {"step": 2, "assets": ["Guardian", "Wolf", "MagicStone", "Potion"]}
            ]
        }, ensure_ascii=False)
    },
    
    # 样本 2: 城堡主题
    {
        "user": json.dumps({
            "story": "The kingdom is under attack. The hero must defend the castle from invaders.",
            "steps": [
                {
                    "step": 1,
                    "location": 0,
                    "objective": "Receive mission from king",
                    "key_characters": ["Knight", "King"],
                    "key_items": ["Royal Sword", "Shield"]
                },
                {
                    "step": 2,
                    "location": 3,
                    "objective": "Defend the castle",
                    "key_characters": ["Castle Guard", "Enemy Soldier"],
                    "key_items": ["Armor", "Battle Horn"]
                }
            ],
            "assets": [
                {"name": "Knight", "type": "MainCharacter", "aliases": ["骑士", "主角"], "description": "英勇的骑士"},
                {"name": "King", "type": "NPC", "aliases": ["国王"], "description": "王国的统治者"},
                {"name": "Guard", "type": "NPC", "aliases": ["守卫", "士兵"], "description": "城堡守卫"},
                {"name": "Invader", "type": "Enemy", "aliases": ["入侵者", "敌人"], "description": "入侵的敌军"},
                {"name": "RoyalSword", "type": "Props", "aliases": ["王剑", "神剑"], "description": "国王赐予的宝剑"},
                {"name": "Shield", "type": "Props", "aliases": ["盾牌"], "description": "坚固的盾牌"},
                {"name": "Armor", "type": "Props", "aliases": ["盔甲"], "description": "重型盔甲"},
                {"name": "Horn", "type": "Props", "aliases": ["号角"], "description": "战斗号角"}
            ]
        }, ensure_ascii=False),
        "assistant": json.dumps({
            "scene_params": {
                "noiseScale": 20,
                "landThreshold": 0.15,
                "mountainThreshold": 0.8
            },
            "selected_assets": [
                {"step": 1, "assets": ["Knight", "King", "RoyalSword", "Shield"]},
                {"step": 2, "assets": ["Guard", "Invader", "Armor", "Horn"]}
            ]
        }, ensure_ascii=False)
    },
    
    # 样本 3: 洞穴冒险
    {
        "user": json.dumps({
            "story": "Adventurers explore a deep cave filled with treasures and dangers.",
            "steps": [
                {
                    "step": 1,
                    "location": 2,
                    "objective": "Gather the team",
                    "key_characters": ["Adventurer", "Merchant"],
                    "key_items": ["Torch", "Rope"]
                },
                {
                    "step": 2,
                    "location": 4,
                    "objective": "Enter the cave",
                    "key_characters": ["Cave Troll", "Bat Swarm"],
                    "key_items": ["Treasure Chest", "Ancient Relic"]
                }
            ],
            "assets": [
                {"name": "Adventurer", "type": "MainCharacter", "aliases": ["冒险者"], "description": "经验丰富的冒险者"},
                {"name": "Merchant", "type": "NPC", "aliases": ["商人"], "description": "提供装备的商人"},
                {"name": "Troll", "type": "Enemy", "aliases": ["巨魔"], "description": "洞穴中的巨魔"},
                {"name": "Bats", "type": "Enemy", "aliases": ["蝙蝠群"], "description": "成群的蝙蝠"},
                {"name": "Torch", "type": "Props", "aliases": ["火把"], "description": "照明用的火把"},
                {"name": "Rope", "type": "Props", "aliases": ["绳索"], "description": "攀爬用的绳索"},
                {"name": "Chest", "type": "Props", "aliases": ["宝箱"], "description": "装满宝物的箱子"},
                {"name": "Relic", "type": "Props", "aliases": ["遗物"], "description": "古代遗物"}
            ]
        }, ensure_ascii=False),
        "assistant": json.dumps({
            "scene_params": {
                "noiseScale": 15,
                "landThreshold": 0.1,
                "mountainThreshold": 0.6
            },
            "selected_assets": [
                {"step": 1, "assets": ["Adventurer", "Merchant", "Torch", "Rope"]},
                {"step": 2, "assets": ["Troll", "Bats", "Chest", "Relic"]}
            ]
        }, ensure_ascii=False)
    },
]


# NarrativeAgent 训练样本
narrative_training_samples = [
    # 样本 1: 简单的冒险故事
    {
        "user": "一个年轻的战士踏上寻找失落宝藏的旅程",
        "assistant": json.dumps({
            "story": "在一个古老的王国，年轻的战士听说了一个失落已久的宝藏。为了证明自己的勇气，他决定踏上这段充满危险的旅程。沿途他将遇到各种挑战和盟友，最终在森林深处找到传说中的宝藏。",
            "steps": [
                {
                    "step": 1,
                    "title": "The Beginning: Village Departure",
                    "location": 0,
                    "objective": "从村庄出发，获得长老的祝福和地图",
                    "key_characters": ["Warrior", "Village Elder"],
                    "main_dialogues": [
                        {"character": "Warrior", "dialogue": "I will prove myself worthy!"},
                        {"character": "Village Elder", "dialogue": "Take this map, young warrior. May it guide you safely."}
                    ],
                    "key_items": ["Ancient Map", "Wooden Sword"]
                },
                {
                    "step": 2,
                    "title": "Into the Wild: Forest Encounter",
                    "location": 1,
                    "objective": "穿越森林，遭遇第一个挑战",
                    "key_characters": ["Forest Spirit", "Wolf Pack"],
                    "main_dialogues": [
                        {"character": "Forest Spirit", "dialogue": "Only the brave may pass through my domain."},
                        {"character": "Wolf Pack", "dialogue": "[Growling sounds]"}
                    ],
                    "key_items": ["Magic Herb", "Silver Key"]
                },
                {
                    "step": 3,
                    "title": "The Final Test: Cave of Treasures",
                    "location": 4,
                    "objective": "进入洞穴，找到宝藏",
                    "key_characters": ["Guardian Golem", "Ancient Ghost"],
                    "main_dialogues": [
                        {"character": "Guardian Golem", "dialogue": "Turn back or face your doom!"},
                        {"character": "Ancient Ghost", "dialogue": "The treasure belongs to those who are worthy."}
                    ],
                    "key_items": ["Golden Chest", "Legendary Sword"]
                }
            ]
        }, ensure_ascii=False)
    },
    
    # 样本 2: 拯救王国
    {
        "user": "王国被黑暗势力威胁，主角必须组建队伍拯救王国",
        "assistant": json.dumps({
            "story": "黑暗魔法师带领邪恶军队入侵了和平的王国。年轻的骑士接受国王的召唤，必须组建一支勇敢的队伍，穿越危险的土地，最终在城堡中击败黑暗魔法师，拯救王国。",
            "steps": [
                {
                    "step": 1,
                    "title": "The Call: Royal Mission",
                    "location": 3,
                    "objective": "接受国王的任务，获得王室装备",
                    "key_characters": ["Knight", "King"],
                    "main_dialogues": [
                        {"character": "Knight", "dialogue": "I will defend the kingdom with my life!"},
                        {"character": "King", "dialogue": "Take this royal blade. You are our last hope."}
                    ],
                    "key_items": ["Royal Blade", "King's Seal"]
                },
                {
                    "step": 2,
                    "title": "Gathering Allies: Grassland Journey",
                    "location": 2,
                    "objective": "在草原上寻找盟友",
                    "key_characters": ["Archer", "Healer"],
                    "main_dialogues": [
                        {"character": "Archer", "dialogue": "I will join your quest!"},
                        {"character": "Healer", "dialogue": "Let me help heal the wounded."}
                    ],
                    "key_items": ["Healing Staff", "Bow and Arrows"]
                },
                {
                    "step": 3,
                    "title": "Final Battle: Castle Siege",
                    "location": 3,
                    "objective": "攻入城堡，击败黑暗魔法师",
                    "key_characters": ["Dark Wizard", "Shadow Knights"],
                    "main_dialogues": [
                        {"character": "Dark Wizard", "dialogue": "You cannot stop the darkness!"},
                        {"character": "Shadow Knights", "dialogue": "[Battle cries]"}
                    ],
                    "key_items": ["Magic Staff", "Victory Banner"]
                }
            ]
        }, ensure_ascii=False)
    },
    
    # 样本 3: 寻找神器
    {
        "user": "寻找古代神器来阻止即将到来的灾难",
        "assistant": json.dumps({
            "story": "预言中的灾难即将降临大陆。唯一的希望是找到三件古代神器并将它们组合起来。冒险者必须跨越不同的地域，面对各种考验，最终收集齐神器阻止灾难。",
            "steps": [
                {
                    "step": 1,
                    "title": "The Prophecy: Village Warning",
                    "location": 0,
                    "objective": "从预言者处得知任务",
                    "key_characters": ["Hero", "Prophet"],
                    "main_dialogues": [
                        {"character": "Hero", "dialogue": "Tell me what I must do!"},
                        {"character": "Prophet", "dialogue": "Seek the three artifacts of power scattered across the land."}
                    ],
                    "key_items": ["Prophecy Scroll", "Compass"]
                },
                {
                    "step": 2,
                    "title": "First Artifact: Forest Temple",
                    "location": 1,
                    "objective": "在森林神殿找到第一件神器",
                    "key_characters": ["Temple Guardian", "Forest Sprite"],
                    "main_dialogues": [
                        {"character": "Temple Guardian", "dialogue": "Prove your worth to claim the artifact."},
                        {"character": "Forest Sprite", "dialogue": "I can guide you through the trials."}
                    ],
                    "key_items": ["Wind Amulet", "Temple Key"]
                },
                {
                    "step": 3,
                    "title": "Second Artifact: Cave Depths",
                    "location": 4,
                    "objective": "深入洞穴获得第二件神器",
                    "key_characters": ["Stone Elemental", "Cave Dweller"],
                    "main_dialogues": [
                        {"character": "Stone Elemental", "dialogue": "The earth artifact is mine to protect!"},
                        {"character": "Cave Dweller", "dialogue": "Beware the traps within!"}
                    ],
                    "key_items": ["Earth Crystal", "Mining Pick"]
                }
            ]
        }, ensure_ascii=False)
    },
]


def generate_training_file(samples, system_message, output_file):
    """
    生成训练数据文件
    
    参数:
        samples: 训练样本列表
        system_message: 系统消息
        output_file: 输出文件路径
    """
    output_dir = "training_data"
    os.makedirs(output_dir, exist_ok=True)
    
    filepath = os.path.join(output_dir, output_file)
    
    with open(filepath, 'w', encoding='utf-8') as f:
        for sample in samples:
            training_data = {
                "messages": [
                    {"role": "system", "content": system_message},
                    {"role": "user", "content": sample["user"]},
                    {"role": "assistant", "content": sample["assistant"]}
                ]
            }
            f.write(json.dumps(training_data, ensure_ascii=False) + '\n')
    
    print(f"[OK] 生成 {len(samples)} 条样本 -> {filepath}")
    return filepath


if __name__ == "__main__":
    print("=" * 70)
    print("生成训练数据样本")
    print("=" * 70)
    
    # 生成 SceneAgent 训练数据
    scene_file = generate_training_file(
        scene_training_samples,
        SCENE_AGENT_SYSTEM,
        "scene_data.jsonl"
    )
    
    # 生成 NarrativeAgent 训练数据
    narrative_file = generate_training_file(
        narrative_training_samples,
        NARRATIVE_AGENT_SYSTEM,
        "narrative_data.jsonl"
    )
    
    print("\n" + "=" * 70)
    print("[SUCCESS] 数据生成完成！")
    print("=" * 70)
    print(f"\nSceneAgent: {len(scene_training_samples)} 条")
    print(f"NarrativeAgent: {len(narrative_training_samples)} 条")
    print("\n下一步：")
    print("  python simple_finetune.py SceneAgent training_data/scene_data.jsonl")
    print("  python simple_finetune.py NarrativeAgent training_data/narrative_data.jsonl")
    print("\n提示：这些是示例数据，实际使用中建议收集更多真实数据（50-100 条）")

