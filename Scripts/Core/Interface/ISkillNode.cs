
namespace SkillFramework
{
    public interface ISkillNode
    {
        int NodeId { get; }
        bool IsEnabled { get; }
        bool IsFinished { get; }
        void Tick(SkillContext context);
        void Reset();
        bool CanStartNow(SkillContext context);
    }
}
