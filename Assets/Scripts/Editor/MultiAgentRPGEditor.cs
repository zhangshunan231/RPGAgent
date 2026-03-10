using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;

public enum TerrainType
{
    Water,
    Grass,
    Mountain
}

public class MultiAgentRPGEditor : EditorWindow
{

    public string userInput = "";
    public string narrativeResult = "";
    public string sceneResult = "";
    public string mechanicsResult = "";
    public string mechanicsImplementationResult = "";
    
    public bool isNarrativeComplete = false;
    public bool isSceneComplete = false;
    public bool isMechanicsComplete = false;
    
    public bool isGeneratingNarrative = false;
    public bool isExtractingSceneParams = false;
    public bool isGeneratingMechanics = false;
    
    
    
    public Vector2 scrollPosition;
    public string serverStatus = "Disconnected";
    
    // Tab相关
    public int selectedTab = 0;
    public string[] tabNames = { "Narrative Generation", "Scene Generation", "Mechanics Generation" };
    
    // 滚动条位置
    public Vector2 storyScrollPos = Vector2.zero;
    public Vector2 stepsScrollPos = Vector2.zero;
    public Vector2 sceneScrollPos = Vector2.zero;
    public Vector2 mechanicsScrollPos = Vector2.zero;
    
    // 自动换行样式
    public GUIStyle wordWrapStyle;
    public GUIStyle headerStyle;
    
    public static readonly HttpClient httpClient = new HttpClient();
    public const string SERVER_URL = "http://127.0.0.1:5000";
    
    // 1. 在类内添加结构体
    [System.Serializable]
    public class NarrativeStep
    {
        public int step;
        public LocationType location;
        public string objective;
        public List<string> key_characters;
        public List<Dialogue> main_dialogues;
        public List<string> key_items;
        public string title; // 新增：用于存储步骤标题
    }
    [System.Serializable]
    public class Dialogue
    {
        public string character;
        public string dialogue;
    }
    [System.Serializable]
    public class NarrativeData
    {
        public string story;
        public List<NarrativeStep> steps;
    }

    // 2. 在 MultiAgentRPGEditor 类中添加字段
    public NarrativeData narrativeData = null;
    public List<Vector2> stepScrollPos = new List<Vector2>();
    
    // 地图参数字段
    public int mapWidth = 100;
    public int mapHeight = 100;
    public float noiseScale = 20f;
    public float landThreshold = 0.5f;
    public float islandFactor = 1.2f;
    public float mountainThreshold = 0.75f;
    public int cliffHeight = 2;
    public int randomSeed = 0; // 通用随机种子
    public int landSeed = 0;
    public int mountainSeed = 1;
    public float perlinScale = 3f;
    public float perlinStrength = 3f;
    
    // Tilemap 配置文件（永久化保存）
    [SerializeField]
    [Tooltip("Tilemap配置文件，保存所有Tilemap和RuleTile的引用")]
    public TilemapConfig tilemapConfig;
    
    // 兼容旧版本：如果没有配置文件，可以直接拖拽
    public Tilemap waterTilemap;
    public Tilemap landTilemap;
    public Tilemap mountainTilemap;
    public Tilemap cliffTilemap;
    public Tilemap elementsTilemap;
    public TileBase waterRuleTile;
    public TileBase landRuleTile;
    public TileBase mountainRuleTile;
    public TileBase cliffRuleTile;
    
    public Texture2D previewTex;
    public Texture2D partitionPreviewTex;
    
    // 1. 相关变量
    public Color[] voronoiColors;
    public System.Random previewRandom;

    [SerializeField]
    public List<ElementDistributionConfig> areaElementConfigs = new List<ElementDistributionConfig>();
    
    [SerializeField]
    public List<PartitionRuleTileConfig> partitionRuleTileConfigs = new List<PartitionRuleTileConfig>();
    
    // 在类字段区添加：
    // 1. 在类字段区定义LocationType枚举
    // 2. 删除类内部的private enum LocationType。
    // 3. NarrativeStep等所有用到LocationType的地方直接用public LocationType。
    public bool showMapParams = true;
    public bool showTileMapParams = false;
    public bool showRuleTileParams = false;
    public bool showElementParams = false;
    public bool showPartitionRuleTileParams = false;
    public bool showKeyItemParams = false;
    public bool enableAreaBalancing = true; // 区域平衡优化

    // AssetIndex引用
    [SerializeField]
    public AssetIndex assetIndex;
    public List<GameObject> spawnedKeyObjects = new List<GameObject>();

    // 机制生成相关字段
    // 移除characterMechanicsResults和itemMechanicsResults字段
    // 场景Agent参数结构
    [System.Serializable]
    public class SceneParamResult {
        public float noiseScale;
        public float landThreshold;
        public float mountainThreshold;
    }
    
    [System.Serializable]
    public class SelectedAsset {
        public int step;
        public List<string> assets;
    }
    
    // 新增：用于解析LLM返回的根结构
    [System.Serializable]
    public class SceneAgentResponse {
        public SceneParamResult scene_params;
        public List<SelectedAsset> selected_assets;
    }
    
    // 新增：分区RuleTile配置
    [System.Serializable]
    public class PartitionRuleTileConfig
    {
        public int stepIndex; // 对应的步骤索引
        public TileBase waterRuleTile;
        public TileBase landRuleTile;
        public TileBase mountainRuleTile;
        public TileBase cliffRuleTile;
        
        public PartitionRuleTileConfig(int step)
        {
            stepIndex = step;
        }
    }
    
    // 缓存Scene Agent的提取结果
    public SceneAgentResponse cachedSceneAgentResult = null;

    // 场景Agent LLM参数提取接口
    public async void ExtractSceneParamsWithLLM(string story, List<NarrativeStep> steps)
    {
        if (string.IsNullOrEmpty(story) || steps == null) return;
        isExtractingSceneParams = true;
        Repaint();
        
        try
        {
            // 构建Scene Agent的输入
            var allAssetsList = new List<object>();
            if (assetIndex?.entries != null)
            {
                foreach (var entry in assetIndex.entries)
                {
                    allAssetsList.Add(new Dictionary<string, object>
                    {
                        ["name"] = entry.name ?? "",
                        ["type"] = entry.type.ToString().ToLower(),
                        ["aliases"] = entry.aliases ?? new List<string>(),
                        ["description"] = entry.description ?? ""
                    });
                }
            }
            
            // 压缩narrativeData.steps，只保留关键信息
            var simplifiedSteps = new List<Dictionary<string, object>>();
            if (steps != null)
            {
                foreach (var step in steps)
                {
                    var stepDict = new Dictionary<string, object>
                    {
                        ["step"] = step.step,
                        ["title"] = step.title ?? "",
                        ["location"] = (int)step.location,
                        ["objective"] = step.objective ?? "",
                        ["key_characters"] = step.key_characters ?? new List<string>(),
                        ["key_items"] = step.key_items ?? new List<string>()
                    };
                    simplifiedSteps.Add(stepDict);
                }
            }
            
            var sceneInput = new Dictionary<string, object>
            {
                ["steps"] = simplifiedSteps,
                ["assets"] = allAssetsList
            };
            
            string input = MiniJSON.JsonSerialize(sceneInput);
            Debug.Log($"[Scene Agent输入] 步骤数量: {steps?.Count ?? 0}, 资产数量: {allAssetsList.Count}");
            
            // 显示每个step的关键角色和物品
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                Debug.Log($"[Scene Agent输入] Step {step.step}: 角色=[{string.Join(", ", step.key_characters ?? new List<string>())}], 物品=[{string.Join(", ", step.key_items ?? new List<string>())}]");
            }
            
            Debug.Log($"[Scene Agent输入] 输入内容: {input}");
            
            try
            {
                // 尝试解析生成的JSON，验证其有效性
                var testParse = MiniJSON.Json.Deserialize(input) as Dictionary<string, object>;
                if (testParse == null)
                {
                    Debug.LogError("无法解析生成的JSON，可能格式有误");
                    EditorUtility.DisplayDialog("错误", "生成的JSON格式有误，无法发送请求", "确定");
                    isExtractingSceneParams = false;
                    Repaint();
                    return;
                }
            }
            catch (Exception jsonEx)
            {
                Debug.LogError($"JSON验证失败: {jsonEx.Message}");
                EditorUtility.DisplayDialog("错误", $"JSON验证失败: {jsonEx.Message}", "确定");
                isExtractingSceneParams = false;
                Repaint();
                return;
            }
            
            string llmResult = await AgentCommunication.CallPythonAgent("scene", input);
            
            if (!string.IsNullOrEmpty(llmResult))
            {
                // 去除markdown代码块包裹
                string jsonStr = llmResult.Trim();
                if (jsonStr.StartsWith("```json"))
                {
                    jsonStr = jsonStr.Substring(7);
                }
                if (jsonStr.StartsWith("```"))
                {
                    jsonStr = jsonStr.Substring(3);
                }
                if (jsonStr.EndsWith("```"))
                {
                    jsonStr = jsonStr.Substring(0, jsonStr.Length - 3);
                }
                jsonStr = jsonStr.Trim();
                
                Debug.Log($"[场景Agent返回] 清理后的JSON: {jsonStr}");
                
                try {
                    // 先尝试使用MiniJSON解析
                    Dictionary<string, object> parsedJson = null;
                    try 
                    {
                        parsedJson = MiniJSON.Json.Deserialize(jsonStr) as Dictionary<string, object>;
                    }
                    catch (Exception miniJsonEx) 
                    {
                        Debug.LogWarning($"MiniJSON解析失败: {miniJsonEx.Message}，尝试使用JsonUtility");
                    }
                    
                    if (parsedJson != null)
                    {
                        // 从解析的字典构建SceneAgentResponse对象
                        var resp = new SceneAgentResponse();
                        
                        // 处理scene_params
                        if (parsedJson.ContainsKey("scene_params") && parsedJson["scene_params"] is Dictionary<string, object> sceneParams)
                        {
                            resp.scene_params = new SceneParamResult();
                            if (sceneParams.ContainsKey("noiseScale"))
                                resp.scene_params.noiseScale = Convert.ToSingle(sceneParams["noiseScale"]);
                            if (sceneParams.ContainsKey("landThreshold"))
                                resp.scene_params.landThreshold = Convert.ToSingle(sceneParams["landThreshold"]);
                            if (sceneParams.ContainsKey("mountainThreshold"))
                                resp.scene_params.mountainThreshold = Convert.ToSingle(sceneParams["mountainThreshold"]);
                        }
                        
                        // 处理selected_assets
                        if (parsedJson.ContainsKey("selected_assets") && parsedJson["selected_assets"] is List<object> selectedAssets)
                        {
                            resp.selected_assets = new List<SelectedAsset>();
                            foreach (var assetObj in selectedAssets)
                            {
                                if (assetObj is Dictionary<string, object> assetDict)
                                {
                                    var selectedAsset = new SelectedAsset();
                                    if (assetDict.ContainsKey("step"))
                                        selectedAsset.step = Convert.ToInt32(assetDict["step"]);
                                    
                                    if (assetDict.ContainsKey("assets") && assetDict["assets"] is List<object> stepAssetsList)
                                    {
                                        selectedAsset.assets = new List<string>();
                                        foreach (var asset in stepAssetsList)
                                        {
                                            selectedAsset.assets.Add(asset.ToString());
                                        }
                                    }
                                    
                                    resp.selected_assets.Add(selectedAsset);
                                }
                            }
                        }
                        
                        // 缓存Scene Agent的结果
                        cachedSceneAgentResult = resp;
                        
                        // 更新地图参数
                        if (resp.scene_params != null) {
                            noiseScale = resp.scene_params.noiseScale;
                            landThreshold = resp.scene_params.landThreshold;
                            mountainThreshold = resp.scene_params.mountainThreshold;
                            Debug.Log($"Scene Agent参数提取成功：noiseScale={noiseScale}, landThreshold={landThreshold}, mountainThreshold={mountainThreshold}");
                        }
                        
                        // 显示资产选择结果
                        if (resp.selected_assets != null && resp.selected_assets.Count > 0) {
                            Debug.Log($"Scene Agent选择了 {resp.selected_assets.Count} 个步骤的资产");
                            foreach (var asset in resp.selected_assets)
                            {
                                Debug.Log($"Step {asset.step}: 选择了 {asset.assets.Count} 个资产 - {string.Join(", ", asset.assets)}");
                            }
                        } else {
                            Debug.LogWarning("Scene Agent没有选择任何资产，将使用后备方案");
                            // 后备方案：为每个step选择一些默认资产
                            resp.selected_assets = new List<SelectedAsset>();
                            if (narrativeData?.steps != null && assetIndex?.entries != null) {
                                foreach (var step in narrativeData.steps) {
                                    var selectedAsset = new SelectedAsset { step = step.step, assets = new List<string>() };
                                    
                                    // 选择一些角色（只有step 1选择MainCharacter，其他step选择NPC和Enemy）
                                    if (step.step == 1) {
                                        // step 1: 可以选择MainCharacter
                                        var mainCharacters = assetIndex.entries.Where(e => e.type == AssetIndex.AssetType.MainCharacter && e.prefab != null).Take(1).ToList();
                                        if (mainCharacters.Count == 0) {
                                            // 如果没有MainCharacter，尝试其他类型
                                            var fallback = assetIndex.entries.FirstOrDefault(e => e.prefab != null);
                                            if (fallback != null) {
                                                selectedAsset.assets.Add(fallback.name);
                                                Debug.Log($"后备方案：Step {step.step} 使用fallback资产 {fallback.name} 代替MainCharacter");
                                            }
                                        } else {
                                            foreach (var character in mainCharacters) {
                                                selectedAsset.assets.Add(character.name);
                                            }
                                        }
                                    }
                                    
                                    // 所有step都可以选择NPC和Enemy
                                    var npcs = assetIndex.entries.Where(e => e.type == AssetIndex.AssetType.NPC && e.prefab != null).Take(1).ToList();
                                    var enemies = assetIndex.entries.Where(e => e.type == AssetIndex.AssetType.Enemy && e.prefab != null).Take(1).ToList();
                                    
                                    if (npcs.Count > 0) {
                                        foreach (var npc in npcs) {
                                            selectedAsset.assets.Add(npc.name);
                                        }
                                    } else {
                                        // 如果没有NPC，尝试其他类型
                                        var fallback = assetIndex.entries.FirstOrDefault(e => e.prefab != null && e.type != AssetIndex.AssetType.MainCharacter);
                                        if (fallback != null) {
                                            selectedAsset.assets.Add(fallback.name);
                                            Debug.Log($"后备方案：Step {step.step} 使用fallback资产 {fallback.name} 代替NPC");
                                        }
                                    }
                                    
                                    if (enemies.Count > 0) {
                                        foreach (var enemy in enemies) {
                                            selectedAsset.assets.Add(enemy.name);
                                        }
                                    } else {
                                        // 如果没有Enemy，尝试其他类型
                                        var fallback = assetIndex.entries.FirstOrDefault(e => e.prefab != null && e.type != AssetIndex.AssetType.MainCharacter);
                                        if (fallback != null) {
                                            selectedAsset.assets.Add(fallback.name);
                                            Debug.Log($"后备方案：Step {step.step} 使用fallback资产 {fallback.name} 代替Enemy");
                                        }
                                    }
                                    
                                    // 选择一些道具
                                    var props = assetIndex.entries.Where(e => e.type == AssetIndex.AssetType.Props && e.prefab != null).Take(2).ToList();
                                    if (props.Count > 0) {
                                        foreach (var prop in props) {
                                            selectedAsset.assets.Add(prop.name);
                                        }
                                    } else {
                                        // 如果没有Props，尝试其他类型
                                        var fallback = assetIndex.entries.FirstOrDefault(e => e.prefab != null);
                                        if (fallback != null) {
                                            selectedAsset.assets.Add(fallback.name);
                                            Debug.Log($"后备方案：Step {step.step} 使用fallback资产 {fallback.name} 代替Props");
                                        }
                                    }
                                    
                                    resp.selected_assets.Add(selectedAsset);
                                }
                                Debug.Log($"后备方案：为 {resp.selected_assets.Count} 个步骤选择了资产");
                            }
                        }
                        
                        isSceneComplete = true;
                        EditorUtility.DisplayDialog("成功", "场景参数提取成功！", "确定");
                    }
                    else
                    {
                        // 尝试使用JsonUtility
                        var resp = JsonUtility.FromJson<SceneAgentResponse>(jsonStr);
                        if (resp != null) {
                            // 缓存Scene Agent的结果
                            cachedSceneAgentResult = resp;
                            
                            // 更新地图参数
                            if (resp.scene_params != null) {
                                noiseScale = resp.scene_params.noiseScale;
                                landThreshold = resp.scene_params.landThreshold;
                                mountainThreshold = resp.scene_params.mountainThreshold;
                                Debug.Log($"Scene Agent参数提取成功：noiseScale={noiseScale}, landThreshold={landThreshold}, mountainThreshold={mountainThreshold}");
                            }
                            
                            // 显示资产选择结果
                            if (resp.selected_assets != null && resp.selected_assets.Count > 0) {
                                Debug.Log($"Scene Agent选择了 {resp.selected_assets.Count} 个步骤的资产");
                                foreach (var asset in resp.selected_assets)
                                {
                                    Debug.Log($"Step {asset.step}: 选择了 {asset.assets.Count} 个资产 - {string.Join(", ", asset.assets)}");
                                }
                            } else {
                                Debug.LogWarning("Scene Agent没有选择任何资产，将使用后备方案");
                                // 后备方案：为每个step选择一些默认资产
                                resp.selected_assets = new List<SelectedAsset>();
                                if (narrativeData?.steps != null && assetIndex?.entries != null) {
                                    foreach (var step in narrativeData.steps) {
                                        var selectedAsset = new SelectedAsset { step = step.step, assets = new List<string>() };
                                        
                                        // 选择一些角色（只有step 1选择MainCharacter，其他step选择NPC和Enemy）
                                        if (step.step == 1) {
                                            // step 1: 可以选择MainCharacter
                                            var mainCharacters = assetIndex.entries.Where(e => e.type == AssetIndex.AssetType.MainCharacter && e.prefab != null).Take(1).ToList();
                                            if (mainCharacters.Count == 0) {
                                                // 如果没有MainCharacter，尝试其他类型
                                                var fallback = assetIndex.entries.FirstOrDefault(e => e.prefab != null);
                                                if (fallback != null) {
                                                    selectedAsset.assets.Add(fallback.name);
                                                    Debug.Log($"后备方案：Step {step.step} 使用fallback资产 {fallback.name} 代替MainCharacter");
                                                }
                                            } else {
                                                foreach (var character in mainCharacters) {
                                                    selectedAsset.assets.Add(character.name);
                                                }
                                            }
                                        }
                                        
                                        // 所有step都可以选择NPC和Enemy
                                        var npcs = assetIndex.entries.Where(e => e.type == AssetIndex.AssetType.NPC && e.prefab != null).Take(1).ToList();
                                        var enemies = assetIndex.entries.Where(e => e.type == AssetIndex.AssetType.Enemy && e.prefab != null).Take(1).ToList();
                                        
                                        if (npcs.Count > 0) {
                                            foreach (var npc in npcs) {
                                                selectedAsset.assets.Add(npc.name);
                                            }
                                        } else {
                                            // 如果没有NPC，尝试其他类型
                                            var fallback = assetIndex.entries.FirstOrDefault(e => e.prefab != null && e.type != AssetIndex.AssetType.MainCharacter);
                                            if (fallback != null) {
                                                selectedAsset.assets.Add(fallback.name);
                                                Debug.Log($"后备方案：Step {step.step} 使用fallback资产 {fallback.name} 代替NPC");
                                            }
                                        }
                                        
                                        if (enemies.Count > 0) {
                                            foreach (var enemy in enemies) {
                                                selectedAsset.assets.Add(enemy.name);
                                            }
                                        } else {
                                            // 如果没有Enemy，尝试其他类型
                                            var fallback = assetIndex.entries.FirstOrDefault(e => e.prefab != null && e.type != AssetIndex.AssetType.MainCharacter);
                                            if (fallback != null) {
                                                selectedAsset.assets.Add(fallback.name);
                                                Debug.Log($"后备方案：Step {step.step} 使用fallback资产 {fallback.name} 代替Enemy");
                                            }
                                        }
                                        
                                        // 选择一些道具
                                        var props = assetIndex.entries.Where(e => e.type == AssetIndex.AssetType.Props && e.prefab != null).Take(2).ToList();
                                        if (props.Count > 0) {
                                            foreach (var prop in props) {
                                                selectedAsset.assets.Add(prop.name);
                                            }
                                        } else {
                                            // 如果没有Props，尝试其他类型
                                            var fallback = assetIndex.entries.FirstOrDefault(e => e.prefab != null);
                                            if (fallback != null) {
                                                selectedAsset.assets.Add(fallback.name);
                                                Debug.Log($"后备方案：Step {step.step} 使用fallback资产 {fallback.name} 代替Props");
                                            }
                                        }
                                        
                                        resp.selected_assets.Add(selectedAsset);
                                    }
                                    Debug.Log($"后备方案：为 {resp.selected_assets.Count} 个步骤选择了资产");
                                }
                            }
                            
                            isSceneComplete = true;
                            EditorUtility.DisplayDialog("成功", "场景参数提取成功！", "确定");
                        }
                        else
                        {
                            Debug.LogError("无法解析场景Agent返回的JSON");
                            EditorUtility.DisplayDialog("错误", "无法解析场景Agent返回的JSON", "确定");
                        }
                    }
                } catch (System.Exception e) { 
                    Debug.LogError($"Scene Agent返回参数解析失败: {e.Message}\n原始结果: {llmResult}");
                    EditorUtility.DisplayDialog("错误", $"解析场景Agent返回参数失败: {e.Message}", "确定");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"调用Scene Agent失败: {e.Message}");
            EditorUtility.DisplayDialog("错误", $"调用Scene Agent失败: {e.Message}", "确定");
        }
        finally
        {
            isExtractingSceneParams = false;
            Repaint();
        }
    }

    // 1. 确保GenerateKeyObjectsAsync为类内async方法
    public async void GenerateKeyObjectsAsync()
    {
        if (narrativeData == null || assetIndex == null) return;
        
        // 检查是否有缓存的Scene Agent结果
        if (cachedSceneAgentResult == null || cachedSceneAgentResult.selected_assets == null)
        {
            EditorUtility.DisplayDialog("提示", "请先点击'场景Agent智能提取参数'来获取资产选择信息", "确定");
            return;
        }
        
        KeyObjectGenerator.ClearKeyObjects(spawnedKeyObjects);
        
        // 使用缓存的结果进行生成
        var newObjs = await KeyObjectGenerator.GenerateKeyObjectsFromSceneAgentResultAsync(
            narrativeData.steps, 
            assetIndex, 
            cachedSceneAgentResult.selected_assets,
            mapWidth, mapHeight,
            noiseScale, landThreshold, islandFactor, mountainThreshold,
            landSeed, mountainSeed, perlinScale, perlinStrength,
            elementsTilemap,
            landTilemap,
            mountainTilemap,
            cliffTilemap
        );
        
        spawnedKeyObjects.AddRange(newObjs);
        Debug.Log($"已生成{spawnedKeyObjects.Count}个关键角色/道具");
    }

    [MenuItem("Tools/Multi-Agent RPG Generator")]
    public static void ShowWindow()
    {
        GetWindow<MultiAgentRPGEditor>("RPG Generator");
    }
    
    public void OnEnable()
    {
        // 初始化样式
        // wordWrapStyle = EditorStyles.textArea != null ? new GUIStyle(EditorStyles.textArea) : new GUIStyle();
        // wordWrapStyle.wordWrap = true;
        // wordWrapStyle.richText = true;
        
        // headerStyle = new GUIStyle(EditorStyles.boldLabel);
        // headerStyle.fontSize = 14;
        // headerStyle.margin = new RectOffset(0, 0, 10, 5);
        
        // InitializeMechanismTypes(); // This method is in MechanismDesigner class
        CheckServerStatus();
        
        // 尝试自动加载叙事数据
        LoadNarrativeDataFromFile();
        
        // 从配置文件加载 Tilemap 引用
        LoadTilemapFromConfig();
    }
    
    /// <summary>
    /// 从 TilemapConfig 加载 Tilemap 和 RuleTile 引用
    /// </summary>
    private void LoadTilemapFromConfig()
    {
        if (tilemapConfig != null)
        {
            // 从配置文件加载
            waterTilemap = tilemapConfig.waterTilemap;
            landTilemap = tilemapConfig.landTilemap;
            mountainTilemap = tilemapConfig.mountainTilemap;
            cliffTilemap = tilemapConfig.cliffTilemap;
            elementsTilemap = tilemapConfig.elementsTilemap;
            
            waterRuleTile = tilemapConfig.waterRuleTile;
            landRuleTile = tilemapConfig.landRuleTile;
            mountainRuleTile = tilemapConfig.mountainRuleTile;
            cliffRuleTile = tilemapConfig.cliffRuleTile;
            
            Debug.Log("[RPGEditor] 已从 TilemapConfig 加载配置");
        }
        else
        {
            // 尝试自动查找默认配置文件
            string[] guids = AssetDatabase.FindAssets("t:TilemapConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                tilemapConfig = AssetDatabase.LoadAssetAtPath<TilemapConfig>(path);
                
                if (tilemapConfig != null)
                {
                    Debug.Log($"[RPGEditor] 自动找到 TilemapConfig: {path}");
                    LoadTilemapFromConfig(); // 递归调用以加载配置
                }
            }
        }
    }
    
    // 新增成员变量
    private Dictionary<string, object> lastCodegenJsonResult;

    public void OnGUI()
    {
        if (wordWrapStyle == null)
        {
            wordWrapStyle = new GUIStyle(EditorStyles.textArea);
            wordWrapStyle.wordWrap = true;
            wordWrapStyle.richText = true;
            
            // 为亮色模式调整文字颜色
            if (EditorGUIUtility.isProSkin == false) // 亮色模式
            {
                wordWrapStyle.normal.textColor = Color.black;
                wordWrapStyle.active.textColor = Color.black;
                wordWrapStyle.focused.textColor = Color.black;
                wordWrapStyle.hover.textColor = Color.black;
            }
        }
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.margin = new RectOffset(0, 0, 10, 5);
            
            // 为亮色模式调整标题颜色
            if (EditorGUIUtility.isProSkin == false) // 亮色模式
            {
                headerStyle.normal.textColor = Color.black;
            }
        }
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        try
        {
            // 服务器状态
            EditorGUILayout.BeginHorizontal();
            var serverStatusStyle = new GUIStyle(EditorStyles.boldLabel);
            if (EditorGUIUtility.isProSkin == false)
                serverStatusStyle.normal.textColor = Color.black;
            EditorGUILayout.LabelField("Server Status:", serverStatusStyle);
            EditorGUILayout.LabelField(serverStatus, serverStatus == "Connected" ? 
                new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.0f, 0.6f, 0.0f) } } : 
                new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } });
            if (GUILayout.Button("Check Connection", GUILayout.Width(120)))
            {
                CheckServerStatus();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Tab选择
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            EditorGUILayout.Space();
            // 根据选中的Tab显示对应内容
            switch (selectedTab)
            {
                case 0:
                    DrawNarrativeTab();
                    break;
                case 1:
                    DrawSceneTab();
                    break;
                case 2:
                    DrawMechanicsTab();
                    break;
            }
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }
    }
    
    public void DrawNarrativeTab()
    {
        EditorGUILayout.LabelField("Narrative Generation", headerStyle);

        if (isNarrativeComplete)
            EditorGUILayout.HelpBox("✓ Narrative Generation Completed", MessageType.Info);

        EditorGUILayout.LabelField("Please input your RPG story idea:");
        userInput = EditorGUILayout.TextArea(userInput, wordWrapStyle, GUILayout.Height(60));

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = !string.IsNullOrEmpty(userInput) && !isGeneratingNarrative && serverStatus == "Connected";
        if (GUILayout.Button(isGeneratingNarrative ? "Generating..." : "Generate Story"))
            GenerateNarrative();
        GUI.enabled = !string.IsNullOrEmpty(userInput);
        if (GUILayout.Button("Clear Input"))
        {
            userInput = "";
            GUI.FocusControl(null);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (narrativeData != null)
        {
            EditorGUILayout.Space();
            var storyTitleStyle = new GUIStyle(EditorStyles.boldLabel);
            if (EditorGUIUtility.isProSkin == false)
                storyTitleStyle.normal.textColor = Color.black;
            EditorGUILayout.LabelField("=== Overall Story ===", storyTitleStyle);
            storyScrollPos = EditorGUILayout.BeginScrollView(storyScrollPos, GUILayout.Height(80));
            EditorGUILayout.TextArea(narrativeData.story, wordWrapStyle);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
            var stepTitleStyle = new GUIStyle(EditorStyles.boldLabel);
            if (EditorGUIUtility.isProSkin == false)
                stepTitleStyle.normal.textColor = Color.black;
            EditorGUILayout.LabelField("=== Step Decomposition ===", stepTitleStyle);
            stepsScrollPos = EditorGUILayout.BeginScrollView(stepsScrollPos, GUILayout.Height(220));
            if (narrativeData.steps != null && narrativeData.steps.Count > 0)
            {
                for (int i = 0; i < narrativeData.steps.Count; i++)
                {
                    var step = narrativeData.steps[i];
                    EditorGUILayout.LabelField($"Step {step.step}: {step.title}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Location: {step.location}", wordWrapStyle);
                    EditorGUILayout.LabelField($"Objective: {step.objective}", wordWrapStyle);
                    EditorGUILayout.LabelField($"Main Characters: {string.Join(", ", step.key_characters)}", wordWrapStyle);
                    EditorGUILayout.LabelField("Main Dialogues:", wordWrapStyle);
                    foreach (var d in step.main_dialogues)
                        EditorGUILayout.LabelField($"{d.character}：{d.dialogue}", wordWrapStyle);
                    EditorGUILayout.LabelField($"Key Items: {string.Join(", ", step.key_items)}", wordWrapStyle);
                    EditorGUILayout.LabelField("--------------------------------------------------");
                }
            }
            else
            {
                EditorGUILayout.LabelField("暂无分解步骤");
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
            GUI.enabled = !isGeneratingNarrative && serverStatus == "Connected";
            if (GUILayout.Button("Regenerate Story"))
                GenerateNarrative();
            GUI.enabled = true;
        }
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load Narrative Data"))
        {
            LoadNarrativeDataFromFile();
        }
        if (GUILayout.Button("Save to Python Server Directory"))
        {
            SaveStepsToDisk();
        }
        EditorGUILayout.EndHorizontal();
    }
    
    public void DrawSceneTab()
    {
        EditorGUILayout.LabelField("Scene Generation", headerStyle);

        bool canOperate = isNarrativeComplete && narrativeData != null && narrativeData.steps != null && narrativeData.steps.Count > 0;
        if (!isNarrativeComplete)
            EditorGUILayout.HelpBox("请先完成叙事生成", MessageType.Warning);
        if (narrativeData == null || narrativeData.steps == null || narrativeData.steps.Count == 0)
        {
            EditorGUILayout.HelpBox("叙事步骤无效，无法生成地图", MessageType.Error);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("快速加载叙事数据"))
            {
                LoadNarrativeDataFromFile();
            }
            if (GUILayout.Button("切换到叙事生成"))
            {
                selectedTab = 0;
            }
            EditorGUILayout.EndHorizontal();
        }

        // 提前声明stepCount
        int stepCount = (narrativeData != null && narrativeData.steps != null) ? narrativeData.steps.Count : 0;
    
        // 显示叙事数据状态
        if (narrativeData != null && narrativeData.steps != null && narrativeData.steps.Count > 0)
        {
            EditorGUILayout.HelpBox($"✓ Loaded Narrative Data: {narrativeData.steps.Count} steps", MessageType.Info);
        }
        
        // 显示已生成的关键对象数量
        if (spawnedKeyObjects.Count > 0)
        {
            EditorGUILayout.HelpBox($"Generated {spawnedKeyObjects.Count} key objects", MessageType.Info);
        }

        // 地图参数
        showMapParams = EditorGUILayout.Foldout(showMapParams, "Map Parameters");
        if (showMapParams)
        {
            mapWidth = EditorGUILayout.IntField("Width", mapWidth);
            mapHeight = EditorGUILayout.IntField("Height", mapHeight);
            noiseScale = EditorGUILayout.FloatField("Noise Scale", noiseScale);
            landThreshold = EditorGUILayout.Slider("Land Threshold", landThreshold, 0f, 0.8f);
            islandFactor = EditorGUILayout.Slider("Island Factor", islandFactor, 0.5f, 10f);
            mountainThreshold = EditorGUILayout.Slider("Mountain Threshold", mountainThreshold, landThreshold, 1f);
            cliffHeight = EditorGUILayout.IntSlider("Cliff Height", cliffHeight, 1, 5);
            
            // 随机种子控制
            EditorGUILayout.BeginHorizontal();
            int newRandomSeed = EditorGUILayout.IntField("Random Seed", randomSeed);
            if (newRandomSeed != randomSeed)
            {
                randomSeed = newRandomSeed;
                landSeed = randomSeed;
                mountainSeed = randomSeed + 1;
            }
            if (GUILayout.Button("Random", GUILayout.Width(60)))
            {
                randomSeed = UnityEngine.Random.Range(0, 10000);
                landSeed = randomSeed;
                mountainSeed = randomSeed + 1;
            }
            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            {
                randomSeed = 0;
                landSeed = 0;
                mountainSeed = 1;
            }
            EditorGUILayout.EndHorizontal();
            
            // 显示当前种子值
            EditorGUILayout.LabelField($"Current Seed - Land: {landSeed}, Mountain: {mountainSeed}");
            
            // 允许单独调整种子
            EditorGUILayout.BeginHorizontal();
            int newLandSeed = EditorGUILayout.IntField("Land Seed", landSeed);
            int newMountainSeed = EditorGUILayout.IntField("Mountain Seed", mountainSeed);
            
            // 如果种子发生变化，更新通用种子
            if (newLandSeed != landSeed || newMountainSeed != mountainSeed)
            {
                landSeed = newLandSeed;
                mountainSeed = newMountainSeed;
                // 更新通用种子为地形种子
                randomSeed = landSeed;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField($"Partition Count (step): {stepCount}");;
        }
        // Tilemap 设置
        showTileMapParams = EditorGUILayout.Foldout(showTileMapParams, "Tilemap Settings");
        if(showTileMapParams){
            EditorGUILayout.Space(5);
            
            // Tilemap 配置文件
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("💾 Tilemap 配置文件（推荐）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("使用配置文件可以永久保存 Tilemap 和 RuleTile 的引用，避免每次都要手动拖拽。", MessageType.Info);
            
            TilemapConfig oldConfig = tilemapConfig;
            tilemapConfig = (TilemapConfig)EditorGUILayout.ObjectField("Tilemap Config", tilemapConfig, typeof(TilemapConfig), false);
            
            // 如果配置文件改变，重新加载
            if (tilemapConfig != oldConfig && tilemapConfig != null)
            {
                LoadTilemapFromConfig();
            }
            
            EditorGUILayout.BeginHorizontal();
            if (tilemapConfig == null)
            {
                if (GUILayout.Button("创建新的 TilemapConfig", GUILayout.Height(25)))
                {
                    CreateNewTilemapConfig();
                }
            }
            else
            {
                if (GUILayout.Button("保存当前配置到文件", GUILayout.Height(25)))
                {
                    SaveCurrentConfigToFile();
                }
                
                if (GUILayout.Button("从文件重新加载", GUILayout.Height(25)))
                {
                    LoadTilemapFromConfig();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 显示配置状态
            if (tilemapConfig != null)
            {
                if (tilemapConfig.IsValid())
                {
                    EditorGUILayout.HelpBox("✅ 配置完整", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox($"⚠️ 配置不完整：{tilemapConfig.GetMissingItems()}", MessageType.Warning);
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
            
            // 手动 Tilemap 引用（兼容旧版本或快速修改）
            EditorGUILayout.LabelField("🎯 Tilemap 引用", EditorStyles.boldLabel);
            waterTilemap = (Tilemap)EditorGUILayout.ObjectField("Water Tilemap", waterTilemap, typeof(Tilemap), true);
            landTilemap = (Tilemap)EditorGUILayout.ObjectField("Land Tilemap", landTilemap, typeof(Tilemap), true);
            mountainTilemap = (Tilemap)EditorGUILayout.ObjectField("Mountain Tilemap", mountainTilemap, typeof(Tilemap), true);
            cliffTilemap = (Tilemap)EditorGUILayout.ObjectField("Cliff Tilemap", cliffTilemap, typeof(Tilemap), true);
            elementsTilemap = (Tilemap)EditorGUILayout.ObjectField("Elements Tilemap", elementsTilemap, typeof(Tilemap), true);
        }

        // Rule Tile设置
        showRuleTileParams = EditorGUILayout.Foldout(showRuleTileParams, "Default Rule Tile Settings");
        if (showRuleTileParams)
        {
            EditorGUILayout.HelpBox("这些是默认的Rule Tile，当分区没有配置自定义Rule Tile时会使用这些默认值。", MessageType.Info);
            waterRuleTile = (TileBase)EditorGUILayout.ObjectField("Water Rule Tile", waterRuleTile, typeof(TileBase), false);
            landRuleTile = (TileBase)EditorGUILayout.ObjectField("Land Rule Tile", landRuleTile, typeof(TileBase), false);
            mountainRuleTile = (TileBase)EditorGUILayout.ObjectField("Mountain Rule Tile", mountainRuleTile, typeof(TileBase), false);
            cliffRuleTile = (TileBase)EditorGUILayout.ObjectField("Cliff Rule Tile", cliffRuleTile, typeof(TileBase), false);
        }
        // 元素分布配置
        showElementParams = EditorGUILayout.Foldout(showElementParams, "Partition Element Distribution Configuration");
        if (showElementParams)
        {
            EditorGUILayout.LabelField("Assign ElementDistributionConfig assets for each partition (step):");
            SerializedObject soCfg = new SerializedObject(this);
            SerializedProperty propCfg = soCfg.FindProperty("areaElementConfigs");
            if (propCfg != null) EditorGUILayout.PropertyField(propCfg, true);
            soCfg.ApplyModifiedProperties();
            GUI.enabled = canOperate;
        }
        
        // 区域平衡优化选项
        enableAreaBalancing = EditorGUILayout.Toggle("Enable Area Balancing Optimization", enableAreaBalancing);
        if (enableAreaBalancing)
        {
            EditorGUILayout.HelpBox("Area Balancing Optimization will automatically adjust partition seed positions to make each area more uniform. Maximum 3 iterations.", MessageType.Info);
        }
        
        // 分区RuleTile配置
        showPartitionRuleTileParams = EditorGUILayout.Foldout(showPartitionRuleTileParams, "Partition RuleTile Configuration");
        if (showPartitionRuleTileParams)
        {
            EditorGUILayout.HelpBox("为每个分区配置特定的Rule Tile。如果某个分区没有配置，将使用默认的Rule Tile。", MessageType.Info);
            
            if (narrativeData == null || narrativeData.steps == null || narrativeData.steps.Count == 0)
            {
                EditorGUILayout.HelpBox("请先加载叙事数据以配置分区Rule Tile", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"为 {narrativeData.steps.Count} 个分区配置RuleTile:");
            }
            // 确保配置列表与步骤数量匹配
            if (narrativeData != null && narrativeData.steps != null)
            {
                while (partitionRuleTileConfigs.Count < narrativeData.steps.Count)
                {
                    partitionRuleTileConfigs.Add(new PartitionRuleTileConfig(partitionRuleTileConfigs.Count));
                }
                
                while (partitionRuleTileConfigs.Count > narrativeData.steps.Count)
                {
                    partitionRuleTileConfigs.RemoveAt(partitionRuleTileConfigs.Count - 1);
                }
                
                // 显示每个分区的配置
                for (int i = 0; i < partitionRuleTileConfigs.Count; i++)
                {
                    var config = partitionRuleTileConfigs[i];
                    var step = narrativeData.steps[i];
                    
                    EditorGUILayout.BeginVertical("box");
                    var partitionStyle = new GUIStyle(EditorStyles.boldLabel);
                    if (EditorGUIUtility.isProSkin == false)
                        partitionStyle.normal.textColor = Color.black;
                    EditorGUILayout.LabelField($"Partition {i + 1}: {step.title ?? $"Step {step.step}"} ({step.location})", partitionStyle);
                    
                    config.waterRuleTile = (TileBase)EditorGUILayout.ObjectField("Water Rule Tile", config.waterRuleTile, typeof(TileBase), false);
                    config.landRuleTile = (TileBase)EditorGUILayout.ObjectField("Land Rule Tile", config.landRuleTile, typeof(TileBase), false);
                    config.mountainRuleTile = (TileBase)EditorGUILayout.ObjectField("Mountain Rule Tile", config.mountainRuleTile, typeof(TileBase), false);
                    config.cliffRuleTile = (TileBase)EditorGUILayout.ObjectField("Cliff Rule Tile", config.cliffRuleTile, typeof(TileBase), false);
                    
                    // 显示配置状态
                    bool hasCustomConfig = config.waterRuleTile != null || config.landRuleTile != null || 
                                         config.mountainRuleTile != null || config.cliffRuleTile != null;
                    string statusText = hasCustomConfig ? "✓ 已配置自定义RuleTile" : "使用默认RuleTile";
                    Color originalColor = GUI.color;
                    GUI.color = hasCustomConfig ? Color.green : Color.gray;
                    EditorGUILayout.LabelField(statusText, EditorStyles.miniLabel);
                    GUI.color = originalColor;
                    
                    EditorGUILayout.EndVertical();
                }
            }
        }

        showKeyItemParams = EditorGUILayout.Foldout(showKeyItemParams, "Key Item Role Configuration");
        if  (showKeyItemParams){
            assetIndex = (AssetIndex)EditorGUILayout.ObjectField("AssetIndex设置", assetIndex, typeof(AssetIndex), false);
            
            if (assetIndex == null)
            {
                EditorGUILayout.HelpBox("请先设置AssetIndex以使用AssetIndex-based关键对象生成", MessageType.Warning);
            }
        }

        // 关键对象生成
        EditorGUILayout.Space();
                    var keyObjectsStyle = new GUIStyle(EditorStyles.boldLabel);
            if (EditorGUIUtility.isProSkin == false)
                keyObjectsStyle.normal.textColor = Color.black;
            EditorGUILayout.LabelField("Key Objects Generation:", keyObjectsStyle);
        
        
        GUI.enabled = !isExtractingSceneParams && narrativeData != null && narrativeData.steps != null && narrativeData.steps.Count > 0;
        if (GUILayout.Button(isExtractingSceneParams ? "Extracting..." : "Scene Agent Smart Extract Parameters"))
        {
            if (narrativeData != null && narrativeData.steps != null && narrativeData.steps.Count > 0)
                ExtractSceneParamsWithLLM(narrativeData.story, narrativeData.steps);
        }
        GUI.enabled = true;

        EditorGUILayout.BeginHorizontal();
        
        // 确定按钮文本
        string buttonText = "Generate Map";
        if (narrativeData != null && narrativeData.steps != null && narrativeData.steps.Count > 0)
        {
            bool hasCustomConfig = false;
            if (partitionRuleTileConfigs != null && partitionRuleTileConfigs.Count > 0)
            {
                foreach (var config in partitionRuleTileConfigs)
                {
                    if (config.waterRuleTile != null || config.landRuleTile != null || 
                        config.mountainRuleTile != null || config.cliffRuleTile != null)
                    {
                        hasCustomConfig = true;
                        break;
                    }
                }
            }
            buttonText = hasCustomConfig ? "Generate Partition Map" : "Generate Map";
        }
        
        if (GUILayout.Button(buttonText))
        {
            if (narrativeData != null && narrativeData.steps != null && narrativeData.steps.Count > 0)
            {
                // 检查是否有分区配置
                bool hasPartitionConfig = partitionRuleTileConfigs != null && partitionRuleTileConfigs.Count > 0;
                bool hasCustomConfig = false;
                
                if (hasPartitionConfig)
                {
                    // 检查是否有任何分区配置了自定义ruletile
                    foreach (var config in partitionRuleTileConfigs)
                    {
                        if (config.waterRuleTile != null || config.landRuleTile != null || 
                            config.mountainRuleTile != null || config.cliffRuleTile != null)
                        {
                            hasCustomConfig = true;
                            break;
                        }
                    }
                }
                
                if (hasCustomConfig)
                {
                    // 确保分区配置列表与步骤数量匹配
                    while (partitionRuleTileConfigs.Count < narrativeData.steps.Count)
                    {
                        partitionRuleTileConfigs.Add(new PartitionRuleTileConfig(partitionRuleTileConfigs.Count));
                    }
                    
                    Debug.Log($"[MultiAgentRPGEditor] 生成分区地图 - 使用种子: 地形={landSeed}, 山地={mountainSeed}");
                    MapGenerator.GenerateMapWithPartitionRuleTiles(waterTilemap, landTilemap, mountainTilemap, cliffTilemap,
                        waterRuleTile, landRuleTile, mountainRuleTile, cliffRuleTile,
                        partitionRuleTileConfigs, narrativeData.steps,
                        mapWidth, mapHeight, noiseScale, landThreshold, islandFactor, mountainThreshold, cliffHeight, landSeed, mountainSeed, perlinScale, perlinStrength, elementsTilemap, enableAreaBalancing);
                }
                else
                {
                    Debug.Log($"[MultiAgentRPGEditor] 生成基础地图 - 使用种子: 地形={landSeed}, 山地={mountainSeed}");
                    MapGenerator.GenerateMap(waterTilemap, landTilemap, mountainTilemap, cliffTilemap,
                        waterRuleTile, landRuleTile, mountainRuleTile, cliffRuleTile,
                        mapWidth, mapHeight, noiseScale, landThreshold, islandFactor, mountainThreshold, cliffHeight, landSeed, mountainSeed, elementsTilemap);
                }
            }
            else
            {
                Debug.Log($"[MultiAgentRPGEditor] 生成基础地图 - 使用种子: 地形={landSeed}, 山地={mountainSeed}");
                MapGenerator.GenerateMap(waterTilemap, landTilemap, mountainTilemap, cliffTilemap,
                    waterRuleTile, landRuleTile, mountainRuleTile, cliffRuleTile,
                    mapWidth, mapHeight, noiseScale, landThreshold, islandFactor, mountainThreshold, cliffHeight, landSeed, mountainSeed, elementsTilemap);
            }
        }
        if (GUILayout.Button("Clear Map"))
        {
            ClearAllTilemaps();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = narrativeData != null && narrativeData.steps != null && narrativeData.steps.Count > 0;
        if (GUILayout.Button("Distribute Scene Elements"))
        {
            if (narrativeData != null && narrativeData.steps != null && narrativeData.steps.Count > 0)
            {
                MapGenerator.AutoDistributeElements(elementsTilemap, areaElementConfigs, narrativeData.steps,
                    mapWidth, mapHeight, perlinScale, perlinStrength, landSeed, mountainSeed,
                    noiseScale, landThreshold, islandFactor, mountainThreshold, cliffTilemap, enableAreaBalancing);
            }
        }
        GUI.enabled = true;
        if (GUILayout.Button("Clear Scene Elements"))
        {
            if (elementsTilemap) elementsTilemap.ClearAllTiles();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Key Objects"))
        {
            if (assetIndex != null)
            {
                GenerateKeyObjectsAsync();
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "请先设置AssetIndex", "确定");
            }
        }
        if (GUILayout.Button("Clear Key Objects"))
        {
            KeyObjectGenerator.ClearKeyObjects(spawnedKeyObjects);
        }
        EditorGUILayout.EndHorizontal();
        // 自动生成预览
        //Debug.Log($"[分区预览] 当前stepCount={stepCount} narrativeData.steps.Count={(narrativeData?.steps?.Count ?? -1)}");
        GeneratePreview(stepCount);

        if (previewTex != null)
        {
            GUILayout.Label("Terrain Preview:");
            float aspect = (float)mapWidth / mapHeight;
            float width = Mathf.Min(position.width - 40, 400);
            float height = width / aspect;
            width = Mathf.Clamp(width, 10, position.width - 10);
            height = Mathf.Clamp(height, 10, position.height - 100);
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            GUI.DrawTexture(rect, previewTex, ScaleMode.ScaleToFit, false);
            if (GUILayout.Button("Save Terrain Preview Image"))
            {
                MapGenerationManager.SaveTextureAsPNG(previewTex, "Terrain Preview");
            }
        }
        if (partitionPreviewTex != null)
        {
            GUILayout.Label("分区预览：");
            float aspect = (float)mapWidth / mapHeight;
            float width = Mathf.Min(position.width - 40, 400);
            float height = width / aspect;
            width = Mathf.Clamp(width, 10, position.width - 10);
            height = Mathf.Clamp(height, 10, position.height - 100);
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            GUI.DrawTexture(rect, partitionPreviewTex, ScaleMode.ScaleToFit, false);
            if (GUILayout.Button("保存分区预览图像"))
            {
                MapGenerationManager.SaveTextureAsPNG(partitionPreviewTex, "分区预览");
            }
        }
    }

    public async void GenerateNarrative()
    {
        if (string.IsNullOrEmpty(userInput)) return;
        
        isGeneratingNarrative = true;
        Debug.Log($"[MultiAgentRPGEditor] 开始生成叙事，输入: {userInput.Substring(0, Math.Min(userInput.Length, 100))}...");
        Repaint();
        
        try
        {
            Debug.Log("[MultiAgentRPGEditor] 调用 AgentCommunication.CallPythonAgent...");
            var result = await AgentCommunication.CallPythonAgent("narrative", userInput);
            Debug.Log($"[MultiAgentRPGEditor] 叙事生成完成，结果长度: {result.Length}");
            Debug.Log($"[MultiAgentRPGEditor] 结果前200字符: {result.Substring(0, Math.Min(result.Length, 200))}...");
            narrativeResult = result;
            
            // 4. 添加 ParseNarrativeJson 方法
            Debug.Log("[MultiAgentRPGEditor] 开始解析叙事JSON...");
            ParseNarrativeJson(result);
            
            if (narrativeData != null && narrativeData.steps != null)
            {
                Debug.Log($"[MultiAgentRPGEditor] 解析成功，步骤数: {narrativeData.steps.Count}");
            }
            else
            {
                Debug.LogError("[MultiAgentRPGEditor] 解析失败，narrativeData 为空或 steps 为空");
            }
            
            isNarrativeComplete = true;
            
            // // 自动切换到场景生成Tab
            // selectedTab = 1;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"生成故事失败: {e.Message}\n{e.StackTrace}");
            narrativeResult = $"错误: {e.Message}";
            narrativeData = null;
        }
        finally
        {
            isGeneratingNarrative = false;
            Repaint();
        }
    }
    
    // 4. 添加 ParseNarrativeJson 方法
    public void ParseNarrativeJson(string json)
    {
        narrativeData = null;
        stepScrollPos.Clear();
        if (string.IsNullOrEmpty(json)) 
        {
            Debug.LogError("[ParseNarrativeJson] 输入的JSON为空");
            return;
        }
        
        // 检查是否是Markdown代码块，如果是，提取其中的JSON
        if (json.Contains("```json") || json.Contains("```"))
        {
            Debug.Log("[ParseNarrativeJson] 检测到Markdown代码块，尝试提取JSON");
            int start = json.IndexOf("```");
            if (start >= 0)
            {
                start = json.IndexOf("\n", start) + 1;
                int end = json.IndexOf("```", start);
                if (end >= 0)
                {
                    json = json.Substring(start, end - start).Trim();
                    Debug.Log($"[ParseNarrativeJson] 提取后的JSON长度: {json.Length}");
                }
            }
        }
        
        try
        {
            Debug.Log("[ParseNarrativeJson] 尝试使用JsonUtility解析");
            narrativeData = JsonUtility.FromJson<NarrativeData>(json);
            if (narrativeData != null && narrativeData.steps != null)
            {
                Debug.Log($"[ParseNarrativeJson] JsonUtility解析成功，步骤数: {narrativeData.steps.Count}");
                for (int i = 0; i < narrativeData.steps.Count; i++)
                    stepScrollPos.Add(Vector2.zero);
                return;
            }
            else
            {
                Debug.LogWarning("[ParseNarrativeJson] JsonUtility解析成功，但数据不完整，尝试使用MiniJSON");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ParseNarrativeJson] JsonUtility解析失败: {e.Message}，尝试使用MiniJSON");
        }
        
        // JsonUtility不支持嵌套List的反序列化，尝试用MiniJSON
        try
        {
            Debug.Log("[ParseNarrativeJson] 尝试使用MiniJSON解析");
            var dict = MiniJSON.JsonDeserialize(json) as Dictionary<string, object>;
            if (dict != null)
            {
                Debug.Log($"[ParseNarrativeJson] MiniJSON解析成功，字段数: {dict.Count}");
                narrativeData = new NarrativeData();
                
                if (dict.ContainsKey("story"))
                {
                    narrativeData.story = dict["story"] as string;
                    Debug.Log($"[ParseNarrativeJson] 获取到story字段，长度: {narrativeData.story?.Length ?? 0}");
                }
                else
                {
                    Debug.LogError("[ParseNarrativeJson] JSON中缺少story字段");
                    narrativeData.story = "无法解析故事内容";
                }
                
                narrativeData.steps = new List<NarrativeStep>();
                
                if (dict.ContainsKey("steps"))
                {
                    var stepsList = dict["steps"] as List<object>;
                    if (stepsList != null)
                    {
                        Debug.Log($"[ParseNarrativeJson] 获取到steps字段，步骤数: {stepsList.Count}");
                        foreach (var stepObj in stepsList)
                        {
                            try
                            {
                                var stepDict = stepObj as Dictionary<string, object>;
                                if (stepDict == null) continue;
                                
                                var step = new NarrativeStep();
                                
                                if (stepDict.ContainsKey("step"))
                                    step.step = (int)(long)stepDict["step"];
                                
                                if (stepDict.ContainsKey("title"))
                                    step.title = stepDict["title"] as string;
                                
                                // 3. 相关JSON解析时将location数字转为LocationType
                                if (stepDict.ContainsKey("location"))
                                {
                                    try
                                    {
                                        if (stepDict["location"] is long)
                                            step.location = (LocationType)System.Convert.ToInt32(stepDict["location"]);
                                        else if (stepDict["location"] is string locStr && int.TryParse(locStr, out int locInt))
                                            step.location = (LocationType)locInt;
                                        else
                                            Debug.LogWarning($"[ParseNarrativeJson] 无法解析location字段: {stepDict["location"]}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.LogError($"[ParseNarrativeJson] 解析location时出错: {ex.Message}");
                                        step.location = LocationType.Village; // 默认为村庄
                                    }
                                }
                                
                                if (stepDict.ContainsKey("objective"))
                                    step.objective = stepDict["objective"] as string;
                                
                                step.key_characters = new List<string>();
                                if (stepDict.ContainsKey("key_characters"))
                                {
                                    foreach (var c in (List<object>)stepDict["key_characters"])
                                        step.key_characters.Add(c as string);
                                }
                                
                                step.key_items = new List<string>();
                                if (stepDict.ContainsKey("key_items"))
                                {
                                    foreach (var k in (List<object>)stepDict["key_items"])
                                        step.key_items.Add(k as string);
                                }
                                
                                step.main_dialogues = new List<Dialogue>();
                                if (stepDict.ContainsKey("main_dialogues"))
                                {
                                    foreach (var d in (List<object>)stepDict["main_dialogues"])
                                    {
                                        var dd = d as Dictionary<string, object>;
                                        if (dd != null)
                                        {
                                            step.main_dialogues.Add(new Dialogue
                                            {
                                                character = dd.ContainsKey("character") ? dd["character"] as string : "",
                                                dialogue = dd.ContainsKey("dialogue") ? dd["dialogue"] as string : ""
                                            });
                                        }
                                    }
                                }
                                
                                narrativeData.steps.Add(step);
                            }
                            catch (Exception stepEx)
                            {
                                Debug.LogError($"[ParseNarrativeJson] 解析步骤时出错: {stepEx.Message}");
                            }
                        }
                        
                        for (int i = 0; i < narrativeData.steps.Count; i++)
                            stepScrollPos.Add(Vector2.zero);
                        
                        Debug.Log($"[ParseNarrativeJson] 成功解析 {narrativeData.steps.Count} 个步骤");
                    }
                    else
                    {
                        Debug.LogError("[ParseNarrativeJson] steps不是有效的数组");
                    }
                }
                else
                {
                    Debug.LogError("[ParseNarrativeJson] JSON中缺少steps字段");
                }
            }
            else
            {
                Debug.LogError("[ParseNarrativeJson] MiniJSON解析失败，结果为null");
                narrativeData = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ParseNarrativeJson] MiniJSON解析失败: {e.Message}\n{e.StackTrace}");
            narrativeData = null;
        }
    }
    

    
    public void DrawMechanicsTab()
    {
        EditorGUILayout.LabelField("Mechanics Design", headerStyle);
        
        // 机制设计按钮
        GUI.enabled = !isGeneratingMechanics && serverStatus == "Connected" && narrativeData != null && narrativeData.steps != null && narrativeData.steps.Count > 0;
        if (GUILayout.Button(isGeneratingMechanics ? "Generating..." : "Mechanics Design (AI Recommended)"))
        {
            GenerateMechanicsDesignAll();
        }
        GUI.enabled = true;

        // 机制设计结果显示区域（分条显示，带滚动条）
        if (!string.IsNullOrEmpty(mechanicsResult))
        {
            EditorGUILayout.Space();
            var mechanicsTitleStyle = new GUIStyle(EditorStyles.boldLabel);
            if (EditorGUIUtility.isProSkin == false)
                mechanicsTitleStyle.normal.textColor = Color.black;
            EditorGUILayout.LabelField("=== Mechanics Design Results ===", mechanicsTitleStyle);
            mechanicsScrollPos = EditorGUILayout.BeginScrollView(mechanicsScrollPos, GUILayout.Height(220));
            
            // 直接显示文本，参考叙事步骤的显示方式
            var lines = mechanicsResult.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            string currentType = "";
            bool inMechanism = false;
            int indentLevel = 0;
            
            foreach (var line in lines)
            {
                // 清理括号等不必要的字符，但保留JSON结构
                string cleanLine = line.Trim();
                
                // 跳过空行
                if (string.IsNullOrEmpty(cleanLine))
                    continue;
                
                // 检查是否是JSON开始或结束
                if (cleanLine.StartsWith("{") || cleanLine.StartsWith("}") || cleanLine.StartsWith("[") || cleanLine.StartsWith("]"))
                    continue;
                
                // 检查缩进级别
                if (cleanLine.StartsWith("  ") || cleanLine.StartsWith("\t"))
                {
                    indentLevel = 1;
                    cleanLine = cleanLine.TrimStart();
                }
                else
                {
                    indentLevel = 0;
                }
                
                // 检查是否包含type字段
                if (cleanLine.Contains("\"type\""))
                {
                    // 提取type值
                    int typeIndex = cleanLine.IndexOf("\"type\"");
                    int colonIndex = cleanLine.IndexOf(":", typeIndex);
                    int quoteStart = cleanLine.IndexOf("\"", colonIndex);
                    int quoteEnd = cleanLine.IndexOf("\"", quoteStart + 1);
                    
                    if (quoteStart > 0 && quoteEnd > quoteStart)
                    {
                        string type = cleanLine.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                        if (type != currentType)
                        {
                            if (!string.IsNullOrEmpty(currentType))
                            {
                                EditorGUILayout.LabelField("--------------------------------------------------");
                            }
                            currentType = type;
                            var typeStyle = new GUIStyle(EditorStyles.boldLabel);
                            if (EditorGUIUtility.isProSkin == false)
                                typeStyle.normal.textColor = Color.black;
                            EditorGUILayout.LabelField($"Type: {type.ToUpper()}", typeStyle);
                        }
                    }
                    inMechanism = false;
                    continue;
                }
                
                // 检查是否包含name字段
                if (cleanLine.Contains("\"name\""))
                {
                    // 提取name值
                    int nameIndex = cleanLine.IndexOf("\"name\"");
                    int colonIndex = cleanLine.IndexOf(":", nameIndex);
                    int quoteStart = cleanLine.IndexOf("\"", colonIndex);
                    int quoteEnd = cleanLine.IndexOf("\"", quoteStart + 1);
                    
                    if (quoteStart > 0 && quoteEnd > quoteStart)
                    {
                        string name = cleanLine.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                        var nameStyle = new GUIStyle(EditorStyles.boldLabel);
                        if (EditorGUIUtility.isProSkin == false)
                            nameStyle.normal.textColor = Color.black;
                        EditorGUILayout.LabelField($"Name: {name}", nameStyle);
                    }
                    inMechanism = false;
                    continue;
                }
                
                // 检查是否包含mechanism字段
                if (cleanLine.Contains("\"mechanism\""))
                {
                    var mechanismStyle = new GUIStyle(EditorStyles.boldLabel);
                    if (EditorGUIUtility.isProSkin == false)
                        mechanismStyle.normal.textColor = Color.black;
                    EditorGUILayout.LabelField("Mechanism:", mechanismStyle);
                    inMechanism = true;
                    continue;
                }
                
                // 如果在mechanism内部且有缩进，说明是mechanism的子字段
                if (inMechanism && indentLevel > 0 && cleanLine.Contains("\"") && cleanLine.Contains(":"))
                {
                    // 提取字段名和值
                    int firstQuote = cleanLine.IndexOf("\"");
                    int secondQuote = cleanLine.IndexOf("\"", firstQuote + 1);
                    int colonIndex = cleanLine.IndexOf(":", secondQuote);
                    int valueStart = cleanLine.IndexOf("\"", colonIndex);
                    int valueEnd = cleanLine.IndexOf("\"", valueStart + 1);
                    
                    if (firstQuote >= 0 && secondQuote > firstQuote && valueStart > 0 && valueEnd > valueStart)
                    {
                        string fieldName = cleanLine.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                        string fieldValue = cleanLine.Substring(valueStart + 1, valueEnd - valueStart - 1);
                        var fieldStyle = new GUIStyle(wordWrapStyle);
                        if (EditorGUIUtility.isProSkin == false)
                            fieldStyle.normal.textColor = Color.black;
                        EditorGUILayout.LabelField($"  {fieldName}: {fieldValue}", fieldStyle);
                    }
                    continue;
                }
                
                // 检查mechanism结束
                if (inMechanism && cleanLine.StartsWith("}"))
                {
                    inMechanism = false;
                    continue;
                }
                
                // 其他内容按原样显示（但跳过JSON标记）
                if (!string.IsNullOrEmpty(cleanLine) && !cleanLine.StartsWith("```") && !cleanLine.EndsWith("```"))
                {
                    // 移除JSON引号、逗号和中括号，但保留内容
                    string displayLine = cleanLine.Replace("\"", "").Replace(",", "").Replace("[", "").Replace("]", "");
                    if (!string.IsNullOrEmpty(displayLine.Trim()))
                    {
                        var displayStyle = new GUIStyle(wordWrapStyle);
                        if (EditorGUIUtility.isProSkin == false)
                            displayStyle.normal.textColor = Color.black;
                        EditorGUILayout.LabelField(displayLine, displayStyle);
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        // 保存和导出按钮
        if (!string.IsNullOrEmpty(mechanicsResult))
        {
            EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Save to File"))
        {
            MechanismDesignManager.SaveMechanicsToFile(mechanicsResult);
        }
        if (GUILayout.Button("Export as JSON"))
        {
            MechanismDesignManager.ExportMechanicsAsJSON(mechanicsResult);
        }
            EditorGUILayout.EndHorizontal();
        }
        
        // 代码生成区域
        EditorGUILayout.Space();
        var codeGenStyle = new GUIStyle(EditorStyles.boldLabel);
        if (EditorGUIUtility.isProSkin == false)
            codeGenStyle.normal.textColor = Color.black;
        EditorGUILayout.LabelField("Code Generation:", codeGenStyle);
        
        // 场景物品状态
        if (spawnedKeyObjects != null && spawnedKeyObjects.Count > 0)
        {
            EditorGUILayout.HelpBox($"There are {spawnedKeyObjects.Count} key objects in the current scene that can be used to attach scripts", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("场景中没有关键对象，请先在场景生成选项卡中生成关键物品和角色", MessageType.Warning);
        }
        
        // 自动挂载选项
        MechanismDesigner.autoAttachToObjects = EditorGUILayout.Toggle("Auto Attach Scripts to Objects", MechanismDesigner.autoAttachToObjects);
        
        // 代码输入区域
        var inputStyle = new GUIStyle(EditorStyles.boldLabel);
        if (EditorGUIUtility.isProSkin == false)
            inputStyle.normal.textColor = Color.black;
        EditorGUILayout.LabelField("Input Mechanism Description:", inputStyle);
        MechanismDesigner.codeInputScrollPosition = EditorGUILayout.BeginScrollView(MechanismDesigner.codeInputScrollPosition, GUILayout.Height(150));
        MechanismDesigner.codeInput = EditorGUILayout.TextArea(MechanismDesigner.codeInput, wordWrapStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        
        // 使用机制设计结果按钮
        if (!string.IsNullOrEmpty(mechanicsResult))
        {
            if (GUILayout.Button("Use Mechanism Design Result as Input"))
            {
                MechanismDesigner.codeInput = mechanicsResult;
            }
        }
        
        // 生成代码按钮
        GUI.enabled = !MechanismDesigner.isGeneratingCode && serverStatus == "Connected" && !string.IsNullOrEmpty(MechanismDesigner.codeInput);
        if (GUILayout.Button(MechanismDesigner.isGeneratingCode ? "Generating..." : "Generate Code"))
        {
                    // 异步生成代码并自动挂载
        _ = GenerateAndAttachCodeAsync();
        }
        GUI.enabled = true;
        
        // 挂载脚本按钮
        GUI.enabled = lastCodegenJsonResult != null && lastCodegenJsonResult.ContainsKey("object_mechanisms");
        if (GUILayout.Button("Attach Scripts"))
        {
            ApplyLastCodegenJsonResult();
        }
        GUI.enabled = true;
        
        // 代码生成结果
        if (!string.IsNullOrEmpty(MechanismDesigner.codeGenerationResult))
        {
            EditorGUILayout.Space();
            var resultStyle = new GUIStyle(EditorStyles.boldLabel);
            if (EditorGUIUtility.isProSkin == false)
                resultStyle.normal.textColor = Color.black;
            EditorGUILayout.LabelField("Code Generation Results:", resultStyle);
            MechanismDesigner.codeResultScrollPosition = EditorGUILayout.BeginScrollView(MechanismDesigner.codeResultScrollPosition, GUILayout.Height(200));
            EditorGUILayout.TextArea(MechanismDesigner.codeGenerationResult, wordWrapStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            // 尝试解析JSON结果
            try
            {
                Dictionary<string, object> jsonResult = MiniJSON.Json.Deserialize(MechanismDesigner.codeGenerationResult) as Dictionary<string, object>;
                if (jsonResult != null && 
                    jsonResult.ContainsKey("template_name") && 
                    jsonResult.ContainsKey("output_file_name") && 
                    jsonResult.ContainsKey("replacements"))
                {
                    // 统一机制类型字段名，确保所有生成/保存/应用代码的分支都能批量挂载
                    string mechanismType = null;
                    if (jsonResult.ContainsKey("mechanism_type"))
                        mechanismType = jsonResult["mechanism_type"].ToString();
                    else if (jsonResult.ContainsKey("template_name"))
                        mechanismType = jsonResult["template_name"].ToString();
                    if (jsonResult.ContainsKey("target_objects") && mechanismType != null)
                    {
                        Debug.Log($"[MultiAgentRPGEditor] 批量挂载机制: {mechanismType}");
                        var targetNames = jsonResult["target_objects"] as List<object>;
                        string[] dialogueLines = null;
                        if (mechanismType == "Dialogue" && jsonResult.ContainsKey("dialogue_lines"))
                        {
                            var lines = jsonResult["dialogue_lines"] as List<object>;
                            dialogueLines = lines?.Select(l => l.ToString()).ToArray();
                        }
                        var allObjects = GameObject.FindObjectsOfType<GameObject>();
                        foreach (var objName in targetNames)
                        {
                            foreach (var go in allObjects)
                            {
                                if (go.name == objName.ToString())
                                {
                                    MechanismAttacher.AttachMechanism(go, mechanismType, dialogueLines);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception) 
            {
                EditorGUILayout.TextArea(mechanicsResult, wordWrapStyle);
            }
        }
    }
    
    // 2. 机制设计（调用task_agent）
    public async void GenerateMechanicsDesign()
    {
        isGeneratingMechanics = true;
        Repaint();
        try
        {
            var result = await AgentCommunication.CallPythonAgent("mechanics", narrativeResult);
            mechanicsResult = result;
            mechanicsImplementationResult = "";
        }
        catch (System.Exception e)
        {
            mechanicsResult = $"错误: {e.Message}";
            mechanicsImplementationResult = "";
        }
        finally
        {
            isGeneratingMechanics = false;
            Repaint();
        }
    }

    // 3. 机制实现（调用codegen_agent.py）
    public async void GenerateMechanicsImplementation()
    {
        isGeneratingMechanics = true;
        Repaint();
        try
        {
            var result = await AgentCommunication.CallPythonAgent("codegen", mechanicsResult);
            mechanicsImplementationResult = result;
            // 尝试解析返回的JSON
            try
            {
                // 检查是否为JSON格式
                if (result.Trim().StartsWith("{"))
                {
                    // 使用 LitJSON 或 Newtonsoft.Json 库可能会更好，但这里使用内置的 JsonUtility
                    // 创建一个包装类来解析JSON
                    var wrapper = new Dictionary<string, object>();
                    var hasSuccessfullyParsed = false;
                    
                    // 尝试使用System.Text.Json解析（如果可用）
                    try
                    {
                        wrapper = JsonUtility.FromJson<Dictionary<string, object>>(result);
                        hasSuccessfullyParsed = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"JsonUtility解析失败，尝试手动解析: {ex.Message}");
                        
                        // 手动解析JSON
                        if (result.Contains("template_name") && result.Contains("output_file_name"))
                        {
                            var templateNameMatch = System.Text.RegularExpressions.Regex.Match(result, "\"template_name\":\\s*\"([^\"]+)\"");
                            var outputPathMatch = System.Text.RegularExpressions.Regex.Match(result, "\"output_path\":\\s*\"([^\"]+)\"");
                            var outputFileNameMatch = System.Text.RegularExpressions.Regex.Match(result, "\"output_file_name\":\\s*\"([^\"]+)\"");
                            var replacementsMatch = System.Text.RegularExpressions.Regex.Match(result, "\"replacements\":\\s*\\{([^\\}]+)\\}");
                            
                            if (templateNameMatch.Success && outputFileNameMatch.Success)
                            {
                                wrapper["template_name"] = templateNameMatch.Groups[1].Value;
                                wrapper["output_path"] = outputPathMatch.Success ? outputPathMatch.Groups[1].Value : "Assets/Scripts/Generated";
                                wrapper["output_file_name"] = outputFileNameMatch.Groups[1].Value;
                                
                                // 解析replacements
                                if (replacementsMatch.Success)
                                {
                                    var replacements = new Dictionary<string, object>();
                                    var replacementsStr = replacementsMatch.Groups[1].Value;
                                    var keyValueMatches = System.Text.RegularExpressions.Regex.Matches(replacementsStr, "\"([^\"]+)\":\\s*\"([^\"]+)\"");
                                    
                                    foreach (System.Text.RegularExpressions.Match match in keyValueMatches)
                                    {
                                        replacements[match.Groups[1].Value] = match.Groups[2].Value;
                                    }
                                    
                                    wrapper["replacements"] = replacements;
                                }
                                
                                hasSuccessfullyParsed = true;
                            }
                        }
                    }
                    
                    if (hasSuccessfullyParsed)
                    {
                        // 应用代码生成结果
                        Debug.Log("成功解析JSON，应用代码生成结果");
                        // 删除所有 MechanismAttacher.AttachMechanism(wrapper, spawnedKeyObjects) 或类似错误调用
                        // 只保留如下结构：
                        if (wrapper.ContainsKey("target_objects") && wrapper.ContainsKey("mechanism_type"))
                        {
                            var targetNames = wrapper["target_objects"] as List<object>;
                            string mechanismType = wrapper["mechanism_type"].ToString();
                            string[] dialogueLines = null;
                            if (mechanismType == "Dialogue" && wrapper.ContainsKey("dialogue_lines"))
                            {
                                var lines = wrapper["dialogue_lines"] as List<object>;
                                dialogueLines = lines?.Select(l => l.ToString()).ToArray();
                            }
                            var allObjects = GameObject.FindObjectsOfType<GameObject>();
                            foreach (var objName in targetNames)
                            {
                                foreach (var go in allObjects)
                                {
                                    if (go.name == objName.ToString())
                                    {
                                        MechanismAttacher.AttachMechanism(go, mechanismType, dialogueLines);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("无法解析JSON结果: " + result);
                    }
                }
                else
                {
                    Debug.LogWarning("返回结果不是JSON格式，无法自动应用代码生成");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"解析或应用代码生成结果时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }
        catch (System.Exception e)
        {
            mechanicsImplementationResult = $"错误: {e.Message}";
        }
        finally
        {
            isGeneratingMechanics = false;
            Repaint();
        }
    }
    
    public string GenerateMechanicsDescription(Dictionary<string, object> mechanicsData)
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
    
    public void SaveMechanicsToFile()
    {
        if (string.IsNullOrEmpty(mechanicsResult))
        {
            EditorUtility.DisplayDialog("Warning", "No mechanics to save. Generate mechanics first.", "OK");
            return;
        }

        var path = EditorUtility.SaveFilePanel("Save Mechanics", "Assets", "rpg_mechanics", "txt");
        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, mechanicsResult);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Mechanics saved successfully!", "OK");
        }
    }
    
    public void ExportMechanicsAsJSON()
    {
        if (string.IsNullOrEmpty(mechanicsResult))
        {
            EditorUtility.DisplayDialog("Warning", "No mechanics to export. Generate mechanics first.", "OK");
            return;
        }

        // var selectedMechanisms = mechanismToggles.Where(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value); // This line is removed
        var exportData = new Dictionary<string, object>();

        // foreach (var mechanism in selectedMechanisms) // This loop is removed
        // {
        //     if (mechanismParameters.ContainsKey(mechanism.Key))
        //     {
        //         exportData[mechanism.Key] = mechanismParameters[mechanism.Key];
        //     }
        // }

        var json = JsonUtility.ToJson(new { mechanics = exportData }, true);
        
        var path = EditorUtility.SaveFilePanel("Export Mechanics", "Assets", "rpg_mechanics", "json");
        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Mechanics exported as JSON successfully!", "OK");
        }
    }
    

    
    public async void CheckServerStatus()
    {
        try
        {
            var response = await httpClient.GetAsync($"{SERVER_URL}/health");
            if (response.IsSuccessStatusCode)
            {
                serverStatus = "Connected";
            }
            else
            {
                serverStatus = "Connection Failed";
            }
        }
        catch
        {
            serverStatus = "Disconnected";
        }
        Repaint();
    }
    
    // 移除MechanismEntry、MechanismList结构体和机制Tab相关UI代码（已迁移到MechanismDesigner.cs）

    public void GeneratePreview(int stepCount)
    {
        // 使用新的地图生成管理器
        previewTex = MapGenerationManager.GenerateTerrainPreview(mapWidth, mapHeight, noiseScale, landThreshold, islandFactor, mountainThreshold, cliffHeight, landSeed, mountainSeed);
        
        // 生成分区预览
        var terrainMap = MapGenerator.GenerateTerrainTypeMap(mapWidth, mapHeight, noiseScale, landThreshold, islandFactor, mountainThreshold, landSeed, mountainSeed);
        MapGenerator.GeneratePartitionPreview(terrainMap, mapWidth, mapHeight, perlinScale, perlinStrength, landSeed, mountainSeed, stepCount, ref voronoiColors, ref partitionPreviewTex);
    }

    public void ClearAllTilemaps()
    {
        MapGenerationManager.ClearAllTilemaps(waterTilemap, landTilemap, mountainTilemap, cliffTilemap, elementsTilemap);
    }

    public void LoadNarrativeDataFromFile()
    {
        // 优先从 python_agents 目录加载，然后从 Assets/story 目录加载
        string[] possiblePaths = {
            System.IO.Path.Combine(Application.dataPath, "..", "python_agents", "narrative_steps.json"),
            "Assets/story/narrative_steps.json"
        };
        
        string loadedPath = null;
        string jsonContent = null;
        
        foreach (string filePath in possiblePaths)
        {
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    // 尝试多种编码方式读取文件
                    try
                    {
                        // 首先尝试 UTF8（无 BOM）
                        jsonContent = System.IO.File.ReadAllText(filePath, new System.Text.UTF8Encoding(false));
                    }
                    catch
                    {
                        try
                        {
                            // 如果失败，尝试 UTF8（带 BOM）
                            jsonContent = System.IO.File.ReadAllText(filePath, new System.Text.UTF8Encoding(true));
                        }
                        catch
                        {
                            // 最后尝试默认编码
                            jsonContent = System.IO.File.ReadAllText(filePath);
                        }
                    }
                    loadedPath = filePath;
                    Debug.Log($"找到叙事数据文件: {filePath}");
                    break;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"读取文件失败 {filePath}: {e.Message}");
                }
            }
        }
        
        if (jsonContent == null)
        {
            Debug.LogWarning("未找到可用的叙事数据文件");
            return;
        }
        
        try
        {
            ParseNarrativeJson(jsonContent);
            
            if (narrativeData != null && narrativeData.steps != null && narrativeData.steps.Count > 0)
            {
                isNarrativeComplete = true;
                Debug.Log($"成功加载叙事数据: {narrativeData.steps.Count} 个步骤 (来源: {loadedPath})");
            }
            else
            {
                Debug.LogWarning("加载的叙事数据无效或为空");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析叙事数据失败: {e.Message}");
        }
    }
    
    public void SaveStepsToDisk()
    {
        if (narrativeData == null || narrativeData.steps == null || narrativeData.steps.Count == 0)
        {
            Debug.LogWarning("没有可保存的叙事Step数据！");
            return;
        }
        string json = JsonUtility.ToJson(new NarrativeData { story = narrativeData.story, steps = narrativeData.steps }, true);
        
        // 直接保存到 python_agents 目录，方便 Python 服务器读取
        string pythonAgentsPath = System.IO.Path.Combine(Application.dataPath, "..", "python_agents");
        string defaultPath = System.IO.Path.Combine(pythonAgentsPath, "narrative_steps.json");
        
        string path = EditorUtility.SaveFilePanel("保存叙事Step为JSON", pythonAgentsPath, "narrative_steps.json", "json");
        if (!string.IsNullOrEmpty(path))
        {
            // 使用 UTF8 编码（无 BOM）保存，避免 Python 读取时的编码问题
            System.IO.File.WriteAllText(path, json, new System.Text.UTF8Encoding(false));
            Debug.Log($"叙事Step已保存到: {path}");
            
            // 如果保存到了 python_agents 目录，显示提示
            if (path.Contains("python_agents"))
            {
                Debug.Log("✓ 叙事数据已保存到 Python 服务器目录，服务器可以直接读取");
            }
        }
    }

    // 一次性为所有关键对象生成机制设计
    public async void GenerateMechanicsDesignAll()
    {
        isGeneratingMechanics = true;
        Repaint();
        try
        {
            // 读取 narrative_assets.json
            string assetsJson = "";
            string assetsPath = System.IO.Path.Combine(Application.dataPath, "../python_agents/narrative_assets.json");
            if (System.IO.File.Exists(assetsPath))
            {
                assetsJson = System.IO.File.ReadAllText(assetsPath);
                // 确保是有效的JSON
                try
                {
                    // 尝试解析JSON以验证其有效性
                    var testParse = MiniJSON.Json.Deserialize(assetsJson);
                    if (testParse == null)
                    {
                        Debug.LogError("[机制Agent] narrative_assets.json不是有效的JSON");
                        mechanicsResult = "错误: narrative_assets.json不是有效的JSON";
                        isGeneratingMechanics = false;
                        Repaint();
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[机制Agent] JSON解析失败: {e.Message}");
                    mechanicsResult = $"错误: JSON解析失败: {e.Message}";
                    isGeneratingMechanics = false;
                    Repaint();
                    return;
                }
            }
            else
            {
                Debug.LogWarning($"未找到 narrative_assets.json: {assetsPath}");
                mechanicsResult = $"错误: 未找到 narrative_assets.json: {assetsPath}";
                isGeneratingMechanics = false;
                Repaint();
                return;
            }
            Debug.Log("[机制Agent] 发送给Agent的输入: " + assetsJson);
            // 直接发送assetsJson，不要额外嵌套
            string design = await AgentCommunication.CallPythonAgent("mechanics", assetsJson);
            mechanicsResult = design;
            mechanicsImplementationResult = "";
            Debug.Log("[机制Agent] Agent返回: " + design.Substring(0, Mathf.Min(300, design.Length)) + (design.Length > 300 ? "..." : ""));
        }
        catch (System.Exception e)
        {
            mechanicsResult = $"错误: {e.Message}";
            mechanicsImplementationResult = "";
            Debug.LogError($"[机制Agent] 机制设计失败: {e.Message}");
        }
        finally
        {
            isGeneratingMechanics = false;
            Repaint();
        }
    }

    private async Task GenerateAndAttachCodeAsync()
    {
        var jsonResult = await MechanismDesigner.GenerateCodeAndGetResult(this);
        if (jsonResult != null &&
            jsonResult.ContainsKey("template_name") &&
            jsonResult.ContainsKey("output_file_name") &&
            jsonResult.ContainsKey("replacements"))
        {
            // 统一机制类型字段名，确保所有生成/保存/应用代码的分支都能批量挂载
            string mechanismType = null;
            if (jsonResult.ContainsKey("mechanism_type"))
                mechanismType = jsonResult["mechanism_type"].ToString();
            else if (jsonResult.ContainsKey("template_name"))
                mechanismType = jsonResult["template_name"].ToString();
            if (jsonResult.ContainsKey("target_objects") && mechanismType != null)
            {
                Debug.Log($"[MultiAgentRPGEditor] 批量挂载机制: {mechanismType}");
                var targetNames = jsonResult["target_objects"] as List<object>;
                string[] dialogueLines = null;
                if (mechanismType == "Dialogue" && jsonResult.ContainsKey("dialogue_lines"))
                {
                    var lines = jsonResult["dialogue_lines"] as List<object>;
                    dialogueLines = lines?.Select(l => l.ToString()).ToArray();
                }
                var allObjects = GameObject.FindObjectsOfType<GameObject>();
                foreach (var objName in targetNames)
                {
                    foreach (var go in allObjects)
                    {
                        if (go.name == objName.ToString())
                        {
                            MechanismAttacher.AttachMechanism(go, mechanismType, dialogueLines);
                        }
                    }
                }
            }
        }
        // 优化object_mechanisms处理逻辑
        if (!jsonResult.ContainsKey("object_mechanisms"))
        {
            List<object> objectNames = null;
            if (jsonResult.ContainsKey("scene_objects"))
            {
                var sceneObjs = jsonResult["scene_objects"] as List<object>;
                objectNames = sceneObjs?.Select(obj => {
                    if (obj is Dictionary<string, object> dict && dict.ContainsKey("name"))
                        return dict["name"].ToString();
                    return null;
                }).Where(n => !string.IsNullOrEmpty(n)).Cast<object>().ToList();
            }
            else if (jsonResult.ContainsKey("target_objects"))
            {
                objectNames = jsonResult["target_objects"] as List<object>;
            }
            string mechanismType = null;
            if (jsonResult.ContainsKey("mechanism_type"))
                mechanismType = jsonResult["mechanism_type"].ToString();
            else if (jsonResult.ContainsKey("template_name"))
                mechanismType = jsonResult["template_name"].ToString();
            if (objectNames != null && mechanismType != null)
            {
                var autoMap = new Dictionary<string, object>();
                foreach (var n in objectNames)
                    autoMap[n.ToString()] = mechanismType;
                jsonResult["object_mechanisms"] = autoMap;
            }
        }
        if (jsonResult.ContainsKey("object_mechanisms"))
        {
            var objectMechanisms = jsonResult["object_mechanisms"] as Dictionary<string, object>;
            var allObjects = GameObject.FindObjectsOfType<GameObject>();
            // 支持对话内容为每个对象单独指定
            Dictionary<string, string[]> dialogueMap = null;
            if (jsonResult.ContainsKey("dialogue_lines"))
            {
                dialogueMap = new Dictionary<string, string[]>();
                var dialogueDict = jsonResult["dialogue_lines"] as Dictionary<string, object>;
                foreach (var kv in dialogueDict)
                {
                    var linesList = kv.Value as List<object>;
                    if (linesList != null)
                        dialogueMap[kv.Key] = linesList.Select(l => l.ToString()).ToArray();
                }
            }
            foreach (var kvp in objectMechanisms)
            {
                string objName = kvp.Key;
                string mechanismType = kvp.Value.ToString();
                foreach (var go in allObjects)
                {
                    if (go.name == objName)
                    {
                        string[] lines = null;
                        if (mechanismType == "Dialogue" && dialogueMap != null && dialogueMap.ContainsKey(objName))
                            lines = dialogueMap[objName];
                        MechanismAttacher.AttachMechanism(go, mechanismType, lines);
                    }
                }
            }
        }
        lastCodegenJsonResult = jsonResult; // 缓存结果
    }

    // 新增：批量挂载脚本方法
    private void ApplyLastCodegenJsonResult()
    {
        if (lastCodegenJsonResult == null || !lastCodegenJsonResult.ContainsKey("object_mechanisms"))
        {
            Debug.LogWarning("没有可用的 object_mechanisms，无法挂载脚本。");
            return;
        }
        var objectMechanisms = lastCodegenJsonResult["object_mechanisms"] as Dictionary<string, object>;
        var allObjects = GameObject.FindObjectsOfType<GameObject>();
        // 支持对话内容为每个对象单独指定
        Dictionary<string, string[]> dialogueMap = null;
        if (lastCodegenJsonResult.ContainsKey("dialogue_lines"))
        {
            dialogueMap = new Dictionary<string, string[]>();
            var dialogueDict = lastCodegenJsonResult["dialogue_lines"] as Dictionary<string, object>;
            foreach (var kv in dialogueDict)
            {
                var linesList = kv.Value as List<object>;
                if (linesList != null)
                    dialogueMap[kv.Key] = linesList.Select(l => l.ToString()).ToArray();
            }
        }
        foreach (var kvp in objectMechanisms)
        {
            string objName = kvp.Key;
            string mechanismType = kvp.Value.ToString();
            foreach (var go in allObjects)
            {
                if (go.name == objName)
                {
                    string[] lines = null;
                    if (mechanismType == "Dialogue" && dialogueMap != null && dialogueMap.ContainsKey(objName))
                        lines = dialogueMap[objName];
                    MechanismAttacher.AttachMechanism(go, mechanismType, lines);
                }
            }
        }
        Debug.Log("[MultiAgentRPGEditor] 挂载脚本完成");
    }

    // 新增：保存Texture2D为PNG的方法
    private void SaveTextureAsPNG(Texture2D tex, string defaultName)
    {
        string path = EditorUtility.SaveFilePanel("保存图像", "Assets", defaultName, "png");
        if (!string.IsNullOrEmpty(path))
        {
            byte[] pngData = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, pngData);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("保存成功", $"图像已保存到: {path}", "确定");
        }
    }
    
    /// <summary>
    /// 创建新的 TilemapConfig 文件
    /// </summary>
    private void CreateNewTilemapConfig()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "创建 TilemapConfig",
            "TilemapConfig",
            "asset",
            "请选择保存位置",
            "Assets"
        );
        
        if (!string.IsNullOrEmpty(path))
        {
            TilemapConfig config = ScriptableObject.CreateInstance<TilemapConfig>();
            
            // 将当前的配置保存到新文件中（保存 GameObject）
            config.waterTilemapObject = waterTilemap != null ? waterTilemap.gameObject : null;
            config.landTilemapObject = landTilemap != null ? landTilemap.gameObject : null;
            config.mountainTilemapObject = mountainTilemap != null ? mountainTilemap.gameObject : null;
            config.cliffTilemapObject = cliffTilemap != null ? cliffTilemap.gameObject : null;
            config.elementsTilemapObject = elementsTilemap != null ? elementsTilemap.gameObject : null;
            
            config.waterRuleTile = waterRuleTile;
            config.landRuleTile = landRuleTile;
            config.mountainRuleTile = mountainRuleTile;
            config.cliffRuleTile = cliffRuleTile;
            
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            tilemapConfig = config;
            
            Debug.Log($"[RPGEditor] 已创建 TilemapConfig: {path}");
            EditorUtility.DisplayDialog("创建成功", $"TilemapConfig 已创建：\n{path}\n\n现在可以在这个文件中永久保存你的 Tilemap 配置了！", "确定");
            
            // 选中新创建的文件
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
    }
    
    /// <summary>
    /// 将当前配置保存到 TilemapConfig 文件
    /// </summary>
    private void SaveCurrentConfigToFile()
    {
        if (tilemapConfig == null)
        {
            EditorUtility.DisplayDialog("错误", "没有加载 TilemapConfig 文件！", "确定");
            return;
        }
        
        tilemapConfig.waterTilemapObject = waterTilemap != null ? waterTilemap.gameObject : null;
        tilemapConfig.landTilemapObject = landTilemap != null ? landTilemap.gameObject : null;
        tilemapConfig.mountainTilemapObject = mountainTilemap != null ? mountainTilemap.gameObject : null;
        tilemapConfig.cliffTilemapObject = cliffTilemap != null ? cliffTilemap.gameObject : null;
        tilemapConfig.elementsTilemapObject = elementsTilemap != null ? elementsTilemap.gameObject : null;
        
        tilemapConfig.waterRuleTile = waterRuleTile;
        tilemapConfig.landRuleTile = landRuleTile;
        tilemapConfig.mountainRuleTile = mountainRuleTile;
        tilemapConfig.cliffRuleTile = cliffRuleTile;
        
        EditorUtility.SetDirty(tilemapConfig);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[RPGEditor] 已保存配置到 {AssetDatabase.GetAssetPath(tilemapConfig)}");
        EditorUtility.DisplayDialog("保存成功", "当前配置已保存到 TilemapConfig 文件！\n\n下次打开编辑器时会自动加载这些配置。", "确定");
    }
}
