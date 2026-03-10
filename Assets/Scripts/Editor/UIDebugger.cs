using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIDebugger : EditorWindow
{
    [MenuItem("Tools/UI调试器")]
    public static void ShowWindow()
    {
        GetWindow<UIDebugger>("UI调试器");
    }

    private void OnGUI()
    {
        GUILayout.Label("UI组件调试工具", EditorStyles.boldLabel);
        
        if (GUILayout.Button("扫描所有UI组件"))
        {
            ScanAllUIComponents();
        }
        
        if (GUILayout.Button("检查Canvas结构"))
        {
            CheckCanvasStructure();
        }
        
        if (GUILayout.Button("创建标准UI"))
        {
            CreateStandardUI();
        }
        
        if (GUILayout.Button("创建EventSystem"))
        {
            CreateEventSystem();
        }
    }
    
    private void ScanAllUIComponents()
    {
        Debug.Log("=== 扫描所有UI组件 ===");
        
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        Debug.Log($"找到 {allCanvases.Length} 个Canvas");
        
        Text[] texts = FindObjectsOfType<Text>();
        Debug.Log($"找到 {texts.Length} 个Text组件");
        
        foreach (var text in texts)
        {
            Debug.Log($"Text: {text.name} (在 {text.transform.parent?.name ?? "根"})");
        }
        
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            Debug.Log($"找到EventSystem: {eventSystem.name}");
        }
        else
        {
            Debug.Log("未找到EventSystem");
        }
    }
    
    private void CheckCanvasStructure()
    {
        Debug.Log("=== 检查Canvas结构 ===");
        
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            Debug.Log($"Canvas: {canvas.name}");
            Transform[] children = canvas.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child != canvas.transform)
                {
                    string indent = "  ";
                    Transform parent = child.parent;
                    while (parent != canvas.transform && parent != null)
                    {
                        indent += "  ";
                        parent = parent.parent;
                    }
                    Debug.Log($"{indent}{child.name}");
                    
                    // 检查是否有Text组件
                    Text text = child.GetComponent<Text>();
                    if (text != null)
                    {
                        Debug.Log($"{indent}  -> 包含Text组件");
                    }
                }
            }
            
            // 专门列出所有Text组件
            Text[] texts = canvas.GetComponentsInChildren<Text>();
            Debug.Log($"Canvas {canvas.name} 中的Text组件:");
            foreach (Text text in texts)
            {
                string path = GetGameObjectPath(text.gameObject, canvas.gameObject);
                Debug.Log($"  {path}");
            }
        }
    }
    
    private string GetGameObjectPath(GameObject obj, GameObject root)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null && parent.gameObject != root)
        {
            path = parent.name + " -> " + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    private void CreateEventSystem()
    {
        Debug.Log("=== 创建EventSystem ===");
        
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystem = eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
            Debug.Log("创建了EventSystem");
        }
        else
        {
            Debug.Log($"EventSystem已存在: {eventSystem.name}");
        }
    }
    
    private void CreateStandardUI()
    {
        Debug.Log("=== 创建标准UI ===");
        
        // 确保有EventSystem
        CreateEventSystem();
        
        // 查找或创建Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("ItemCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("创建了Canvas: ItemCanvas");
        }
        else
        {
            Debug.Log($"使用现有Canvas: {canvas.name}");
        }
        
        // 创建对话UI
        GameObject dialoguePanel = new GameObject("DialoguePanel");
        dialoguePanel.transform.SetParent(canvas.transform, false);
        
        Image panelImage = dialoguePanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.1f);
        panelRect.anchorMax = new Vector2(0.9f, 0.3f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        GameObject textGO = new GameObject("DialoguePanelText");
        textGO.transform.SetParent(dialoguePanel.transform, false);
        
        Text dialogueText = textGO.AddComponent<Text>();
        dialogueText.text = "测试对话内容";
        dialogueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        dialogueText.fontSize = 18;
        dialogueText.color = Color.white;
        dialogueText.alignment = TextAnchor.MiddleLeft;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);
        
        // 确保对话面板一开始是禁用的
        dialoguePanel.SetActive(false);
        
        Debug.Log("创建了对话UI: ItemCanvas -> DialoguePanel -> DialoguePanelText (初始禁用)");
        
        // 创建收集UI
        GameObject collectPanel = new GameObject("CollectPanel");
        collectPanel.transform.SetParent(canvas.transform, false);
        
        Image collectImage = collectPanel.AddComponent<Image>();
        collectImage.color = new Color(0, 0.5f, 0, 0.8f);
        
        RectTransform collectRect = collectPanel.GetComponent<RectTransform>();
        collectRect.anchorMin = new Vector2(0.3f, 0.7f);
        collectRect.anchorMax = new Vector2(0.7f, 0.8f);
        collectRect.offsetMin = Vector2.zero;
        collectRect.offsetMax = Vector2.zero;
        
        GameObject collectTextGO = new GameObject("CollectText");
        collectTextGO.transform.SetParent(collectPanel.transform, false);
        
        Text collectText = collectTextGO.AddComponent<Text>();
        collectText.text = "按 E 拾取道具";
        collectText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        collectText.fontSize = 16;
        collectText.color = Color.white;
        collectText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform collectTextRect = collectTextGO.GetComponent<RectTransform>();
        collectTextRect.anchorMin = Vector2.zero;
        collectTextRect.anchorMax = Vector2.one;
        collectTextRect.offsetMin = new Vector2(5, 5);
        collectTextRect.offsetMax = new Vector2(-5, -5);
        
        // 确保收集面板一开始是禁用的
        collectPanel.SetActive(false);
        
        Debug.Log("创建了收集UI: ItemCanvas -> CollectPanel -> CollectText (初始禁用)");
        
        // 创建背包UI
        GameObject inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(canvas.transform, false);
        
        Image inventoryImage = inventoryPanel.AddComponent<Image>();
        inventoryImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        RectTransform inventoryRect = inventoryPanel.GetComponent<RectTransform>();
        inventoryRect.anchorMin = new Vector2(0.1f, 0.1f);
        inventoryRect.anchorMax = new Vector2(0.9f, 0.9f);
        inventoryRect.offsetMin = Vector2.zero;
        inventoryRect.offsetMax = Vector2.zero;
        
        GameObject inventoryTextGO = new GameObject("InventoryText");
        inventoryTextGO.transform.SetParent(inventoryPanel.transform, false);
        
        Text inventoryText = inventoryTextGO.AddComponent<Text>();
        inventoryText.text = "背包内容:\n背包为空";
        inventoryText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        inventoryText.fontSize = 16;
        inventoryText.color = Color.white;
        inventoryText.alignment = TextAnchor.UpperLeft;
        
        RectTransform inventoryTextRect = inventoryTextGO.GetComponent<RectTransform>();
        inventoryTextRect.anchorMin = Vector2.zero;
        inventoryTextRect.anchorMax = Vector2.one;
        inventoryTextRect.offsetMin = new Vector2(10, 10);
        inventoryTextRect.offsetMax = new Vector2(-10, -10);
        
        // 确保背包面板一开始是禁用的
        inventoryPanel.SetActive(false);
        
        Debug.Log("创建了背包UI: ItemCanvas -> InventoryPanel -> InventoryText (初始禁用)");
        
        Debug.Log("所有标准UI创建完成！所有面板初始状态为禁用");
    }
    
    private void DiagnoseDialogueSystem()
    {
        Debug.Log("=== 诊断对话系统 ===");
        
        // 检查EventSystem
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            Debug.Log($"✅ EventSystem存在: {eventSystem.name}");
        }
        else
        {
            Debug.LogError("❌ EventSystem不存在！");
        }
        
        // 检查Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"找到 {canvases.Length} 个Canvas");
        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"Canvas: {canvas.name} - 激活: {canvas.gameObject.activeSelf}");
        }
        
        // 检查对话面板
        GameObject dialoguePanel = GameObject.Find("DialoguePanel");
        if (dialoguePanel != null)
        {
            Debug.Log($"✅ 找到对话面板: {dialoguePanel.name} - 激活: {dialoguePanel.activeSelf}");
            
            // 检查Text组件
            Text dialogueText = dialoguePanel.GetComponentInChildren<Text>();
            if (dialogueText != null)
            {
                Debug.Log($"✅ 找到对话文本: {dialogueText.name} - 激活: {dialogueText.gameObject.activeSelf}");
            }
            else
            {
                Debug.LogError("❌ 对话面板中没有Text组件！");
            }
        }
        else
        {
            Debug.LogError("❌ 未找到DialoguePanel！");
        }
        
        // 检查MyDialogueSystem组件
        MyDialogueSystem[] dialogueSystems = FindObjectsOfType<MyDialogueSystem>();
        Debug.Log($"找到 {dialogueSystems.Length} 个MyDialogueSystem组件");
        foreach (MyDialogueSystem ds in dialogueSystems)
        {
            Debug.Log($"对话系统: {ds.name} - 激活: {ds.gameObject.activeSelf}");
        }
        
        // 检查Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log($"✅ 找到Player: {player.name}");
        }
        else
        {
            Debug.LogError("❌ 未找到Player标签的对象！");
        }
        
        Debug.Log("=== 诊断完成 ===");
    }
} 