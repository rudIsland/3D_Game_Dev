using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Player.Input;
using rudIsland.RPG3D.Player.States;

namespace rudIsland.RPG3D.Player
{
    // 플레이어 입력과 상태머신을 WorldObject 생명주기로 실행한다.
    public sealed class PlayerWorldUnit : PlayerUnit
    {
        private readonly PlayerInputReader playerInput;
        private readonly PlayerStateMachine playerStateMachine;

        public bool IsBlocking => playerStateMachine.IsBlocking;

        public PlayerWorldUnit(
            float maxHealth,
            PlayerInputReader playerInput,
            PlayerStateMachine playerStateMachine)
            : base(maxHealth)
        {
            this.playerInput = playerInput;
            this.playerStateMachine = playerStateMachine;
        }

        protected override void OnUnitCreate()
        {
            playerInput.Create();
        }

        protected override void OnUnitEnable()
        {
            playerInput.Enable();
            playerStateMachine.Enable();
        }

        // 입력을 한 번 소비하고 현재 이동 상태를 갱신한다.
        protected override void OnUnitTick(float deltaTime)
        {
            bool rollPressed = playerInput.TakeRollInput();
            bool attackPressed = playerInput.TakeAttackInput();
            bool blockImpactTestPressed =
                playerInput.TakeBlockImpactTestInput();

            playerStateMachine.Update(
                deltaTime,
                rollPressed,
                attackPressed);

            if (blockImpactTestPressed)
            {
                PlayBlockImpact();
            }
        }

        protected override void OnUnitDisable()
        {
            playerStateMachine.Disable();
            playerInput.Disable();
        }

        protected override void OnUnitDispose()
        {
            playerInput.Destroy();
        }

        public void PlayBlockImpact()
        {
            playerStateMachine.PlayBlockImpact();
        }
    }
}
