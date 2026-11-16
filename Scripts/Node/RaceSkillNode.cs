namespace SkillFramework
{
    public sealed class RaceSkillNode : CompositeSkillNode
    {
        public RaceSkillNode(RaceSkillNodeConfig nodeConfig) : base(nodeConfig)
        {
        }

        protected override void OnTick(SkillContext context)
        {
            bool anyChildFinished = false;
            for (int i = 0; i < Children.Count; i++)
            {
                ISkillNode child = Children[i];
                child?.Tick(context);
                if (child != null && child.IsFinished)
                {
                    anyChildFinished = true;
                }
            }

            if (anyChildFinished)
            {
                SetFinished();
            }
        }
    }
}
