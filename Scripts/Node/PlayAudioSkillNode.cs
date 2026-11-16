namespace SkillFramework
{
    public sealed class PlayAudioSkillNode : SkillNode
    {
        private readonly int soundId;

        public PlayAudioSkillNode(PlayAudioSkillNodeConfig nodeConfig) :
            base(nodeConfig)
        {
            soundId = nodeConfig.SoundId;
        }

        protected override void OnTick(SkillContext context)
        {
            context.AudioPlayer?.Play(context, soundId);
        }
    }
}