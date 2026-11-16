namespace SkillFramework
{
    public sealed class PlayAnimationSkillNode : SkillNode
    {
        private readonly string animationName;
        private readonly bool loop;

        public PlayAnimationSkillNode(PlayAnimationSkillNodeConfig nodeConfig) : base(nodeConfig)
        {
            animationName = nodeConfig.AnimationName;
            loop = nodeConfig.Loop;
        }

        protected override void OnEnter(SkillContext context)
        {
            context.AnimationPlayer?.Play(
                context,
                animationName,
                loop);
        }

        protected override void OnTick(SkillContext context)
        {
            
        }
    }
}