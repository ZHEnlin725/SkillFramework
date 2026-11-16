using System;

namespace SkillFramework
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class SkillNodeAliasAttribute : Attribute
    {
        public string Name { get; }

        public SkillNodeAliasAttribute(string name)
        {
            Name = name;
        }
    }
}
