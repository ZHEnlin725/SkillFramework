using System;
using UnityEditor;
using UnityEngine;

namespace SkillFramework.Editor
{
    [CustomPropertyDrawer(typeof(SkillDamageExpression), true)]
    public sealed class SkillDamageExpressionPropertyDrawer : PropertyDrawer
    {
        private const float HeaderButtonWidth = 54f;
        private const float HeaderSpacing = 4f;

        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property == null || !property.isExpanded ||
                property.managedReferenceValue == null)
            {
                return height;
            }

            height += EditorGUIUtility.standardVerticalSpacing;
            switch (property.managedReferenceValue)
            {
                case SkillConstantExpression:
                    height += EditorGUI.GetPropertyHeight(
                        property.FindPropertyRelative("Value"),
                        GUIContent.none,
                        true);
                    break;
                case SkillValueExpression:
                    height += EditorGUI.GetPropertyHeight(
                        property.FindPropertyRelative("Key"),
                        GUIContent.none,
                        true);
                    break;
                case SkillBinaryExpression:
                    height += EditorGUI.GetPropertyHeight(
                        property.FindPropertyRelative("Operator"),
                        GUIContent.none,
                        true);
                    height += EditorGUIUtility.standardVerticalSpacing;
                    height += GetChildExpressionHeight(property, "Left");
                    height += EditorGUIUtility.standardVerticalSpacing;
                    height += GetChildExpressionHeight(property, "Right");
                    break;
            }

            return height;
        }

        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            Rect headerRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);
            DrawHeader(headerRect, property, label);

            if (!property.isExpanded || property.managedReferenceValue == null)
            {
                return;
            }

            Rect contentRect = new Rect(
                position.x,
                headerRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                position.width,
                position.height - headerRect.height - EditorGUIUtility.standardVerticalSpacing);
            using (new EditorGUI.IndentLevelScope())
            {
                switch (property.managedReferenceValue)
                {
                    case SkillConstantExpression:
                        DrawDirectField(contentRect.x, contentRect.y, contentRect.width, property, "Value");
                        break;
                    case SkillValueExpression:
                        DrawDirectField(contentRect.x, contentRect.y, contentRect.width, property, "Key");
                        break;
                    case SkillBinaryExpression:
                        DrawBinaryFields(contentRect.x, contentRect.y, contentRect.width, property);
                        break;
                }
            }
        }

        private static void DrawHeader(
            Rect headerRect,
            SerializedProperty property,
            GUIContent label)
        {
            string headerText = GetHeaderText(property, label);
            Rect foldoutRect = new Rect(
                headerRect.x,
                headerRect.y,
                Mathf.Max(0f, headerRect.width - HeaderButtonWidth - HeaderSpacing),
                headerRect.height);
            Rect buttonRect = new Rect(
                foldoutRect.xMax + HeaderSpacing,
                headerRect.y,
                Mathf.Min(HeaderButtonWidth, Mathf.Max(0f, headerRect.xMax - foldoutRect.xMax)),
                headerRect.height);

            if (property.managedReferenceValue == null)
            {
                EditorGUI.LabelField(foldoutRect, headerText, EditorStyles.miniLabel);
            }
            else
            {
                property.isExpanded = EditorGUI.Foldout(
                    foldoutRect,
                    property.isExpanded,
                    headerText,
                    true);
            }

            if (GUI.Button(
                    buttonRect,
                    property.managedReferenceValue == null ? "Add" : "Change"))
            {
                SkillDamageExpressionEditorUtility.ShowExpressionTypeMenu(
                    buttonRect,
                    property);
            }
        }

        private static string GetHeaderText(
            SerializedProperty property,
            GUIContent label)
        {
            string displayName = SkillDamageExpressionEditorUtility.GetExpressionDisplayName(
                property.managedReferenceValue?.GetType());
            if (label == null || string.IsNullOrEmpty(label.text))
            {
                return displayName;
            }

            return $"{label.text}: {displayName}";
        }

        private static void DrawBinaryFields(
            float x,
            float y,
            float width,
            SerializedProperty property)
        {
            float operatorHeight = GetDirectPropertyHeight(property, "Operator");
            Rect operatorRect = new Rect(x, y, width, operatorHeight);
            DrawDirectProperty(operatorRect, property, "Operator");
            y += operatorHeight + EditorGUIUtility.standardVerticalSpacing;

            float leftHeight = GetChildExpressionHeight(property, "Left");
            Rect leftRect = new Rect(x, y, width, leftHeight);
            DrawChildExpression(leftRect, property, "Left");
            y += leftHeight + EditorGUIUtility.standardVerticalSpacing;

            float rightHeight = GetChildExpressionHeight(property, "Right");
            Rect rightRect = new Rect(x, y, width, rightHeight);
            DrawChildExpression(rightRect, property, "Right");
        }

        private static void DrawDirectField(
            float x,
            float y,
            float width,
            SerializedProperty property,
            string fieldName)
        {
            float fieldHeight = GetDirectPropertyHeight(property, fieldName);
            DrawDirectProperty(new Rect(x, y, width, fieldHeight), property, fieldName);
        }

        private static void DrawDirectProperty(
            Rect rect,
            SerializedProperty property,
            string fieldName)
        {
            SerializedProperty childProperty = property.FindPropertyRelative(fieldName);
            if (childProperty == null)
            {
                EditorGUI.LabelField(rect, $"{fieldName} is missing.", EditorStyles.miniLabel);
                return;
            }

            EditorGUI.PropertyField(
                rect,
                childProperty,
                new GUIContent(ObjectNames.NicifyVariableName(fieldName)),
                true);
        }

        private static void DrawChildExpression(
            Rect rect,
            SerializedProperty property,
            string fieldName)
        {
            SerializedProperty childProperty = property.FindPropertyRelative(fieldName);
            if (childProperty == null)
            {
                EditorGUI.LabelField(rect, $"{fieldName} is missing.", EditorStyles.miniLabel);
                return;
            }

            if (childProperty.managedReferenceValue == null)
            {
                EditorGUI.LabelField(rect, $"No {fieldName}", EditorStyles.miniLabel);
                Rect addRect = new Rect(
                    rect.xMax - HeaderButtonWidth,
                    rect.y,
                    Mathf.Min(HeaderButtonWidth, rect.width),
                    EditorGUIUtility.singleLineHeight);
                if (GUI.Button(addRect, "Add"))
                {
                    SkillDamageExpressionEditorUtility.ShowExpressionTypeMenu(
                        addRect,
                        childProperty);
                }

                return;
            }

            EditorGUI.PropertyField(
                rect,
                childProperty,
                new GUIContent(ObjectNames.NicifyVariableName(fieldName)),
                true);
        }

        private static float GetDirectPropertyHeight(
            SerializedProperty property,
            string fieldName)
        {
            SerializedProperty childProperty = property.FindPropertyRelative(fieldName);
            return EditorGUI.GetPropertyHeight(
                childProperty,
                GUIContent.none,
                true);
        }

        private static float GetChildExpressionHeight(
            SerializedProperty property,
            string fieldName)
        {
            SerializedProperty childProperty = property.FindPropertyRelative(fieldName);
            if (childProperty == null || childProperty.managedReferenceValue == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            return EditorGUI.GetPropertyHeight(
                childProperty,
                GUIContent.none,
                true);
        }
    }
}

