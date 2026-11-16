using System.Collections.Generic;

namespace SkillFramework
{
    public sealed class SwitchSkillNode : CompositeSkillNode
    {
        private readonly List<ISkillCondition> branchConditions = new();
        private int selectedChildIndex = -1;

        public SwitchSkillNode(SwitchSkillNodeConfig nodeConfig) : base(nodeConfig)
        {
            if (nodeConfig?.BranchConditions != null)
            {
                for (int i = 0; i < nodeConfig.BranchConditions.Count; i++)
                {
                    branchConditions.Add(
                        SkillConditionFactory.CreateCondition(nodeConfig.BranchConditions[i]));
                }
            }
        }

        protected override void OnEnter(SkillContext context)
        {
            selectedChildIndex = SelectBranch(context);
        }

        private int SelectBranch(SkillContext context)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                ISkillCondition condition = i < branchConditions.Count
                    ? branchConditions[i]
                    : null;
                if (condition == null || condition.Evaluate(context))
                {
                    return i;
                }
            }

            return -1;
        }
        protected override void OnTick(SkillContext context)
        {
            if (selectedChildIndex < 0 || selectedChildIndex >= Children.Count)
            {
                return;
            }

            ISkillNode child = Children[selectedChildIndex];
            child?.Tick(context);

            if (child.IsFinished)
            {
                SetFinished();
            }
        }

        public override void Reset()
        {
            selectedChildIndex = -1;
            base.Reset();
        }
    }
}
