using UnityEngine;

public enum ItemType
{
    Weapon,
    Armor,
    Consumable,
    Material,
    Quest,
    Currency
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[System.Serializable]
public class ItemEffect
{
    public string effectName;
    public float value;
    public string description;
}

public class RPGItem : MonoBehaviour
{
    [Header("物品信息")]
    public string itemName;
    public string description;
    public ItemType itemType;
    public ItemRarity rarity = ItemRarity.Common;
    public Sprite icon;
    
    [Header("基础属性")]
    public int value = 1;
    public float weight = 1f;
    public bool isStackable = false;
    public int maxStackSize = 99;
    
    [Header("装备属性")]
    public bool isEquippable = false;
    public ItemEffect[] effects;
    public string[] compatibleSlots; // 装备槽位
    
    [Header("消耗品属性")]
    public bool isConsumable = false;
    public float cooldown = 0f;
    public string useEffect;
    
    [Header("拾取设置")]
    public bool isPickable = true;
    public float pickupRange = 2f;
    public string pickupPrompt = "按E拾取";
    public bool autoPickup = false;
    
    [Header("视觉效果")]
    public bool hasGlow = false;
    public Color glowColor = Color.yellow;
    public float glowIntensity = 1f;
    public bool rotateInWorld = false;
    public float rotationSpeed = 50f;
    
    // 私有变量
    private Transform player;
    private bool isInRange = false;
    private float lastUseTime = 0f;
    private Renderer itemRenderer;
    private Material originalMaterial;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        itemRenderer = GetComponent<Renderer>();
        
        if (itemRenderer != null)
        {
            originalMaterial = itemRenderer.material;
        }
        
        InitializeItem();
    }
    
    void Update()
    {
        if (player != null)
        {
            CheckPlayerDistance();
        }
        
        if (rotateInWorld)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
        
        if (hasGlow && itemRenderer != null)
        {
            UpdateGlow();
        }
    }
    
    void InitializeItem()
    {
        // 根据物品类型设置默认属性
        switch (itemType)
        {
            case ItemType.Weapon:
                isEquippable = true;
                isConsumable = false;
                compatibleSlots = new string[] { "Weapon" };
                break;
            case ItemType.Armor:
                isEquippable = true;
                isConsumable = false;
                compatibleSlots = new string[] { "Armor" };
                break;
            case ItemType.Consumable:
                isEquippable = false;
                isConsumable = true;
                break;
            case ItemType.Material:
                isEquippable = false;
                isConsumable = false;
                isStackable = true;
                break;
            case ItemType.Currency:
                isEquippable = false;
                isConsumable = false;
                isStackable = true;
                autoPickup = true;
                break;
        }
        
        // 根据稀有度设置视觉效果
        switch (rarity)
        {
            case ItemRarity.Common:
                glowColor = Color.white;
                break;
            case ItemRarity.Uncommon:
                glowColor = Color.green;
                hasGlow = true;
                break;
            case ItemRarity.Rare:
                glowColor = Color.blue;
                hasGlow = true;
                break;
            case ItemRarity.Epic:
                glowColor = Color.magenta;
                hasGlow = true;
                break;
            case ItemRarity.Legendary:
                glowColor = Color.yellow;
                hasGlow = true;
                rotateInWorld = true;
                break;
        }
    }
    
    void CheckPlayerDistance()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        bool wasInRange = isInRange;
        isInRange = distance <= pickupRange;
        
        if (isInRange && !wasInRange)
        {
            OnPlayerEnterRange();
        }
        else if (!isInRange && wasInRange)
        {
            OnPlayerExitRange();
        }
        
        // 自动拾取
        if (isInRange && autoPickup)
        {
            Pickup();
        }
    }
    
    void OnPlayerEnterRange()
    {
        if (isPickable)
        {
            Debug.Log($"靠近 {itemName}: {pickupPrompt}");
        }
    }
    
    void OnPlayerExitRange()
    {
        Debug.Log($"离开 {itemName}");
    }
    
    public void Pickup()
    {
        if (!isPickable || !isInRange) return;
        
        Debug.Log($"拾取物品: {itemName}");
        
        // 这里可以集成到背包系统
        // InventoryManager.Instance.AddItem(this);
        
        // 播放拾取音效
        // AudioManager.Instance.PlaySound("pickup");
        
        // 销毁物品
        Destroy(gameObject);
    }
    
    public void Use()
    {
        if (!isConsumable) return;
        
        if (Time.time - lastUseTime < cooldown)
        {
            Debug.Log($"{itemName} 还在冷却中");
            return;
        }
        
        Debug.Log($"使用物品: {itemName}");
        
        // 应用效果
        ApplyUseEffect();
        
        lastUseTime = Time.time;
        
        // 如果是消耗品，减少数量或销毁
        if (isConsumable)
        {
            // 这里可以集成到背包系统
            // InventoryManager.Instance.RemoveItem(this, 1);
        }
    }
    
    public void Equip()
    {
        if (!isEquippable) return;
        
        Debug.Log($"装备物品: {itemName}");
        
        // 这里可以集成到装备系统
        // EquipmentManager.Instance.EquipItem(this);
        
        // 应用装备效果
        ApplyEquipEffects();
    }
    
    public void Unequip()
    {
        if (!isEquippable) return;
        
        Debug.Log($"卸下物品: {itemName}");
        
        // 移除装备效果
        RemoveEquipEffects();
        
        // 这里可以集成到装备系统
        // EquipmentManager.Instance.UnequipItem(this);
    }
    
    void ApplyUseEffect()
    {
        switch (useEffect.ToLower())
        {
            case "heal":
                // 治疗效果
                var playerHealth = player?.GetComponent<RPGCharacter>();
                if (playerHealth != null)
                {
                    playerHealth.Heal(50);
                }
                break;
            case "mana":
                // 魔法恢复
                Debug.Log("恢复魔法值");
                break;
            case "buff":
                // 增益效果
                Debug.Log("获得增益效果");
                break;
            default:
                Debug.Log($"应用效果: {useEffect}");
                break;
        }
    }
    
    void ApplyEquipEffects()
    {
        if (effects == null) return;
        
        foreach (var effect in effects)
        {
            Debug.Log($"装备效果: {effect.effectName} +{effect.value}");
            // 这里可以应用到角色属性系统
        }
    }
    
    void RemoveEquipEffects()
    {
        if (effects == null) return;
        
        foreach (var effect in effects)
        {
            Debug.Log($"移除效果: {effect.effectName} -{effect.value}");
            // 这里可以从角色属性系统移除
        }
    }
    
    void UpdateGlow()
    {
        if (itemRenderer != null && originalMaterial != null)
        {
            // 简单的发光效果
            Color emissionColor = glowColor * glowIntensity;
            itemRenderer.material.SetColor("_EmissionColor", emissionColor);
        }
    }
    
    // 获取物品信息
    public string GetItemInfo()
    {
        string info = $"{itemName}\n";
        info += $"类型: {itemType}\n";
        info += $"稀有度: {rarity}\n";
        info += $"价值: {value}\n";
        info += $"重量: {weight}\n";
        
        if (!string.IsNullOrEmpty(description))
        {
            info += $"描述: {description}\n";
        }
        
        if (effects != null && effects.Length > 0)
        {
            info += "效果:\n";
            foreach (var effect in effects)
            {
                info += $"  {effect.effectName}: {effect.value}\n";
            }
        }
        
        return info;
    }
    
    // 编辑器辅助方法
    void OnDrawGizmosSelected()
    {
        // 显示拾取范围
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
        
        // 显示稀有度颜色
        Gizmos.color = glowColor;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
} 