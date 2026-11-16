namespace SkillFramework
{
    public interface ISkillTarget
    {
        void OnDamage(SkillContext context, int damage);

        void OnHeal(SkillContext context, int heal);
    }
}