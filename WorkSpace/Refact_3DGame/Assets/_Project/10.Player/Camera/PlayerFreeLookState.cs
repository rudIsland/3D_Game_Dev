namespace Player
{
    // 자유 시점 이동을 활성화하고 Tab 입력으로 TargetLook 전이를 요청한다.
    internal sealed class PlayerFreeLookState : IPlayerState
    {
        private readonly IPlayerLookStateChange lookStateChange;
        private readonly PlayerActionStateMachine actionStateMachine;
        private readonly PlayerMovement playerMovement;
        private readonly PlayerTargetCamera targetCamera;

        /// <summary>시점 전환 요청과 자유 이동·카메라·행동 갱신을 연결한다.</summary>
        public PlayerFreeLookState(
            IPlayerLookStateChange lookStateChange,
            PlayerActionStateMachine actionStateMachine,
            PlayerMovement playerMovement,
            PlayerTargetCamera targetCamera)
        {
            this.lookStateChange = lookStateChange;
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

        /// <summary>현재 행동을 먼저 갱신한 뒤 같은 프레임의 락온 입력을 처리한다.</summary>
        public void Update(float deltaTime, PlayerStateInput input)
        {
            actionStateMachine.Update(deltaTime, input);
            if (input.TargetTogglePressed)
            {
                lookStateChange.TryChangeToTargetLookState();
            }
        }

        public void Exit()
        {
        }
    }
}
