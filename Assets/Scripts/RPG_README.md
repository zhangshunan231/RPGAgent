# RPG 脚本系统

## 概述

这是一个简单的RPG脚本系统，为角色和物品提供基础功能。系统包含三个主要脚本：

1. **RPGCharacter** - 角色脚本
2. **RPGItem** - 物品脚本  
3. **PlayerInteraction** - 玩家交互脚本

## 脚本功能

### RPGCharacter (角色脚本)

#### 主要功能
- **对话系统**: 处理NPC对话
- **战斗系统**: 处理角色战斗属性
- **AI行为**: 控制NPC行为
- **交互系统**: 与玩家交互

#### 配置选项
```csharp
[Header("角色信息")]
public string characterName;        // 角色名称
public string characterType;        // 角色类型 (NPC, Enemy, Player)
public string description;          // 角色描述

[Header("对话系统")]
public List<DialogueData> dialogues; // 对话数据
public float interactionRange;      // 交互范围
public bool canTalk;                // 是否可以对话

[Header("战斗系统")]
public CombatStats combatStats;     // 战斗属性
public bool canFight;               // 是否可以战斗
public string[] attackAnimations;   // 攻击动画

[Header("AI行为")]
public bool hasAI;                  // 是否有AI
public float patrolRadius;          // 巡逻半径
public Transform[] patrolPoints;    // 巡逻点
```

#### 使用方法
1. 将 `RPGCharacter` 脚本添加到角色GameObject
2. 在Inspector中配置角色属性
3. 设置对话内容和战斗属性
4. 配置AI行为（如果需要）

### RPGItem (物品脚本)

#### 主要功能
- **拾取系统**: 处理物品拾取
- **装备系统**: 处理装备属性
- **消耗品**: 处理使用效果
- **视觉效果**: 发光、旋转等

#### 物品类型
- **Weapon**: 武器
- **Armor**: 防具
- **Consumable**: 消耗品
- **Material**: 材料
- **Quest**: 任务物品
- **Currency**: 货币

#### 稀有度
- **Common**: 普通 (白色)
- **Uncommon**: 优秀 (绿色)
- **Rare**: 稀有 (蓝色)
- **Epic**: 史诗 (紫色)
- **Legendary**: 传说 (黄色，会旋转)

#### 配置选项
```csharp
[Header("物品信息")]
public string itemName;             // 物品名称
public ItemType itemType;           // 物品类型
public ItemRarity rarity;           // 稀有度
public Sprite icon;                 // 图标

[Header("基础属性")]
public int value;                   // 价值
public float weight;                // 重量
public bool isStackable;            // 是否可堆叠

[Header("装备属性")]
public bool isEquippable;           // 是否可装备
public ItemEffect[] effects;        // 装备效果

[Header("消耗品属性")]
public bool isConsumable;           // 是否可消耗
public string useEffect;            // 使用效果

[Header("拾取设置")]
public bool isPickable;             // 是否可拾取
public bool autoPickup;             // 是否自动拾取
```

### PlayerInteraction (玩家交互脚本)

#### 主要功能
- **检测交互对象**: 自动检测附近的角色和物品
- **处理交互输入**: 响应玩家按键
- **UI提示**: 显示交互提示

#### 配置选项
```csharp
[Header("交互设置")]
public float interactionRange;      // 交互范围
public KeyCode interactionKey;      // 交互按键 (默认E)
public LayerMask interactableLayers; // 可交互层

[Header("UI提示")]
public GameObject interactionPrompt; // 交互提示UI
public TMPro.TextMeshProUGUI promptText; // 提示文本
```

## 使用流程

### 1. 设置玩家
1. 创建玩家角色
2. 添加 `PlayerInteraction` 脚本
3. 设置交互范围和按键
4. 创建交互提示UI（可选）

### 2. 创建角色
1. 创建角色GameObject
2. 添加 `RPGCharacter` 脚本
3. 配置角色属性
4. 添加碰撞器（用于交互检测）

### 3. 创建物品
1. 创建物品GameObject
2. 添加 `RPGItem` 脚本
3. 配置物品属性
4. 添加碰撞器（用于拾取检测）

### 4. 自动生成（推荐）
使用 `MultiAgentRPGEditor` 的"生成关键item/character"功能，系统会自动：
- 根据叙事步骤生成角色和物品
- 自动添加相应的脚本组件
- 设置基础属性

## 扩展功能

### 对话系统扩展
```csharp
// 在RPGCharacter中添加更多对话
character.dialogues.Add(new DialogueData
{
    dialogueId = "quest_dialogue",
    dialogueLines = new string[] { "你有任务要完成吗？", "我可以帮助你。" },
    isCompleted = false
});
```

### 物品效果扩展
```csharp
// 在RPGItem中添加装备效果
item.effects = new ItemEffect[]
{
    new ItemEffect { effectName = "Attack", value = 10, description = "增加攻击力" },
    new ItemEffect { effectName = "Defense", value = 5, description = "增加防御力" }
};
```

### 自定义交互
```csharp
// 在RPGCharacter中重写交互方法
public override void Interact()
{
    // 自定义交互逻辑
    if (hasQuest)
    {
        StartQuest();
    }
    else
    {
        base.Interact();
    }
}
```

## 注意事项

1. **标签设置**: 确保玩家有 "Player" 标签
2. **碰撞器**: 角色和物品都需要碰撞器用于交互检测
3. **层级设置**: 根据需要设置适当的层级
4. **性能优化**: 大量对象时考虑使用对象池

## 未来扩展

- 背包系统
- 装备系统
- 任务系统
- 战斗系统
- 存档系统
- UI系统集成 