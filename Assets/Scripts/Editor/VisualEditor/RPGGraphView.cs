using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.Linq;

public class RPGGraphView : GraphView
{
    public RPGGraphView()
    {
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        
        // 添加拖拽功能
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        
        // 设置网格背景
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
        
        // 设置样式
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/VisualEditor/RPGGraphView.uss");
        if (styleSheet != null)
        {
            styleSheets.Add(styleSheet);
        }
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        
        ports.ForEach(port =>
        {
            if (startPort.node != port.node && startPort.direction != port.direction)
            {
                compatiblePorts.Add(port);
            }
        });
        
        return compatiblePorts;
    }

    // 公共方法，供外部调用创建节点
    public void CreateNodeAtPosition<T>(Vector2 position) where T : RPGNode, new()
    {
        var node = new T();
        node.SetPosition(new Rect(position, Vector2.zero));
        AddElement(node);
    }
} 