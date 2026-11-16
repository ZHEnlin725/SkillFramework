namespace SkillFramework
{
    public sealed class ParallelSkillNode : CompositeSkillNode
    {
        public ParallelSkillNode(ParallelSkillNodeConfig nodeConfig) : base(nodeConfig)
        {
        }

        protected override void OnTick(SkillContext context)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].Tick(context);
            }
        }
    }
}
