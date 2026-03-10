using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;

[System.Serializable]
public class ExportAssetEntry
{
    public string name;
    public string type;
    public List<string> aliases;
    public string description;
}

[System.Serializable]
public class ExportAssetData
{
    public List<ExportAssetEntry> assets;
}

public class AssetIndexExporter
{
    [MenuItem("Tools/导出AssetIndex为JSON")]
    public static void ExportAssetIndexToJson()
    {
        ExportAssetIndex();
    }
    
    // 供外部调用的静态方法
    public static void ExportAssetIndex()
    {
        Debug.Log("[AssetIndexExporter] 开始导出AssetIndex...");
        
        var asset = AssetDatabase.LoadAssetAtPath<AssetIndex>("Assets/Defination/AssetIndex/AssetIndex.asset");
        if (asset == null)
        {
            Debug.LogError("[AssetIndexExporter] 未找到AssetIndex.asset");
            return;
        }
        
        Debug.Log($"[AssetIndexExporter] 找到AssetIndex，entries数量: {asset.entries?.Count ?? 0}");
        
        var exportData = new ExportAssetData();
        exportData.assets = new List<ExportAssetEntry>();
        
        if (asset.entries != null)
        {
            foreach (var entry in asset.entries)
            {
                if (entry != null)
                {
                    var exportEntry = new ExportAssetEntry
                    {
                        name = entry.name ?? "",
                        type = entry.type.ToString().ToLower(),
                        aliases = entry.aliases ?? new List<string>(),
                        description = entry.description ?? ""
                    };
                    exportData.assets.Add(exportEntry);
                    string aliasesStr = exportEntry.aliases != null && exportEntry.aliases.Count > 0 ? 
                        $"别名:[{string.Join(", ", exportEntry.aliases)}]" : "无别名";
                    Debug.Log($"[AssetIndexExporter] 添加资产: {exportEntry.name} ({exportEntry.type}) - {exportEntry.description} - {aliasesStr}");
                }
            }
        }
        
        Debug.Log($"[AssetIndexExporter] 准备导出 {exportData.assets.Count} 个资产");
        
        var json = JsonUtility.ToJson(exportData, true);
        string exportPath = "python_agents/narrative_assets.json";
        
        try
        {
            File.WriteAllText(exportPath, json, System.Text.Encoding.UTF8);
            Debug.Log($"[AssetIndexExporter] 已导出到 {exportPath}");
            Debug.Log($"[AssetIndexExporter] JSON内容长度: {json.Length} 字符");
            Debug.Log($"[AssetIndexExporter] JSON内容预览: {json.Substring(0, Math.Min(200, json.Length))}...");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AssetIndexExporter] 导出失败: {e.Message}");
        }
    }
} 