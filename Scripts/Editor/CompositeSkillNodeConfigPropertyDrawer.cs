using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SkillFramework.Editor
{
    [CustomPropertyDrawer(typeof(CompositeSkillNodeConfig), true)]
    public class CompositeSkillNodeConfigPropertyDrawer : SkillNodeConfigPropertyDrawer
    {
        private const float ChildSpacing = 2f;
        private const int DefaultChildDuration = 10;
        private static readonly Dictionary<string, ReorderableList> ChildLists = new();

        protected override bool IsExtraProperty(SerializedProperty property)
        {
            return property.name == "Children";
        }

        protected override float GetExtraHeight(SerializedProperty property)
        {
            return GetChildList(property).GetHeight();
        }

        protected override void DrawExtraFields(Rect position, SerializedProperty property, GUIContent label)
        {
            GetChildList(property).DoList(position);
        }

        protected static ReorderableList GetChildList(SerializedProperty property)
        {
            SerializedProperty childrenProperty = property.FindPropertyRelative("Children");
            string key = $"{property.serializedObject.targetObject.GetInstanceID()}:" +
                         property.propertyPath;
            if (ChildLists.TryGetValue(key, out ReorderableList childList) &&
                childList != null &&
                childList.serializedProperty != null &&
                childList.serializedProperty.propertyPath == childrenProperty?.propertyPath)
            {
                return childList;
            }

            childList = CreateChildList(property);
            ChildLists[key] = childList;
            return childList;
        }

        private static ReorderableList CreateChildList(SerializedProperty property)
        {
            var childrenProperty = property.FindPropertyRelative("Children");
            return new ReorderableList(
                property.serializedObject,
                childrenProperty,
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Children"),
                drawElementCallback = (rect, index, active, focused) =>
                {
                    SerializedProperty childProperty =
                        childrenProperty.GetArrayElementAtIndex(index);
                    DrawChildElement(rect, childProperty);
                },
                elementHeightCallback = index =>
                {
                    return EditorGUIUtility.singleLineHeight + ChildSpacing;
                },
                onAddDropdownCallback = (buttonRect, list) => ShowAddChildMenu(list),
                onRemoveCallback = RemoveChild
            };
        }

        private static void ShowAddChildMenu(ReorderableList list)
        {
            GenericMenu menu = new GenericMenu();
            foreach (SkillNodeConfigType nodeType in SkillNodeConfigTypeCache.GetTypes())
            {
                Type type = nodeType.Type;
                menu.AddItem(
                    new GUIContent(nodeType.DisplayName),
                    false,
                    () => AddChild(list, type));
            }

            menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
        }

        private static void DrawChildElement(Rect rect, SerializedProperty childProperty)
        {
            SkillNodeEditorUtility.DrawChildNodeElement(rect, childProperty);
        }

        private static void AddChild(ReorderableList list, Type nodeType)
        {
            SerializedProperty childrenProperty = list.serializedProperty;
            childrenProperty.serializedObject.Update();
            int childIndex = childrenProperty.arraySize++;
            SerializedProperty childProperty =
                childrenProperty.GetArrayElementAtIndex(childIndex);
            childProperty.managedReferenceValue = SkillNodeConfigTypeCache.Create(nodeType);
            childProperty.FindPropertyRelative("NodeId").intValue =
                SkillNodeEditorUtility.GetNextNodeId(childProperty);
            SkillNodeEditorUtility.InitializeChildNodeTiming(childProperty, DefaultChildDuration);
            childrenProperty.serializedObject.ApplyModifiedProperties();
            list.index = childIndex;
        }

        private static void RemoveChild(ReorderableList list)
        {
            SerializedProperty childrenProperty = list.serializedProperty;
            int childIndex = list.index;
            if (childIndex < 0 || childIndex >= childrenProperty.arraySize)
            {
                return;
            }

            childrenProperty.serializedObject.Update();
            int originalSize = childrenProperty.arraySize;
            childrenProperty.DeleteArrayElementAtIndex(childIndex);

            // Unity keeps a SerializeReference element as null on the first
            // delete, so call again only when the array still contains it.
            if (childrenProperty.arraySize == originalSize)
            {
                childrenProperty.DeleteArrayElementAtIndex(childIndex);
            }

            childrenProperty.serializedObject.ApplyModifiedProperties();
            list.index = Mathf.Min(childIndex, childrenProperty.arraySize - 1);
        }
    }
}
