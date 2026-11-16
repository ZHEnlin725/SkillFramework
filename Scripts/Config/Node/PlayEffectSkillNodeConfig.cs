using System;
using UnityEngine;

namespace SkillFramework
{
    [Serializable]
    [SkillNodeAlias("Play Effect")]
    public sealed class PlayEffectSkillNodeConfig : SkillNodeConfig
    {
        public string EffectPath;
        public Vector3 OffsetDirection;
        public float OffsetDistance;
        public Vector3 Rotation;
    }
}
