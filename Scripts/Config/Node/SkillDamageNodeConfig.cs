using System;

namespace SkillFramework
{
    [Serializable]
    [SkillNodeAlias("Damage")]
    public sealed class SkillDamageNodeConfig : SkillNodeConfig
    {
        public int TargetSourceNodeId;
        public SkillDamageFormulaAsset Formula;
    }
}
