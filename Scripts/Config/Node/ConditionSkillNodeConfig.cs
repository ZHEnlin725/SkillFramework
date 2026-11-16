using System;
using UnityEngine;

namespace SkillFramework
{
    [Serializable]
    [SkillNodeAlias("Condition")]
    public sealed class ConditionSkillNodeConfig : DecoratorSkillNodeConfig
    {
        [SerializeReference]
        public SkillConditionConfig Condition;

        public SkillConditionEvaluateMode EvaluateMode = SkillConditionEvaluateMode.EveryFrame;
    }
}
