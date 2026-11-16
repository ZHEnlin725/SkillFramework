using UnityEngine;

namespace SkillFramework
{
    [CreateAssetMenu(
        fileName = "SkillDamageFormula",
        menuName = "SkillFramework/Damage/Skill Damage Formula",
        order = 0)]
    public sealed class SkillDamageFormulaAsset : ScriptableObject, ISkillDamageFormula
    {
        [SerializeReference]
        private SkillDamageExpression rootExpression;

        public SkillDamageExpression RootExpression
        {
            get => rootExpression;
            set => rootExpression = value;
        }

        public float Calculate(SkillDamageFormulaContext context)
        {
            return rootExpression != null ? rootExpression.Evaluate(context) : 0f;
        }
    }
}
