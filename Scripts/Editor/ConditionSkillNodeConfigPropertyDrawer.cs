using UnityEditor;
using UnityEngine;

namespace SkillFramework.Editor
{
    [CustomPropertyDrawer(typeof(ConditionSkillNodeConfig))]
    public sealed class ConditionSkillNodeConfigPropertyDrawer :
        DecoratorSkillNodeConfigPropertyDrawer
    {
        private const float ActionButtonWidth = 96f;
        private const float ClearButtonWidth = 48f;
        private const float RowSpacing = 2f;

        protected override bool IsExtraProperty(SerializedProperty property)
        {
            return base.IsExtraProperty(property) ||
                   property.name == "Condition";
        }

        protected override float GetExtraHeight(SerializedProperty property)
        {
            float height = 0f;
            foreach (SerializedProperty childProperty in EnumerateProperties(property))
            {
                if (!IsExtraProperty(childProperty))
                {
                    continue;
                }

                height += GetExtraRowHeight(childProperty) +
                          EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }

        protected override void DrawExtraFields(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect rowRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);

            foreach (SerializedProperty childProperty in EnumerateProperties(property))
            {
                if (!IsExtraProperty(childProperty))
                {
                    continue;
                }

                float propertyHeight = GetExtraRowHeight(childProperty);
                Rect propertyRect = new Rect(rowRect.x, rowRect.y, rowRect.width, propertyHeight);

                if (IsChildNodeProperty(childProperty))
                {
                    DrawChildNodeRow(propertyRect, position.xMax, childProperty);
                    rowRect.y += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
                    continue;
                }

                if (childProperty.managedReferenceValue == null)
                {
                    DrawAddButton(rowRect, childProperty);
                }
                else
                {
                    string conditionName = SkillConditionEditorUtility.GetConditionDisplayName(
                        childProperty.managedReferenceValue.GetType());
                    Rect nameRect = new Rect(
                        propertyRect.x,
                        propertyRect.y,
                        propertyRect.width,
                        EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(nameRect, conditionName, EditorStyles.boldLabel);

                    float fieldWidth = Mathf.Max(
                        0f,
                        propertyRect.width - ClearButtonWidth - EditorGUIUtility.standardVerticalSpacing);
                    propertyRect = new Rect(
                        propertyRect.x,
                        propertyRect.y + EditorGUIUtility.singleLineHeight,
                        fieldWidth,
                        propertyRect.height - EditorGUIUtility.singleLineHeight);
                    Rect clearRect = new Rect(
                        propertyRect.xMax + EditorGUIUtility.standardVerticalSpacing,
                        nameRect.y,
                        Mathf.Min(ClearButtonWidth, position.xMax - propertyRect.xMax),
                        EditorGUIUtility.singleLineHeight);

                    ExpandCondition(childProperty);
                    EditorGUI.PropertyField(
                        propertyRect,
                        childProperty,
                        new GUIContent(childProperty.displayName),
                        true);

                    if (GUI.Button(clearRect, "Clear"))
                    {
                        childProperty.managedReferenceValue = null;
                        childProperty.serializedObject.ApplyModifiedProperties();
                    }
                }

                rowRect.y += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private static float GetExtraRowHeight(SerializedProperty property)
        {
            if (IsChildNodeProperty(property))
            {
                return EditorGUIUtility.singleLineHeight;
            }

            if (property.managedReferenceValue != null)
            {
                return EditorGUIUtility.singleLineHeight +
                       EditorGUI.GetPropertyHeight(
                           property,
                           new GUIContent(property.displayName),
                           true) +
                       EditorGUIUtility.standardVerticalSpacing;
            }

            ExpandCondition(property);
            float height = EditorGUI.GetPropertyHeight(
                property,
                new GUIContent(property.displayName),
                true);
            return Mathf.Max(EditorGUIUtility.singleLineHeight, height);
        }

        private static void ExpandCondition(SerializedProperty property)
        {
            if (!IsChildNodeProperty(property) && property.managedReferenceValue != null)
            {
                property.isExpanded = true;
            }
        }

        private static void DrawAddButton(Rect rowRect, SerializedProperty property)
        {
            string buttonText = property.name == "Condition" ? "Add Condition" : "Add Child Node";
            Rect buttonRect = new Rect(
                rowRect.x,
                rowRect.y,
                Mathf.Min(ActionButtonWidth, rowRect.width),
                rowRect.height);
            Rect labelRect = new Rect(
                buttonRect.xMax + RowSpacing,
                rowRect.y,
                Mathf.Max(0f, rowRect.xMax - buttonRect.xMax - RowSpacing),
                rowRect.height);

            if (GUI.Button(buttonRect, buttonText))
            {
                GenericMenu menu = new GenericMenu();
                SkillConditionEditorUtility.AddConditionMenuItems(menu, property);

                menu.ShowAsContext();
            }

            GUI.Label(labelRect, property.displayName, EditorStyles.miniLabel);
        }
    }
}
