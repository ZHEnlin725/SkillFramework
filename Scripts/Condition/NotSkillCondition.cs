namespace SkillFramework
{
    public sealed class NotSkillCondition : ISkillCondition
    {
        private readonly ISkillCondition condition;

        public NotSkillCondition(NotSkillConditionConfig config)
        {
            condition = SkillConditionFactory.CreateCondition(config?.Condition);
        }

        public bool Evaluate(SkillContext context)
        {
            return condition == null || !condition.Evaluate(context);
        }
    }
}
