using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MyItemCollector : MonoBehaviour
{
    [Header("收集设置")]
    public float collectDistance = 2f;
    public bool autoCollect = false;
    public bool showDebugInfo = true;
    
    [Header("触发设置")]
    public string playerTag = "Player";
    
    [Header("UI组件 - 请手动指定")]
    public GameObject collectPanel;
    public Text collectText;
    public GameObject inventoryPanel;
    public Text inventoryText;
    
    [Header("物品设置")]
    public string itemName = "道具";
    public string itemDescription = "这是一个道具";
    public Sprite itemIcon;
    public int itemValue = 1;
    public bool isConsumable = false;
    
    private bool isPlayerNearby = false;
    private bool isCollected = false;
    private Transform playerTransform;
    private static List<CollectedItem> inventory = new List<CollectedItem>();
    private EventSystem eventSystem;
    
    [System.Serializable]
    public class CollectedItem
    {
        public string name;
        public string description;
        public Sprite icon;
        public int value;
        public bool isConsumable;
        public int quantity;
        
        public CollectedItem(string itemName, string itemDesc, Sprite itemIcon, int itemValue, bool consumable)
        {
            name = itemName;
            description = itemDesc;
            icon = itemIcon;
            value = itemValue;
            isConsumable = consumable;
            quantity = 1;
        }
    }
    
    void Start()
    {
        SetupTriggerCollider();
        SetupEventSystem();
        HideCollectUI();
        HideInventoryUI();
        
        // // 检查UI组件是否已指定
        // if (collectPanel == null || collectText == null)
        // {
        //     Debug.LogError($"请手动指定收集UI组件！collectPanel: {(collectPanel != null ? "已指定" : "未指定")}, collectText: {(collectText != null ? "已指定" : "未指定")}");
        // }
        
        // if (inventoryPanel == null || inventoryText == null)
        // {
        //     Debug.LogError($"请手动指定背包UI组件！inventoryPanel: {(inventoryPanel != null ? "已指定" : "未指定")}, inventoryText: {(inventoryText != null ? "已指定" : "未指定")}");
        // }
    }
    
    void Update()
    {
        CheckPlayerDistance();
        
        if (isPlayerNearby && !isCollected)
        {
            if (Input.GetKeyDown(KeyCode.E) || autoCollect)
            {
                CollectItem();
            }
        }
        
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }
    
    void SetupTriggerCollider()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = collectDistance;
            sphereCollider.isTrigger = true;
        }
        else
        {
            triggerCollider.isTrigger = true;
        }
    }
    
    void SetupEventSystem()
    {
        // 查找或创建EventSystem
        eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystem = eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
            Debug.Log("创建了EventSystem");
        }
    }
    
    void CheckPlayerDistance()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
                playerTransform = player.transform;
        }
        
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            isPlayerNearby = distance <= collectDistance;
        }
    }
    
    void ShowCollectUI()
    {
        if (collectPanel != null)
        {
            collectPanel.SetActive(true);
            Debug.Log("收集面板已显示");    
            // 使用EventSystem触发UI显示事件
            if (eventSystem != null)
            {
                try
                {
                    PointerEventData pointerEventData = new PointerEventData(eventSystem);
                    pointerEventData.position = Input.mousePosition;
                    ExecuteEvents.Execute(collectPanel, pointerEventData, ExecuteEvents.pointerEnterHandler);
                    Debug.Log("通过EventSystem触发了收集UI显示事件");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"EventSystem触发失败: {e.Message}");
                }
            }
        }
        else
        {
            Debug.LogError("收集面板未指定！");
        }
        
        if (collectText != null)
            collectText.text = $"按 E 拾取 {itemName}";
    }
    
    void HideCollectUI()
    {
        if (collectPanel != null)
        {
            // 使用EventSystem触发UI隐藏事件
            if (eventSystem != null)
            {
                try
                {
                    PointerEventData pointerEventData = new PointerEventData(eventSystem);
                    pointerEventData.position = Input.mousePosition;
                    ExecuteEvents.Execute(collectPanel, pointerEventData, ExecuteEvents.pointerExitHandler);
                    Debug.Log("通过EventSystem触发了收集UI隐藏事件");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"EventSystem触发失败: {e.Message}");
                }
            }
            
            collectPanel.SetActive(false);
        }
        
        if (collectText != null)
            collectText.text = "";
    }
    
    void ShowInventoryUI()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            
            // 使用EventSystem触发UI显示事件
            if (eventSystem != null)
            {
                try
                {
                    PointerEventData pointerEventData = new PointerEventData(eventSystem);
                    pointerEventData.position = Input.mousePosition;
                    ExecuteEvents.Execute(inventoryPanel, pointerEventData, ExecuteEvents.pointerEnterHandler);
                    Debug.Log("通过EventSystem触发了背包UI显示事件");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"EventSystem触发失败: {e.Message}");
                }
            }
        }
        else
        {
            Debug.LogError("背包面板未指定！");
        }
        
        UpdateInventoryDisplay();
    }
    
    void HideInventoryUI()
    {
        if (inventoryPanel != null)
        {
            // 使用EventSystem触发UI隐藏事件
            if (eventSystem != null)
            {
                try
                {
                    PointerEventData pointerEventData = new PointerEventData(eventSystem);
                    pointerEventData.position = Input.mousePosition;
                    ExecuteEvents.Execute(inventoryPanel, pointerEventData, ExecuteEvents.pointerExitHandler);
                    Debug.Log("通过EventSystem触发了背包UI隐藏事件");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"EventSystem触发失败: {e.Message}");
                }
            }
            
            inventoryPanel.SetActive(false);
        }
    }
    
    void UpdateInventoryDisplay()
    {
        if (inventoryText != null)
        {
            string inventoryContent = "背包内容:\n";
            
            if (inventory.Count == 0)
            {
                inventoryContent += "背包为空";
            }
            else
            {
                foreach (CollectedItem item in inventory)
                {
                    inventoryContent += $"{item.name} x{item.quantity}\n";
                    if (!string.IsNullOrEmpty(item.description))
                    {
                        inventoryContent += $"  {item.description}\n";
                    }
                }
            }
            
            inventoryText.text = inventoryContent;
        }
    }
    
    public void CollectItem()
    {
        if (isCollected)
            return;
        
        AddToInventory();
        isCollected = true;
        ShowCollectUI();
        gameObject.SetActive(false);
        
        if (showDebugInfo)
            Debug.Log($"成功拾取 {itemName}！");
    }
    
    void AddToInventory()
    {
        CollectedItem existingItem = inventory.Find(item => item.name == itemName);
        
        if (existingItem != null)
        {
            existingItem.quantity++;
        }
        else
        {
            CollectedItem newItem = new CollectedItem(itemName, itemDescription, itemIcon, itemValue, isConsumable);
            inventory.Add(newItem);
        }
        
        UpdateInventoryDisplay();
    }
    
    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            bool isVisible = inventoryPanel.activeSelf;
            if (isVisible)
                HideInventoryUI();
            else
                ShowInventoryUI();
        }
    }
    
    public static List<CollectedItem> GetInventory()
    {
        return new List<CollectedItem>(inventory);
    }
    
    public static void ClearInventory()
    {
        inventory.Clear();
    }
    
    public static bool UseItem(string itemName)
    {
        CollectedItem item = inventory.Find(i => i.name == itemName);
        if (item != null)
        {
            if (item.isConsumable)
            {
                item.quantity--;
                if (item.quantity <= 0)
                {
                    inventory.Remove(item);
                }
            }
            return true;
        }
        return false;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !isCollected)
        {
            isPlayerNearby = true;
            playerTransform = other.transform;
            ShowCollectUI();
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerNearby = false;
            HideCollectUI();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectDistance);
    }
    
    // 公共测试方法
    [ContextMenu("测试收集UI显示")]
    public void TestCollectUIShow()
    {
        Debug.Log("=== 手动测试收集UI显示 ===");
        
        if (collectPanel == null || collectText == null)
        {
            Debug.LogError("收集UI组件未指定，无法测试！");
            return;
        }
        
        ShowCollectUI();
        Debug.Log("收集UI测试完成，请检查UI是否显示");
    }
    
    [ContextMenu("测试背包UI显示")]
    public void TestInventoryUIShow()
    {
        Debug.Log("=== 手动测试背包UI显示 ===");
        
        if (inventoryPanel == null || inventoryText == null)
        {
            Debug.LogError("背包UI组件未指定，无法测试！");
            return;
        }
        
        ShowInventoryUI();
        Debug.Log("背包UI测试完成，请检查UI是否显示");
    }
} 