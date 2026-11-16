namespace SkillFramework
{
    public sealed class SkillDamageFormulaContext
    {
        public SkillContext Context { get; }
        public ISkillTarget Target { get; }

        private readonly ISkillValueProvider valueProvider;

        public SkillDamageFormulaContext(SkillContext context, ISkillTarget target, ISkillValueProvider valueProvider)
        {
            Context = context;
            Target = target;
            this.valueProvider = valueProvider;
        }

        public float GetValue(SkillValueKey key)
        {
            if (valueProvider != null &&
                valueProvider.TryGet(key, Context, Target, out float value))
            {
                return value;
            }

            return 0f;
        }
    }
}
