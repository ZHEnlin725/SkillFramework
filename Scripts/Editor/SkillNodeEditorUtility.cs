using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SkillFramework.Editor
{
    internal static class SkillNodeEditorUtility
    {
        private const string ChildNodePathSuffix = ".ChildNode";
        private const string ChildrenListPathSuffix = ".Children.Array.data[";

        public static void PrepareCopiedNode(
            SkillNodeConfig copiedConfig,
            SerializedProperty nodesProperty)
        {
            Dictionary<int, int> nodeIdMap = new();
            int nextNodeId = GetNextNodeId(nodesProperty);
            AssignNodeIds(copiedConfig, nodeIdMap, ref nextNodeId);
            RemapSubtreeDependencies(copiedConfig, nodeIdMap);
        }

        public static void InitializeChildNodeTiming(
            SerializedProperty childProperty,
            int defaultDurationFrames)
        {
            SerializedProperty parentProperty = GetParentNodeProperty(childProperty);
            if (parentProperty != null &&
                parentProperty.managedReferenceValue is SequenceSkillNodeConfig)
            {
                InitializeSequenceChildNodeTiming(
                    childProperty,
                    parentProperty,
                    defaultDurationFrames);
                return;
            }

            InitializeDirectChildNodeTiming(childProperty, parentProperty, defaultDurationFrames);
        }

        private static void InitializeDirectChildNodeTiming(
            SerializedProperty childProperty,
            SerializedProperty parentProperty,
            int defaultDurationFrames)
        {
            int parentStart = GetStartFrame(parentProperty);
            int parentDuration = GetDuration(parentProperty);
            childProperty.FindPropertyRelative("StartFrameId").intValue = parentStart;
            childProperty.FindPropertyRelative("DurationFrames").intValue = Mathf.Clamp(
                defaultDurationFrames,
                1,
                parentDuration);
        }

        private static void InitializeSequenceChildNodeTiming(
            SerializedProperty childProperty,
            SerializedProperty parentProperty,
            int defaultDurationFrames)
        {
            SerializedProperty childrenProperty =
                parentProperty.FindPropertyRelative("Children");
            int parentStart = GetStartFrame(parentProperty);
            int parentEnd = parentStart + GetDuration(parentProperty);
            int start = parentStart;

            if (childrenProperty != null)
            {
                for (int i = 0; i < childrenProperty.arraySize; i++)
                {
                    SerializedProperty siblingProperty =
                        childrenProperty.GetArrayElementAtIndex(i);
                    if (siblingProperty == null ||
                        siblingProperty.propertyPath == childProperty.propertyPath ||
                        siblingProperty.managedReferenceValue == null)
                    {
                        continue;
                    }

                    int siblingEnd = GetStartFrame(siblingProperty) +
                                     GetDuration(siblingProperty);
                    start = Mathf.Max(start, siblingEnd + 1);
                }
            }

            int duration = Mathf.Max(1, defaultDurationFrames);
            int childEnd = start + duration;
            int requiredDuration = Mathf.Max(1, childEnd - parentStart);
            SerializedProperty parentDurationProperty =
                parentProperty.FindPropertyRelative("DurationFrames");
            if (parentDurationProperty != null &&
                requiredDuration > parentDurationProperty.intValue)
            {
                parentDurationProperty.intValue = requiredDuration;
                parentEnd = parentStart + requiredDuration;
            }

            childProperty.FindPropertyRelative("StartFrameId").intValue =
                Mathf.Max(parentStart, start);
            childProperty.FindPropertyRelative("DurationFrames").intValue =
                Mathf.Clamp(duration, 1, Mathf.Max(1, parentEnd - start));
        }

        public static void ClampChildNodeFrames(SerializedProperty nodeProperty)
        {
            SerializedProperty rootNodeProperty = GetRootNodeProperty(nodeProperty);
            EnsureDurationAtLeastOne(rootNodeProperty);
            ClampDescendantFrames(rootNodeProperty);
        }

        public static void SetNodeStartFrame(
            SerializedProperty nodeProperty,
            int startFrame)
        {
            if (nodeProperty == null || nodeProperty.managedReferenceValue == null)
            {
                return;
            }

            SerializedProperty startProperty =
                nodeProperty.FindPropertyRelative("StartFrameId");
            if (startProperty == null)
            {
                return;
            }

            int previousStart = startProperty.intValue;
            int nextStart = Mathf.Max(0, startFrame);
            startProperty.intValue = nextStart;
            if (nodeProperty.managedReferenceValue is CompositeSkillNodeConfig or
                DecoratorSkillNodeConfig)
            {
                ShiftDescendantStartFrames(nodeProperty, nextStart - previousStart);
            }
        }

        public static bool CanSetSequenceDuration(
            SerializedProperty sequenceProperty,
            int durationFrames)
        {
            if (sequenceProperty == null ||
                sequenceProperty.managedReferenceValue is not SequenceSkillNodeConfig)
            {
                return true;
            }

            int parentStart = GetStartFrame(sequenceProperty);
            int parentEnd = parentStart + Mathf.Max(1, durationFrames);
            SerializedProperty childrenProperty =
                sequenceProperty.FindPropertyRelative("Children");
            if (childrenProperty == null)
            {
                return true;
            }

            for (int i = 0; i < childrenProperty.arraySize; i++)
            {
                SerializedProperty childProperty =
                    childrenProperty.GetArrayElementAtIndex(i);
                if (childProperty == null || childProperty.managedReferenceValue == null)
                {
                    continue;
                }

                int childEnd = GetStartFrame(childProperty) + GetDuration(childProperty);
                if (childEnd > parentEnd)
                {
                    return false;
                }
            }

            return true;
        }

        public static SerializedProperty GetParentNodeProperty(SerializedProperty childProperty)
        {
            string propertyPath = childProperty.propertyPath;
            int childSuffixIndex = propertyPath.LastIndexOf(
                ChildNodePathSuffix,
                StringComparison.Ordinal);
            int childrenSuffixIndex = propertyPath.LastIndexOf(
                ChildrenListPathSuffix,
                StringComparison.Ordinal);
            int suffixIndex = childSuffixIndex >= childrenSuffixIndex
                ? childSuffixIndex
                : childrenSuffixIndex;
            if (suffixIndex < 0)
            {
                return null;
            }

            return childProperty.serializedObject.FindProperty(
                childProperty.propertyPath.Substring(0, suffixIndex));
        }

        private static void ShiftDescendantStartFrames(
            SerializedProperty parentProperty,
            int deltaFrames)
        {
            if (deltaFrames == 0)
            {
                return;
            }

            SerializedProperty childNodeProperty =
                parentProperty.FindPropertyRelative("ChildNode");
            if (childNodeProperty != null &&
                childNodeProperty.managedReferenceValue != null)
            {
                SerializedProperty startProperty =
                    childNodeProperty.FindPropertyRelative("StartFrameId");
                if (startProperty != null)
                {
                    startProperty.intValue += deltaFrames;
                }

                ShiftDescendantStartFrames(childNodeProperty, deltaFrames);
            }

            SerializedProperty childrenProperty =
                parentProperty.FindPropertyRelative("Children");
            if (childrenProperty == null)
            {
                return;
            }

            for (int i = 0; i < childrenProperty.arraySize; i++)
            {
                SerializedProperty childProperty =
                    childrenProperty.GetArrayElementAtIndex(i);
                if (childProperty == null || childProperty.managedReferenceValue == null)
                {
                    continue;
                }

                SerializedProperty startProperty =
                    childProperty.FindPropertyRelative("StartFrameId");
                if (startProperty != null)
                {
                    startProperty.intValue += deltaFrames;
                }

                ShiftDescendantStartFrames(childProperty, deltaFrames);
            }
        }

        public static void DrawChildNodeSummary(Rect rect, SerializedProperty childProperty)
        {
            if (childProperty == null || childProperty.managedReferenceValue == null)
            {
                EditorGUI.LabelField(rect, "No Child");
                return;
            }

            EditorGUI.LabelField(rect, GetChildNodeSummary(childProperty));
        }

        public static void DrawChildNodeElement(
            Rect rect,
            SerializedProperty childProperty)
        {
            const float spacing = 3f;
            float timingWidth = Mathf.Min(rect.width * 0.58f, 240f);
            float timingX = rect.xMax - timingWidth;
            Rect aliasRect = new Rect(
                rect.x,
                rect.y + 1f,
                Mathf.Max(0f, timingX - rect.x - spacing),
                EditorGUIUtility.singleLineHeight);
            string aliasName = SkillNodeConfigTypeCache.GetDisplayName(
                childProperty?.managedReferenceValue?.GetType());
            GUI.Label(aliasRect, aliasName, EditorStyles.boldLabel);

            int startFrameId =
                childProperty?.FindPropertyRelative("StartFrameId")?.intValue ?? 0;
            int durationFrames =
                childProperty?.FindPropertyRelative("DurationFrames")?.intValue ?? 0;
            int intervalFrames =
                childProperty?.FindPropertyRelative("IntervalFrames")?.intValue ?? 0;
            float fieldWidth = Mathf.Max(
                0f,
                (timingWidth - spacing * 2f) / 3f);

            DrawChildNodeSummaryBox(
                new Rect(
                    timingX,
                    rect.y + 1f,
                    fieldWidth,
                    EditorGUIUtility.singleLineHeight),
                "Start",
                startFrameId);
            DrawChildNodeSummaryBox(
                new Rect(
                    timingX + fieldWidth + spacing,
                    rect.y + 1f,
                    fieldWidth,
                    EditorGUIUtility.singleLineHeight),
                "Duration",
                durationFrames);
            DrawChildNodeSummaryBox(
                new Rect(
                    timingX + (fieldWidth + spacing) * 2f,
                    rect.y + 1f,
                    fieldWidth,
                    EditorGUIUtility.singleLineHeight),
                "Interval",
                intervalFrames);
        }

        private static void DrawChildNodeSummaryBox(
            Rect rect,
            string label,
            int value)
        {
            EditorGUI.HelpBox(rect, $"{label}: {value}", MessageType.None);
        }

        public static string GetChildNodeSummary(SerializedProperty childProperty)
        {
            if (childProperty == null || childProperty.managedReferenceValue == null)
            {
                return "No Child";
            }

            int startFrameId = childProperty.FindPropertyRelative("StartFrameId")?.intValue ?? 0;
            int durationFrames = childProperty.FindPropertyRelative("DurationFrames")?.intValue ?? 0;
            int intervalFrames = childProperty.FindPropertyRelative("IntervalFrames")?.intValue ?? 0;
            return $"Start: {startFrameId}   Duration: {durationFrames}   Interval: {intervalFrames}";
        }

        public static int GetNextNodeId(SerializedProperty nodeProperty)
        {
            int nextNodeId = 1;
            SerializedProperty nodesProperty = nodeProperty?.serializedObject.FindProperty("Nodes");
            if (nodesProperty == null)
            {
                return nextNodeId;
            }

            for (int i = 0; i < nodesProperty.arraySize; i++)
            {
                SerializedProperty rootProperty = nodesProperty.GetArrayElementAtIndex(i);
                nextNodeId = Mathf.Max(nextNodeId, GetMaxNodeId(rootProperty) + 1);
            }

            return nextNodeId;
        }

        private static int GetStartFrame(SerializedProperty nodeProperty)
        {
            return nodeProperty?.FindPropertyRelative("StartFrameId")?.intValue ?? 0;
        }

        private static int GetDuration(SerializedProperty nodeProperty)
        {
            return Mathf.Max(
                1,
                nodeProperty?.FindPropertyRelative("DurationFrames")?.intValue ?? 0);
        }

        private static void ClampDescendantFrames(SerializedProperty parentProperty)
        {
            if (parentProperty == null || parentProperty.managedReferenceValue == null)
            {
                return;
            }

            if (parentProperty.managedReferenceValue is SequenceSkillNodeConfig)
            {
                ClampSequenceChildFrames(parentProperty);
            }

            SerializedProperty childProperty = parentProperty.FindPropertyRelative("ChildNode");
            if (childProperty != null && childProperty.managedReferenceValue != null)
            {
                ClampChildFrames(childProperty, parentProperty);
                ClampDescendantFrames(childProperty);
            }

            SerializedProperty childrenProperty = parentProperty.FindPropertyRelative("Children");
            if (childrenProperty == null)
            {
                return;
            }

            for (int i = 0; i < childrenProperty.arraySize; i++)
            {
                SerializedProperty childPropertyInList =
                    childrenProperty.GetArrayElementAtIndex(i);
                if (childPropertyInList == null || childPropertyInList.managedReferenceValue == null)
                {
                    continue;
                }

                ClampChildFrames(childPropertyInList, parentProperty);
                ClampDescendantFrames(childPropertyInList);
            }
        }

        private static void ClampSequenceChildFrames(SerializedProperty parentProperty)
        {
            SerializedProperty childrenProperty = parentProperty.FindPropertyRelative("Children");
            if (childrenProperty == null)
            {
                return;
            }

            int parentStart = GetStartFrame(parentProperty);
            int parentEnd = parentStart + GetDuration(parentProperty);
            int previousEnd = parentStart - 1;

            for (int i = 0; i < childrenProperty.arraySize; i++)
            {
                SerializedProperty childProperty =
                    childrenProperty.GetArrayElementAtIndex(i);
                if (childProperty == null || childProperty.managedReferenceValue == null)
                {
                    continue;
                }

                SerializedProperty startProperty =
                    childProperty.FindPropertyRelative("StartFrameId");
                SerializedProperty durationProperty =
                    childProperty.FindPropertyRelative("DurationFrames");
                if (startProperty == null || durationProperty == null)
                {
                    continue;
                }

                int nextStart = -1;
                for (int j = i + 1; j < childrenProperty.arraySize; j++)
                {
                    SerializedProperty nextProperty =
                        childrenProperty.GetArrayElementAtIndex(j);
                    if (nextProperty == null || nextProperty.managedReferenceValue == null)
                    {
                        continue;
                    }

                    nextStart = GetStartFrame(nextProperty);
                    break;
                }

                int minimumStart = previousEnd + 1;
                int maximumEnd = nextStart < 0
                    ? parentEnd
                    : Mathf.Min(parentEnd, nextStart);

                int start;
                int duration;
                if (maximumEnd < minimumStart)
                {
                    start = minimumStart;
                    duration = 1;
                }
                else
                {
                    start = Mathf.Clamp(
                        startProperty.intValue,
                        minimumStart,
                        Mathf.Max(minimumStart, maximumEnd - 1));
                    duration = Mathf.Clamp(
                        durationProperty.intValue,
                        1,
                        Mathf.Max(1, maximumEnd - start));
                }

                if (nextStart >= 0)
                {
                    duration = Mathf.Min(
                        duration,
                        Mathf.Max(1, nextStart - start));
                }

                startProperty.intValue = start;
                durationProperty.intValue = duration;
                previousEnd = start + duration;
            }

            SerializedProperty parentDurationProperty =
                parentProperty.FindPropertyRelative("DurationFrames");
            if (parentDurationProperty != null && previousEnd > parentEnd)
            {
                parentDurationProperty.intValue =
                    Mathf.Max(parentDurationProperty.intValue, previousEnd - parentStart);
            }
        }

        private static void ClampChildFrames(
            SerializedProperty childProperty,
            SerializedProperty parentProperty)
        {
            int parentStart = GetStartFrame(parentProperty);
            int parentDuration = GetDuration(parentProperty);
            SerializedProperty childStartProperty =
                childProperty.FindPropertyRelative("StartFrameId");
            SerializedProperty childDurationProperty =
                childProperty.FindPropertyRelative("DurationFrames");

            childStartProperty.intValue = Mathf.Clamp(
                childStartProperty.intValue,
                parentStart,
                parentStart + parentDuration - 1);
            childDurationProperty.intValue = Mathf.Clamp(
                childDurationProperty.intValue,
                1,
                parentStart + parentDuration - childStartProperty.intValue);
        }

        private static void EnsureDurationAtLeastOne(SerializedProperty nodeProperty)
        {
            SerializedProperty durationProperty =
                nodeProperty?.FindPropertyRelative("DurationFrames");
            if (durationProperty != null)
            {
                durationProperty.intValue = Mathf.Max(1, durationProperty.intValue);
            }
        }

        private static SerializedProperty GetRootNodeProperty(SerializedProperty nodeProperty)
        {
            const string rootPrefix = "Nodes.Array.data[";
            string propertyPath = nodeProperty.propertyPath;
            if (!propertyPath.StartsWith(rootPrefix, StringComparison.Ordinal))
            {
                return null;
            }

            int endIndex = propertyPath.IndexOf(']', rootPrefix.Length);
            if (endIndex < 0)
            {
                return null;
            }

            return nodeProperty.serializedObject.FindProperty(
                propertyPath.Substring(0, endIndex + 1));
        }

        private static int GetMaxNodeId(SerializedProperty nodeProperty)
        {
            if (nodeProperty == null || nodeProperty.managedReferenceValue == null)
            {
                return 0;
            }

            int maxNodeId = nodeProperty.FindPropertyRelative("NodeId")?.intValue ?? 0;
            SerializedProperty childNodeProperty = nodeProperty.FindPropertyRelative("ChildNode");
            if (childNodeProperty != null)
            {
                maxNodeId = Mathf.Max(maxNodeId, GetMaxNodeId(childNodeProperty));
            }

            SerializedProperty childrenProperty = nodeProperty.FindPropertyRelative("Children");
            if (childrenProperty != null)
            {
                for (int i = 0; i < childrenProperty.arraySize; i++)
                {
                    maxNodeId = Mathf.Max(
                        maxNodeId,
                        GetMaxNodeId(childrenProperty.GetArrayElementAtIndex(i)));
                }
            }

            return maxNodeId;
        }

        private static void AssignNodeIds(
            SkillNodeConfig nodeConfig,
            Dictionary<int, int> nodeIdMap,
            ref int nextNodeId)
        {
            if (nodeConfig == null)
            {
                return;
            }

            int oldNodeId = nodeConfig.NodeId;
            nodeConfig.NodeId = nextNodeId++;
            nodeIdMap[oldNodeId] = nodeConfig.NodeId;

            if (nodeConfig is DecoratorSkillNodeConfig decoratorConfig)
            {
                AssignNodeIds(decoratorConfig.ChildNode, nodeIdMap, ref nextNodeId);
            }

            if (nodeConfig is CompositeSkillNodeConfig compositeConfig &&
                compositeConfig.Children != null)
            {
                for (int i = 0; i < compositeConfig.Children.Count; i++)
                {
                    AssignNodeIds(compositeConfig.Children[i], nodeIdMap, ref nextNodeId);
                }
            }
        }

        private static void RemapSubtreeDependencies(
            SkillNodeConfig nodeConfig,
            Dictionary<int, int> nodeIdMap)
        {
            if (nodeConfig == null)
            {
                return;
            }

            if (nodeConfig.DependsOnNodeIds != null)
            {
                for (int i = 0; i < nodeConfig.DependsOnNodeIds.Count; i++)
                {
                    if (nodeIdMap.TryGetValue(nodeConfig.DependsOnNodeIds[i], out int newNodeId))
                    {
                        nodeConfig.DependsOnNodeIds[i] = newNodeId;
                    }
                }
            }

            if (nodeConfig is DecoratorSkillNodeConfig decoratorConfig)
            {
                RemapSubtreeDependencies(decoratorConfig.ChildNode, nodeIdMap);
            }

            if (nodeConfig is CompositeSkillNodeConfig compositeConfig &&
                compositeConfig.Children != null)
            {
                for (int i = 0; i < compositeConfig.Children.Count; i++)
                {
                    RemapSubtreeDependencies(compositeConfig.Children[i], nodeIdMap);
                }
            }
        }
    }
}
