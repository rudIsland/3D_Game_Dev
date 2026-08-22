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
        private readonly StopPoint stopPoint;
        private readonly CombatHitStop hitStop;

        public float CurrentHealth => Health.CurrentHealth; // 현재 체력
        public float CurrentStamina => playerStamina.CurrentStamina;
        public float MaxStamina => playerStamina.MaxStamina;
        public PlayerStamina Stamina => playerStamina;

        internal PlayerWorldUnit(
            float maxHealth,
            PlayerStamina playerStamina,
            StopPoint stopPoint,
            PlayerInputReader playerInput,
            PlayerStateMachine playerStateMachine,
            CombatHitStop hitStop)
            : base(maxHealth)
        {
            this.playerInput = playerInput;
            this.playerStateMachine = playerStateMachine;
            this.playerStamina = playerStamina;
            this.stopPoint = stopPoint;
            this.hitStop = hitStop;
        }

        public PlayerHitResult TryTakeHit(in PlayerHitRequest hitRequest)
        {
            bool canBlockHit =
                hitRequest.Damage != null &&
                !IsDead &&
                !playerStateMachine.IsRollInvulnerable &&
                hitRequest.Damage.CanBlock &&
                playerStateMachine.CanBlockHit(hitRequest.PushDirection);
            PlayerHitResult resultBeforeHealthDamage =
                GetHitResultBeforeHealthDamage(
                    hitRequest.Damage != null,
                    IsDead,
                    playerStateMachine.IsRollInvulnerable,
                    canBlockHit,
                    playerStamina.CurrentStamina,
                    hitRequest.Damage != null
                        ? hitRequest.Damage.GuardStaminaDamage
                        : 0f);

            switch (resultBeforeHealthDamage)
            {
                case PlayerHitResult.Ignored:
                
                case PlayerHitResult.Avoided:
                    return resultBeforeHealthDamage;

                case PlayerHitResult.Blocked:
                    playerStamina.TryConsumeGuard(hitRequest.Damage.GuardStaminaDamage);
                    hitStop.Request(CombatHitStop.GuardDuration);
                    playerStateMachine.NotifyAttackBlocked();
                    return PlayerHitResult.Blocked;

                case PlayerHitResult.GuardBroken:
                    playerStamina.TryConsumeGuard(
                        hitRequest.Damage.GuardStaminaDamage);
                    hitStop.Request(hitRequest.Damage.HitStopDuration);
                    playerStateMachine.ChangeToGuardBreakState(
                        HitReaction.BigHit,
                        in hitRequest);
                    return PlayerHitResult.GuardBroken;
            }

            HitDamageResult damageResult = HitDamageCalculator.Apply(
                Health,
                hitRequest.Damage.HealthDamage);
            if (damageResult == HitDamageResult.Ignored)
            {
                return PlayerHitResult.Ignored;
            }

            hitStop.Request(hitRequest.Damage.HitStopDuration);
            if (damageResult != HitDamageResult.Killed)
            {
                bool reachedStopLimit = stopPoint.TryAccumulate(
                    hitRequest.Damage.StaggerDamage);
                HitReaction reaction = HitReactionSelector.Select(
                    hitRequest.Damage.Strength,
                    reachedStopLimit,
                    playerStateMachine.ProtectsSmallHit,
                    false,
                    false);
                if (reaction != HitReaction.None)
                {
                    playerStateMachine.ChangeToHitState(
                        reaction,
                        in hitRequest);
                }
            }

            return PlayerHitResult.Damaged;
        }



        internal static PlayerHitResult GetHitResultBeforeHealthDamage(
            bool hasDamage,
            bool isDead,
            bool isRollInvulnerable,
            bool canBlockHit,
            float currentStamina,
            float guardStaminaDamage)
        {
            if (!hasDamage || isDead)
            {
                return PlayerHitResult.Ignored;
            }

            if (isRollInvulnerable)
            {
                return PlayerHitResult.Avoided;
            }

            if (!canBlockHit)
            {
                return PlayerHitResult.Damaged;
            }

            return guardStaminaDamage <= 0f ||
                currentStamina > guardStaminaDamage
                    ? PlayerHitResult.Blocked
                    : PlayerHitResult.GuardBroken;
        }

        protected override void OnUnitCreate()
        {
            playerInput.Create();
            Health.Died += HandleHealthDied;
        }

        protected override void OnUnitEnable()
        {
            hitStop.Reset();
            stopPoint.Reset();
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

            stopPoint.UpdateRecovery(deltaTime);

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
            playerStamina.UpdateRecovery(deltaTime, playerStateMachine.StaminaRecoveryRate);
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
