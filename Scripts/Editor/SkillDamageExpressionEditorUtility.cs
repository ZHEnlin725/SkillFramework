using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SkillFramework.Editor
{
    internal static class SkillDamageExpressionEditorUtility
    {
        public static List<Type> GetExpressionTypes()
        {
            List<Type> expressionTypes = new List<Type>();

            foreach (Type type in TypeCache.GetTypesDerivedFrom<SkillDamageExpression>())
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

                expressionTypes.Add(type);
            }

            expressionTypes.Sort(
                (left, right) => string.CompareOrdinal(
                    GetExpressionDisplayName(left),
                    GetExpressionDisplayName(right)));
            return expressionTypes;
        }

        public static string GetExpressionDisplayName(Type expressionType)
        {
            if (expressionType == null)
            {
                return "Missing Expression";
            }

            string name = expressionType.Name;
            if (name.StartsWith("Skill", StringComparison.Ordinal))
            {
                name = name.Substring("Skill".Length);
            }

            if (name.EndsWith("Expression", StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - "Expression".Length);
            }

            return string.IsNullOrEmpty(name) ? expressionType.Name : name;
        }

        public static void ShowExpressionTypeMenu(
            Rect buttonRect,
            SerializedProperty expressionProperty)
        {
            GenericMenu menu = new GenericMenu();
            List<Type> expressionTypes = GetExpressionTypes();
            if (expressionTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Expression Types"));
                menu.DropDown(buttonRect);
                return;
            }

            string propertyPath = expressionProperty.propertyPath;
            SerializedObject serializedObject = expressionProperty.serializedObject;
            foreach (Type expressionType in expressionTypes)
            {
                string displayName = GetExpressionDisplayName(expressionType);
                menu.AddItem(
                    new GUIContent(displayName),
                    expressionProperty.managedReferenceValue != null &&
                    expressionProperty.managedReferenceValue.GetType() == expressionType,
                    () => SetExpressionType(serializedObject, propertyPath, expressionType));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(
                new GUIContent("Clear"),
                false,
                () => ClearExpression(serializedObject, propertyPath));
            menu.DropDown(buttonRect);
        }

        private static void SetExpressionType(
            SerializedObject serializedObject,
            string propertyPath,
            Type expressionType)
        {
            serializedObject.Update();
            SerializedProperty expressionProperty = serializedObject.FindProperty(propertyPath);
            if (expressionProperty == null)
            {
                return;
            }

            UnityEngine.Object targetObject = serializedObject.targetObject;
            if (targetObject != null)
            {
                Undo.RecordObject(targetObject, "Change Damage Expression");
            }

            expressionProperty.managedReferenceValue =
                Activator.CreateInstance(expressionType);
            expressionProperty.isExpanded = true;
            serializedObject.ApplyModifiedProperties();
        }

        private static void ClearExpression(
            SerializedObject serializedObject,
            string propertyPath)
        {
            serializedObject.Update();
            SerializedProperty expressionProperty = serializedObject.FindProperty(propertyPath);
            if (expressionProperty == null)
            {
                return;
            }

            UnityEngine.Object targetObject = serializedObject.targetObject;
            if (targetObject != null)
            {
                Undo.RecordObject(targetObject, "Clear Damage Expression");
            }

            expressionProperty.managedReferenceValue = null;
            expressionProperty.isExpanded = false;
            serializedObject.ApplyModifiedProperties();
        }
    }
}

