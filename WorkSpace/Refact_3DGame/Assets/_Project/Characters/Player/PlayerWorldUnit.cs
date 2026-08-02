using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Combat;
using rudIsland.RPG3D.Player.Input;
using rudIsland.RPG3D.Player.States;

namespace rudIsland.RPG3D.Player
{
    // 플레이어 체력과 입력 상태를 Unit 생명주기로 실행한다.
    public sealed class PlayerWorldUnit : Unit
    {
        private readonly PlayerInputReader playerInput;
        private readonly PlayerStateMachine playerStateMachine;

        public float CurrentHealth => Health.CurrentHealth;

        public PlayerWorldUnit(
            float maxHealth,
            PlayerInputReader playerInput,
            PlayerStateMachine playerStateMachine)
            : base(UnitTeam.Player, maxHealth)
        {
            this.playerInput = playerInput;
            this.playerStateMachine = playerStateMachine;
        }

        public void TakeDamage(float damage)
        {
            float healthBeforeDamage = Health.CurrentHealth;
            Health.TakeDamage(damage);

            if (Health.CurrentHealth >= healthBeforeDamage || IsDead)
            {
                return;
            }

            playerStateMachine.ChangeToHitState();
        }

        internal void ApplyHit(in AttackHitData hit)
        {
            if (hit.AttackerTeam != UnitTeam.Enemy)
            {
                return;
            }

            TakeDamage(hit.Damage.HealthDamage);
        }

        protected override void OnUnitCreate()
        {
            playerInput.Create();
            Health.Died += HandleHealthDied;
        }

        protected override void OnUnitEnable()
        {
            if (IsDead)
            {
                playerStateMachine.Enable();
                playerStateMachine.ChangeToDeadState();
                return;
            }

            playerInput.Enable();
            playerStateMachine.Enable();
        }

        protected override void OnUnitTick(float deltaTime)
        {
            if (IsDead)
            {
                playerStateMachine.Update(deltaTime, false, false);
                return;
            }

            playerStateMachine.Update(
                deltaTime,
                playerInput.TakeRollInput(),
                playerInput.TakeAttackInput());
        }

        protected override void OnUnitDisable()
        {
            playerStateMachine.Disable();
            playerInput.Disable();
        }

        protected override void OnUnitDispose()
        {
            Health.Died -= HandleHealthDied;
            playerInput.Destroy();
        }

        private void HandleHealthDied()
        {
            playerInput.Disable();
            playerStateMachine.ChangeToDeadState();
        }
    }
}
