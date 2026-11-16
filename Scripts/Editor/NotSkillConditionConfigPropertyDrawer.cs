using UnityEditor;
using UnityEngine;

namespace SkillFramework.Editor
{
    [CustomPropertyDrawer(typeof(NotSkillConditionConfig))]
    public sealed class NotSkillConditionConfigPropertyDrawer : PropertyDrawer
    {
        private const float ClearButtonWidth = 48f;
        private const float AddButtonWidth = 96f;
        private const float RowSpacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty conditionProperty = property.FindPropertyRelative("Condition");
            if (conditionProperty == null || conditionProperty.managedReferenceValue == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            return EditorGUIUtility.singleLineHeight +
                   EditorGUI.GetPropertyHeight(conditionProperty, new GUIContent("Condition"), true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty conditionProperty = property.FindPropertyRelative("Condition");
            if (conditionProperty == null)
            {
                return;
            }

            if (conditionProperty.managedReferenceValue == null)
            {
                Rect buttonRect = new Rect(
                    position.x,
                    position.y,
                    Mathf.Min(AddButtonWidth, position.width),
                    EditorGUIUtility.singleLineHeight);
                Rect labelRect = new Rect(
                    buttonRect.xMax + RowSpacing,
                    position.y,
                    Mathf.Max(0f, position.xMax - buttonRect.xMax - RowSpacing),
                    EditorGUIUtility.singleLineHeight);

                if (GUI.Button(buttonRect, "Add Condition"))
                {
                    GenericMenu menu = new GenericMenu();
                    SkillConditionEditorUtility.AddConditionMenuItems(menu, conditionProperty);
                    menu.ShowAsContext();
                }

                GUI.Label(labelRect, "Condition", EditorStyles.miniLabel);
            }
            else
            {
                string conditionName = SkillConditionEditorUtility.GetConditionDisplayName(
                    conditionProperty.managedReferenceValue.GetType());
                Rect nameRect = new Rect(
                    position.x,
                    position.y,
                    position.width,
                    EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(nameRect, conditionName, EditorStyles.boldLabel);

                float fieldWidth = Mathf.Max(
                    0f,
                    position.width - ClearButtonWidth - EditorGUIUtility.standardVerticalSpacing);
                Rect fieldRect = new Rect(
                    position.x,
                    position.y + EditorGUIUtility.singleLineHeight,
                    fieldWidth,
                    position.height - EditorGUIUtility.singleLineHeight);
                Rect clearRect = new Rect(
                    fieldRect.xMax + EditorGUIUtility.standardVerticalSpacing,
                    nameRect.y,
                    Mathf.Min(ClearButtonWidth, position.xMax - fieldRect.xMax),
                    EditorGUIUtility.singleLineHeight);

                EditorGUI.PropertyField(fieldRect, conditionProperty, new GUIContent("Condition"), true);

                if (GUI.Button(clearRect, "Clear"))
                {
                    conditionProperty.managedReferenceValue = null;
                    conditionProperty.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
