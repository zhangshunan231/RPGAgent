using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ElementDistributionConfig.ElementEntry))]
public class ElementEntryPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // 计算各个字段的位置
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        // 第一行：Template
        Rect templateRect = new Rect(position.x, position.y, position.width, lineHeight);
        SerializedProperty templateProp = property.FindPropertyRelative("template");
        EditorGUI.PropertyField(templateRect, templateProp);
        
        // 第二行：Distribution Mode
        Rect modeRect = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);
        SerializedProperty modeProp = property.FindPropertyRelative("distributionMode");
        EditorGUI.PropertyField(modeRect, modeProp);
        
        // 第三行：Count/Density (根据模式显示不同标签)
        Rect countRect = new Rect(position.x, position.y + (lineHeight + spacing) * 2, position.width, lineHeight);
        SerializedProperty countProp = property.FindPropertyRelative("count");
        
        ElementDistributionConfig.DistributionMode mode = (ElementDistributionConfig.DistributionMode)modeProp.enumValueIndex;
        if (mode == ElementDistributionConfig.DistributionMode.ByDensity)
        {
            EditorGUI.PropertyField(countRect, countProp, new GUIContent("Density", "每100个可用格子放置的数量"));
        }
        else
        {
            EditorGUI.PropertyField(countRect, countProp, new GUIContent("Count", "要放置的确切数量"));
        }
        
        // 第四行：Min Spacing
        Rect spacingRect = new Rect(position.x, position.y + (lineHeight + spacing) * 3, position.width, lineHeight);
        SerializedProperty minSpacingProp = property.FindPropertyRelative("minSpacing");
        EditorGUI.PropertyField(spacingRect, minSpacingProp);
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        return (lineHeight + spacing) * 4 - spacing; // 4行，减去最后一个spacing
    }
} 