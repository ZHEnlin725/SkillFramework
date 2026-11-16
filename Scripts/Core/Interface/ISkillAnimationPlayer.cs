namespace SkillFramework
{
    public interface ISkillAnimationPlayer
    {
        void Play(SkillContext context, string animationName, bool loop);
    }
}
