using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillFramework
{
    public sealed class SkillDamageNode : SkillNode
    {
        private readonly int targetSourceNodeId;
        private readonly SkillDamageFormulaAsset formula;
        private readonly SkillNodeResultKey<List<ISkillTarget>> targetResultKey;

        public SkillDamageNode(SkillDamageNodeConfig nodeConfig) : base(nodeConfig)
        {
            targetSourceNodeId = nodeConfig.TargetSourceNodeId;
            formula = nodeConfig.Formula;
            targetResultKey = new SkillNodeResultKey<List<ISkillTarget>>(targetSourceNodeId);
            dependsOnNodeIds.Add(targetSourceNodeId);
        }

        protected override void OnTick(SkillContext context)
        {
            if (formula == null ||
                !context.TryGetResult(targetResultKey, out List<ISkillTarget> targets) ||
                targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                ISkillTarget target = targets[i];
                if (target == null)
                {
                    continue;
                }

                SkillDamageFormulaContext formulaContext = new SkillDamageFormulaContext(
                    context,
                    target,
                    context.ValueProvider);
                float damage = formula.Calculate(formulaContext);
                ApplyDamage(context, target, damage);
            }
        }

        private static void ApplyDamage(SkillContext context, ISkillTarget target, float damage)
        {
            target.OnDamage(context, (int)Math.Floor(damage));
        }
    }
}