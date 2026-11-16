namespace SkillFramework
{
    public sealed class SequenceSkillNode : CompositeSkillNode
    {
        private int currentChildIndex;

        public SequenceSkillNode(SequenceSkillNodeConfig nodeConfig) : base(nodeConfig)
        {
        }

        protected override void OnEnter(SkillContext context)
        {
            currentChildIndex = 0;
        }

        protected override void OnTick(SkillContext context)
        {
            if (currentChildIndex >= Children.Count)
            {
                SetFinished();
                return;
            }

            ISkillNode currentChild = Children[currentChildIndex];
            currentChild.Tick(context);

            if (currentChild.IsFinished)
            {
                currentChildIndex++;
                if (currentChildIndex >= Children.Count)
                {
                    SetFinished();
                }
            }
        }

        public override void Reset()
        {
            currentChildIndex = 0;
            base.Reset();
        }
    }
}
