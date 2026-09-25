using UnityEngine;

namespace Player
{
    // 선택한 적을 유지하며 Target 이동과 카메라를 갱신한다.
    internal sealed class PlayerTargetLookState : IPlayerState
    {
        private readonly IPlayerLookStateChange lookStateChange;
        private readonly PlayerActionStateMachine actionStateMachine;
        private readonly PlayerMovement playerMovement;
        private readonly PlayerTargetFinder targetFinder;
        private readonly PlayerTargetCamera targetCamera;
        private readonly float targetBreakDistance;
        private readonly PlayerTargetVisibilityGrace visibilityGrace;

        private Transform currentTarget;

        /// <summary>시점 전환 요청과 대상 탐색·이동·카메라·행동 갱신을 연결한다.</summary>
        public PlayerTargetLookState(
            IPlayerLookStateChange lookStateChange,
            PlayerActionStateMachine actionStateMachine,
            PlayerMovement playerMovement,
            PlayerTargetFinder targetFinder,
            PlayerTargetCamera targetCamera,
            float targetBreakDistance,
            float targetHiddenGraceDuration)
        {
            this.lookStateChange = lookStateChange;
            this.actionStateMachine = actionStateMachine;
            this.playerMovement = playerMovement;
            this.targetFinder = targetFinder;
            this.targetCamera = targetCamera;
            this.targetBreakDistance = Mathf.Max(0f, targetBreakDistance);
            visibilityGrace = new PlayerTargetVisibilityGrace(
                targetHiddenGraceDuration);
        }

        public bool TrySelectTarget()
        {
            bool hasTarget = targetFinder.TryFindTarget(out currentTarget);
            visibilityGrace.Reset();
            return hasTarget;
        }

        public bool IsTargetAvailable()
        {
            return targetFinder.IsTargetAliveAndInRange(
                currentTarget,
                targetBreakDistance);
        }

        internal bool TryGetCurrentTarget(out Transform target)
        {
            target = currentTarget;
            return target != null && IsTargetAvailable();
        }

        internal bool IsCurrentTargetAvailable(Transform target)
        {
            return target != null &&
                ReferenceEquals(currentTarget, target) &&
                IsTargetAvailable();
        }

        public void ReleaseTarget()
        {
            currentTarget = null;
            visibilityGrace.Reset();
            playerMovement.SetFreeLookMovement();
            targetCamera.SetFreeLook();
        }

        /// <summary>대상이 있으면 락온 이동과 카메라를 적용하고 행동을 활성화한다.</summary>
        public void Enter()
        {
            if (currentTarget == null)
            {
                lookStateChange.ChangeToFreeLookState();
                return;
            }

            visibilityGrace.Reset();
            playerMovement.SetTargetMovement(currentTarget);
            targetCamera.SetTarget(currentTarget);
            actionStateMachine.Enable();
        }

        /// <summary>해제·대상 상실을 먼저 처리하며 락온을 유지한 프레임에만 행동을 갱신한다.</summary>
        public void Update(float deltaTime, PlayerStateInput input)
        {
            if (input.TargetTogglePressed || !IsTargetAvailable())
            {
                lookStateChange.ChangeToFreeLookState();
                return;
            }

            if (!visibilityGrace.CanKeepTarget(
                    targetFinder.IsTargetVisible(currentTarget),
                    deltaTime))
            {
                lookStateChange.ChangeToFreeLookState();
                return;
            }

            actionStateMachine.Update(deltaTime, input);
        }

        public void Exit()
        {
        }
    }
}
