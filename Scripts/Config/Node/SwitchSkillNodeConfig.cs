using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillFramework
{
    [Serializable]
    [SkillNodeAlias("Switch")]
    public sealed class SwitchSkillNodeConfig : CompositeSkillNodeConfig
    {
        [SerializeReference]
        public List<SkillConditionConfig> BranchConditions = new();
    }
}
