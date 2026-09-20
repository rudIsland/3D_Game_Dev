using Characters.Player.Animation;
using Characters.Player.Camera;
using Characters.Player.Input;
using Characters.Player.Movement;
using Characters.Player.StateMachine.Actions;
using Characters.Player.StateMachine.States.Attack;
using Characters.Player.StateMachine.States.Block;
using Characters.Player.StateMachine.States.Death;
using Characters.Player.StateMachine.States.FreeLook;
using Characters.Player.StateMachine.States.Hit;
using Characters.Player.StateMachine.States.Movement;
using Characters.Player.StateMachine.States.Target;
using Characters.Player.Combat.Attack;
using Characters.Player.Combat.Hit;
using Characters.Player.Stats;
using Characters.Combat;
using Characters.Player.Config;
using UnityEngine;

namespace Characters.Player.StateMachine
{
    // 시점, 행동, 피격과 사망 상태의 전환 및 생명주기를 관리한다.
    public sealed class PlayerStateMachine
    {
        private readonly PlayerInputReader playerInput;
        private readonly PlayerMovement playerMovement;
        private readonly PlayerAnimationController animationController;
        private readonly PlayerAttackState attackState;
        private readonly PlayerActionStateMachine actionStateMachine;
        private readonly PlayerFreeLookState freeLookState;
        private readonly PlayerTargetLookState targetLookState;
        private readonly PlayerHitState hitState;
        private readonly PlayerDeadState deadState;
        private readonly PlayerStamina playerStamina;
        private readonly float guardStaminaRecoveryRate;
        private readonly float rollStaminaCost;
        private readonly float sprintStaminaCostPerSecond;
        private readonly float sprintRestartStamina;
        private readonly float minimumGuardDot;
        private readonly PlayerAttackEffectPlayer attackEffectPlayer;
        private IPlayerState currentState;
        private IPlayerState returnLookState;
        private bool isSprintingThisFrame;
        private bool wasSprintingLastFrame;
        private bool isSprintRecoveryRequired;
        private bool isEnabled;
        private float attackDamageMultiplier = 1f;


        private readonly PlayerAttackRangeDetector attackRangeDetector;

        internal PlayerMovement Movement => playerMovement;
        internal PlayerInputReader Input => playerInput;
        public bool IsBlocking => actionStateMachine.IsBlocking;
        public bool IsRolling => actionStateMachine.IsRolling;
        public bool IsRollInvulnerable { get; private set; }
        public bool IsAttacking => actionStateMachine.IsAttacking;
        public bool IsTargeting => ReferenceEquals(currentState, targetLookState);
        public bool IsDead => ReferenceEquals(currentState, deadState);
        public bool IsHit => ReferenceEquals(currentState, hitState);
        internal bool ProtectsSmallHit =>
            IsAttacking && attackState.ProtectsSmallHit;
        public float StaminaRecoveryRate
        {
            get
            {
                if (IsDead ||
                    IsHit ||
                    IsAttacking ||
                    IsRolling ||
                    isSprintingThisFrame)
                {
                    return 0f;
                }

                return IsBlocking
                    ? guardStaminaRecoveryRate
                    : 1f;
            }
        }

        internal PlayerStateMachine(
            PlayerInputReader playerInput,
            PlayerMovement playerMovement,
            PlayerStamina playerStamina,
            Animator playerAnimator,
            PlayerCharacterRuntimeConfig config,
            PlayerTargetFinder targetFinder,
            PlayerTargetCamera targetCamera,
            PlayerGuardHitBox guardHitBox,
            Transform attackerRoot,
            PlayerWeaponHitShape weaponHitShape,
            CombatHitStop hitStop,
            CombatHitEffectPlayer hitEffectPlayer,
            PlayerAttackEffectPlayer attackEffectPlayer)
        {
            this.playerInput = playerInput;
            this.playerMovement = playerMovement;
            this.playerStamina = playerStamina;
            PlayerCombatRuntimeConfig combat = config.Combat;
            PlayerMovementRuntimeConfig movement = config.Movement;
            this.guardStaminaRecoveryRate = combat.GuardStaminaRecoveryRate;
            this.rollStaminaCost = combat.RollStaminaCost;
            this.sprintStaminaCostPerSecond =
                combat.SprintStaminaCostPerSecond;
            this.sprintRestartStamina = combat.SprintRestartStamina;
            this.attackEffectPlayer = attackEffectPlayer;
            animationController = new PlayerAnimationController(
                playerAnimator,
                movement.AnimationSmoothTime);
            minimumGuardDot = combat.MinimumGuardDot;

            var moveState = new PlayerMoveState(this, animationController);
            var blockState = new PlayerBlockState(
                this,
                animationController,
                guardHitBox,
                combat.GuardRaiseDuration);
            var rollState = new PlayerRollState(
                this,
                animationController,
                movement.RollDistance,
                movement.SprintRollDistance,
                movement.RollMovementCurve,
                movement.RollCompleteNormalizedTime);
            attackState = new PlayerAttackState(
                this,
                animationController,
                config.Attacks);
            actionStateMachine = new PlayerActionStateMachine(
                this,
                moveState,
                blockState,
                rollState,
                attackState,
                combat.ActionInputBufferDuration);
            freeLookState = new PlayerFreeLookState(
                this,
                actionStateMachine,
                playerMovement,
                targetCamera);
            targetLookState = new PlayerTargetLookState(
                this,
                actionStateMachine,
                playerMovement,
                targetFinder,
                targetCamera,
                config.Target.BreakDistance,
                config.Target.HiddenGraceDuration);

            attackRangeDetector = new PlayerAttackRangeDetector(
                attackerRoot,
                weaponHitShape.StartPoint,
                weaponHitShape.EndPoint,
                weaponHitShape.TargetLayers,
                weaponHitShape.Radius,
                hitStop,
                hitEffectPlayer,
                attackEffectPlayer);
            hitState = new PlayerHitState(
                this,
                animationController,
                combat.HitPushDuration,
                combat.HitPushCurve,
                combat.GuardBreakControlLockDuration);
            deadState = new PlayerDeadState(this, animationController);
        }

        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            isEnabled = true;
            IsRollInvulnerable = false;
            isSprintingThisFrame = false;
            wasSprintingLastFrame = false;
            isSprintRecoveryRequired = false;
            returnLookState = freeLookState;
            ChangeState(freeLookState);
        }

        public void Update(
            float deltaTime,
            bool rollPressed,
            bool attackPressed,
            bool targetTogglePressed)
        {
            if (!isEnabled || currentState == null)
            {
                return;
            }

            wasSprintingLastFrame = isSprintingThisFrame;
            isSprintingThisFrame = false;
            currentState.Update(deltaTime, new PlayerStateInput(rollPressed, attackPressed, targetTogglePressed, playerInput.IsBlocking));


            //공격 판정 윈도우 열려있으면 공격 범위 감지
            attackRangeDetector.Tick();
        }

        public void Disable()
        {
            if (!isEnabled)
            {
                return;
            }

            actionStateMachine.Disable();
            targetLookState.ReleaseTarget();
            currentState?.Exit();
            EndAttackHit();
            currentState = null;
            returnLookState = null;
            IsRollInvulnerable = false;
            isSprintingThisFrame = false;
            wasSprintingLastFrame = false;
            isSprintRecoveryRequired = false;
            isEnabled = false;
            animationController.Reset();
        }

        internal void TryChangeToTargetLookState()
        {
            if (!isEnabled || !targetLookState.TrySelectTarget())
            {
                return;
            }

            returnLookState = targetLookState;
            ChangeState(targetLookState);
        }

        internal void ChangeToFreeLookState()
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            targetLookState.ReleaseTarget();
            returnLookState = freeLookState;
            ChangeState(freeLookState);
        }

        internal void ChangeToLookState()
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            if (ReferenceEquals(returnLookState, targetLookState) && targetLookState.IsTargetAvailable())
            {
                ChangeState(targetLookState);
                return;
            }

            targetLookState.ReleaseTarget();
            returnLookState = freeLookState;
            ChangeState(freeLookState);
        }

        internal void SetAttackDirection(bool rotateImmediately)
        {
            playerMovement.SetAttackDirection(rotateImmediately);
        }

        internal void UpdateAttackDirection()
        {
            playerMovement.UpdateAttackDirection();
        }

        internal void UpdateAttackTurn(float deltaTime)
        {
            playerMovement.UpdateAttackTurn(deltaTime);
        }

        internal void ClearAttackDirection()
        {
            playerMovement.ClearAttackDirection();
        }

        internal bool TryGetCurrentAttackTarget(out Transform target)
        {
            if (!ReferenceEquals(currentState, targetLookState))
            {
                target = null;
                return false;
            }

            return targetLookState.TryGetCurrentTarget(out target);
        }

        internal bool IsAttackTargetAvailable(Transform target)
        {
            return ReferenceEquals(currentState, targetLookState) &&
                targetLookState.IsCurrentTargetAvailable(target);
        }

        internal bool TryPrepareAttack()
        {
            if (!playerMovement.IsGrounded)
            {
                return false;
            }

            bool startAsRunAttack = ShouldStartRunAttack();
            if (!playerStamina.TryConsume(attackState.GetInitialStaminaCost(startAsRunAttack)))
            {
                return false;
            }

            attackState.Prepare(startAsRunAttack);
            return true;
        }

        internal bool TryConsumeAttackStamina(float staminaCost)
        {
            return playerStamina.TryConsume(staminaCost);
        }

        internal bool TryStartRoll()
        {
            if (!playerStamina.CanConsume(rollStaminaCost) || !playerMovement.TryStartRoll())
            {
                return false;
            }

            return playerStamina.TryConsume(rollStaminaCost);
        }

        internal bool TryStartAttackCancelRoll()
        {
            if (!playerStamina.TryConsume(rollStaminaCost))
            {
                return false;
            }

            playerMovement.StartAttackCancelRoll();
            return true;
        }

        internal bool TryConsumeSprintStamina(float deltaTime)
        {
            if (!playerInput.IsSprinting || playerInput.MoveValue.sqrMagnitude < 0.01f)
            {
                return false;
            }

            if (isSprintRecoveryRequired)
            {
                if (playerStamina.CurrentStamina < sprintRestartStamina)
                {
                    return false;
                }

                isSprintRecoveryRequired = false;
            }

            if (!playerStamina.TryConsume(sprintStaminaCostPerSecond * deltaTime))
            {
                isSprintRecoveryRequired = true;
                return false;
            }

            isSprintingThisFrame = true;
            return true;
        }

        private bool ShouldStartRunAttack()
        {
            return ShouldStartRunAttack(wasSprintingLastFrame);
        }

        internal static bool ShouldStartRunAttack(bool wasActuallySprintingLastFrame)
        {
            return wasActuallySprintingLastFrame;
        }

        internal void ChangeToDeadState()
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            actionStateMachine.Disable();
            targetLookState.ReleaseTarget();
            returnLookState = freeLookState;
            EndAttackHit();
            isSprintingThisFrame = false;
            wasSprintingLastFrame = false;
            ChangeState(deadState);
        }

        internal void ChangeToHitState(
            HitReaction reaction,
            in PlayerHitRequest hitRequest)
        {
            ChangeToHitState(
                reaction,
                in hitRequest,
                false);
        }

        internal void ChangeToGuardBreakState(
            HitReaction reaction,
            in PlayerHitRequest hitRequest)
        {
            ChangeToHitState(
                reaction,
                in hitRequest,
                true);
        }

        private void ChangeToHitState(
            HitReaction reaction,
            in PlayerHitRequest hitRequest,
            bool isGuardBreak)
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            if (ReferenceEquals(currentState, hitState))
            {
                hitState.TryRestart(
                    reaction,
                    in hitRequest,
                    isGuardBreak);
                return;
            }

            hitState.SetHitRequest(
                reaction,
                in hitRequest,
                isGuardBreak);
            actionStateMachine.Disable();
            isSprintingThisFrame = false;
            wasSprintingLastFrame = false;
            ChangeState(hitState);
        }


        internal void PlayAttackSound(int attackNumber)
        {
            if (!IsCurrentAttack(attackNumber))
            {
                return;
            }

            attackEffectPlayer?.PlaySound(attackState.CurrentAttackData);
        }

        internal void BeginAttackHit(int attackNumber) //공격 윈도우 시작
        {
            if (!IsCurrentAttack(attackNumber))
            {
                return;
            }

            attackRangeDetector.Open(
                attackState.AttackDamage * attackDamageMultiplier,
                attackState.AttackStaggerDamage,
                attackState.CurrentAttackStrength,
                attackState.AttackPushDistance,
                attackState.AttackHitStopDuration);
            attackEffectPlayer?.BeginTrail();
        }

        internal void SetAttackDamageMultiplier(float multiplier)
        {
            attackDamageMultiplier = Mathf.Max(1f, multiplier);
        }

        internal void EndAttackHit() //공격 윈도우 종료
        {
            attackRangeDetector.Close();
            attackEffectPlayer?.Stop();
        }

        private bool IsCurrentAttack(int attackNumber)
        {
            return isEnabled &&
                IsAttacking &&
                attackState.CurrentAttackData != null &&
                attackState.CurrentAttackData.AttackNumber == attackNumber;
        }

        internal void BeginRollInvulnerability()
        {
            if (!isEnabled || !IsRolling)
            {
                return;
            }

            IsRollInvulnerable = true;
        }

        internal void EndRollInvulnerability()
        {
            IsRollInvulnerable = false;
        }

        internal void NotifyAttackAnimationEnded(int attackNumber)
        {
            if (!isEnabled || !IsAttacking)
            {
                return;
            }

            attackState.NotifyAnimationEnded(attackNumber);
        }

        internal void NotifyAttackHitEnded()
        {
            if (!isEnabled || !IsAttacking)
            {
                return;
            }

            attackState.NotifyAttackHitEnded();
        }

        internal void NotifyAttackBlocked()
        {
            if (!isEnabled || !IsBlocking)
            {
                return;
            }

            animationController.PlayBlockImpact();
        }

        internal bool CanBlockHit(Vector3 pushDirection)
        {
            if (!isEnabled || !actionStateMachine.IsGuardReady)
            {
                return false;
            }

            pushDirection.y = 0f;
            if (pushDirection.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            Vector3 attackerDirection = -pushDirection.normalized;
            Vector3 guardForward = playerMovement.Forward;
            guardForward.y = 0f;
            if (guardForward.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            return Vector3.Dot(guardForward.normalized, attackerDirection) >= minimumGuardDot;
        }


        private void ChangeState(IPlayerState nextState)
        {
            if (ReferenceEquals(currentState, nextState))
            {
                return;
            }

            currentState?.Exit();
            currentState = nextState;
            currentState.Enter();
        }
    }
}
