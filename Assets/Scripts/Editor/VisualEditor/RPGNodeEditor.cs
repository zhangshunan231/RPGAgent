using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System.Linq;

public class RPGNodeEditor : EditorWindow
{
    private RPGGraphView graphView;
    private Toolbar toolbar;
    private Label statusLabel;
    private VisualElement nodeCreationPanel;

    // 移除Tools菜单项，改为内部调用
    public static void ShowWindow()
    {
        var window = GetWindow<RPGNodeEditor>();
        window.titleContent = new GUIContent("RPG节点编辑器");
        window.minSize = new Vector2(1000, 700);
    }

    private void CreateGUI()
    {
        // 创建根容器
        var root = rootVisualElement;
        
        // 创建主布局
        var mainContainer = new VisualElement();
        mainContainer.style.flexDirection = FlexDirection.Row;
        mainContainer.style.flexGrow = 1;
        
        // 创建左侧面板
        CreateLeftPanel(mainContainer);
        
        // 创建右侧GraphView
        CreateRightPanel(mainContainer);
        
        root.Add(mainContainer);
        
        // 创建底部状态栏
        CreateStatusBar();
        
        // 添加样式
        AddStyles();
    }

    private void CreateLeftPanel(VisualElement parent)
    {
        var leftPanel = new VisualElement();
        leftPanel.style.width = 250;
        leftPanel.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
        leftPanel.style.borderRightWidth = 1;
        leftPanel.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
        leftPanel.style.paddingTop = 10;
        leftPanel.style.paddingBottom = 10;
        leftPanel.style.paddingLeft = 10;
        leftPanel.style.paddingRight = 10;
        
        // 标题
        var titleLabel = new Label("节点创建");
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.fontSize = 16;
        titleLabel.style.color = Color.white;
        titleLabel.style.marginBottom = 15;
        leftPanel.Add(titleLabel);
        
        // 节点类型说明
        var descriptionLabel = new Label("选择要创建的节点类型：");
        descriptionLabel.style.color = Color.white;
        descriptionLabel.style.marginBottom = 10;
        leftPanel.Add(descriptionLabel);
        
        // 创建节点按钮
        CreateNodeButton(leftPanel, "📖 叙事节点", "生成RPG故事和剧情", () => AddNarrativeNode());
        CreateNodeButton(leftPanel, "🗺️ 场景节点", "生成地图和场景参数", () => AddSceneNode());
        CreateNodeButton(leftPanel, "⚙️ 机制节点", "生成游戏机制和系统", () => AddMechanicsNode());
        
        // 分隔线
        var separator = new VisualElement();
        separator.style.height = 1;
        separator.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
        separator.style.marginTop = 20;
        separator.style.marginBottom = 20;
        leftPanel.Add(separator);
        
        // 操作按钮
        var operationLabel = new Label("操作");
        operationLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        operationLabel.style.color = Color.white;
        operationLabel.style.marginBottom = 10;
        leftPanel.Add(operationLabel);
        
        CreateActionButton(leftPanel, "▶️ 执行生成", "运行整个节点图", () => ExecuteGraph());
        CreateActionButton(leftPanel, "🗑️ 清除所有", "删除所有节点", () => ClearAllNodes());
        CreateActionButton(leftPanel, "💾 保存图", "保存当前节点图", () => SaveGraph());
        CreateActionButton(leftPanel, "📂 加载图", "加载已保存的节点图", () => LoadGraph());
        
        parent.Add(leftPanel);
    }

    private void CreateRightPanel(VisualElement parent)
    {
        var rightPanel = new VisualElement();
        rightPanel.style.flexGrow = 1;
        
        // 创建GraphView
        graphView = new RPGGraphView
        {
            name = "RPG Graph"
        };
        
        graphView.StretchToParentSize();
        rightPanel.Add(graphView);
        
        parent.Add(rightPanel);
    }

    private void CreateNodeButton(VisualElement parent, string text, string tooltip, System.Action onClick)
    {
        var button = new Button(onClick);
        button.text = text;
        button.tooltip = tooltip;
        button.style.height = 40;
        button.style.marginBottom = 8;
        button.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
        button.style.borderLeftWidth = 1;
        button.style.borderRightWidth = 1;
        button.style.borderTopWidth = 1;
        button.style.borderBottomWidth = 1;
        button.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f);
        button.style.borderRightColor = new Color(0.5f, 0.5f, 0.5f);
        button.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f);
        button.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f);
        button.style.color = Color.white;
        
        button.RegisterCallback<MouseEnterEvent>((evt) => {
            button.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
        });
        
        button.RegisterCallback<MouseLeaveEvent>((evt) => {
            button.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
        });
        
        parent.Add(button);
    }

    private void CreateActionButton(VisualElement parent, string text, string tooltip, System.Action onClick)
    {
        var button = new Button(onClick);
        button.text = text;
        button.tooltip = tooltip;
        button.style.height = 35;
        button.style.marginBottom = 6;
        button.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
        button.style.borderLeftWidth = 1;
        button.style.borderRightWidth = 1;
        button.style.borderTopWidth = 1;
        button.style.borderBottomWidth = 1;
        button.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
        button.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
        button.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
        button.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
        button.style.color = Color.white;
        button.style.fontSize = 12;
        
        button.RegisterCallback<MouseEnterEvent>((evt) => {
            button.style.backgroundColor = new Color(0.35f, 0.35f, 0.35f);
        });
        
        button.RegisterCallback<MouseLeaveEvent>((evt) => {
            button.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
        });
        
        parent.Add(button);
    }

    private void CreateStatusBar()
    {
        var statusBar = new VisualElement();
        statusBar.style.height = 25;
        statusBar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        statusBar.style.flexDirection = FlexDirection.Row;
        statusBar.style.alignItems = Align.Center;
        statusBar.style.paddingLeft = 10;
        statusBar.style.paddingRight = 10;
        statusBar.style.borderTopWidth = 1;
        statusBar.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
        
        statusLabel = new Label("就绪 - 在左侧面板选择要创建的节点类型");
        statusLabel.style.color = Color.white;
        statusLabel.style.fontSize = 12;
        statusBar.Add(statusLabel);
        
        rootVisualElement.Add(statusBar);
    }

    private void AddStyles()
    {
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/VisualEditor/RPGNodeEditor.uss");
        if (styleSheet != null)
        {
            rootVisualElement.styleSheets.Add(styleSheet);
        }
    }

    private void AddNarrativeNode()
    {
        var node = new NarrativeNode();
        node.SetPosition(new Rect(100, 100, 300, 400));
        graphView.AddElement(node);
        UpdateStatus("✅ 已添加叙事节点");
    }

    private void AddSceneNode()
    {
        var node = new SceneNode();
        node.SetPosition(new Rect(500, 100, 300, 400));
        graphView.AddElement(node);
        UpdateStatus("✅ 已添加场景节点");
    }

    private void AddMechanicsNode()
    {
        var node = new MechanicsNode();
        node.SetPosition(new Rect(900, 100, 300, 400));
        graphView.AddElement(node);
        UpdateStatus("✅ 已添加机制节点");
    }

    private void ExecuteGraph()
    {
        UpdateStatus("🔄 执行生成中...");
        // TODO: 实现图执行逻辑
        UpdateStatus("⚠️ 图执行功能待实现");
    }

    private void ClearAllNodes()
    {
        var nodesToRemove = graphView.nodes.ToList();
        foreach (var node in nodesToRemove)
        {
            graphView.RemoveElement(node);
        }
        UpdateStatus("🗑️ 已清除所有节点");
    }

    private void SaveGraph()
    {
        UpdateStatus("💾 保存功能待实现");
    }

    private void LoadGraph()
    {
        UpdateStatus("📂 加载功能待实现");
    }

    private void UpdateStatus(string message)
    {
        if (statusLabel != null)
        {
            statusLabel.text = message;
        }
    }
} 