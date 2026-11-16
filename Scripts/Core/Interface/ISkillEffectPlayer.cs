using UnityEngine;

namespace SkillFramework
{
    public interface ISkillEffectPlayer
    {
        void Play(SkillContext context, string effectPath, Vector3 position, Quaternion rotation);
    }
}
