using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SkillFramework.Editor
{
    internal static class SkillConditionListEditorUtility
    {
        private const float ClearButtonWidth = 48f;
        private const float AddButtonWidth = 96f;
        private const float NestedSpacing = 2f;
        private static readonly Dictionary<string, ReorderableList> Lists = new();

        internal static ReorderableList GetList(
            SerializedProperty property,
            string fieldName,
            string headerLabel)
        {
            SerializedProperty listProperty = property.FindPropertyRelative(fieldName);
            string key = $"{property.serializedObject.targetObject.GetInstanceID()}:" +
                         listProperty?.propertyPath;
            if (Lists.TryGetValue(key, out ReorderableList conditionList) &&
                conditionList != null &&
                conditionList.serializedProperty != null &&
                conditionList.serializedProperty.propertyPath == listProperty?.propertyPath)
            {
                return conditionList;
            }

            conditionList = CreateConditionList(listProperty, headerLabel);
            Lists[key] = conditionList;
            return conditionList;
        }

        private static ReorderableList CreateConditionList(
            SerializedProperty conditionsProperty,
            string headerLabel)
        {
            return new ReorderableList(
                conditionsProperty.serializedObject,
                conditionsProperty,
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, headerLabel),
                drawElementCallback = (rect, index, active, focused) =>
                {
                    SerializedProperty conditionProperty =
                        conditionsProperty.GetArrayElementAtIndex(index);
                    string displayName = conditionProperty.managedReferenceValue != null
                        ? SkillConditionEditorUtility.GetConditionDisplayName(
                            conditionProperty.managedReferenceValue.GetType())
                        : "No Condition";
                    Rect foldoutRect = new Rect(
                        rect.x,
                        rect.y,
                        rect.width,
                        EditorGUIUtility.singleLineHeight);
                    conditionProperty.isExpanded = EditorGUI.Foldout(
                        foldoutRect,
                        conditionProperty.isExpanded,
                        displayName,
                        true);

                    if (conditionProperty.isExpanded)
                    {
                        DrawConditionChildren(rect, conditionProperty);
                    }
                },
                elementHeightCallback = index =>
                {
                    if (index >= conditionsProperty.arraySize)
                    {
                        return EditorGUIUtility.singleLineHeight;
                    }

                    SerializedProperty conditionProperty =
                        conditionsProperty.GetArrayElementAtIndex(index);
                    return GetConditionElementHeight(conditionProperty);
                },
                onAddDropdownCallback = (buttonRect, list) =>
                    ShowAddConditionMenu(list),
                onRemoveCallback = list =>
                {
                    SerializedProperty conditions = list.serializedProperty;
                    int index = list.index;
                    if (index >= 0 && index < conditions.arraySize)
                    {
                        conditions.DeleteArrayElementAtIndex(index);
                        conditions.serializedObject.ApplyModifiedProperties();
                        list.index = Mathf.Min(index, conditions.arraySize - 1);
                    }
                }
            };
        }

        private static void ShowAddConditionMenu(ReorderableList list)
        {
            GenericMenu menu = new GenericMenu();
            List<Type> conditionTypes = SkillConditionEditorUtility.GetConditionTypes();
            if (conditionTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Condition Configs"));
                menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
                return;
            }

            foreach (Type conditionType in conditionTypes)
            {
                Type type = conditionType;
                menu.AddItem(
                    new GUIContent(SkillConditionEditorUtility.GetConditionDisplayName(type)),
                    false,
                    () => AddCondition(list, type));
            }

            menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
        }

        private static void AddCondition(ReorderableList list, Type conditionType)
        {
            SerializedProperty conditionsProperty = list.serializedProperty;
            conditionsProperty.serializedObject.Update();
            int conditionIndex = conditionsProperty.arraySize++;
            SerializedProperty conditionProperty =
                conditionsProperty.GetArrayElementAtIndex(conditionIndex);
            conditionProperty.managedReferenceValue =
                Activator.CreateInstance(conditionType);
            conditionsProperty.serializedObject.ApplyModifiedProperties();
            list.index = conditionIndex;
        }

        private static float GetConditionElementHeight(
            SerializedProperty conditionProperty)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!conditionProperty.isExpanded)
            {
                return height;
            }

            SerializedProperty iterator = conditionProperty.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();

            if (iterator.Next(true) &&
                !SerializedProperty.EqualContents(iterator, endProperty))
            {
                do
                {
                    if (iterator.depth != conditionProperty.depth + 1)
                    {
                        continue;
                    }

                    height += GetChildPropertyHeight(conditionProperty, iterator) +
                              EditorGUIUtility.standardVerticalSpacing;
                }
                while (iterator.Next(false) &&
                       !SerializedProperty.EqualContents(iterator, endProperty));
            }

            return height;
        }

        private static void DrawConditionChildren(
            Rect elementRect,
            SerializedProperty conditionProperty)
        {
            float y = elementRect.y + EditorGUIUtility.singleLineHeight;
            SerializedProperty iterator = conditionProperty.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();

            if (iterator.Next(true) &&
                !SerializedProperty.EqualContents(iterator, endProperty))
            {
                do
                {
                    if (iterator.depth != conditionProperty.depth + 1)
                    {
                        continue;
                    }

                    float childHeight = GetChildPropertyHeight(conditionProperty, iterator);
                    Rect childRect = new Rect(
                        elementRect.x,
                        y,
                        elementRect.width,
                        childHeight);
                    DrawConditionChild(childRect, conditionProperty, iterator);
                    y += childHeight + EditorGUIUtility.standardVerticalSpacing;
                }
                while (iterator.Next(false) &&
                       !SerializedProperty.EqualContents(iterator, endProperty));
            }
        }

        private static float GetChildPropertyHeight(
            SerializedProperty conditionParent,
            SerializedProperty childProperty)
        {
            if (childProperty.isArray)
            {
                return GetList(
                    conditionParent,
                    childProperty.name,
                    childProperty.displayName).GetHeight();
            }

            if (childProperty.propertyType == SerializedPropertyType.ManagedReference &&
                childProperty.managedReferenceValue == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            return EditorGUI.GetPropertyHeight(
                childProperty,
                new GUIContent(childProperty.displayName),
                true);
        }

        private static void DrawConditionChild(
            Rect rect,
            SerializedProperty conditionParent,
            SerializedProperty childProperty)
        {
            if (childProperty.isArray)
            {
                GetList(conditionParent, childProperty.name, childProperty.displayName)
                    .DoList(rect);
                return;
            }

            if (childProperty.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(rect, childProperty, true);
                return;
            }

            if (childProperty.managedReferenceValue == null)
            {
                Rect buttonRect = new Rect(
                    rect.x,
                    rect.y,
                    Mathf.Min(AddButtonWidth, rect.width),
                    EditorGUIUtility.singleLineHeight);
                Rect labelRect = new Rect(
                    buttonRect.xMax + NestedSpacing,
                    rect.y,
                    Mathf.Max(0f, rect.xMax - buttonRect.xMax - NestedSpacing),
                    EditorGUIUtility.singleLineHeight);

                if (GUI.Button(buttonRect, "Add Condition"))
                {
                    GenericMenu menu = new GenericMenu();
                    SkillConditionEditorUtility.AddConditionMenuItems(menu, childProperty);
                    menu.ShowAsContext();
                }

                GUI.Label(labelRect, childProperty.displayName, EditorStyles.miniLabel);
            }
            else
            {
                float fieldWidth = Mathf.Max(
                    0f,
                    rect.width - ClearButtonWidth - EditorGUIUtility.standardVerticalSpacing);
                Rect fieldRect = new Rect(
                    rect.x,
                    rect.y,
                    fieldWidth,
                    rect.height);
                Rect clearRect = new Rect(
                    fieldRect.xMax + EditorGUIUtility.standardVerticalSpacing,
                    rect.y,
                    Mathf.Min(ClearButtonWidth, rect.xMax - fieldRect.xMax),
                    EditorGUIUtility.singleLineHeight);

                EditorGUI.PropertyField(
                    fieldRect,
                    childProperty,
                    new GUIContent(childProperty.displayName),
                    true);

                if (GUI.Button(clearRect, "Clear"))
                {
                    childProperty.managedReferenceValue = null;
                    childProperty.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
