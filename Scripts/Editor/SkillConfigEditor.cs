using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SkillFramework.Editor
{
    [CustomEditor(typeof(SkillConfig))]
    public sealed class SkillConfigEditor : UnityEditor.Editor
    {
        private ReorderableList nodesList;

        private void OnEnable()
        {
            CreateNodesList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SkillId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("DurationFrames"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CooldownFrames"));

            EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing * 2f);
            nodesList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void CreateNodesList()
        {
            SerializedProperty nodesProperty = serializedObject.FindProperty("Nodes");
            nodesList = new ReorderableList(
                serializedObject,
                nodesProperty,
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Nodes"),
                drawElementCallback = (rect, index, active, focused) =>
                    EditorGUI.PropertyField(
                        rect,
                        nodesProperty.GetArrayElementAtIndex(index),
                        GUIContent.none,
                        true),
                elementHeightCallback = index =>
                {
                    SerializedProperty element = nodesProperty.GetArrayElementAtIndex(index);
                    return EditorGUI.GetPropertyHeight(element, GUIContent.none, true) +
                           EditorGUIUtility.standardVerticalSpacing;
                },
                onAddDropdownCallback = (buttonRect, list) => ShowAddNodeMenu(list),
                onRemoveCallback = list =>
                {
                    list.serializedProperty.DeleteArrayElementAtIndex(list.index);
                    list.index = -1;
                }
            };
        }

        private static void ShowAddNodeMenu(ReorderableList list)
        {
            GenericMenu menu = new GenericMenu();
            foreach (SkillNodeConfigType nodeType in SkillNodeConfigTypeCache.GetTypes())
            {
                Type type = nodeType.Type;
                menu.AddItem(
                    new GUIContent(nodeType.DisplayName),
                    false,
                    () => AddNode(list, type));
            }

            menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
        }

        private static void AddNode(ReorderableList list, Type type)
        {
            SerializedProperty nodesProperty = list.serializedProperty;
            int nodeIndex = nodesProperty.arraySize++;
            SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(nodeIndex);
            nodeProperty.managedReferenceValue = Activator.CreateInstance(type);
            nodeProperty.FindPropertyRelative("NodeId").intValue =
                SkillNodeEditorUtility.GetNextNodeId(nodesProperty);
        }

    }
}
