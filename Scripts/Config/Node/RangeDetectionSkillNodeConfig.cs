using System;
using UnityEngine;

namespace SkillFramework
{
    [Serializable]
    [SkillNodeAlias("Range Detection")]
    public sealed class RangeDetectionSkillNodeConfig : SkillNodeConfig
    {
        public Vector3 Offset;
        public float Radius;
        public SkillTargetType TargetType = SkillTargetType.Enemy;
        
        [SerializeReference]
        public SkillRangeDetectorConfig RangeDetectorConfig;
    }
}
