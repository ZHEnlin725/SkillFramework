using UnityEngine;

namespace SkillFramework
{
    public interface ISkillMovementController
    {
        void Move(SkillContext context, Vector3 displacement);
    }
}
