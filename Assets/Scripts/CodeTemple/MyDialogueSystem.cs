using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class MyDialogueSystem : MonoBehaviour
{
    [Header("对话设置")]
    public string[] dialogueLines; // 对话内容数组
    public float triggerDistance = 2f; // 触发距离
    public bool autoStart = true; // 是否自动开始对话
    public bool showDebugInfo = true; // 是否显示调试信息
    
    [Header("触发设置")]
    public string playerTag = "Player"; // 玩家标签
    
    [Header("UI组件 - 请手动指定")]
    public GameObject dialoguePanel; // 对话面板
    public Text dialogueText; // 对话文本（显示dialogueLines内容）
    
    private int currentLine = 0;
    private bool isInDialogue = false;
    private bool isPlayerNearby = false;
    private Transform playerTransform;
    private EventSystem eventSystem;
    
    void Start()
    {
        SetupTriggerCollider();
        SetupEventSystem();
        HideDialogueUI();
        
        // 检查UI组件是否已指定
        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogError($"请手动指定UI组件！dialoguePanel: {(dialoguePanel != null ? "已指定" : "未指定")}, dialogueText: {(dialogueText != null ? "已指定" : "未指定")}");
        }
    }
    
    void Update()
    {
        CheckPlayerDistance();
        
        if (isPlayerNearby && autoStart && !isInDialogue)
        {
            StartDialogue();
        }
        
        if (isInDialogue)
        {
            HandleDialogueInput();
        }
    }
    
    void SetupTriggerCollider()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = triggerDistance;
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
            isPlayerNearby = distance <= triggerDistance;
        }
    }
    
    void ShowDialogueUI()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            Debug.Log("对话面板已显示");
            // 使用EventSystem触发UI显示事件
            if (eventSystem != null)
            {
                try
                {
                    PointerEventData pointerEventData = new PointerEventData(eventSystem);
                    pointerEventData.position = Input.mousePosition;
                    ExecuteEvents.Execute(dialoguePanel, pointerEventData, ExecuteEvents.pointerEnterHandler);
                    Debug.Log("通过EventSystem触发了对话UI显示事件");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"EventSystem触发失败: {e.Message}");
                }
            }
        }
        else
        {
            Debug.LogError("对话面板未指定！");
        }
    }
    
    void HideDialogueUI()
    {
        if (dialoguePanel != null)
        {
            // 使用EventSystem触发UI隐藏事件
            if (eventSystem != null)
            {
                try
                {
                    PointerEventData pointerEventData = new PointerEventData(eventSystem);
                    pointerEventData.position = Input.mousePosition;
                    ExecuteEvents.Execute(dialoguePanel, pointerEventData, ExecuteEvents.pointerExitHandler);
                    Debug.Log("通过EventSystem触发了对话UI隐藏事件");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"EventSystem触发失败: {e.Message}");
                }
            }
            
            dialoguePanel.SetActive(false);
        }
        
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
    }
    
    public void StartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning("没有设置对话内容！");
            return;
        }
        
        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogError("UI组件未指定，无法开始对话！");
            return;
        }
        
        currentLine = 0;
        isInDialogue = true;
        
        // 显示UI
        ShowDialogueUI();
        
        // 显示第一句对话
        ShowCurrentLine();
        
        if (showDebugInfo)
            Debug.Log($"开始与 {gameObject.name} 的对话");
    }
    
    public void NextLine()
    {
        currentLine++;
        if (dialogueLines != null && currentLine < dialogueLines.Length)
        {
            ShowCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }
    
    public void EndDialogue()
    {
        isInDialogue = false;
        currentLine = 0;
        
        // 隐藏对话UI
        HideDialogueUI();
        
        if (showDebugInfo)
            Debug.Log($"结束与 {gameObject.name} 的对话");
    }
    
    private void ShowCurrentLine()
    {
        if (dialogueText != null && dialogueLines != null && currentLine < dialogueLines.Length)
        {
            string currentDialogue = dialogueLines[currentLine];
            dialogueText.text = $"{gameObject.name}: {currentDialogue}";
            
            if (showDebugInfo)
                Debug.Log($"显示对话: {currentDialogue}");
        }
    }
    
    private void HandleDialogueInput()
    {
        // 空格键或鼠标左键继续下一句对话
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            NextLine();
        }
        
        // ESC键结束对话
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EndDialogue();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerNearby = true;
            playerTransform = other.transform;
            
            if (showDebugInfo)
                Debug.Log($"玩家进入 {gameObject.name} 的触发区域");
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerNearby = false;
            
            if (showDebugInfo)
                Debug.Log($"玩家离开 {gameObject.name} 的触发区域");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
    
    // 公共测试方法 - 可以在Inspector中调用或通过代码调用
    [ContextMenu("测试UI显示")]
    public void TestUIShow()
    {
        Debug.Log("=== 手动测试UI显示 ===");
        
        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogError("UI组件未指定，无法测试！");
            return;
        }
        
        // 设置测试对话内容
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            dialogueLines = new string[] { "这是测试对话内容", "第二行测试内容", "第三行测试内容" };
            Debug.Log("设置了测试对话内容");
        }
        
        // 强制显示UI
        ShowDialogueUI();
        
        // 显示测试对话
        dialogueText.text = $"{gameObject.name}: 这是测试对话内容";
        Debug.Log("已设置测试文本内容");
        
        Debug.Log("UI测试完成，请检查UI是否显示");
    }
    
    [ContextMenu("测试UI隐藏")]
    public void TestUIHide()
    {
        Debug.Log("=== 手动测试UI隐藏 ===");
        HideDialogueUI();
        Debug.Log("UI隐藏测试完成");
    }
    
    [ContextMenu("检查UI状态")]
    public void CheckUIStatus()
    {
        Debug.Log("=== 检查UI状态 ===");
        Debug.Log($"对话面板: {(dialoguePanel != null ? dialoguePanel.name : "null")} - 激活: {(dialoguePanel != null ? dialoguePanel.activeSelf : false)}");
        Debug.Log($"对话文本: {(dialogueText != null ? dialogueText.name : "null")} - 激活: {(dialogueText != null ? dialogueText.gameObject.activeSelf : false)}");
        Debug.Log($"EventSystem: {(eventSystem != null ? eventSystem.name : "null")}");
        Debug.Log($"对话内容行数: {(dialogueLines != null ? dialogueLines.Length : 0)}");
        Debug.Log($"自动开始: {autoStart}");
        Debug.Log($"显示调试信息: {showDebugInfo}");
    }
}