using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

    
    /// <summary>
/// 机制设计器 - 提供代码生成和脚本挂载功能
/// 这是一个纯功能类，所有UI渲染都在MultiAgentRPGEditor中
    /// </summary>
public static class MechanismDesigner
{
    // 静态数据，用于存储代码生成状态
    public static string codeInput = "";
    public static Vector2 codeInputScrollPosition;
    public static bool isGeneratingCode = false;
    public static string codeGenerationResult = "";
    public static Vector2 codeResultScrollPosition;
    public static bool autoAttachToObjects = true; // 自动挂载到对象的开关
    
    /// <summary>
    /// 只负责机制输入、场景对象收集、LLM通信和响应JSON提取
    /// </summary>
    public static async Task<Dictionary<string, object>> GenerateCodeAndGetResult(MultiAgentRPGEditor editor)
    {
        if (string.IsNullOrEmpty(codeInput))
        {
            EditorUtility.DisplayDialog("错误", "请输入机制描述", "确定");
            return null;
        }
        isGeneratingCode = true;
        editor.Repaint();
        try
        {
            // 收集场景对象信息
            List<Dictionary<string, string>> sceneObjectsInfo = new List<Dictionary<string, string>>();
            if (editor?.spawnedKeyObjects != null && editor.spawnedKeyObjects.Count > 0)
            {
                foreach (var obj in editor.spawnedKeyObjects)
        {
            if (obj != null)
            {
                        Dictionary<string, string> objInfo = new Dictionary<string, string>();
                        objInfo["name"] = obj.name;
                var metadata = obj.GetComponent<KeyObjectMetadata>();
                if (metadata != null)
                {
                            objInfo["type"] = metadata.objectType;
                            objInfo["displayName"] = metadata.displayName;
                            objInfo["description"] = metadata.description;
                        }
                        else if (obj.GetComponent<MainCharacterController>() != null)
                        {
                            objInfo["type"] = "MainCharacter";
                            objInfo["displayName"] = obj.name;
                            objInfo["description"] = "主角角色";
                        }
                        else if (obj.GetComponent<NPCController>() != null)
                        {
                            objInfo["type"] = "NPC";
                            var npc = obj.GetComponent<NPCController>();
                            objInfo["displayName"] = npc.npcName;
                            objInfo["description"] = "NPC角色";
                        }
                        else if (obj.GetComponent<EnemyController>() != null)
                        {
                            objInfo["type"] = "Enemy";
                            var enemy = obj.GetComponent<EnemyController>();
                            objInfo["displayName"] = enemy.enemyName;
                            objInfo["description"] = "敌人角色";
                        }
                        else if (obj.GetComponent<PropsController>() != null)
                        {
                            objInfo["type"] = "Props";
                            var prop = obj.GetComponent<PropsController>();
                            objInfo["displayName"] = prop.itemName;
                            objInfo["description"] = prop.description;
                        }
                        else if (obj.GetComponent<MyItemCollector>() != null)
                        {
                            objInfo["type"] = "ItemCollector";
                            var collector = obj.GetComponent<MyItemCollector>();
                            objInfo["displayName"] = collector.itemName;
                            objInfo["description"] = collector.itemDescription;
                        }
                        else
                        {
                            objInfo["type"] = "Unknown";
                        }
                        sceneObjectsInfo.Add(objInfo);
                    }
                }
            }
            else
            {
                // 从场景中查找关键对象
                var mainCharacters = UnityEngine.Object.FindObjectsByType<MainCharacterController>(FindObjectsSortMode.None);
                var npcs = UnityEngine.Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);
                var enemies = UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
                var props = UnityEngine.Object.FindObjectsByType<PropsController>(FindObjectsSortMode.None);
                var itemCollectors = UnityEngine.Object.FindObjectsByType<MyItemCollector>(FindObjectsSortMode.None);
                var metadataObjects = UnityEngine.Object.FindObjectsByType<KeyObjectMetadata>(FindObjectsSortMode.None);
                
                foreach (var character in mainCharacters)
                {
                    Dictionary<string, string> objInfo = new Dictionary<string, string>();
                    objInfo["name"] = character.gameObject.name;
                    objInfo["type"] = "MainCharacter";
                    objInfo["displayName"] = character.gameObject.name;
                    objInfo["description"] = "主角角色";
                    sceneObjectsInfo.Add(objInfo);
                }
                foreach (var npc in npcs)
                {
                    Dictionary<string, string> objInfo = new Dictionary<string, string>();
                    objInfo["name"] = npc.gameObject.name;
                    objInfo["type"] = "NPC";
                    objInfo["displayName"] = npc.npcName;
                    objInfo["description"] = "NPC角色";
                    sceneObjectsInfo.Add(objInfo);
                }
                foreach (var enemy in enemies)
                {
                    Dictionary<string, string> objInfo = new Dictionary<string, string>();
                    objInfo["name"] = enemy.gameObject.name;
                    objInfo["type"] = "Enemy";
                    objInfo["displayName"] = enemy.enemyName;
                    objInfo["description"] = "敌人角色";
                    sceneObjectsInfo.Add(objInfo);
                }
                foreach (var prop in props)
                {
                    Dictionary<string, string> objInfo = new Dictionary<string, string>();
                    objInfo["name"] = prop.gameObject.name;
                    objInfo["type"] = "Props";
                    objInfo["displayName"] = prop.itemName;
                    objInfo["description"] = prop.description;
                    sceneObjectsInfo.Add(objInfo);
                }
                foreach (var collector in itemCollectors)
                {
                    Dictionary<string, string> objInfo = new Dictionary<string, string>();
                    objInfo["name"] = collector.gameObject.name;
                    objInfo["type"] = "ItemCollector";
                    objInfo["displayName"] = collector.itemName;
                    objInfo["description"] = collector.itemDescription;
                    sceneObjectsInfo.Add(objInfo);
                }
                foreach (var meta in metadataObjects)
                {
                    bool alreadyAdded = false;
                    foreach (var info in sceneObjectsInfo)
                    {
                        if (info["name"] == meta.gameObject.name)
                        {
                            alreadyAdded = true;
                            break;
                        }
                    }
                    if (!alreadyAdded)
                    {
                        Dictionary<string, string> objInfo = new Dictionary<string, string>();
                        objInfo["name"] = meta.gameObject.name;
                        objInfo["type"] = meta.objectType;
                        objInfo["displayName"] = meta.displayName;
                        objInfo["description"] = meta.description;
                        sceneObjectsInfo.Add(objInfo);
                    }
                }
            }
            // 构建输入数据
            string requestJson = "{";
            requestJson += "\"input\": " + JsonUtility.ToJson(new { description = codeInput }) + ",";
            requestJson += "\"scene_objects\": [";
            for (int i = 0; i < sceneObjectsInfo.Count; i++)
            {
                var objInfo = sceneObjectsInfo[i];
                requestJson += "{";
                requestJson += "\"name\": \"" + objInfo["name"] + "\",";
                requestJson += "\"type\": \"" + objInfo["type"] + "\",";
                requestJson += "\"displayName\": \"" + (objInfo.ContainsKey("displayName") ? objInfo["displayName"] : "") + "\",";
                requestJson += "\"description\": \"" + (objInfo.ContainsKey("description") ? objInfo["description"] : "") + "\"";
                requestJson += "}";
                if (i < sceneObjectsInfo.Count - 1)
                {
                    requestJson += ",";
                }
            }
            requestJson += "],";
            requestJson += "\"template_info\": {";
            requestJson += "\"available_templates\": [";
            string templateDir = "Assets/Templates/CodeTemplates";
            string[] templates = Directory.GetFiles(templateDir, "*.template");
            for (int i = 0; i < templates.Length; i++)
            {
                string templateName = Path.GetFileNameWithoutExtension(templates[i]);
                requestJson += "\"" + templateName + "\"";
                if (i < templates.Length - 1)
                {
                    requestJson += ",";
                }
            }
            requestJson += "]";
            requestJson += "}";
            requestJson += "}";
            // 调用CodeGen Agent
            string llmResponse = await AgentCommunication.CallPythonAgent("codegen", requestJson);
            // 提取JSON
            string jsonStr = ExtractJsonFromResponse(llmResponse);
            codeGenerationResult = jsonStr;
            var jsonResult = MiniJSON.Json.Deserialize(jsonStr) as Dictionary<string, object>;
            return jsonResult;
        }
        catch (Exception e)
        {
            Debug.LogError($"[MechanismDesigner] 生成代码时出错: {e.Message}\n{e.StackTrace}");
            codeGenerationResult = $"错误: {e.Message}";
            return null;
        }
        finally
        {
            isGeneratingCode = false;
            editor.Repaint();
        }
    }

    // 提取JSON代码块
    private static string ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrEmpty(response)) return "";
        if (response.Contains("```json") || response.Contains("```"))
        {
            var codeBlockMatch = System.Text.RegularExpressions.Regex.Match(response, "```(?:json)?\\s*\\n(.*?)\\n```", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (codeBlockMatch.Success)
            {
                return codeBlockMatch.Groups[1].Value.Trim();
            }
        }
        return response;
    }
    
    /// <summary>
    /// 验证脚本文件是否存在并且可被加载
    /// </summary>
    public static void VerifyScriptFiles(string path, string fileName)
    {
        // 检查文件是否存在
        string fullPath = Path.Combine(path, fileName);
        if (File.Exists(fullPath))
        {
            Debug.Log($"✅ 脚本文件存在: {fullPath}");
            codeGenerationResult += $"\n[调试信息] 脚本文件存在: {fullPath}";
            
            // 检查是否可加载为MonoScript
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(fullPath.Replace('\\', '/'));
            if (script != null)
            {
                Debug.Log($"✅ 脚本可作为MonoScript加载: {script.name}");
                codeGenerationResult += $"\n[调试信息] 脚本可作为MonoScript加载: {script.name}";
                
                // 检查类型
                var scriptType = script.GetClass();
                if (scriptType != null)
                {
                    Debug.Log($"✅ 脚本类型有效: {scriptType.FullName}");
                    codeGenerationResult += $"\n[调试信息] 脚本类型有效: {scriptType.FullName}, 是否继承MonoBehaviour: {typeof(MonoBehaviour).IsAssignableFrom(scriptType)}";
                    
                    // 列出脚本的所有公共方法
                    var methods = scriptType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
                    if (methods.Length > 0)
                    {
                        Debug.Log($"✅ 脚本包含 {methods.Length} 个公共方法");
                        codeGenerationResult += $"\n[调试信息] 脚本包含 {methods.Length} 个公共方法:";
                        foreach (var method in methods.Take(5)) // 只显示前5个，避免过多输出
                        {
                            codeGenerationResult += $"\n  - {method.Name}";
                        }
                        if (methods.Length > 5)
                        {
                            codeGenerationResult += $"\n  - ... 还有 {methods.Length - 5} 个方法未显示";
                        }
                                }
                                else
                                {
                        Debug.LogWarning("⚠️ 脚本没有公共方法");
                        codeGenerationResult += "\n[调试信息] ⚠️ 脚本没有公共方法";
                                }
                            }
                            else
                            {
                    Debug.LogError("❌ 无法获取脚本类型");
                    codeGenerationResult += "\n[调试信息] ❌ 无法获取脚本类型 - 可能是脚本有编译错误";
                    
                    // 检查脚本编译状态
                    // 注意：AssetDatabase.GetImportErrors在某些Unity版本中不可用
                    // 通过检查Console日志或直接读取文件内容来尝试调试
                    Debug.LogError($"❌ 无法获取脚本类型 - 可能是脚本有编译错误");
                    codeGenerationResult += $"\n[调试信息] ❌ 无法获取脚本类型 - 可能是脚本有编译错误";
                    codeGenerationResult += $"\n[调试信息] 请检查Unity控制台中的编译错误信息";
                    
                    // 尝试读取脚本内容提供更多信息
                    try
                    {
                        string fileContent = File.ReadAllText(fullPath);
                        int contentLength = fileContent.Length;
                        string preview = contentLength > 200 ? fileContent.Substring(0, 200) + "..." : fileContent;
                        codeGenerationResult += $"\n[调试信息] 脚本内容预览 ({contentLength} 字节): {preview}";
                        }
                        catch (Exception ex)
                        {
                        codeGenerationResult += $"\n[调试信息] 读取脚本内容时出错: {ex.Message}";
                    }
                        }
                    }
                    else
                    {
                Debug.LogError("❌ 无法加载脚本为MonoScript");
                codeGenerationResult += "\n[调试信息] ❌ 无法加载脚本为MonoScript - 可能是AssetDatabase尚未刷新";
                
                // 检查文件内容
                try
                {
                    string fileContent = File.ReadAllText(fullPath);
                    int contentLength = fileContent.Length;
                    string preview = contentLength > 100 ? fileContent.Substring(0, 100) + "..." : fileContent;
                    Debug.Log($"文件内容预览 ({contentLength} 字节): {preview}");
                    codeGenerationResult += $"\n[调试信息] 文件内容预览 ({contentLength} 字节): {preview}";
                }
                catch (Exception ex)
                {
                    Debug.LogError($"读取文件内容时出错: {ex.Message}");
                    codeGenerationResult += $"\n[调试信息] 读取文件内容时出错: {ex.Message}";
                }
            }
        }
        else
        {
            Debug.LogError($"❌ 脚本文件不存在: {fullPath}");
            codeGenerationResult += $"\n[调试信息] ❌ 脚本文件不存在: {fullPath}";
            
            // 检查目录是否存在
            if (!Directory.Exists(path))
            {
                Debug.LogError($"❌ 输出目录不存在: {path}");
                codeGenerationResult += $"\n[调试信息] ❌ 输出目录不存在: {path}";
                    }
                    else
                    {
                // 列出目录中的文件
                var files = Directory.GetFiles(path);
                Debug.Log($"目录 {path} 中包含 {files.Length} 个文件");
                codeGenerationResult += $"\n[调试信息] 目录 {path} 中包含 {files.Length} 个文件:";
                foreach (var file in files.Take(5)) // 只显示前5个，避免过多输出
                {
                    codeGenerationResult += $"\n  - {Path.GetFileName(file)}";
                }
                if (files.Length > 5)
                {
                    codeGenerationResult += $"\n  - ... 还有 {files.Length - 5} 个文件未显示";
                }
            }
        }
    }
    
    // 删除 ApplyCodeGeneration、AttachScriptToObjects、InitializeComponentFromMetadata、UpdatePrefabs 相关实现和方法声明
    // 只保留机制设计相关内容
    }
    
    /// <summary>
/// 机制设计管理器 - 提供机制设计和保存功能
    /// </summary>
public static class MechanismDesignManager
{
    /// <summary>
    /// 生成机制描述
    /// </summary>
    public static string GenerateMechanicsDescription(Dictionary<string, object> mechanicsData)
    {
        var description = "=== RPG MECHANICS CONFIGURATION ===\n\n";

        foreach (var mechanism in mechanicsData)
        {
            description += $"## {mechanism.Key.ToUpper()} MECHANICS\n";
            
            if (mechanism.Value is Dictionary<string, object> parameters)
            {
                foreach (var parameter in parameters)
                {
                    description += $"• {parameter.Key}: {parameter.Value}\n";
                }
            }
            
            description += "\n";
        }

        description += "=== IMPLEMENTATION NOTES ===\n";
        description += "• Combat: Implement turn-based system with damage calculations\n";
        description += "• Exploration: Create open world with fast travel points\n";
        description += "• Social: Add dialogue system with reputation tracking\n";
        description += "• Economy: Balance currency and trading mechanics\n";
        description += "• Progression: Design skill trees and leveling system\n";

        return description;
    }
    
    /// <summary>
    /// 保存机制到文件
    /// </summary>
    public static void SaveMechanicsToFile(string mechanicsResult)
    {
        if (string.IsNullOrEmpty(mechanicsResult))
        {
            UnityEditor.EditorUtility.DisplayDialog("Warning", "No mechanics to save. Generate mechanics first.", "OK");
            return;
        }

        var path = UnityEditor.EditorUtility.SaveFilePanel("Save Mechanics", "Assets", "rpg_mechanics", "txt");
        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, mechanicsResult);
            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.EditorUtility.DisplayDialog("Success", "Mechanics saved successfully!", "OK");
        }
    }
    
    /// <summary>
    /// 导出机制为JSON
    /// </summary>
    public static void ExportMechanicsAsJSON(string mechanicsResult)
    {
        if (string.IsNullOrEmpty(mechanicsResult))
        {
            UnityEditor.EditorUtility.DisplayDialog("Warning", "No mechanics to export. Generate mechanics first.", "OK");
            return;
        }

        var exportData = new Dictionary<string, object>();
        var json = JsonUtility.ToJson(new { mechanics = exportData }, true);
        
        var path = UnityEditor.EditorUtility.SaveFilePanel("Export Mechanics", "Assets", "rpg_mechanics", "json");
        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, json);
            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.EditorUtility.DisplayDialog("Success", "Mechanics exported as JSON successfully!", "OK");
        }
    }
    
    /// <summary>
    /// 初始化机制类型
    /// </summary>
    public static Dictionary<string, Dictionary<string, object>> InitializeMechanismTypes()
    {
        return new Dictionary<string, Dictionary<string, object>>
        {
            ["Combat"] = new Dictionary<string, object>
            {
                ["Turn-based"] = true,
                ["Real-time"] = false,
                ["Damage System"] = "Physical/Magical",
                ["Critical Hits"] = true,
                ["Status Effects"] = true,
                ["Combat Speed"] = 1.0f
            },
            ["Exploration"] = new Dictionary<string, object>
            {
                ["Open World"] = true,
                ["Linear"] = false,
                ["Fast Travel"] = true,
                ["Hidden Areas"] = true,
                ["Exploration Rewards"] = true,
                ["Map Size"] = "Large"
            },
            ["Social"] = new Dictionary<string, object>
            {
                ["Dialogue System"] = true,
                ["Reputation"] = true,
                ["Factions"] = true,
                ["Relationship System"] = true,
                ["NPC Schedules"] = false,
                ["Social Depth"] = "Medium"
            },
            ["Economy"] = new Dictionary<string, object>
            {
                ["Currency System"] = true,
                ["Trading"] = true,
                ["Crafting"] = true,
                ["Inflation Control"] = false,
                ["Dynamic Pricing"] = false,
                ["Economy Balance"] = "Stable"
            },
            ["Progression"] = new Dictionary<string, object>
            {
                ["Level System"] = true,
                ["Skill Trees"] = true,
                ["Equipment"] = true,
                ["Achievements"] = true,
                ["Prestige System"] = false,
                ["Progression Speed"] = "Normal"
            },
            ["ItemCollection"] = new Dictionary<string, object>
            {
                ["Auto Collect"] = false,
                ["Manual Collect"] = true,
                ["Collect Distance"] = 2.0f,
                ["Inventory System"] = true,
                ["Item Categories"] = true,
                ["Stack Items"] = true,
                ["Item Descriptions"] = true,
                ["Collection UI"] = true,
                ["Inventory UI"] = true
            }
        };
    }
} 