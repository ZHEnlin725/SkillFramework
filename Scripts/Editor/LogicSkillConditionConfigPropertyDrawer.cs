using UnityEditor;
using UnityEngine;

namespace SkillFramework.Editor
{
    [CustomPropertyDrawer(typeof(LogicSkillConditionConfig))]
    public sealed class LogicSkillConditionConfigPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight +
                           EditorGUIUtility.standardVerticalSpacing;
            height += SkillConditionListEditorUtility.GetList(
                property, "Conditions", "Conditions").GetHeight();
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty operatorProperty = property.FindPropertyRelative("Operator");
            if (operatorProperty != null)
            {
                Rect operatorRect = new Rect(
                    position.x,
                    position.y,
                    position.width,
                    EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(operatorRect, operatorProperty, true);
            }

            Rect listRect = new Rect(
                position.x,
                position.y + EditorGUIUtility.singleLineHeight +
                EditorGUIUtility.standardVerticalSpacing,
                position.width,
                SkillConditionListEditorUtility.GetList(
                    property, "Conditions", "Conditions").GetHeight());
            SkillConditionListEditorUtility.GetList(property, "Conditions", "Conditions")
                .DoList(listRect);
        }
    }
}
