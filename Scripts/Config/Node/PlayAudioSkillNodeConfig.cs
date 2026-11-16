using System;

namespace SkillFramework
{
    [Serializable]
    [SkillNodeAlias("Play Audio")]
    public sealed class PlayAudioSkillNodeConfig : SkillNodeConfig
    {
        public int SoundId;
    }
}
