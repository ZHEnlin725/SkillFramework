using System.Collections.Generic;
using UnityEngine;

namespace SkillFramework
{
    public sealed class RangeDetectionSkillNode : SkillNode
    {
        private readonly ISkillRangeDetector detector;
        private readonly Vector3 offset;
        private readonly float radius;
        private readonly SkillTargetType targetType;
        private readonly SkillNodeResultKey<List<ISkillTarget>> resultKey;

        public RangeDetectionSkillNode(RangeDetectionSkillNodeConfig nodeConfig) : base(nodeConfig)
        {
            offset = nodeConfig.Offset;
            radius = nodeConfig.Radius;
            targetType = nodeConfig.TargetType;
            detector = SkillRangeDetectorFactory.CreateRangeDetector(nodeConfig.RangeDetectorConfig);
            resultKey = new SkillNodeResultKey<System.Collections.Generic.List<ISkillTarget>>(NodeId);
        }

        protected override void OnTick(SkillContext context)
        {
            if (detector == null)
            {
                return;
            }

            Vector3 center = context.Owner != null
                ? context.Owner.Position + offset
                : offset;
            SkillRangeRequest request = new SkillRangeRequest(
                center,
                radius,
                targetType);

            var results = detector.Detect(context, request);
            context.SetResult(resultKey, results);
        }
    }
}