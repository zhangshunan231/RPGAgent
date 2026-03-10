using UnityEngine;
using UnityEditor;

public class NodeEditorTest : MonoBehaviour
{
    [MenuItem("Tools/打开RPG节点编辑器")]
    public static void OpenRPGNodeEditor()
    {
        // 打开RPG节点编辑器
        RPGNodeEditor.ShowWindow();
        
        Debug.Log("RPG节点编辑器已打开！");
        Debug.Log("使用说明：");
        Debug.Log("1. 在左侧面板点击按钮创建节点");
        Debug.Log("2. 拖拽节点调整位置");
        Debug.Log("3. 连接节点端口（如果支持）");
        Debug.Log("4. 配置节点参数");
        Debug.Log("5. 点击执行生成运行");
    }
    
    [MenuItem("Tools/测试节点创建")]
    public static void TestNodeCreation()
    {
        var window = EditorWindow.GetWindow<RPGNodeEditor>();
        var graphView = window.GetType().GetField("graphView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(window) as RPGGraphView;
        
        if (graphView != null)
        {
            // 测试创建各种节点
            var narrativeNode = new NarrativeNode();
            var sceneNode = new SceneNode();
            var mechanicsNode = new MechanicsNode();
            
            // 设置节点位置
            narrativeNode.SetPosition(new Rect(100, 100, 300, 400));
            sceneNode.SetPosition(new Rect(500, 100, 300, 400));
            mechanicsNode.SetPosition(new Rect(900, 100, 300, 400));
            
            // 添加到图形视图
            graphView.AddElement(narrativeNode);
            graphView.AddElement(sceneNode);
            graphView.AddElement(mechanicsNode);
            
            Debug.Log("✅ 测试节点已创建并添加到画布");
            Debug.Log("📖 叙事节点 - 位置: (100, 100)");
            Debug.Log("🗺️ 场景节点 - 位置: (500, 100)");
            Debug.Log("⚙️ 机制节点 - 位置: (900, 100)");
        }
        else
        {
            Debug.LogError("❌ 无法获取GraphView引用");
        }
    }
} 