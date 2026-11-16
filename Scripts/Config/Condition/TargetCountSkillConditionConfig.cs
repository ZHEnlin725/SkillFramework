using System;
using SkillFramework;

[Serializable]
public sealed class TargetCountSkillConditionConfig : SkillConditionConfig
{
    public int TargetSourceNodeId;
    public SkillConditionCompareOperator Operator =
        SkillConditionCompareOperator.GreaterOrEqual;
    public int Count = 1;
}
