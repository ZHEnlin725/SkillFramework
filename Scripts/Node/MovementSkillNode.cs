using UnityEngine;

namespace SkillFramework
{
    public sealed class MovementSkillNode : SkillNode
    {
        private readonly Vector3 offsetDirection;
        private readonly float offsetDistance;

        public MovementSkillNode(MovementSkillNodeConfig nodeConfig) : base(nodeConfig)
        {
            offsetDirection = nodeConfig.OffsetDirection;
            offsetDistance = nodeConfig.OffsetDistance;
        }

        protected override void OnTick(SkillContext context)
        {
            Vector3 totalDisplacement;
            if (context.Owner != null)
            {
                Vector3 localDirection = context.Owner.Rotation * offsetDirection.normalized;
                totalDisplacement = localDirection * offsetDistance;
            }
            else
            {
                totalDisplacement = offsetDirection.normalized * offsetDistance;
            }

            Vector3 displacementPerFrame = totalDisplacement / durationFrames;

            context.MovementController?.Move(
                context,
                displacementPerFrame);
        }
    }
}
