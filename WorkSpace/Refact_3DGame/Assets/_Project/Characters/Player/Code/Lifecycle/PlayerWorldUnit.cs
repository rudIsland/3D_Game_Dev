using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Player.Input;
using rudIsland.RPG3D.Player.Runtime;
using rudIsland.RPG3D.Player.States;
using rudIsland.RPG3D.Player.Runtime.Hit;
using rudIsland.RPG3D.Characters.Combat;

namespace rudIsland.RPG3D.Player
{
    // 플레이어 체력, Stamina와 입력 상태를 Unit 생명주기로 실행한다.
    public sealed class PlayerWorldUnit : PlayerUnit
    {
        private readonly PlayerInputReader playerInput; // 입력 또는 행동 여부
        private readonly PlayerStateMachine playerStateMachine; // 현재 행동 상태
        private readonly PlayerStamina playerStamina;
        private readonly CombatHitStop hitStop;

        public float CurrentHealth => Health.CurrentHealth; // 현재 체력
        public float CurrentStamina => playerStamina.CurrentStamina;
        public float MaxStamina => playerStamina.MaxStamina;
        public PlayerStamina Stamina => playerStamina;

        public PlayerWorldUnit(
            float maxHealth,
            PlayerStamina playerStamina,
            PlayerInputReader playerInput,
            PlayerStateMachine playerStateMachine,
            CombatHitStop hitStop)
            : base(maxHealth)
        {
            this.playerInput = playerInput;
            this.playerStateMachine = playerStateMachine;
            this.playerStamina = playerStamina;
            this.hitStop = hitStop;
        }

        public void TakeDamage(float damage)
        {
            if (TryApplyDamage(damage) && !IsDead)
            {
                hitStop.Request(
                    CombatHitStop.DefaultDamageDuration);
                PlayerHitRequest hitRequest = default;
                playerStateMachine.ChangeToHitState(in hitRequest);
            }
        }

        public PlayerHitResult TryTakeHit(in PlayerHitRequest hitRequest)
        {
            if (hitRequest.Damage == null ||
                IsDead)
            {
                return PlayerHitResult.Ignored;
            }

            if (hitRequest.HitSurface == PlayerHitSurface.Guard &&
                hitRequest.Damage.CanBeBlocked &&
                playerStateMachine.CanBlockHit(hitRequest.PushDirection))
            {
                if (playerStamina.TryConsumeGuard(
                    hitRequest.Damage.GuardStaminaDamage))
                {
                    hitStop.Request(CombatHitStop.GuardDuration);
                    playerStateMachine.NotifyAttackBlocked();
                    return PlayerHitResult.Blocked;
                }

                hitStop.Request(hitRequest.Damage.HitStopDuration);
                playerStateMachine.ChangeToHitState(in hitRequest);
                return PlayerHitResult.GuardBroken;
            }

            if (!TryApplyDamage(hitRequest.Damage.HealthDamage))
            {
                return PlayerHitResult.Ignored;
            }

            hitStop.Request(hitRequest.Damage.HitStopDuration);
            if (!IsDead)
            {
                playerStateMachine.ChangeToHitState(in hitRequest);
            }

            return PlayerHitResult.Damaged;
        }

        private bool TryApplyDamage(float damage)
        {
            float healthBeforeDamage = Health.CurrentHealth;
            Health.TakeDamage(damage);
            return Health.CurrentHealth < healthBeforeDamage;
        }



        protected override void OnUnitCreate()
        {
            playerInput.Create();
            Health.Died += HandleHealthDied;
        }

        protected override void OnUnitEnable()
        {
            hitStop.Reset();
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
            if (hitStop.Update(deltaTime))
            {
                return;
            }

            if (IsDead)
            {
                playerStateMachine.Update(deltaTime, false, false, false);
                return;
            }

            playerStateMachine.Update(
                deltaTime,
                playerInput.TakeRollInput(),
                playerInput.TakeAttackInput(),
                playerInput.TakeTargetToggleInput());
            playerStamina.UpdateRecovery(
                deltaTime,
                playerStateMachine.IsWalking);
        }

        protected override void OnUnitDisable()
        {
            hitStop.Reset();
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
