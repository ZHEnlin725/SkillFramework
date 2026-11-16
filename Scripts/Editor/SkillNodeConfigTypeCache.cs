using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SkillFramework.Editor
{
    public readonly struct SkillNodeConfigType
    {
        public string DisplayName { get; }
        public Type Type { get; }

        public SkillNodeConfigType(Type type, string displayName)
        {
            Type = type;
            DisplayName = displayName;
        }
    }

    public static class SkillNodeConfigTypeCache
    {
        private static readonly Dictionary<Type, string> DisplayNames = new();
        private static List<SkillNodeConfigType> nodeConfigTypes;

        public static IReadOnlyList<SkillNodeConfigType> GetTypes()
        {
            if (nodeConfigTypes == null)
            {
                Refresh();
            }

            return nodeConfigTypes;
        }

        public static SkillNodeConfig Create(Type nodeType)
        {
            if (!CanCreate(nodeType))
            {
                return null;
            }

            try
            {
                return Activator.CreateInstance(nodeType) as SkillNodeConfig;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
        }

        public static SkillNodeConfig CreateCopy(SkillNodeConfig sourceConfig)
        {
            SkillNodeConfig copiedConfig = Create(sourceConfig?.GetType());
            if (copiedConfig == null)
            {
                return null;
            }

            CopyFields(sourceConfig, copiedConfig, new Dictionary<object, object>());
            return copiedConfig;
        }

        public static string GetDisplayName(Type nodeType)
        {
            if (nodeType == null)
            {
                return "Unknown";
            }

            if (nodeConfigTypes == null)
            {
                Refresh();
            }

            return DisplayNames.TryGetValue(nodeType, out string displayName)
                ? displayName
                : GetFallbackDisplayName(nodeType);
        }

        private static void Refresh()
        {
            nodeConfigTypes = new List<SkillNodeConfigType>();
            DisplayNames.Clear();

            foreach (Type type in TypeCache.GetTypesDerivedFrom<SkillNodeConfig>())
            {
                if (!CanCreate(type))
                {
                    continue;
                }

                string displayName = type.GetCustomAttribute<SkillNodeAliasAttribute>(false)?.Name;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = GetFallbackDisplayName(type);
                }

                nodeConfigTypes.Add(new SkillNodeConfigType(type, displayName));
                DisplayNames.Add(type, displayName);
            }

            nodeConfigTypes.Sort(
                (left, right) => string.CompareOrdinal(left.DisplayName, right.DisplayName));
        }

        private static bool CanCreate(Type type)
        {
            return type != null &&
                   typeof(SkillNodeConfig).IsAssignableFrom(type) &&
                   !type.IsAbstract &&
                   !type.IsInterface &&
                   !type.IsGenericType &&
                   type.IsDefined(typeof(SerializableAttribute), false) &&
                   type.GetConstructor(Type.EmptyTypes) != null;
        }

        private static string GetFallbackDisplayName(Type type)
        {
            string name = type.Name;
            return name.EndsWith("Config", StringComparison.Ordinal)
                ? name.Substring(0, name.Length - "Config".Length)
                : name;
        }

        private static void CopyFields(
            object source,
            object destination,
            Dictionary<object, object> clonedObjects)
        {
            for (Type type = source.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                FieldInfo[] fields = type.GetFields(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);

                foreach (FieldInfo field in fields)
                {
                    if (field.IsInitOnly || field.IsStatic ||
                        (!field.IsPublic && !field.IsDefined(typeof(SerializeField), true)))
                    {
                        continue;
                    }

                    field.SetValue(destination, CloneValue(field.GetValue(source), clonedObjects));
                }
            }
        }

        private static object CloneValue(object value, Dictionary<object, object> clonedObjects)
        {
            if (value == null || value.GetType().IsPrimitive || value is string || value is Enum ||
                value is UnityEngine.Object)
            {
                return value;
            }

            if (clonedObjects.TryGetValue(value, out object cachedClone))
            {
                return cachedClone;
            }

            if (value is IList list)
            {
                return CloneList(list, clonedObjects);
            }

            if (value is Array array)
            {
                return CloneArray(array, clonedObjects);
            }

            Type type = value.GetType();
            if (!type.IsClass || type.IsGenericType || !type.IsDefined(typeof(SerializableAttribute), false) ||
                type.GetConstructor(Type.EmptyTypes) == null)
            {
                return value;
            }

            object clone = Activator.CreateInstance(type);
            clonedObjects.Add(value, clone);
            CopyFields(value, clone, clonedObjects);
            return clone;
        }

        private static object CloneList(IList sourceList, Dictionary<object, object> clonedObjects)
        {
            if (Activator.CreateInstance(sourceList.GetType()) is not IList clonedList)
            {
                return sourceList;
            }

            clonedObjects.Add(sourceList, clonedList);
            foreach (object item in sourceList)
            {
                clonedList.Add(CloneValue(item, clonedObjects));
            }

            return clonedList;
        }

        private static object CloneArray(Array sourceArray, Dictionary<object, object> clonedObjects)
        {
            Array clonedArray = Array.CreateInstance(
                sourceArray.GetType().GetElementType(),
                sourceArray.Length);
            clonedObjects.Add(sourceArray, clonedArray);

            for (int i = 0; i < sourceArray.Length; i++)
            {
                clonedArray.SetValue(CloneValue(sourceArray.GetValue(i), clonedObjects), i);
            }

            return clonedArray;
        }
    }
}
