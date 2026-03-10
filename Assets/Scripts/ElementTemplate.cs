using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "ElementTemplate", menuName = "RPG/ElementTemplate", order = 1)]
public class ElementTemplate : ScriptableObject
{
    public string elementName;
    public int width = 1;
    public int height = 1;
    public TileBase[] tiles; // 行优先展开，如2x2: [左下, 右下, 左上, 右上]
    // 可扩展：类型、可放置地形、优先级等
} 