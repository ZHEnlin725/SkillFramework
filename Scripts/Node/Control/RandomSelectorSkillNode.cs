using UnityEngine;

namespace SkillFramework
{
    public sealed class RandomSelectorSkillNode : CompositeSkillNode
    {
        private int selectedChildIndex = -1;

        public RandomSelectorSkillNode(RandomSelectorSkillNodeConfig nodeConfig) : base(nodeConfig)
        {
        }

        protected override void OnEnter(SkillContext context)
        {
            selectedChildIndex = Children.Count > 0
                ? Random.Range(0, Children.Count)
                : -1;
        }

        protected override void OnTick(SkillContext context)
        {
            if (selectedChildIndex >= 0 && selectedChildIndex < Children.Count)
            {
                Children[selectedChildIndex]?.Tick(context);
            }
        }

        public override void Reset()
        {
            selectedChildIndex = -1;
            base.Reset();
        }
    }
}
