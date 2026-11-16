using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SkillFramework.Editor
{
    [CustomPropertyDrawer(typeof(SkillNodeConfig), true)]
    public class SkillNodeConfigPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            height += GetDependencyAreaHeight(property);
            height += EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            foreach (SerializedProperty childProperty in EnumerateProperties(property))
            {
                if (childProperty.name == "NodeId" ||
                    childProperty.name == "Enabled" ||
                    childProperty.name == "DependsOnNodeIds" ||
                    IsTimingProperty(childProperty.name) ||
                    IsExtraProperty(childProperty))
                {
                    continue;
                }

                height += GetDirectPropertyHeight(childProperty) +
                          EditorGUIUtility.standardVerticalSpacing;
            }

            height += GetExtraHeight(property);
            return Mathf.Max(EditorGUIUtility.singleLineHeight, height);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect rowRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);

            DrawNodeId(rowRect, property);
            rowRect.y = rowRect.yMax + EditorGUIUtility.standardVerticalSpacing;

            if (HasDependencies(property))
            {
                ReorderableList dependencyList = CreateDependencyList(property);
                Rect dependencyRect = new Rect(
                    position.x,
                    rowRect.y,
                    position.width,
                    dependencyList.GetHeight());
                dependencyList.DoList(dependencyRect);
                rowRect.y = dependencyRect.yMax + EditorGUIUtility.standardVerticalSpacing;
            }

            DrawTimingFields(rowRect, property);
            rowRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            foreach (SerializedProperty childProperty in EnumerateProperties(property))
            {
                if (childProperty.name == "NodeId" ||
                    childProperty.name == "Enabled" ||
                    childProperty.name == "DependsOnNodeIds" ||
                    IsTimingProperty(childProperty.name) ||
                    IsExtraProperty(childProperty))
                {
                    continue;
                }

                float propertyHeight = GetDirectPropertyHeight(childProperty);
                Rect propertyRect = new Rect(rowRect.x, rowRect.y, rowRect.width, propertyHeight);

                EditorGUI.PropertyField(
                    propertyRect,
                    childProperty,
                    !IsManagedReferenceProperty(childProperty));

                rowRect.y += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            Rect extraRect = new Rect(
                rowRect.x,
                rowRect.y,
                rowRect.width,
                GetExtraHeight(property));
            DrawExtraFields(extraRect, property, label);
            SkillNodeEditorUtility.ClampChildNodeFrames(property);
        }

        protected virtual bool IsExtraProperty(SerializedProperty property)
        {
            return false;
        }

        protected virtual float GetExtraHeight(SerializedProperty property)
        {
            return 0f;
        }

        protected virtual void DrawExtraFields(Rect position, SerializedProperty property, GUIContent label)
        {
        }

        private static bool IsTimingProperty(string propertyName)
        {
            return propertyName == "StartFrameId" ||
                   propertyName == "DurationFrames" ||
                   propertyName == "IntervalFrames";
        }

        private static bool HasDependencies(SerializedProperty property)
        {
            SerializedProperty dependsOnNodeIds =
                property.FindPropertyRelative("DependsOnNodeIds");
            return dependsOnNodeIds != null && dependsOnNodeIds.arraySize > 0;
        }

        private static float GetDependencyAreaHeight(SerializedProperty property)
        {
            return HasDependencies(property)
                ? CreateDependencyList(property).GetHeight()
                : 0f;
        }

        private static void DrawDependencyAddButton(Rect rect, SerializedProperty property)
        {
            if (GUI.Button(rect, "+", EditorStyles.miniButton))
            {
                ShowDependencyMenu(property);
            }
        }

        private static void DrawTimingFields(Rect rowRect, SerializedProperty property)
        {
            const float spacing = 3f;
            float columnWidth = Mathf.Max(0f, (rowRect.width - spacing * 2f) / 3f);
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 54f;

            try
            {
                DrawTimingField(
                    new Rect(rowRect.x, rowRect.y, columnWidth, rowRect.height),
                    property.FindPropertyRelative("StartFrameId"),
                    "Start");
                DrawTimingField(
                    new Rect(rowRect.x + columnWidth + spacing, rowRect.y, columnWidth, rowRect.height),
                    property.FindPropertyRelative("DurationFrames"),
                    "Duration");
                DrawTimingField(
                    new Rect(rowRect.x + (columnWidth + spacing) * 2f, rowRect.y, columnWidth, rowRect.height),
                    property.FindPropertyRelative("IntervalFrames"),
                    "Interval");
            }
            finally
            {
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }

        private static void DrawTimingField(Rect rect, SerializedProperty property, string label)
        {
            if (property == null)
            {
                return;
            }

            EditorGUI.HelpBox(rect, string.Empty, MessageType.None);

            Rect fieldRect = new Rect(
                rect.x + 2f,
                rect.y + 1f,
                Mathf.Max(0f, rect.width - 4f),
                Mathf.Max(0f, rect.height - 2f));
            EditorGUI.PropertyField(fieldRect, property, new GUIContent(label), false);
        }

        private static void DrawNodeId(Rect rect, SerializedProperty property)
        {
            const float spacing = 3f;
            const float enabledToggleWidth = 72f;
            bool showAddDependencyButton = !HasDependencies(property);

            GUIContent addDependencyContent = new GUIContent("+", "Add dependency");
            float addDependencyButtonWidth = showAddDependencyButton
                ? EditorStyles.miniButton.CalcSize(addDependencyContent).x
                : 0f;
            Rect toggleRect = new Rect(
                rect.xMax - enabledToggleWidth,
                rect.y,
                Mathf.Min(enabledToggleWidth, Mathf.Max(0f, rect.width)),
                rect.height);
            float addDependencyButtonX = toggleRect.x - spacing - addDependencyButtonWidth;
            Rect addDependencyButtonRect = new Rect(
                addDependencyButtonX,
                rect.y + (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f,
                Mathf.Min(
                    addDependencyButtonWidth,
                    Mathf.Max(0f, toggleRect.x - spacing - addDependencyButtonX)),
                EditorGUIUtility.singleLineHeight);

            SerializedProperty nodeIdProperty = property.FindPropertyRelative("NodeId");
            string nodeIdValue = nodeIdProperty != null ? nodeIdProperty.intValue.ToString() : "0";
            float nodeIdContentRight = addDependencyButtonRect.x - spacing;
            DrawNodeIdContent(rect, nodeIdContentRight, "Node ID", nodeIdValue);

            SerializedProperty enabledProperty = property.FindPropertyRelative("Enabled");
            if (enabledProperty != null)
            {
                bool enabled = EditorGUI.ToggleLeft(
                    toggleRect,
                    "Enabled",
                    enabledProperty.boolValue);
                if (enabled != enabledProperty.boolValue)
                {
                    enabledProperty.boolValue = enabled;
                }
            }

            if (showAddDependencyButton)
            {
                DrawDependencyAddButton(addDependencyButtonRect, property);
            }
        }

        private static void DrawNodeIdContent(
            Rect rect,
            float contentRight,
            string labelText,
            string valueText)
        {
            GUIContent labelContent = new GUIContent(labelText);
            GUIContent valueContent = new GUIContent(valueText);
            float labelWidth = EditorStyles.label.CalcSize(labelContent).x;
            float valueWidth = EditorStyles.label.CalcSize(valueContent).x;
            float labelRectWidth = Mathf.Min(
                labelWidth,
                Mathf.Max(0f, contentRight - rect.x));
            Rect labelRect = new Rect(
                rect.x,
                rect.y,
                labelRectWidth,
                rect.height);
            float valueX = labelRect.xMax + 4f;
            float valueRectWidth = Mathf.Min(
                valueWidth,
                Mathf.Max(0f, contentRight - valueX));
            Rect valueRect = new Rect(
                valueX,
                rect.y,
                valueRectWidth,
                rect.height);

            GUI.Label(labelRect, labelContent, EditorStyles.label);
            GUI.Label(valueRect, valueContent, EditorStyles.label);
        }

        private static ReorderableList CreateDependencyList(SerializedProperty property)
        {
            SerializedProperty dependsOnNodeIds = property.FindPropertyRelative("DependsOnNodeIds");
            return new ReorderableList(
                property.serializedObject,
                dependsOnNodeIds,
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Depends On Node Ids"),
                drawElementCallback = (rect, index, active, focused) =>
                {
                    SerializedProperty element = dependsOnNodeIds.GetArrayElementAtIndex(index);
                    Rect fieldRect = new Rect(
                        rect.x,
                        rect.y + (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f,
                        rect.width,
                        EditorGUIUtility.singleLineHeight);
                    string elementLabel = GetDependencyElementLabel(
                        property.serializedObject,
                        element.intValue);
                    EditorGUI.LabelField(
                        fieldRect,
                        GUIContent.none,
                        new GUIContent(elementLabel),
                        EditorStyles.miniLabel);
                },
                onAddCallback = _ => ShowDependencyMenu(property)
            };
        }

        private static string GetDependencyElementLabel(
            SerializedObject serializedObject,
            int dependencyNodeId)
        {
            foreach (SerializedProperty candidateProperty in
                     EnumerateNodeProperties(serializedObject))
            {
                SerializedProperty nodeIdProperty =
                    candidateProperty.FindPropertyRelative("NodeId");
                if (nodeIdProperty == null || nodeIdProperty.intValue != dependencyNodeId)
                {
                    continue;
                }

                string displayName = SkillNodeConfigTypeCache.GetDisplayName(
                    candidateProperty.managedReferenceValue?.GetType());
                string summary = SkillNodeEditorUtility.GetChildNodeSummary(candidateProperty);
                return $"[{dependencyNodeId}] {displayName}   {summary}";
            }

            return $"[{dependencyNodeId}] Unknown Node";
        }

        private static void ShowDependencyMenu(SerializedProperty nodeProperty)
        {
            GenericMenu menu = new GenericMenu();
            SerializedObject serializedObject = nodeProperty.serializedObject;

            foreach (SerializedProperty candidateProperty in EnumerateNodeProperties(serializedObject))
            {
                if (candidateProperty.propertyPath == nodeProperty.propertyPath)
                {
                    continue;
                }

                int nodeId = candidateProperty.FindPropertyRelative("NodeId").intValue;
                string displayName = SkillNodeConfigTypeCache.GetDisplayName(
                    candidateProperty.managedReferenceValue?.GetType());
                string label = $"[{nodeId}] {displayName}";
                int dependencyNodeId = nodeId;

                menu.AddItem(
                    new GUIContent(label),
                    false,
                    () => AddDependency(serializedObject, nodeProperty.propertyPath, dependencyNodeId));
            }

            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Nodes"));
            }

            menu.ShowAsContext();
        }

        private static void AddDependency(
            SerializedObject serializedObject,
            string nodePropertyPath,
            int dependencyNodeId)
        {
            serializedObject.Update();
            SerializedProperty nodeProperty = serializedObject.FindProperty(nodePropertyPath);
            if (nodeProperty == null)
            {
                return;
            }

            SerializedProperty dependsOnNodeIds = nodeProperty.FindPropertyRelative("DependsOnNodeIds");
            if (dependsOnNodeIds == null)
            {
                return;
            }

            for (int i = 0; i < dependsOnNodeIds.arraySize; i++)
            {
                if (dependsOnNodeIds.GetArrayElementAtIndex(i).intValue == dependencyNodeId)
                {
                    return;
                }
            }

            dependsOnNodeIds.arraySize++;
            dependsOnNodeIds.GetArrayElementAtIndex(dependsOnNodeIds.arraySize - 1).intValue =
                dependencyNodeId;
            serializedObject.ApplyModifiedProperties();
        }

        private static IEnumerable<SerializedProperty> EnumerateNodeProperties(
            SerializedObject serializedObject)
        {
            SerializedProperty nodesProperty = serializedObject.FindProperty("Nodes");
            if (nodesProperty == null)
            {
                yield break;
            }

            for (int i = 0; i < nodesProperty.arraySize; i++)
            {
                SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(i);
                if (nodeProperty == null || nodeProperty.managedReferenceValue == null)
                {
                    continue;
                }

                yield return nodeProperty;

                if (nodeProperty.managedReferenceValue is DecoratorSkillNodeConfig)
                {
                    SerializedProperty childNodeProperty = nodeProperty.FindPropertyRelative("ChildNode");
                    if (childNodeProperty != null && childNodeProperty.managedReferenceValue != null)
                    {
                        yield return childNodeProperty;
                    }
                }
                else if (nodeProperty.managedReferenceValue is CompositeSkillNodeConfig)
                {
                    SerializedProperty childrenProperty =
                        nodeProperty.FindPropertyRelative("Children");
                    if (childrenProperty == null)
                    {
                        continue;
                    }

                    for (int childIndex = 0; childIndex < childrenProperty.arraySize; childIndex++)
                    {
                        SerializedProperty childProperty =
                            childrenProperty.GetArrayElementAtIndex(childIndex);
                        if (childProperty != null && childProperty.managedReferenceValue != null)
                        {
                            yield return childProperty;
                        }
                    }
                }
            }
        }

        protected static IEnumerable<SerializedProperty> EnumerateProperties(SerializedProperty property)
        {
            int rootDepth = property.depth;
            SerializedProperty iterator = property.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();

            if (!iterator.NextVisible(true) ||
                SerializedProperty.EqualContents(iterator, endProperty))
            {
                yield break;
            }

            do
            {
                if (iterator.depth != rootDepth + 1)
                {
                    continue;
                }

                if (iterator.name == "NodeId" || iterator.name == "DependsOnNodeIds")
                {
                    continue;
                }

                if (iterator.name == "Enabled")
                {
                    continue;
                }

                yield return iterator.Copy();
            } while (iterator.NextVisible(false) &&
                     !SerializedProperty.EqualContents(iterator, endProperty));
        }

        protected static bool IsManagedReferenceProperty(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.ManagedReference;
        }

        protected static float GetDirectPropertyHeight(SerializedProperty property)
        {
            return EditorGUI.GetPropertyHeight(
                property,
                !IsManagedReferenceProperty(property));
        }
    }
}
