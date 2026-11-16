using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SkillFramework.Editor
{
    internal static class SkillConditionEditorUtility
    {
        internal static List<Type> GetConditionTypes()
        {
            List<Type> conditionTypes = new List<Type>();

            foreach (Type type in TypeCache.GetTypesDerivedFrom<SkillConditionConfig>())
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericType)
                {
                    continue;
                }

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                if (!type.IsDefined(typeof(SerializableAttribute), false))
                {
                    continue;
                }

                conditionTypes.Add(type);
            }

            conditionTypes.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
            return conditionTypes;
        }

        internal static string GetConditionDisplayName(Type conditionType)
        {
            string name = conditionType.Name;
            return name.EndsWith("Config", StringComparison.Ordinal)
                ? name.Substring(0, name.Length - "Config".Length)
                : name;
        }

        internal static void AddConditionMenuItems(
            GenericMenu menu,
            SerializedProperty conditionProperty)
        {
            string propertyPath = conditionProperty.propertyPath;
            List<Type> conditionTypes = GetConditionTypes();
            if (conditionTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Condition Configs"));
                return;
            }

            foreach (Type conditionType in conditionTypes)
            {
                Type type = conditionType;
                menu.AddItem(
                    new GUIContent(GetConditionDisplayName(conditionType)),
                    false,
                    () =>
                    {
                        SerializedProperty resolvedProperty =
                            conditionProperty.serializedObject.FindProperty(propertyPath);
                        if (resolvedProperty != null)
                        {
                            AddCondition(resolvedProperty, type);
                        }
                    });
            }
        }

        internal static void AddCondition(
            SerializedProperty conditionProperty,
            Type conditionType)
        {
            string fieldTypeName = conditionProperty.managedReferenceFieldTypename;
            if (!string.IsNullOrEmpty(fieldTypeName) &&
                !fieldTypeName.Contains(nameof(SkillConditionConfig)))
            {
                Debug.LogWarning(
                    $"Cannot add condition {conditionType.Name} to " +
                    $"{conditionProperty.propertyPath} (field type: {fieldTypeName})");
                return;
            }

            conditionProperty.managedReferenceValue = Activator.CreateInstance(conditionType);
            conditionProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}
