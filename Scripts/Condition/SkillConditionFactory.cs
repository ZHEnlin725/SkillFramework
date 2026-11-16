namespace SkillFramework
{
    public static class SkillConditionFactory
    {
        public static ISkillCondition CreateCondition(SkillConditionConfig config)
        {
            return config switch
            {
                AlwaysSkillConditionConfig alwaysConfig => new AlwaysSkillCondition(alwaysConfig),
                RandomChanceSkillConditionConfig randomChanceConfig =>
                    new RandomChanceSkillCondition(randomChanceConfig),
                NotSkillConditionConfig notConfig => new NotSkillCondition(notConfig),
                LogicSkillConditionConfig logicConfig => new LogicSkillCondition(logicConfig),
                ValueCompareSkillConditionConfig valueCompareConfig =>
                    new ValueCompareSkillCondition(valueCompareConfig),
                TargetCountSkillConditionConfig targetCountConfig =>
                    new TargetCountSkillCondition(targetCountConfig),
                _ => null,
            };
        }
    }
}
