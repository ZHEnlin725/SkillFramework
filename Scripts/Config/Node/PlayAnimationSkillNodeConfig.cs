using System;

namespace SkillFramework
{
    [Serializable]
    [SkillNodeAlias("Play Animation")]
    public sealed class PlayAnimationSkillNodeConfig : SkillNodeConfig
    {
        public string AnimationName;
        public bool Loop;
    }
}
