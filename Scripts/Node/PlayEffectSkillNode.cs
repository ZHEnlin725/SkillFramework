using UnityEngine;

namespace SkillFramework
{
    public sealed class PlayEffectSkillNode : SkillNode
    {
        private readonly string effectPath;
        private readonly Vector3 offsetDirection;
        private readonly float offsetDistance;
        private readonly Vector3 rotation;

        public PlayEffectSkillNode(PlayEffectSkillNodeConfig nodeConfig) :
            base(nodeConfig)
        {
            effectPath = nodeConfig.EffectPath;
            offsetDirection = nodeConfig.OffsetDirection;
            offsetDistance = nodeConfig.OffsetDistance;
            rotation = nodeConfig.Rotation;
        }

        protected override void OnTick(SkillContext context)
        {
            if (context.EffectPlayer == null)
            {
                return;
            }

            Vector3 position;
            if (context.Owner != null)
            {
                Vector3 localDirection = context.Owner.Rotation * offsetDirection.normalized;
                position = context.Owner.Position + localDirection * offsetDistance;
            }
            else
            {
                position = offsetDirection.normalized * offsetDistance;
            }

            Quaternion effectRotation = Quaternion.Euler(rotation);
            context.EffectPlayer.Play(
                context,
                effectPath,
                position,
                effectRotation);
        }
    }
}
