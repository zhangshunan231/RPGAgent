using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Threading.Tasks;

public class MechanicsNode : RPGNode
{
    private TextField narrativeInput;
    private TextField sceneInput;
    private TextField resultOutput;
    private Button generateButton;
    private Label statusLabel;
    
    public MechanicsNode()
    {
        title = "机制生成";
        CreateInputPorts();
        CreateOutputPorts();
        CreateFields();
    }

    public override void CreateInputPorts()
    {
        CreateInputPort("叙事数据", typeof(string));
        CreateInputPort("场景数据", typeof(string));
    }

    public override void CreateOutputPorts()
    {
        CreateOutputPort("机制数据", typeof(string));
    }

    public override void CreateFields()
    {
        // 叙事输入
        narrativeInput = CreateTextField("叙事数据", "", true);
        narrativeInput.SetEnabled(false);
        
        // 场景输入
        sceneInput = CreateTextField("场景数据", "", true);
        sceneInput.SetEnabled(false);
        
        // 生成按钮
        generateButton = CreateButton("生成机制", OnGenerateMechanics);
        
        // 状态标签
        statusLabel = new Label("等待输入数据");
        statusLabel.style.color = Color.gray;
        mainContainer.Add(statusLabel);
        
        // 结果显示
        resultOutput = CreateTextField("生成结果", "", true);
        resultOutput.SetEnabled(false);
    }

    private async void OnGenerateMechanics()
    {
        if (string.IsNullOrEmpty(narrativeInput.value) || string.IsNullOrEmpty(sceneInput.value))
        {
            statusLabel.text = "请先连接叙事和场景数据";
            statusLabel.style.color = Color.red;
            return;
        }

        generateButton.SetEnabled(false);
        statusLabel.text = "生成机制中...";
        statusLabel.style.color = Color.yellow;

        try
        {
            var combinedInput = $"故事: {narrativeInput.value}\n场景: {sceneInput.value}";
            var result = await AgentCommunication.CallPythonAgent("mechanics", combinedInput);
            resultOutput.value = result;
            statusLabel.text = "机制生成完成";
            statusLabel.style.color = Color.green;
            
            // 更新节点数据
            NodeData = result;
        }
        catch (System.Exception e)
        {
            resultOutput.value = $"错误: {e.Message}";
            statusLabel.text = "机制生成失败";
            statusLabel.style.color = Color.red;
            Debug.LogError($"机制生成失败: {e.Message}");
        }
        finally
        {
            generateButton.SetEnabled(true);
        }
    }

    // 当输入端口连接时更新数据
    public void UpdateNarrativeInput(string narrativeData)
    {
        narrativeInput.value = narrativeData;
        UpdateStatus();
    }

    public void UpdateSceneInput(string sceneData)
    {
        sceneInput.value = sceneData;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (!string.IsNullOrEmpty(narrativeInput.value) && !string.IsNullOrEmpty(sceneInput.value))
        {
            statusLabel.text = "数据已连接，可以生成机制";
            statusLabel.style.color = Color.green;
        }
        else if (!string.IsNullOrEmpty(narrativeInput.value))
        {
            statusLabel.text = "等待场景数据";
            statusLabel.style.color = Color.yellow;
        }
        else if (!string.IsNullOrEmpty(sceneInput.value))
        {
            statusLabel.text = "等待叙事数据";
            statusLabel.style.color = Color.yellow;
        }
        else
        {
            statusLabel.text = "等待输入数据";
            statusLabel.style.color = Color.gray;
        }
    }
} 