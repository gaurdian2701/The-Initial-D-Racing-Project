using System;
using UnityEditor;
using UnityEngine;

namespace ExternalForInspector
{
    //<Summary>
    // Makes selected variables show in inspector only if a bool is true
    public class ShowIfAttribute : PropertyAttribute
    {
        public string conditionalField;

        public ShowIfAttribute(string conditionalField)
        {
            this.conditionalField = conditionalField;
        }
    }

    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ShowIfAttribute showIf = (ShowIfAttribute)attribute;
            SerializedProperty conditionalProperty =
                property.serializedObject.FindProperty(showIf.conditionalField);

            if (conditionalProperty != null && conditionalProperty.boolValue)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ShowIfAttribute showIf = (ShowIfAttribute)attribute;
            SerializedProperty conditionalProperty =
                property.serializedObject.FindProperty(showIf.conditionalField);

            if (conditionalProperty != null && conditionalProperty.boolValue)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            return 0f;
        }
    }
}
