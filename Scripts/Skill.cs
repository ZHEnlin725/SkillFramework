using System.Collections.Generic;

namespace SkillFramework
{
    public sealed class Skill
    {
        public SkillConfig Config { get; }
        public SkillContext Context { get; }
        public int CurrentFrameId { get; private set; }
        public bool IsPlaying { get; private set; }
        public int CooldownFrames { get; }
        public int RemainingCooldownFrames { get; private set; }
        public bool IsCoolingDown => RemainingCooldownFrames > 0;

        private readonly List<ISkillNode> nodes;
        private readonly int durationFrames;

        public Skill(SkillConfig config, SkillContext context)
        {
            Config = config;
            Context = context;
            CooldownFrames = config != null ? config.CooldownFrames : 0;
            durationFrames = config != null && config.DurationFrames >= 1
                ? config.DurationFrames
                : 1;
            nodes = SkillNodeFactory.CreateNodes(config);
        }

        public bool CanUse()
        {
            return !IsPlaying && !IsCoolingDown;
        }

        public bool Play()
        {
            if (!CanUse())
            {
                return false;
            }

            Reset();
            CurrentFrameId = 0;
            Context.CurrentFrameId = 0;
            Context.ClearNodeResults();
            IsPlaying = true;
            RemainingCooldownFrames = CooldownFrames;
            return true;
        }

        public void Stop()
        {
            IsPlaying = false;
        }

        public void Reset()
        {
            IsPlaying = false;
            CurrentFrameId = 0;
            Context.CurrentFrameId = 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Reset();
            }
        }

        public void Tick()
        {
            if (IsPlaying)
            {
                Context.CurrentFrameId = CurrentFrameId++;

                var allFinished = true;
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Tick(Context);
                    if (!nodes[i].IsFinished) 
                        allFinished = false;
                }

                if (allFinished || CurrentFrameId >= durationFrames)
                {
                    Stop();
                }
            }
        }

        public void UpdateCooldown()
        {
            if (!IsCoolingDown)
            {
                return;
            }

            RemainingCooldownFrames--;
            if (RemainingCooldownFrames <= 0)
            {
                Reset();
            }
        }
    }
}
