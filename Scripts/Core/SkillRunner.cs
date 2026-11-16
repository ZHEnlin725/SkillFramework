using System.Collections.Generic;
using UnityEngine;

namespace SkillFramework
{
    public sealed class SkillRunner : MonoBehaviour
    {
        private readonly List<Skill> activeSkills = new List<Skill>();
        private readonly List<Skill> cooldownSkills = new List<Skill>();

        public Skill Play(SkillConfig config, ISkillOwner owner)
        {
            if (config == null)
            {
                return null;
            }

            Skill skill = config.CreateSkill(owner);
            skill.Play();
            activeSkills.Add(skill);
            if (skill.IsCoolingDown)
            {
                cooldownSkills.Add(skill);
            }
            return skill;
        }

        private void Update()
        {
            UpdateActiveSkills();
            UpdateCooldownSkills();
        }

        private void UpdateActiveSkills()
        {
            for (int i = activeSkills.Count - 1; i >= 0; i--)
            {
                activeSkills[i].Tick();
            }
        }

        private void UpdateCooldownSkills()
        {
            for (int i = cooldownSkills.Count - 1; i >= 0; i--)
            {
                cooldownSkills[i].UpdateCooldown();

                if (!cooldownSkills[i].IsCoolingDown)
                {
                    cooldownSkills.RemoveAt(i);
                }
            }
        }
    }
}
