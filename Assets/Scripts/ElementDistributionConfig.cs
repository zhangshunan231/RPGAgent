using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ElementDistributionConfig", menuName = "RPG/ElementDistributionConfig", order = 2)]
public class ElementDistributionConfig : ScriptableObject
{
    public enum DistributionMode
    {
        ByCount,    // 按数量分布
        ByDensity   // 按密度分布（每100格子的数量）
    }

    [System.Serializable]
    public class ElementEntry
    {
        public ElementTemplate template;
        public float count = 1f; // 该元素在本分区内的数量（ByCount模式）或每100格子的数量（ByDensity模式）
        public int minSpacing = 0; // 元素间最小间距（格）
        public DistributionMode distributionMode = DistributionMode.ByDensity; // 该元素的分布模式，默认密度
    }
    
    public List<ElementEntry> elements;
    public LocationType location; // 适用的Location类型，使用全局定义
} 