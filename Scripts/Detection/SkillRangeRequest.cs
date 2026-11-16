using UnityEngine;

namespace SkillFramework
{
    public sealed class SkillRangeRequest
    {
        public Vector3 Center { get; }
        public float Radius { get; }
        public SkillTargetType TargetType { get; }

        public SkillRangeRequest(Vector3 center, float radius, SkillTargetType targetType)
        {
            Center = center;
            Radius = radius;
            TargetType = targetType;
        }
    }
}
