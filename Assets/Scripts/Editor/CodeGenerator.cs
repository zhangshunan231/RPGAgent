using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// 代码模板管理器 - 提供代码模板管理和脚本挂载功能
/// </summary>
public static class CodeTemplateManager
{
    /// <summary>
    /// 获取所有可用的代码模板
    /// </summary>
    public static Dictionary<string, string> GetTemplates()
    {
        // 直接使用CodeTemple文件夹中的脚本，不需要重复定义模板
        return new Dictionary<string, string>
        {
            // 这些模板已经不需要了，直接使用CodeTemple文件夹中的脚本
            ["MainCharacter"] = "使用 MyCharacterController.cs",
            ["NPC"] = "使用 MyDialogueSystem.cs", 
            ["Enemy"] = "使用 MyEnemyAI.cs",
            ["Trader"] = "使用 MyTrader.cs",
            ["Props"] = "使用 MyItemCollector.cs",
            ["ItemCollector"] = "使用 MyItemCollector.cs"
        };
    }
    
    public static readonly string TemplatesPath = "Assets/Templates/CodeTemplates";
    
    [MenuItem("Tools/代码模板管理器")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<CodeTemplateManagerWindow>("代码模板管理器");
    }
    
    // 静态方法，供CodeGenAgent使用
    public static string GenerateCodeFromTemplate(string templateName, Dictionary<string, string> replacements)
    {
        string templatePath = Path.Combine(TemplatesPath, templateName + ".template");
        
        if (!File.Exists(templatePath))
        {
            Debug.LogError($"模板不存在: {templatePath}");
            return null;
        }
        
        string templateContent = File.ReadAllText(templatePath);
        string generatedCode = templateContent;
        
        foreach (var kvp in replacements)
        {
            generatedCode = generatedCode.Replace("{{" + kvp.Key + "}}", kvp.Value);
        }
        
        return generatedCode;
    }
    
    public static void SaveGeneratedCode(string code, string outputPath, string fileName)
    {
        if (string.IsNullOrEmpty(outputPath) || string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("输出路径和文件名不能为空");
            return;
        }
        
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
        
        string fullPath = Path.Combine(outputPath, fileName);
        
        File.WriteAllText(fullPath, code);
        
        AssetDatabase.Refresh();
        
        Debug.Log($"代码已生成到: {fullPath}");
    }
}

/// <summary>
/// 代码模板管理器窗口
/// </summary>
public class CodeTemplateManagerWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private string[] templateFiles;
    private int selectedTemplateIndex = -1;
    private string generatedCode = "";
    private string outputPath = "Assets/Scripts/Generated";
    private string outputFileName = "GeneratedScript.cs";
    private Dictionary<string, string> replacementValues = new Dictionary<string, string>();
    private List<string> placeholders = new List<string>();
    
    private void OnEnable()
    {
        RefreshTemplateList();
    }
    
    private void RefreshTemplateList()
    {
        if (!Directory.Exists(CodeTemplateManager.TemplatesPath))
        {
            Directory.CreateDirectory(CodeTemplateManager.TemplatesPath);
        }
        
        templateFiles = Directory.GetFiles(CodeTemplateManager.TemplatesPath, "*.template");
        
        for (int i = 0; i < templateFiles.Length; i++)
        {
            templateFiles[i] = Path.GetFileNameWithoutExtension(templateFiles[i]);
        }
    }
    
    private void OnGUI()
    {
        GUILayout.Label("代码模板管理器", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("刷新模板列表"))
        {
            RefreshTemplateList();
        }
        
        EditorGUILayout.Space();
        
        GUILayout.Label("选择模板:", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
        
        for (int i = 0; i < templateFiles.Length; i++)
        {
            if (GUILayout.Toggle(selectedTemplateIndex == i, templateFiles[i], EditorStyles.radioButton))
            {
                if (selectedTemplateIndex != i)
                {
                    selectedTemplateIndex = i;
                    LoadTemplate(templateFiles[i]);
                }
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        
        if (selectedTemplateIndex >= 0)
        {
            GUILayout.Label("填充占位符:", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            foreach (var placeholder in placeholders)
            {
                if (!replacementValues.ContainsKey(placeholder))
                {
                    replacementValues[placeholder] = "";
                }
                
                replacementValues[placeholder] = EditorGUILayout.TextField(placeholder, replacementValues[placeholder]);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                UpdateGeneratedCode();
            }
            
            EditorGUILayout.Space();
            
            GUILayout.Label("输出设置:", EditorStyles.boldLabel);
            outputPath = EditorGUILayout.TextField("输出路径", outputPath);
            outputFileName = EditorGUILayout.TextField("文件名", outputFileName);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("生成代码"))
            {
                GenerateCode();
            }
            
            EditorGUILayout.Space();
            
            GUILayout.Label("预览:", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(generatedCode, GUILayout.Height(300));
        }
    }
    
    private void LoadTemplate(string templateName)
    {
        string templatePath = Path.Combine(CodeTemplateManager.TemplatesPath, templateName + ".template");
        
        if (File.Exists(templatePath))
        {
            string templateContent = File.ReadAllText(templatePath);
            
            // 提取所有占位符
            placeholders.Clear();
            replacementValues.Clear();
            
            Regex regex = new Regex(@"\{\{([^{}]+)\}\}");
            MatchCollection matches = regex.Matches(templateContent);
            
            foreach (Match match in matches)
            {
                string placeholder = match.Groups[1].Value;
                if (!placeholders.Contains(placeholder))
                {
                    placeholders.Add(placeholder);
                    replacementValues[placeholder] = GetDefaultValueForPlaceholder(placeholder);
                }
            }
            
            generatedCode = templateContent;
        }
    }
    
    private string GetDefaultValueForPlaceholder(string placeholder)
    {
        // 根据占位符名称提供默认值
        switch (placeholder)
        {
            case "CLASS_NAME":
                return "MyCharacter";
            case "MOVE_SPEED":
                return "5.0f";
            case "JUMP_FORCE":
                return "10.0f";
            case "HEALTH":
                return "100.0f";
            case "ITEM_CLASS_NAME":
                return "Item";
            case "INVENTORY_CLASS_NAME":
                return "Inventory";
            case "INVENTORY_SLOT_CLASS_NAME":
                return "InventorySlot";
            case "ITEM_TYPE_ENUM":
                return "ItemType";
            case "ITEM_TYPES":
                return "Weapon, Armor, Consumable, Quest, Misc";
            case "MAX_STACK_SIZE":
                return "99";
            case "MAX_SLOTS":
                return "20";
            case "MAX_WEIGHT":
                return "100.0f";
            case "WEIGHT":
                return "1.0f";
            case "COMBAT_SYSTEM_CLASS_NAME":
                return "CombatSystem";
            case "ATTACK_DAMAGE":
                return "10.0f";
            case "ATTACK_SPEED":
                return "1.0f";
            case "ATTACK_RANGE":
                return "1.5f";
            case "CRITICAL_CHANCE":
                return "0.1f";
            case "CRITICAL_MULTIPLIER":
                return "2.0f";
            default:
                if (placeholder.StartsWith("ADDITIONAL_"))
                    return "// 在这里添加自定义内容";
                return "";
        }
    }
    
    private void UpdateGeneratedCode()
    {
        string templatePath = Path.Combine(CodeTemplateManager.TemplatesPath, templateFiles[selectedTemplateIndex] + ".template");
        string templateContent = File.ReadAllText(templatePath);
        
        generatedCode = templateContent;
        
        foreach (var kvp in replacementValues)
        {
            generatedCode = generatedCode.Replace("{{" + kvp.Key + "}}", kvp.Value);
        }
    }
    
    private void GenerateCode()
    {
        if (string.IsNullOrEmpty(outputPath) || string.IsNullOrEmpty(outputFileName))
        {
            EditorUtility.DisplayDialog("错误", "输出路径和文件名不能为空", "确定");
            return;
        }
        
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
        
        string fullPath = Path.Combine(outputPath, outputFileName);
        
        File.WriteAllText(fullPath, generatedCode);
        
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("成功", $"代码已生成到: {fullPath}", "确定");
        
        // 打开生成的文件
        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(fullPath, 1);
    }
}

public static class MechanismAttacher
{
    // mechanismType: "MainCharacter", "NPC", "Enemy", "Props", "Dialogue", "CharacterController", "EnemyAI", "Trader"
    // dialogueLines: 仅对对话机制有效
    public static void AttachMechanism(GameObject go, string mechanismType, string[] dialogueLines = null)
    {
        Debug.Log($"[AttachMechanism] 尝试挂载 {mechanismType} 到 {go?.name}");
        string scriptClassName = "";
        
        switch (mechanismType.ToLower())
        {
            case "maincharacter":
            case "charactercontroller":
            case "character":
                scriptClassName = "MyCharacterController";
                break;
            case "npc":
            case "dialogue":
                scriptClassName = "MyDialogueSystem";
                break;
            case "enemy":
            case "enemyai":
                scriptClassName = "MyEnemyAI";
                break;
            case "trader":
                scriptClassName = "MyTrader";
                break;
            case "props":
            case "itemcollector":
            case "collector":
                scriptClassName = "MyItemCollector";
                break;
            default:
                Debug.LogWarning("未知机制类型: " + mechanismType);
                return;
        }

        // 查找类型
        var scriptType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == scriptClassName);

        if (scriptType == null)
        {
            Debug.LogError("找不到脚本类型: " + scriptClassName);
            return;
        }

        // 挂载脚本
        var comp = go.GetComponent(scriptType) ?? go.AddComponent(scriptType);
        Debug.Log($"已挂载脚本 {scriptClassName} 到对象: {go.name}");

        // 对话内容自动赋值
        if (mechanismType.ToLower() == "dialogue" && dialogueLines != null)
        {
            var field = scriptType.GetField("dialogueLines");
            if (field != null)
                field.SetValue(comp, dialogueLines);
        }
        
        // 为NPC设置对话内容
        if (mechanismType.ToLower() == "npc" && dialogueLines != null)
        {
            var field = scriptType.GetField("dialogueLines");
            if (field != null)
                field.SetValue(comp, dialogueLines);
        }
        
        // 为新资产类型设置默认属性
        if (mechanismType.ToLower() == "maincharacter")
        {
            // 设置主角标签
            go.tag = "Player";
        }
        else if (mechanismType.ToLower() == "enemy")
        {
            // 设置敌人标签
            go.tag = "Enemy";
        }
    }
}

/// <summary>
/// 代码生成管理器 - 提供简化的脚本挂载功能
/// </summary>
public static class CodeGenerationManager
{
    /// <summary>
    /// 简化的脚本挂载方法 - 只处理四个基本脚本类型
    /// </summary>
    public static void AttachScriptToObject(GameObject targetObject, string scriptType, Dictionary<string, object> parameters = null)
    {
        if (targetObject == null)
        {
            Debug.LogError("目标对象为空，无法挂载脚本");
            return;
        }

        Debug.Log($"[CodeGenerationManager] 尝试挂载 {scriptType} 到 {targetObject.name}");

        switch (scriptType.ToLower())
        {
            case "charactercontroller":
            case "character":
            case "maincharacter":
                AttachCharacterController(targetObject, parameters);
                break;
            case "dialoguesystem":
            case "dialogue":
            case "npc":
                AttachDialogueSystem(targetObject, parameters);
                break;
            case "enemyai":
            case "enemy":
                AttachEnemyAI(targetObject, parameters);
                break;
            case "trader":
                AttachTrader(targetObject, parameters);
                break;
            case "itemcollector":
            case "collector":
            case "props":
                AttachItemCollector(targetObject, parameters);
                break;
            default:
                Debug.LogWarning($"未知的脚本类型: {scriptType}");
                break;
        }
    }

    private static void AttachCharacterController(GameObject go, Dictionary<string, object> parameters)
    {
        var component = go.GetComponent<MyCharacterController>();
        if (component == null)
        {
            component = go.AddComponent<MyCharacterController>();
        }

        if (parameters != null && parameters.ContainsKey("moveSpeed"))
        {
            if (float.TryParse(parameters["moveSpeed"].ToString(), out float moveSpeed))
            {
                component.moveSpeed = moveSpeed;
            }
        }

        // 设置主角标签
        go.tag = "Player";

        Debug.Log($"已挂载 MyCharacterController 到 {go.name}");
    }

    private static void AttachDialogueSystem(GameObject go, Dictionary<string, object> parameters)
    {
        var component = go.GetComponent<MyDialogueSystem>();
        if (component == null)
        {
            component = go.AddComponent<MyDialogueSystem>();
        }

        if (parameters != null && parameters.ContainsKey("dialogueLines"))
        {
            var lines = parameters["dialogueLines"] as List<object>;
            if (lines != null)
            {
                component.dialogueLines = lines.Select(l => l.ToString()).ToArray();
            }
        }

        Debug.Log($"已挂载 MyDialogueSystem 到 {go.name}");
    }

    private static void AttachEnemyAI(GameObject go, Dictionary<string, object> parameters)
    {
        var component = go.GetComponent<MyEnemyAI>();
        if (component == null)
        {
            component = go.AddComponent<MyEnemyAI>();
        }

        if (parameters != null)
        {
            if (parameters.ContainsKey("health"))
            {
                if (int.TryParse(parameters["health"].ToString(), out int health))
                {
                    component.health = health;
                }
            }
            if (parameters.ContainsKey("attackPower"))
            {
                if (int.TryParse(parameters["attackPower"].ToString(), out int attackPower))
                {
                    component.attackPower = attackPower;
                }
            }
        }

        // 设置敌人标签
        go.tag = "Enemy";

        Debug.Log($"已挂载 MyEnemyAI 到 {go.name}");
    }

    private static void AttachTrader(GameObject go, Dictionary<string, object> parameters)
    {
        var component = go.GetComponent<MyTrader>();
        if (component == null)
        {
            component = go.AddComponent<MyTrader>();
        }

        if (parameters != null && parameters.ContainsKey("gold"))
        {
            if (int.TryParse(parameters["gold"].ToString(), out int gold))
            {
                component.gold = gold;
            }
        }

        Debug.Log($"已挂载 MyTrader 到 {go.name}");
    }

    private static void AttachItemCollector(GameObject go, Dictionary<string, object> parameters)
    {
        var component = go.GetComponent<MyItemCollector>();
        if (component == null)
        {
            component = go.AddComponent<MyItemCollector>();
        }

        if (parameters != null)
        {
            if (parameters.ContainsKey("itemName"))
            {
                component.itemName = parameters["itemName"].ToString();
            }
            if (parameters.ContainsKey("itemDescription"))
            {
                component.itemDescription = parameters["itemDescription"].ToString();
            }
            if (parameters.ContainsKey("collectDistance"))
            {
                if (float.TryParse(parameters["collectDistance"].ToString(), out float distance))
                {
                    component.collectDistance = distance;
                }
            }
            if (parameters.ContainsKey("autoCollect"))
            {
                if (bool.TryParse(parameters["autoCollect"].ToString(), out bool autoCollect))
                {
                    component.autoCollect = autoCollect;
                }
            }
            if (parameters.ContainsKey("itemValue"))
            {
                if (int.TryParse(parameters["itemValue"].ToString(), out int value))
                {
                    component.itemValue = value;
                }
            }
            if (parameters.ContainsKey("isConsumable"))
            {
                if (bool.TryParse(parameters["isConsumable"].ToString(), out bool consumable))
                {
                    component.isConsumable = consumable;
                }
            }
        }

        Debug.Log($"已挂载 MyItemCollector 到 {go.name}");
    }

    /// <summary>
    /// 批量挂载脚本到多个对象
    /// </summary>
    public static void AttachScriptsToObjects(List<GameObject> objects, string scriptType, Dictionary<string, object> parameters = null)
    {
        foreach (var obj in objects)
        {
            AttachScriptToObject(obj, scriptType, parameters);
        }
    }

    /// <summary>
    /// 根据对象名称查找并挂载脚本
    /// </summary>
    public static void AttachScriptToObjectByName(string objectName, string scriptType, Dictionary<string, object> parameters = null)
    {
        var targetObject = GameObject.Find(objectName);
        if (targetObject != null)
        {
            AttachScriptToObject(targetObject, scriptType, parameters);
        }
        else
        {
            Debug.LogWarning($"未找到名为 {objectName} 的对象");
        }
    }
} 