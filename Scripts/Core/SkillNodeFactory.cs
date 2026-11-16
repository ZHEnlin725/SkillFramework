using System.Collections.Generic;

namespace SkillFramework
{
    public static class SkillNodeFactory
    {
        public static List<ISkillNode> CreateNodes(SkillConfig config)
        {
            List<ISkillNode> nodes = new List<ISkillNode>();
            if (config == null || config.Nodes == null)
            {
                return nodes;
            }

            for (int i = 0; i < config.Nodes.Count; i++)
            {
                ISkillNode node = CreateNode(config.Nodes[i]);
                if (node != null)
                {
                    nodes.Add(node);
                }
            }

            return nodes;
        }

        public static ISkillNode CreateNode(SkillNodeConfig config)
        {
            if (config == null)
            {
                return null;
            }

            return config switch
            {
                ConditionSkillNodeConfig conditionConfig => new ConditionSkillNode(conditionConfig),
                SequenceSkillNodeConfig sequenceConfig => new SequenceSkillNode(sequenceConfig),
                ParallelSkillNodeConfig parallelConfig => new ParallelSkillNode(parallelConfig),
                RandomSelectorSkillNodeConfig randomSelectorConfig =>
                    new RandomSelectorSkillNode(randomSelectorConfig),
                SelectorSkillNodeConfig selectorConfig => new SelectorSkillNode(selectorConfig),
                RaceSkillNodeConfig raceConfig => new RaceSkillNode(raceConfig),
                SwitchSkillNodeConfig switchConfig => new SwitchSkillNode(switchConfig),
                SkillDamageNodeConfig damageConfig => new SkillDamageNode(damageConfig),
                RangeDetectionSkillNodeConfig rangeDetectionConfig => new RangeDetectionSkillNode(rangeDetectionConfig),
                MovementSkillNodeConfig movementConfig => new MovementSkillNode(movementConfig),
                PlayAnimationSkillNodeConfig playAnimationConfig => new PlayAnimationSkillNode(playAnimationConfig),
                PlayAudioSkillNodeConfig playAudioConfig => new PlayAudioSkillNode(playAudioConfig),
                PlayEffectSkillNodeConfig playEffectConfig => new PlayEffectSkillNode(playEffectConfig),
                CompositeSkillNodeConfig compositeConfig => new CompositeSkillNode(compositeConfig),
                _ => null
            };
        }
    }
}
