using System.Collections.Generic;

namespace SkillFramework
{
    public sealed class LogicSkillCondition : ISkillCondition
    {
        private readonly SkillConditionLogicOperator logicOperator;
        private readonly List<ISkillCondition> conditions = new();

        public LogicSkillCondition(LogicSkillConditionConfig config)
        {
            logicOperator = config?.Operator ?? SkillConditionLogicOperator.All;
            if (config?.Conditions == null)
            {
                return;
            }

            for (int i = 0; i < config.Conditions.Count; i++)
            {
                ISkillCondition condition = SkillConditionFactory.CreateCondition(
                    config.Conditions[i]);
                if (condition != null)
                {
                    conditions.Add(condition);
                }
            }
        }

        public bool Evaluate(SkillContext context)
        {
            if (logicOperator == SkillConditionLogicOperator.All)
            {
                for (int i = 0; i < conditions.Count; i++)
                {
                    if (!conditions[i].Evaluate(context))
                    {
                        return false;
                    }
                }

                return true;
            }

            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i].Evaluate(context))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
