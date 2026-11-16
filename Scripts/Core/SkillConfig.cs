using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillFramework
{
    public enum SkillNodeType
    {
        Condition,
        DamageJudge,
        RangeDetection,
        Movement,
        PlayAnimation,
        PlayAudio,
        PlayEffect
    }

    [Serializable]
    public abstract class SkillNodeConfig
    {
        public int NodeId;
        public List<int> DependsOnNodeIds = new();
        public int StartFrameId;
        public int IntervalFrames;
        [Min(1)]
        public int DurationFrames = 1;

        public int EndFrameId => StartFrameId + DurationFrames;

        public bool Enabled = true;
    }

    [CreateAssetMenu(
        fileName = "SkillConfig",
        menuName = "SkillFramework/Skill Config",
        order = 0)]
    public sealed class SkillConfig : ScriptableObject
    {
        public int SkillId;
        [Min(1)]
        public int DurationFrames = 1;
        public int CooldownFrames;
        [SerializeReference]
        public List<SkillNodeConfig> Nodes = new List<SkillNodeConfig>();

        public Skill CreateSkill(ISkillOwner owner)
        {
            return new Skill(this, new SkillContext(owner, SkillId));
        }
    }
}
