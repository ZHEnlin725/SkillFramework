using SkillFramework;

namespace SkillFramework
{
    public sealed class ValueCompareSkillCondition : ISkillCondition
    {
        private readonly SkillValueKey leftKey;
        private readonly SkillValueKey rightKey;
        private readonly SkillConditionCompareOperator compareOperator;

        public ValueCompareSkillCondition(ValueCompareSkillConditionConfig config)
        {
            leftKey = config?.LeftKey ?? SkillValueKey.None;
            rightKey = config?.RightKey ?? SkillValueKey.None;
            compareOperator = config?.Operator ?? SkillConditionCompareOperator.Equal;
        }

        public bool Evaluate(SkillContext context)
        {
            if (context?.ValueProvider == null ||
                !context.ValueProvider.TryGet(leftKey, context, null, out float left) ||
                !context.ValueProvider.TryGet(rightKey, context, null, out float right))
            {
                return false;
            }

            return SkillConditionComparer.Compare(left, compareOperator, right);
        }
    }
}
