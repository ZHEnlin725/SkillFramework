using System;
using UnityEngine;

namespace SkillFramework
{
    [Serializable]
    public abstract class DecoratorSkillNodeConfig : SkillNodeConfig
    {
        [SerializeReference]
        public SkillNodeConfig ChildNode;
    }
}
