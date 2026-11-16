using System;
using UnityEngine;

namespace SkillFramework
{
    [Serializable]
    [SkillNodeAlias("Movement")]
    public sealed class MovementSkillNodeConfig : SkillNodeConfig
    {
        public Vector3 OffsetDirection;
        public float OffsetDistance;
    }
}
