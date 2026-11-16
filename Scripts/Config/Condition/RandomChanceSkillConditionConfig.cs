using System;
using UnityEngine;

[Serializable]
public sealed class RandomChanceSkillConditionConfig : SkillConditionConfig
{
    [Range(0f, 1f)]
    public float Probability = 0.5f;
}
