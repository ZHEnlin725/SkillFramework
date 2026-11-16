using System;
using System.Collections.Generic;
using SkillFramework;
using UnityEngine;

[Serializable]
public sealed class LogicSkillConditionConfig : SkillConditionConfig
{
    public SkillConditionLogicOperator Operator = SkillConditionLogicOperator.All;

    [SerializeReference]
    public List<SkillConditionConfig> Conditions = new();
}
