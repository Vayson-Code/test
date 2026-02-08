using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MinAttribute))]
sealed class MinDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MinAttribute minAttribute = (MinAttribute)attribute;

        if (property.propertyType == SerializedPropertyType.Integer)
        {
            int value = EditorGUI.IntField(position, label, property.intValue);
            property.intValue = Mathf.Max(value, Mathf.RoundToInt(minAttribute.min));
        }
        else if (property.propertyType == SerializedPropertyType.Float)
        {
            float value = EditorGUI.FloatField(position, label, property.floatValue);
            property.floatValue = Mathf.Max(value, minAttribute.min);
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Min works with int or float only.");
        }
    }
}