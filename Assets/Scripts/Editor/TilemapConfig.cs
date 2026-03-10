using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

/// <summary>
/// Tilemap 配置 - 用于永久化保存 Tilemap 和 RuleTile 的引用
/// </summary>
[CreateAssetMenu(fileName = "TilemapConfig", menuName = "RPG/Tilemap Config", order = 1)]
public class TilemapConfig : ScriptableObject
{
    [Header("Tilemap 引用")]
    [Tooltip("水层 Tilemap")]
    [SerializeField] private Tilemap waterTilemapRef;
    
    [Tooltip("陆地层 Tilemap")]
    [SerializeField] private Tilemap landTilemapRef;
    
    [Tooltip("山地层 Tilemap")]
    [SerializeField] private Tilemap mountainTilemapRef;
    
    [Tooltip("悬崖层 Tilemap")]
    [SerializeField] private Tilemap cliffTilemapRef;
    
    [Tooltip("元素层 Tilemap（放置角色和道具）")]
    [SerializeField] private Tilemap elementsTilemapRef;
    
    // 使用 Object 引用来避免序列化问题
    [Header("Tilemap GameObject 引用（拖拽 GameObject）")]
    [Tooltip("拖拽包含 Water Tilemap 组件的 GameObject")]
    public GameObject waterTilemapObject;
    
    [Tooltip("拖拽包含 Land Tilemap 组件的 GameObject")]
    public GameObject landTilemapObject;
    
    [Tooltip("拖拽包含 Mountain Tilemap 组件的 GameObject")]
    public GameObject mountainTilemapObject;
    
    [Tooltip("拖拽包含 Cliff Tilemap 组件的 GameObject")]
    public GameObject cliffTilemapObject;
    
    [Tooltip("拖拽包含 Elements Tilemap 组件的 GameObject")]
    public GameObject elementsTilemapObject;
    
    // 属性访问器，自动从 GameObject 获取 Tilemap
    public Tilemap waterTilemap => waterTilemapObject != null ? waterTilemapObject.GetComponent<Tilemap>() : null;
    public Tilemap landTilemap => landTilemapObject != null ? landTilemapObject.GetComponent<Tilemap>() : null;
    public Tilemap mountainTilemap => mountainTilemapObject != null ? mountainTilemapObject.GetComponent<Tilemap>() : null;
    public Tilemap cliffTilemap => cliffTilemapObject != null ? cliffTilemapObject.GetComponent<Tilemap>() : null;
    public Tilemap elementsTilemap => elementsTilemapObject != null ? elementsTilemapObject.GetComponent<Tilemap>() : null;
    
    [Header("Default RuleTile 引用")]
    [Tooltip("水域 RuleTile")]
    public TileBase waterRuleTile;
    
    [Tooltip("陆地 RuleTile")]
    public TileBase landRuleTile;
    
    [Tooltip("山地 RuleTile")]
    public TileBase mountainRuleTile;
    
    [Tooltip("悬崖 RuleTile")]
    public TileBase cliffRuleTile;
    
    /// <summary>
    /// 验证配置是否完整
    /// </summary>
    public bool IsValid()
    {
        return waterTilemapObject != null && 
               landTilemapObject != null && 
               mountainTilemapObject != null && 
               cliffTilemapObject != null &&
               waterRuleTile != null &&
               landRuleTile != null &&
               mountainRuleTile != null &&
               cliffRuleTile != null;
    }
    
    /// <summary>
    /// 获取缺失的配置项
    /// </summary>
    public string GetMissingItems()
    {
        var missing = new System.Collections.Generic.List<string>();
        
        if (waterTilemapObject == null) missing.Add("Water Tilemap GameObject");
        if (landTilemapObject == null) missing.Add("Land Tilemap GameObject");
        if (mountainTilemapObject == null) missing.Add("Mountain Tilemap GameObject");
        if (cliffTilemapObject == null) missing.Add("Cliff Tilemap GameObject");
        if (waterRuleTile == null) missing.Add("Water RuleTile");
        if (landRuleTile == null) missing.Add("Land RuleTile");
        if (mountainRuleTile == null) missing.Add("Mountain RuleTile");
        if (cliffRuleTile == null) missing.Add("Cliff RuleTile");
        
        return string.Join(", ", missing);
    }
}

#if UNITY_EDITOR
/// <summary>
/// TilemapConfig 的自定义 Inspector
/// </summary>
[CustomEditor(typeof(TilemapConfig))]
public class TilemapConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TilemapConfig config = (TilemapConfig)target;
        
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("配置状态", EditorStyles.boldLabel);
        
        if (config.IsValid())
        {
            EditorGUILayout.HelpBox("✅ 配置完整！所有必需的 Tilemap 和 RuleTile 都已设置。", MessageType.Info);
        }
        else
        {
            string missing = config.GetMissingItems();
            EditorGUILayout.HelpBox($"❌ 配置不完整！\n缺少: {missing}", MessageType.Warning);
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("自动查找场景中的 Tilemap", GUILayout.Height(30)))
        {
            AutoFindTilemaps(config);
        }
        
        if (GUILayout.Button("自动查找项目中的 RuleTile", GUILayout.Height(30)))
        {
            AutoFindRuleTiles(config);
        }
    }
    
    private void AutoFindTilemaps(TilemapConfig config)
    {
        // 查找场景中所有的 Tilemap
        var allTilemaps = FindObjectsOfType<Tilemap>();
        
        foreach (var tilemap in allTilemaps)
        {
            string name = tilemap.gameObject.name.ToLower();
            
            if (name.Contains("water") && config.waterTilemapObject == null)
            {
                config.waterTilemapObject = tilemap.gameObject;
                Debug.Log($"找到 Water Tilemap: {tilemap.gameObject.name}");
            }
            else if (name.Contains("land") && config.landTilemapObject == null)
            {
                config.landTilemapObject = tilemap.gameObject;
                Debug.Log($"找到 Land Tilemap: {tilemap.gameObject.name}");
            }
            else if (name.Contains("mountain") && config.mountainTilemapObject == null)
            {
                config.mountainTilemapObject = tilemap.gameObject;
                Debug.Log($"找到 Mountain Tilemap: {tilemap.gameObject.name}");
            }
            else if (name.Contains("cliff") && config.cliffTilemapObject == null)
            {
                config.cliffTilemapObject = tilemap.gameObject;
                Debug.Log($"找到 Cliff Tilemap: {tilemap.gameObject.name}");
            }
            else if (name.Contains("element") && config.elementsTilemapObject == null)
            {
                config.elementsTilemapObject = tilemap.gameObject;
                Debug.Log($"找到 Elements Tilemap: {tilemap.gameObject.name}");
            }
        }
        
        EditorUtility.SetDirty(config);
        Debug.Log("自动查找 Tilemap 完成！");
    }
    
    private void AutoFindRuleTiles(TilemapConfig config)
    {
        // 查找项目中所有的 RuleTile
        string[] guids = AssetDatabase.FindAssets("t:RuleTile");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            string name = tile.name.ToLower();
            
            if (name.Contains("water") && config.waterRuleTile == null)
            {
                config.waterRuleTile = tile;
                Debug.Log($"找到 Water RuleTile: {tile.name}");
            }
            else if (name.Contains("land") || name.Contains("grass"))
            {
                if (config.landRuleTile == null)
                {
                    config.landRuleTile = tile;
                    Debug.Log($"找到 Land RuleTile: {tile.name}");
                }
            }
            else if (name.Contains("mountain") && config.mountainRuleTile == null)
            {
                config.mountainRuleTile = tile;
                Debug.Log($"找到 Mountain RuleTile: {tile.name}");
            }
            else if (name.Contains("cliff") && config.cliffRuleTile == null)
            {
                config.cliffRuleTile = tile;
                Debug.Log($"找到 Cliff RuleTile: {tile.name}");
            }
        }
        
        EditorUtility.SetDirty(config);
        Debug.Log("自动查找 RuleTile 完成！");
    }
}
#endif

