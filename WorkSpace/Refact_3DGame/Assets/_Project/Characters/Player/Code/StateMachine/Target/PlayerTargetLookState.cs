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

        private Transform currentTarget;

        public PlayerTargetLookState(
            PlayerStateMachine stateMachine,
            PlayerActionStateMachine actionStateMachine,
            PlayerMovement playerMovement,
            PlayerTargetFinder targetFinder,
            PlayerTargetCamera targetCamera,
            float targetBreakDistance)
        {
            this.stateMachine = stateMachine;
            this.actionStateMachine = actionStateMachine;
            this.playerMovement = playerMovement;
            this.targetFinder = targetFinder;
            this.targetCamera = targetCamera;
            this.targetBreakDistance = Mathf.Max(0f, targetBreakDistance);
        }

        public bool TrySelectTarget()
        {
            return targetFinder.TryFindTarget(out currentTarget);
        }

        public bool IsTargetAvailable()
        {
            return targetFinder.IsTargetAvailable(
                currentTarget,
                targetBreakDistance);
        }

        public void ReleaseTarget()
        {
            currentTarget = null;
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

            playerMovement.SetTargetMovement(currentTarget);
            targetCamera.SetTarget(currentTarget);
            actionStateMachine.Enable();
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            targetCamera.Update(deltaTime);
            actionStateMachine.Update(deltaTime, input);

            if (input.TargetTogglePressed || !IsTargetAvailable())
            {
                stateMachine.ChangeToFreeLookState();
            }
        }

        public void Exit()
        {
        }
    }
}
