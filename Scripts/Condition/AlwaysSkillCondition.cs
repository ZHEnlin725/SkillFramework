namespace SkillFramework
{
    public sealed class AlwaysSkillCondition : ISkillCondition
    {
        private readonly bool result;

        public AlwaysSkillCondition(AlwaysSkillConditionConfig config)
        {
            result = config != null && config.Result;
        }

        public bool Evaluate(SkillContext context)
        {
            return result;
        }
    }
}
