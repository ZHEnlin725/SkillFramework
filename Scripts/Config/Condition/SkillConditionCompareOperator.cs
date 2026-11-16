namespace SkillFramework
{
    public enum SkillConditionCompareOperator
    {
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual,
        Equal,
        NotEqual
    }

    public static class SkillConditionComparer
    {
        public static bool Compare(
            float left,
            SkillConditionCompareOperator compareOperator,
            float right)
        {
            return compareOperator switch
            {
                SkillConditionCompareOperator.Less => left < right,
                SkillConditionCompareOperator.LessOrEqual => left <= right,
                SkillConditionCompareOperator.Greater => left > right,
                SkillConditionCompareOperator.GreaterOrEqual => left >= right,
                SkillConditionCompareOperator.Equal => left.Equals(right),
                SkillConditionCompareOperator.NotEqual => !left.Equals(right),
                _ => false
            };
        }
    }
}
