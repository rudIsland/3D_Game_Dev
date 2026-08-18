using rudIsland.RPG3D.Player.Camera;
using rudIsland.RPG3D.Player.Movement;
using rudIsland.RPG3D.Player.States.Actions;
using UnityEngine;

namespace rudIsland.RPG3D.Player.States.Target
{
    // 선택한 적을 유지하며 Target 이동과 카메라를 갱신한다.
    internal sealed class PlayerTargetLookState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private readonly PlayerActionStateMachine actionStateMachine;
        private readonly PlayerMovement playerMovement;
        private readonly PlayerTargetFinder targetFinder;
        private readonly PlayerTargetCamera targetCamera;
        private readonly float targetBreakDistance;
        private readonly PlayerTargetVisibilityGrace visibilityGrace;

        private Transform currentTarget;

        public PlayerTargetLookState(
            PlayerStateMachine stateMachine,
            PlayerActionStateMachine actionStateMachine,
            PlayerMovement playerMovement,
            PlayerTargetFinder targetFinder,
            PlayerTargetCamera targetCamera,
            float targetBreakDistance,
            float targetHiddenGraceDuration)
        {
            this.stateMachine = stateMachine;
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

        public void ReleaseTarget()
        {
            currentTarget = null;
            visibilityGrace.Reset();
            playerMovement.SetFreeLookMovement();
            targetCamera.SetFreeLook();
        }

        public void Enter()
        {
            if (currentTarget == null)
            {
                stateMachine.ChangeToFreeLookState();
                return;
            }

            visibilityGrace.Reset();
            playerMovement.SetTargetMovement(currentTarget);
            targetCamera.SetTarget(currentTarget);
            actionStateMachine.Enable();
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            if (input.TargetTogglePressed || !IsTargetAvailable())
            {
                stateMachine.ChangeToFreeLookState();
                return;
            }

            if (!visibilityGrace.CanKeepTarget(
                    targetFinder.IsTargetVisible(currentTarget),
                    deltaTime))
            {
                stateMachine.ChangeToFreeLookState();
                return;
            }

            actionStateMachine.Update(deltaTime, input);
        }

        public void Exit()
        {
        }
    }
}
