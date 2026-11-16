using System.Collections.Generic;
using SkillFramework;

namespace SkillFramework
{
    public sealed class TargetCountSkillCondition : ISkillCondition
    {
        private readonly SkillNodeResultKey<List<ISkillTarget>> targetResultKey;
        private readonly SkillConditionCompareOperator compareOperator;
        private readonly int expectedCount;

        public TargetCountSkillCondition(TargetCountSkillConditionConfig config)
        {
            targetResultKey = new SkillNodeResultKey<List<ISkillTarget>>(
                config?.TargetSourceNodeId ?? 0);
            compareOperator = config?.Operator ?? SkillConditionCompareOperator.Equal;
            expectedCount = config?.Count ?? 0;
        }

        public bool Evaluate(SkillContext context)
        {
            if (context == null ||
                !context.TryGetResult(targetResultKey, out List<ISkillTarget> targets))
            {
                return false;
            }

            return Compare(targets?.Count ?? 0);
        }

        private bool Compare(int count)
        {
            return SkillConditionComparer.Compare(count, compareOperator, expectedCount);
        }
    }
}
