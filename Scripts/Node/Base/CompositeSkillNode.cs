using System.Collections.Generic;

namespace SkillFramework
{
    public class CompositeSkillNode : SkillNode
    {
        private readonly List<ISkillNode> children = new();

        public CompositeSkillNode(CompositeSkillNodeConfig nodeConfig) : base(nodeConfig)
        {
            if (nodeConfig?.Children == null)
            {
                return;
            }

            for (int i = 0; i < nodeConfig.Children.Count; i++)
            {
                ISkillNode child = SkillNodeFactory.CreateNode(nodeConfig.Children[i]);
                if (child != null)
                {
                    children.Add(child);
                }
            }
        }

        protected IReadOnlyList<ISkillNode> Children => children;

        public override void Reset()
        {
            base.Reset();

            for (int i = 0; i < children.Count; i++)
            {
                children[i].Reset();
            }
        }
    }
}
