using System;
using UnityEngine;

namespace SkillFramework
{
    public enum SkillExpressionOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    [Serializable]
    public abstract class SkillDamageExpression
    {
        public abstract float Evaluate(SkillDamageFormulaContext context);
    }

    [Serializable]
    public sealed class SkillConstantExpression : SkillDamageExpression
    {
        public float Value;

        public override float Evaluate(SkillDamageFormulaContext context)
        {
            return Value;
        }
    }

    [Serializable]
    public sealed class SkillValueExpression : SkillDamageExpression
    {
        public SkillValueKey Key;

        public override float Evaluate(SkillDamageFormulaContext context)
        {
            return context.GetValue(Key);
        }
    }

    [Serializable]
    public sealed class SkillBinaryExpression : SkillDamageExpression
    {
        public SkillExpressionOperator Operator;

        [SerializeReference]
        public SkillDamageExpression Left;

        [SerializeReference]
        public SkillDamageExpression Right;

        public override float Evaluate(SkillDamageFormulaContext context)
        {
            float left = Left != null ? Left.Evaluate(context) : 0f;
            float right = Right != null ? Right.Evaluate(context) : 0f;

            return Operator switch
            {
                SkillExpressionOperator.Add => left + right,
                SkillExpressionOperator.Subtract => left - right,
                SkillExpressionOperator.Multiply => left * right,
                SkillExpressionOperator.Divide => Mathf.Approximately(right, 0f) ? 0f : left / right,
                _ => 0f
            };
        }
    }
}
