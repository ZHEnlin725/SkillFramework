namespace SkillFramework
{
    public sealed class SelectorSkillNode : CompositeSkillNode
    {
        private int selectedChildIndex = -1;

        public SelectorSkillNode(SelectorSkillNodeConfig nodeConfig) : base(nodeConfig)
        {
        }

        protected override void OnEnter(SkillContext context)
        {
            selectedChildIndex = -1;
        }

        protected override void OnTick(SkillContext context)
        {
            if (selectedChildIndex >= 0)
            {
                if (TickSelectedChild(context))
                {
                    return;
                }
            }

            selectedChildIndex = GetFirstReadyChildIndex(context);
            if (selectedChildIndex >= 0)
            {
                TickSelectedChild(context);
            }
        }

        private int GetFirstReadyChildIndex(SkillContext context)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                ISkillNode child = Children[i];
                if (child != null && child.IsEnabled && !child.IsFinished && child.CanStartNow(context))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool TickSelectedChild(SkillContext context)
        {
            ISkillNode child = Children[selectedChildIndex];
            child?.Tick(context);

            if (child == null || child.IsFinished)
            {
                SetFinished();
                return true;
            }

            return true;
        }

        public override void Reset()
        {
            selectedChildIndex = -1;
            base.Reset();
        }
    }
}
