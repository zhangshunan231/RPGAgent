using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;

public abstract class RPGNode : Node
{
    public string GUID;
    public string NodeData;
    public bool EntryPoint = false;

    public RPGNode()
    {
        GUID = System.Guid.NewGuid().ToString();
        
        // 设置节点样式
        styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/VisualEditor/RPGNode.uss"));
    }

    public abstract void CreateInputPorts();
    public abstract void CreateOutputPorts();
    public abstract void CreateFields();
    
    protected Port CreateInputPort(string portName, System.Type type = null)
    {
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, type ?? typeof(object));
        inputPort.portName = portName;
        inputContainer.Add(inputPort);
        return inputPort;
    }
    
    protected Port CreateOutputPort(string portName, System.Type type = null)
    {
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, type ?? typeof(object));
        outputPort.portName = portName;
        outputContainer.Add(outputPort);
        return outputPort;
    }
    
    protected TextField CreateTextField(string label, string defaultValue = "", bool multiline = false)
    {
        var textField = new TextField(label);
        textField.value = defaultValue;
        textField.multiline = multiline;
        mainContainer.Add(textField);
        return textField;
    }
    
    protected Button CreateButton(string text, System.Action onClick)
    {
        var button = new Button(onClick);
        button.text = text;
        mainContainer.Add(button);
        return button;
    }
} 