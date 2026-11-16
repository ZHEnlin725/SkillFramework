using System;
using UnityEngine;

[Serializable]
public sealed class NotSkillConditionConfig : SkillConditionConfig
{
    [SerializeReference]
    public SkillConditionConfig Condition;
}
