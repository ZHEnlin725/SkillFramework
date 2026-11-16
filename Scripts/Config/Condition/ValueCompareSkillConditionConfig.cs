using System;
using SkillFramework;

[Serializable]
public sealed class ValueCompareSkillConditionConfig : SkillConditionConfig
{
    public SkillValueKey LeftKey = SkillValueKey.CasterAttack;
    public SkillValueKey RightKey = SkillValueKey.TargetDefense;
    public SkillConditionCompareOperator Operator =
        SkillConditionCompareOperator.GreaterOrEqual;
}
