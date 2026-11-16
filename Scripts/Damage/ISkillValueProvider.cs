namespace SkillFramework
{
    public interface ISkillValueProvider
    {
        bool TryGet(SkillValueKey key, SkillContext context, ISkillTarget target, out float value);
    }
}
