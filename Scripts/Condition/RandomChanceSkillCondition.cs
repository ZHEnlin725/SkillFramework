using UnityEngine;

namespace SkillFramework
{
    public sealed class RandomChanceSkillCondition : ISkillCondition
    {
        private readonly float probability;

        public RandomChanceSkillCondition(RandomChanceSkillConditionConfig config)
        {
            probability = config != null ? Mathf.Clamp01(config.Probability) : 0f;
        }

        public bool Evaluate(SkillContext context)
        {
            if (probability <= 0f)
            {
                return false;
            }

            if (probability >= 1f)
            {
                return true;
            }

            return Random.value <= probability;
        }
    }
}
