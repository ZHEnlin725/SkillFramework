namespace SkillFramework
{
    public abstract class DecoratorSkillNode : SkillNode
    {
        protected ISkillNode Child { get; }

        protected DecoratorSkillNode(DecoratorSkillNodeConfig nodeConfig) : base(nodeConfig)
        {
            Child = SkillNodeFactory.CreateNode(nodeConfig?.ChildNode);
        }

        public override void Reset()
        {
            base.Reset();
            Child?.Reset();
        }
    }
}
