using UnityEngine;

public class PropsController : MonoBehaviour
{
    [Header("道具属性")]
    public string itemName = "道具";
    public string description = "这是一个道具";
    public bool isConsumable = false;
    public bool isEquipment = false;
    public bool isKeyItem = false;
    public int stackSize = 1;
    
    [Header("效果")]
    public int healthRestore = 0;
    public int attackBonus = 0;
    public int defenseBonus = 0;
    public float speedBonus = 0f;
    
    [Header("交互")]
    public bool isInteractable = true;
    public bool autoCollect = false;
    public float interactionRange = 2f;
    
    private bool isCollected = false;
    
    void Start()
    {
        // 如果是关键物品，确保不会被自动收集
        if (isKeyItem)
        {
            autoCollect = false;
        }
    }
    
    void Update()
    {
        if (autoCollect && !isCollected)
        {
            CheckPlayerProximity();
        }
    }
    
    void CheckPlayerProximity()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance <= interactionRange)
            {
                CollectItem();
            }
        }
    }
    
    public void Interact()
    {
        if (isInteractable && !isCollected)
        {
            CollectItem();
        }
    }
    
    void CollectItem()
    {
        if (isCollected) return;
        
        isCollected = true;
        Debug.Log($"收集了物品：{itemName}");
        
        // 应用道具效果
        ApplyItemEffects();
        
        // 添加到玩家背包
        AddToInventory();
        
        // 销毁道具对象
        Destroy(gameObject);
    }
    
    void ApplyItemEffects()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var playerController = player.GetComponent<MainCharacterController>();
            if (playerController != null)
            {
                if (healthRestore > 0)
                {
                    // 恢复生命值
                    // playerController.RestoreHealth(healthRestore);
                    Debug.Log($"恢复了{healthRestore}点生命值");
                }
                
                if (attackBonus > 0 || defenseBonus > 0 || speedBonus > 0)
                {
                    // 应用装备加成
                    Debug.Log($"获得装备加成：攻击+{attackBonus}，防御+{defenseBonus}，速度+{speedBonus}");
                }
            }
        }
    }
    
    void AddToInventory()
    {
        // 这里可以集成背包系统
        // InventoryManager.Instance.AddItem(this);
        Debug.Log($"{itemName}已添加到背包");
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isInteractable && !isCollected)
        {
            if (autoCollect)
            {
                CollectItem();
            }
            else
            {
                Debug.Log($"按E收集{itemName}");
            }
        }
    }
} 