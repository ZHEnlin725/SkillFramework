namespace SkillFramework
{
    public sealed class ConditionSkillNode : DecoratorSkillNode
    {
        private ISkillCondition condition;

        private readonly SkillConditionEvaluateMode evaluateMode;
        private bool evaluatedOnce;
        private bool evaluatePassed;

        public ConditionSkillNode(ConditionSkillNodeConfig nodeConfig) : base(nodeConfig)
        {
            evaluateMode = nodeConfig.EvaluateMode;
            condition = SkillConditionFactory.CreateCondition(nodeConfig.Condition);
        }

        protected override bool ShouldTickThisFrame(int diffFrameId)
        {
            return evaluateMode switch
            {
                SkillConditionEvaluateMode.EveryFrame => true,
                SkillConditionEvaluateMode.EvaluateOnceKeepChild =>
                    !evaluatedOnce || evaluatePassed,
                SkillConditionEvaluateMode.LatchAfterFirstSuccess => true,
                _ => base.ShouldTickThisFrame(diffFrameId)
            };
        }

        protected override void OnTick(SkillContext context)
        {
            if (evaluateMode == SkillConditionEvaluateMode.EvaluateOnceKeepChild)
            {
                if (!evaluatedOnce)
                {
                    evaluatedOnce = true;
                    evaluatePassed = condition != null &&
                                     condition.Evaluate(context);
                }
            }
            else if (evaluateMode == SkillConditionEvaluateMode.LatchAfterFirstSuccess)
            {
                if (!evaluatePassed)
                {
                    evaluatePassed = condition != null &&
                                     condition.Evaluate(context);
                }
            }
            else
            {
                evaluatePassed = condition != null &&
                                 condition.Evaluate(context);
            }

            if (!evaluatePassed)
            {
                return;
            }

            if (Child != null)
            {
                Child.Tick(context);
            }
        }

        public override void Reset()
        {
            base.Reset();
            evaluatedOnce = false;
            evaluatePassed = false;
        }
    }
}
