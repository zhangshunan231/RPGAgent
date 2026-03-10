using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
// AgentCommunication 没有命名空间，无需using

public class NarrativeNode : RPGNode
{
    private TextField storyInput;
    private Button generateButton;
    private Label statusLabel;
    
    // 主要故事显示
    private VisualElement mainStoryContainer;
    private Label mainStoryLabel;
    private ScrollView mainStoryScrollView;
    
    // 步骤显示
    private ScrollView stepsScrollView;
    private VisualElement stepsContainer;
    
    // 解析后的数据
    private string mainStory = "";
    private List<NarrativeStep> steps = new List<NarrativeStep>();
    
    public NarrativeNode()
    {
        title = "叙事生成";
        CreateInputPorts();
        CreateOutputPorts();
        CreateFields();
        
        // 设置节点可调整大小
        this.capabilities |= Capabilities.Resizable;
        this.SetPosition(new Rect(100, 100, 400, 600)); // 设置初始大小
    }

    public override void CreateInputPorts()
    {
        // 叙事节点通常作为起始节点，不需要输入端口
    }

    public override void CreateOutputPorts()
    {
        CreateOutputPort("叙事数据", typeof(string));
    }

    public override void CreateFields()
    {
        // 故事输入
        storyInput = CreateTextField("故事设想", "请输入你的RPG故事设想...", true);
        
        // 生成按钮
        generateButton = CreateButton("生成故事", OnGenerateStory);
        
        // 状态标签
        statusLabel = new Label("就绪");
        statusLabel.style.color = Color.gray;
        mainContainer.Add(statusLabel);
        
        // 创建结果显示区域
        CreateResultDisplay();
    }

    private void CreateResultDisplay()
    {
        // 主要故事区域
        var storySection = new VisualElement();
        storySection.style.marginTop = 10;
        storySection.style.display = DisplayStyle.None;
        
        var storyTitle = new Label("=== 整体剧情 ===");
        storyTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        storyTitle.style.color = Color.white;
        storyTitle.style.marginBottom = 5;
        storySection.Add(storyTitle);
        
        // 创建主要故事的滚动视图
        mainStoryScrollView = new ScrollView();
        mainStoryScrollView.style.height = 100;
        mainStoryScrollView.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        mainStoryScrollView.style.borderLeftWidth = 1;
        mainStoryScrollView.style.borderRightWidth = 1;
        mainStoryScrollView.style.borderTopWidth = 1;
        mainStoryScrollView.style.borderBottomWidth = 1;
        mainStoryScrollView.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
        mainStoryScrollView.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
        mainStoryScrollView.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
        mainStoryScrollView.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
        mainStoryScrollView.style.paddingLeft = 5;
        mainStoryScrollView.style.paddingRight = 5;
        mainStoryScrollView.style.paddingTop = 5;
        mainStoryScrollView.style.paddingBottom = 5;
        
        mainStoryContainer = new VisualElement();
        mainStoryLabel = new Label("");
        mainStoryLabel.style.color = Color.white;
        mainStoryLabel.style.whiteSpace = WhiteSpace.Normal;
        mainStoryContainer.Add(mainStoryLabel);
        mainStoryScrollView.Add(mainStoryContainer);
        
        storySection.Add(mainStoryScrollView);
        mainContainer.Add(storySection);
        
        // 步骤区域
        var stepsSection = new VisualElement();
        stepsSection.style.marginTop = 10;
        stepsSection.style.display = DisplayStyle.None;
        
        var stepsTitle = new Label("=== 步骤分解 ===");
        stepsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        stepsTitle.style.color = Color.white;
        stepsTitle.style.marginBottom = 5;
        stepsSection.Add(stepsTitle);
        
        // 创建步骤的滚动视图
        stepsScrollView = new ScrollView();
        stepsScrollView.style.height = 300;
        stepsScrollView.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        stepsScrollView.style.borderLeftWidth = 1;
        stepsScrollView.style.borderRightWidth = 1;
        stepsScrollView.style.borderTopWidth = 1;
        stepsScrollView.style.borderBottomWidth = 1;
        stepsScrollView.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
        stepsScrollView.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
        stepsScrollView.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
        stepsScrollView.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
        stepsScrollView.style.paddingLeft = 5;
        stepsScrollView.style.paddingRight = 5;
        stepsScrollView.style.paddingTop = 5;
        stepsScrollView.style.paddingBottom = 5;
        
        stepsContainer = new VisualElement();
        stepsScrollView.Add(stepsContainer);
        
        stepsSection.Add(stepsScrollView);
        mainContainer.Add(stepsSection);
        
        // 重新生成按钮
        var regenerateButton = new Button(() => OnGenerateStory());
        regenerateButton.text = "重新生成故事";
        regenerateButton.style.marginTop = 10;
        mainContainer.Add(regenerateButton);
    }

    private async void OnGenerateStory()
    {
        if (string.IsNullOrEmpty(storyInput.value))
        {
            statusLabel.text = "请输入故事设想";
            statusLabel.style.color = Color.red;
            return;
        }

        generateButton.SetEnabled(false);
        statusLabel.text = "生成中...";
        statusLabel.style.color = Color.yellow;

        try
        {
            var result = await AgentCommunication.CallPythonAgent("narrative", storyInput.value);
            
            // 解析结果
            ParseNarrativeResult(result);
            
            // 显示结果
            DisplayResults();
            
            statusLabel.text = "生成完成";
            statusLabel.style.color = Color.green;
            
            // 更新节点数据
            NodeData = result;
        }
        catch (System.Exception e)
        {
            statusLabel.text = "生成失败";
            statusLabel.style.color = Color.red;
            Debug.LogError($"生成故事失败: {e.Message}");
        }
        finally
        {
            generateButton.SetEnabled(true);
        }
    }

    private void ParseNarrativeResult(string jsonResult)
    {
        mainStory = "";
        steps.Clear();
        
        try
        {
            // 尝试解析JSON
            var narrativeData = JsonUtility.FromJson<NarrativeData>(jsonResult);
            if (narrativeData != null)
            {
                mainStory = narrativeData.story ?? "";
                if (narrativeData.steps != null)
                {
                    steps.AddRange(narrativeData.steps);
                }
            }
        }
        catch
        {
            // 如果JSON解析失败，尝试简单解析
            ParseSimpleResult(jsonResult);
        }
    }

    private void ParseSimpleResult(string result)
    {
        // 简单的文本解析，提取主要故事和步骤
        var lines = result.Split('\n');
        var storyLines = new List<string>();
        var stepLines = new List<string>();
        bool inStory = false;
        bool inSteps = false;
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Contains("故事") || trimmedLine.Contains("story") || trimmedLine.Contains("剧情"))
            {
                inStory = true;
                inSteps = false;
                continue;
            }
            else if (trimmedLine.Contains("步骤") || trimmedLine.Contains("step") || trimmedLine.Contains("阶段"))
            {
                inStory = false;
                inSteps = true;
                continue;
            }
            
            if (inStory && !string.IsNullOrEmpty(trimmedLine))
            {
                storyLines.Add(trimmedLine);
            }
            else if (inSteps && !string.IsNullOrEmpty(trimmedLine))
            {
                stepLines.Add(trimmedLine);
            }
        }
        
        mainStory = string.Join("\n", storyLines);
        
        // 创建简单的步骤
        for (int i = 0; i < stepLines.Count; i++)
        {
            steps.Add(new NarrativeStep
            {
                step = i + 1,
                title = $"步骤 {i + 1}",
                objective = stepLines[i],
                location = (LocationType)1, // 使用数字1代表Grass
                key_characters = new List<string>(),
                key_items = new List<string>(),
                main_dialogues = new List<Dialogue>()
            });
        }
    }

    private void DisplayResults()
    {
        // 显示主要故事
        mainStoryLabel.text = string.IsNullOrEmpty(mainStory) ? "未解析到主要故事" : mainStory;
        mainStoryScrollView.parent.style.display = DisplayStyle.Flex;
        
        // 显示步骤
        DisplaySteps();
        stepsScrollView.parent.style.display = DisplayStyle.Flex;
    }

    private void DisplaySteps()
    {
        stepsContainer.Clear();
        
        if (steps.Count == 0)
        {
            var noStepsLabel = new Label("暂无分解步骤");
            noStepsLabel.style.color = Color.white;
            stepsContainer.Add(noStepsLabel);
            return;
        }
        
        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            
            // 步骤标题
            var stepTitle = new Label($"步骤 {step.step}: {step.title}");
            stepTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            stepTitle.style.color = Color.white;
            stepTitle.style.marginBottom = 3;
            stepTitle.style.whiteSpace = WhiteSpace.Normal;
            stepsContainer.Add(stepTitle);
            
            // 地点
            var locationLabel = new Label($"地点（Location）：{step.location}");
            locationLabel.style.color = Color.white;
            locationLabel.style.fontSize = 11;
            locationLabel.style.whiteSpace = WhiteSpace.Normal;
            stepsContainer.Add(locationLabel);
            
            // 目标
            var objectiveLabel = new Label($"目标：{step.objective}");
            objectiveLabel.style.color = Color.white;
            objectiveLabel.style.fontSize = 11;
            objectiveLabel.style.whiteSpace = WhiteSpace.Normal;
            stepsContainer.Add(objectiveLabel);
            
            // 主要角色
            var charactersLabel = new Label($"主要角色：{string.Join(", ", step.key_characters ?? new List<string>())}");
            charactersLabel.style.color = Color.white;
            charactersLabel.style.fontSize = 11;
            charactersLabel.style.whiteSpace = WhiteSpace.Normal;
            stepsContainer.Add(charactersLabel);
            
            // 主要对话
            var dialogueLabel = new Label("主要对话：");
            dialogueLabel.style.color = Color.white;
            dialogueLabel.style.fontSize = 11;
            dialogueLabel.style.whiteSpace = WhiteSpace.Normal;
            stepsContainer.Add(dialogueLabel);
            
            if (step.main_dialogues != null && step.main_dialogues.Count > 0)
            {
                foreach (var dialogue in step.main_dialogues)
                {
                    var dialogueText = new Label($"{dialogue.character}：{dialogue.dialogue}");
                    dialogueText.style.color = Color.white;
                    dialogueText.style.fontSize = 11;
                    dialogueText.style.marginLeft = 10;
                    dialogueText.style.whiteSpace = WhiteSpace.Normal;
                    stepsContainer.Add(dialogueText);
                }
            }
            
            // 关键物品
            var itemsLabel = new Label($"关键物品：{string.Join(", ", step.key_items ?? new List<string>())}");
            itemsLabel.style.color = Color.white;
            itemsLabel.style.fontSize = 11;
            itemsLabel.style.whiteSpace = WhiteSpace.Normal;
            stepsContainer.Add(itemsLabel);
            
            // 分隔线
            var separator = new Label("--------------------------------------------------");
            separator.style.color = Color.gray;
            separator.style.fontSize = 10;
            separator.style.marginTop = 5;
            separator.style.marginBottom = 5;
            stepsContainer.Add(separator);
        }
    }
}

// 数据结构类
[System.Serializable]
public class NarrativeData
{
    public string story;
    public List<NarrativeStep> steps;
}

[System.Serializable]
public class NarrativeStep
{
    public int step;
    public string title;
    public LocationType location;
    public string objective;
    public List<string> key_characters;
    public List<string> key_items;
    public List<Dialogue> main_dialogues;
}

[System.Serializable]
public class Dialogue
{
    public string character;
    public string dialogue;
} 