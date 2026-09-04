using Characters.Player.Camera;
using Characters.Player.Movement;
using Characters.Player.StateMachine.Actions;

namespace Characters.Player.StateMachine.States.FreeLook
{
    // 자유 시점 이동을 활성화하고 Tab 입력으로 TargetLook 전이를 요청한다.
    internal sealed class PlayerFreeLookState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private readonly PlayerActionStateMachine actionStateMachine;
        private readonly PlayerMovement playerMovement;
        private readonly PlayerTargetCamera targetCamera;

        public PlayerFreeLookState(
            PlayerStateMachine stateMachine,
            PlayerActionStateMachine actionStateMachine,
            PlayerMovement playerMovement,
            PlayerTargetCamera targetCamera)
        {
            this.stateMachine = stateMachine;
            this.actionStateMachine = actionStateMachine;
            this.playerMovement = playerMovement;
            this.targetCamera = targetCamera;
        }

        public void Enter()
        {
            playerMovement.SetFreeLookMovement();
            targetCamera.SetFreeLook();
            actionStateMachine.Enable();
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            actionStateMachine.Update(deltaTime, input);
            if (input.TargetTogglePressed)
            {
                stateMachine.TryChangeToTargetLookState();
            }
        }

        public void Exit()
        {
        }
    }
}
