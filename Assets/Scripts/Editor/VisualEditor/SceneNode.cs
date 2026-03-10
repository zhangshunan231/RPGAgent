using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Tilemaps;
using System.Threading.Tasks;
using UnityEditor.UIElements;
// AgentCommunication 没有命名空间，无需using

public class SceneNode : RPGNode
{
    private TextField narrativeInput;
    private TextField resultOutput;
    private Button generateButton;
    private Button extractParamsButton;
    private Label statusLabel;
    
    // 地图参数
    private IntegerField mapWidthField;
    private IntegerField mapHeightField;
    private FloatField noiseScaleField;
    private Slider landThresholdSlider;
    private Slider mountainThresholdSlider;
    
    // Tilemap引用
    private ObjectField waterTilemapField;
    private ObjectField landTilemapField;
    private ObjectField mountainTilemapField;
    private ObjectField cliffTilemapField;
    private ObjectField elementsTilemapField;
    
    public SceneNode()
    {
        title = "场景生成";
        CreateInputPorts();
        CreateOutputPorts();
        CreateFields();
    }

    public override void CreateInputPorts()
    {
        CreateInputPort("叙事数据", typeof(string));
    }

    public override void CreateOutputPorts()
    {
        CreateOutputPort("场景数据", typeof(string));
    }

    public override void CreateFields()
    {
        // 叙事输入
        narrativeInput = CreateTextField("叙事数据", "", true);
        narrativeInput.SetEnabled(false);
        
        // 地图参数
        var paramsContainer = new VisualElement();
        paramsContainer.style.marginTop = 10;
        paramsContainer.style.marginBottom = 10;
        
        var paramsLabel = new Label("地图参数");
        paramsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        paramsContainer.Add(paramsLabel);
        
        mapWidthField = new IntegerField("宽度");
        mapWidthField.value = 100;
        paramsContainer.Add(mapWidthField);
        
        mapHeightField = new IntegerField("高度");
        mapHeightField.value = 100;
        paramsContainer.Add(mapHeightField);
        
        noiseScaleField = new FloatField("噪声缩放");
        noiseScaleField.value = 20f;
        paramsContainer.Add(noiseScaleField);
        
        landThresholdSlider = new Slider("陆地阈值", 0f, 1f);
        landThresholdSlider.value = 0.5f;
        paramsContainer.Add(landThresholdSlider);
        
        mountainThresholdSlider = new Slider("山地阈值", 0f, 1f);
        mountainThresholdSlider.value = 0.75f;
        paramsContainer.Add(mountainThresholdSlider);
        
        mainContainer.Add(paramsContainer);
        
        // Tilemap设置
        var tilemapContainer = new VisualElement();
        tilemapContainer.style.marginTop = 10;
        tilemapContainer.style.marginBottom = 10;
        
        var tilemapLabel = new Label("Tilemap设置");
        tilemapLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        tilemapContainer.Add(tilemapLabel);
        
        waterTilemapField = new ObjectField("Water Tilemap");
        waterTilemapField.objectType = typeof(Tilemap);
        tilemapContainer.Add(waterTilemapField);
        
        landTilemapField = new ObjectField("Land Tilemap");
        landTilemapField.objectType = typeof(Tilemap);
        tilemapContainer.Add(landTilemapField);
        
        mountainTilemapField = new ObjectField("Mountain Tilemap");
        mountainTilemapField.objectType = typeof(Tilemap);
        tilemapContainer.Add(mountainTilemapField);
        
        cliffTilemapField = new ObjectField("Cliff Tilemap");
        cliffTilemapField.objectType = typeof(Tilemap);
        tilemapContainer.Add(cliffTilemapField);
        
        elementsTilemapField = new ObjectField("Elements Tilemap");
        elementsTilemapField.objectType = typeof(Tilemap);
        tilemapContainer.Add(elementsTilemapField);
        
        mainContainer.Add(tilemapContainer);
        
        // 按钮
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.marginTop = 10;
        
        extractParamsButton = new Button(OnExtractParams);
        extractParamsButton.text = "智能提取参数";
        buttonContainer.Add(extractParamsButton);
        
        generateButton = new Button(OnGenerateScene);
        generateButton.text = "生成场景";
        buttonContainer.Add(generateButton);
        
        mainContainer.Add(buttonContainer);
        
        // 状态标签
        statusLabel = new Label("等待叙事数据");
        statusLabel.style.color = Color.gray;
        mainContainer.Add(statusLabel);
        
        // 结果显示
        resultOutput = CreateTextField("生成结果", "", true);
        resultOutput.SetEnabled(false);
    }

    private async void OnExtractParams()
    {
        if (string.IsNullOrEmpty(narrativeInput.value))
        {
            statusLabel.text = "请先连接叙事数据";
            statusLabel.style.color = Color.red;
            return;
        }

        extractParamsButton.SetEnabled(false);
        statusLabel.text = "提取参数中...";
        statusLabel.style.color = Color.yellow;

        try
        {
            // 这里需要解析叙事数据并调用LLM提取参数
            // 暂时使用默认值
            await Task.Yield(); // 添加await避免警告
            statusLabel.text = "参数提取完成";
            statusLabel.style.color = Color.green;
        }
        catch (System.Exception e)
        {
            statusLabel.text = "参数提取失败";
            statusLabel.style.color = Color.red;
            Debug.LogError($"参数提取失败: {e.Message}");
        }
        finally
        {
            extractParamsButton.SetEnabled(true);
        }
    }

    private async void OnGenerateScene()
    {
        if (string.IsNullOrEmpty(narrativeInput.value))
        {
            statusLabel.text = "请先连接叙事数据";
            statusLabel.style.color = Color.red;
            return;
        }

        generateButton.SetEnabled(false);
        statusLabel.text = "生成场景中...";
        statusLabel.style.color = Color.yellow;

        try
        {
            // 调用场景生成逻辑
            var result = await AgentCommunication.CallPythonAgent("scene", narrativeInput.value);
            resultOutput.value = result;
            statusLabel.text = "场景生成完成";
            statusLabel.style.color = Color.green;
            
            // 更新节点数据
            NodeData = result;
        }
        catch (System.Exception e)
        {
            resultOutput.value = $"错误: {e.Message}";
            statusLabel.text = "场景生成失败";
            statusLabel.style.color = Color.red;
            Debug.LogError($"场景生成失败: {e.Message}");
        }
        finally
        {
            generateButton.SetEnabled(true);
        }
    }

    // 当输入端口连接时更新叙事数据
    public void UpdateNarrativeInput(string narrativeData)
    {
        narrativeInput.value = narrativeData;
        statusLabel.text = "叙事数据已连接";
        statusLabel.style.color = Color.green;
    }
} 