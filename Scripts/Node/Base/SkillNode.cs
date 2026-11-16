using System.Collections.Generic;
using UnityEngine;

namespace SkillFramework
{
    public abstract class SkillNode : ISkillNode
    {
        private NodeState state = NodeState.Waiting;

        protected bool enabled;
        protected int startFrameId;
        protected int intervalFrames;
        protected int durationFrames;
        protected bool exitCalled;
        protected readonly List<int> dependsOnNodeIds;

        public int NodeId { get; }
        public bool IsEnabled => enabled;
        public bool IsFinished => state == NodeState.Finished;

        protected SkillNode(SkillNodeConfig nodeConfig)
        {
            NodeId = nodeConfig.NodeId;
            this.enabled = nodeConfig.Enabled;
            this.startFrameId = nodeConfig.StartFrameId;
            this.intervalFrames = nodeConfig.IntervalFrames;
            this.durationFrames = nodeConfig.DurationFrames >= 1
                ? nodeConfig.DurationFrames
                : 1;
            dependsOnNodeIds = nodeConfig.DependsOnNodeIds;
        }

        public void Tick(SkillContext context)
        {
            if (!enabled || state == NodeState.Finished)
            {
                return;
            }

            int currentFrameId = context.CurrentFrameId;

            if (state == NodeState.Waiting)
            {
                if (currentFrameId < startFrameId || !AreDependenciesSatisfied(context))
                {
                    return;
                }

                state = NodeState.Running;
                OnEnter(context);
            }

            if (state == NodeState.Running)
            {
                var diffFrameId = currentFrameId - startFrameId;

                if (ShouldTickThisFrame(diffFrameId))
                {
                    OnTick(context);
                }

                if (state == NodeState.Running &&
                    diffFrameId >= durationFrames)
                {
                    state = NodeState.Finished;
                }
            }

            if (state == NodeState.Finished && !exitCalled)
            {
                exitCalled = true;
                OnExit(context);
            }

        }

        protected void SetFinished()
        {
            state = NodeState.Finished;
        }

        public bool CanStartNow(SkillContext context)
        {
            return state == NodeState.Waiting &&
                   context != null &&
                   context.CurrentFrameId >= startFrameId &&
                   AreDependenciesSatisfied(context);
        }

        public virtual void Reset()
        {
            state = NodeState.Waiting;
            exitCalled = false;
        }

        private bool AreDependenciesSatisfied(SkillContext context)
        {
            if (dependsOnNodeIds == null || dependsOnNodeIds.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < dependsOnNodeIds.Count; i++)
            {
                if (!context.HasResult(dependsOnNodeIds[i]))
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual bool ShouldTickThisFrame(int diffFrameId)
        {
            return intervalFrames <= 0 ||
                   diffFrameId % (intervalFrames + 1) == 0;
        }

        protected virtual void OnEnter(SkillContext context)
        {
        }

        protected virtual void OnTick(SkillContext context)
        {
        }

        protected virtual void OnExit(SkillContext context)
        {
        }

        private enum NodeState
        {
            Waiting,
            Running,
            Finished
        }
    }
}
