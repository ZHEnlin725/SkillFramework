using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SkillFramework.Editor
{
    [CustomPropertyDrawer(typeof(DecoratorSkillNodeConfig), true)]
    public class DecoratorSkillNodeConfigPropertyDrawer : SkillNodeConfigPropertyDrawer
    {
        private const float ClearButtonWidth = 48f;

        protected override bool IsExtraProperty(SerializedProperty property)
        {
            return IsChildNodeProperty(property);
        }

        protected static bool IsChildNodeProperty(SerializedProperty property)
        {
            return property.name == "ChildNode";
        }

        protected override float GetExtraHeight(SerializedProperty property)
        {
            return EditorGUIUtility.singleLineHeight +
                   EditorGUIUtility.standardVerticalSpacing;
        }

        protected override void DrawExtraFields(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty childProperty = property.FindPropertyRelative("ChildNode");
            if (childProperty == null)
            {
                return;
            }

            Rect rowRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);

            DrawChildNodeRow(rowRect, position.xMax, childProperty);
        }

        protected static void DrawChildNodeRow(
            Rect rowRect,
            float rowRight,
            SerializedProperty childProperty)
        {
            if (childProperty.managedReferenceValue == null)
            {
                DrawAddChildButton(rowRect, childProperty);
                return;
            }

            float fieldWidth = Mathf.Max(
                0f,
                rowRect.width - ClearButtonWidth - EditorGUIUtility.standardVerticalSpacing);
            Rect childRect = new Rect(
                rowRect.x,
                rowRect.y,
                fieldWidth,
                rowRect.height);
            Rect clearRect = new Rect(
                childRect.xMax + EditorGUIUtility.standardVerticalSpacing,
                rowRect.y,
                Mathf.Min(ClearButtonWidth, rowRight - childRect.xMax),
                rowRect.height);

            DrawChildNodeElement(childRect, childProperty);
            if (GUI.Button(clearRect, "Clear"))
            {
                childProperty.managedReferenceValue = null;
                childProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        private static void DrawChildNodeElement(
            Rect rect,
            SerializedProperty childProperty)
        {
            SkillNodeEditorUtility.DrawChildNodeElement(rect, childProperty);
        }

        private static void DrawAddChildButton(Rect rowRect, SerializedProperty childProperty)
        {
            const float buttonWidth = 96f;
            Rect buttonRect = new Rect(
                rowRect.x,
                rowRect.y,
                Mathf.Min(buttonWidth, rowRect.width),
                rowRect.height);
            Rect labelRect = new Rect(
                buttonRect.xMax + 2f,
                rowRect.y,
                Mathf.Max(0f, rowRect.xMax - buttonRect.xMax - 2f),
                rowRect.height);

            if (GUI.Button(buttonRect, "Add Child Node"))
            {
                GenericMenu menu = new GenericMenu();
                foreach (SkillNodeConfigType nodeConfigType in SkillNodeConfigTypeCache.GetTypes())
                {
                    Type nodeType = nodeConfigType.Type;
                    menu.AddItem(
                        new GUIContent(nodeConfigType.DisplayName),
                        false,
                        () => AddChildNode(childProperty, nodeType));
                }

                menu.ShowAsContext();
            }

            GUI.Label(labelRect, childProperty.displayName, EditorStyles.miniLabel);
        }

        protected static void AddChildNode(
            SerializedProperty childProperty,
            Type nodeType)
        {
            childProperty.managedReferenceValue = SkillNodeConfigTypeCache.Create(nodeType);
            childProperty.FindPropertyRelative("NodeId").intValue =
                SkillNodeEditorUtility.GetNextNodeId(childProperty);
            SkillNodeEditorUtility.InitializeChildNodeTiming(childProperty, 10);
            childProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}
