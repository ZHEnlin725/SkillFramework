using System.Collections.Generic;

namespace SkillFramework
{
    public sealed class SkillContext
    {
        public ISkillOwner Owner { get; }
        public int SkillId { get; }
        public int CurrentFrameId { get; set; }
        public ISkillAnimationPlayer AnimationPlayer { get; set; }
        public ISkillAudioPlayer AudioPlayer { get; set; }
        public ISkillEffectPlayer EffectPlayer { get; set; }
        public ISkillMovementController MovementController { get; set; }
        public ISkillValueProvider ValueProvider { get; set; }

        private readonly Dictionary<int, object> nodeResults = new();

        public void SetResult<T>(SkillNodeResultKey<T> key, T value)
        {
            nodeResults[key.Id] = value;
        }

        public bool HasResult(int nodeId)
        {
            return nodeResults.ContainsKey(nodeId);
        }

        public bool TryGetResult<T>(SkillNodeResultKey<T> key, out T value)
        {
            if (nodeResults.TryGetValue(key.Id, out object boxedValue) &&
                boxedValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        public T GetResult<T>(SkillNodeResultKey<T> key)
        {
            return TryGetResult(key, out T value) ? value : default;
        }

        public void ClearNodeResults()
        {
            nodeResults.Clear();
        }

        public SkillContext(ISkillOwner owner, int skillId)
        {
            Owner = owner;
            SkillId = skillId;
            if (owner is ISkillAnimationPlayer animationPlayer)
                AnimationPlayer = animationPlayer;
            if (owner is ISkillAudioPlayer audioPlayer)
                AudioPlayer = audioPlayer;
            if (owner is ISkillEffectPlayer effectPlayer)
                EffectPlayer = effectPlayer;
            if (owner is ISkillMovementController movementController)
                MovementController = movementController;
        }
    }
}
