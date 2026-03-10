using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "RPG/AssetIndex", fileName = "AssetIndex")]
public class AssetIndex : ScriptableObject
{
    public List<AssetEntry> entries;
    
    public enum AssetType 
    { 
        MainCharacter,  // 主角
        NPC,           // 非玩家角色
        Enemy,         // 敌人
        Props          // 道具/物品
    }
    
    [System.Serializable]
    public class AssetEntry
    {
        public string name;         // 资产主名
        public List<string> aliases; // 别名/关键词
        public GameObject prefab;   // 角色/道具Prefab
        public Sprite icon;         // 可选：图标
        public AssetType type;      // 资产类型：MainCharacter/NPC/Enemy/Props
        public string description;  // 可选：描述
    }
} 