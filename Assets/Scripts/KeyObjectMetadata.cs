using UnityEngine;

/// <summary>
/// 存储关键对象元数据，避免在场景生成阶段挂载完整游戏脚本
/// 该组件在代码生成阶段会被用于查找并正确挂载相应的游戏逻辑脚本
/// </summary>
[AddComponentMenu("RPG/Key Object Metadata")]
public class KeyObjectMetadata : MonoBehaviour
{
    /// <summary>
    /// 对象类型：Character, Item 等
    /// </summary>
    public string objectType;
    
    /// <summary>
    /// 显示名称
    /// </summary>
    public string displayName;
    
    /// <summary>
    /// 描述
    /// </summary>
    public string description;
    
    /// <summary>
    /// 是否已经被正式脚本替换
    /// </summary>
    public bool hasBeenProcessed = false;
} 