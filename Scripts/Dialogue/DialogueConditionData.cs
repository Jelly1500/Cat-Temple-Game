using UnityEditor;
using UnityEngine;

[System.Serializable]
public class DialogueConditionData
{
    public ConditionType type;
    public float timeValue;
    public int moneyValue;
    public float coolTimeValue;
}

public enum ConditionType
{
    TimeElapsed,
    MoneyGreaterThan,
    CoolTime
}

public class DialogueCondition
{
    public DialogueConditionData Data { get; private set; }
    private float lastCheckedTime = -99999f;

    public void Initialize(DialogueConditionData data)
    {
        Data = data;
        lastCheckedTime = -99999f;
    }

    public bool Check()
    {
        switch (Data.type)
        {
            case ConditionType.TimeElapsed:
                return Time.time >= Data.timeValue;
            case ConditionType.MoneyGreaterThan:
                return GameDataManager.Instance.Gold > Data.moneyValue;
            case ConditionType.CoolTime:
                if (Time.time - lastCheckedTime >= Data.coolTimeValue)
                    return true;
                return false;
            default:
                return true;
        }
    }

    public void RecordExecutionTime()
    {
        lastCheckedTime = Time.time;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(DialogueConditionData))]
public class DialogueConditionDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var typeProp = property.FindPropertyRelative("type");
        position.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(position, typeProp);
        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        ConditionType type = (ConditionType)typeProp.enumValueIndex;
        switch (type)
        {
            case ConditionType.TimeElapsed:
                var timeProp = property.FindPropertyRelative("timeValue");
                EditorGUI.PropertyField(position, timeProp, new GUIContent("Required Time"));
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                break;
            case ConditionType.MoneyGreaterThan:
                var moneyProp = property.FindPropertyRelative("moneyValue");
                EditorGUI.PropertyField(position, moneyProp, new GUIContent("Required Money"));
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                break;
            case ConditionType.CoolTime:
                var coolTimeProp = property.FindPropertyRelative("coolTimeValue");
                EditorGUI.PropertyField(position, coolTimeProp, new GUIContent("Cool Time (sec)"));
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                break;
        }
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var typeProp = property.FindPropertyRelative("type");
        ConditionType type = (ConditionType)typeProp.enumValueIndex;
        int lines = 1; // type line
        switch (type)
        {
            case ConditionType.TimeElapsed:
            case ConditionType.MoneyGreaterThan:
            case ConditionType.CoolTime:
                lines += 1; // value line
                break;
        }
        return EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * (lines - 1);
    }
}
#endif
