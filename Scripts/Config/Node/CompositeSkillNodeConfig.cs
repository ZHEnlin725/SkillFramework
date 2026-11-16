using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillFramework
{
    [Serializable]
    [SkillNodeAlias("Composite")]
    public class CompositeSkillNodeConfig : SkillNodeConfig
    {
        [SerializeReference]
        public List<SkillNodeConfig> Children = new();
    }
}
