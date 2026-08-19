using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Player.Camera;
using rudIsland.RPG3D.Player.Input;
using rudIsland.RPG3D.Player.Movement;
using rudIsland.RPG3D.Player.States.Actions;
using rudIsland.RPG3D.Player.States.Attack;
using rudIsland.RPG3D.Player.States.Block;
using rudIsland.RPG3D.Player.States.Death;
using rudIsland.RPG3D.Player.States.FreeLook;
using rudIsland.RPG3D.Player.States.Hit;
using rudIsland.RPG3D.Player.States.Movement;
using rudIsland.RPG3D.Player.States.Target;
using rudIsland.RPG3D.Player.Runtime.Attack;
using rudIsland.RPG3D.Player.Runtime.Hit;
using rudIsland.RPG3D.Player.Runtime;
using rudIsland.RPG3D.Characters.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Player.States
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

        public PlayerStateMachine(
            PlayerInputReader playerInput,
            PlayerMovement playerMovement,
            PlayerStamina playerStamina,
            float guardStaminaRecoveryRate,
            float rollStaminaCost,
            float sprintStaminaCostPerSecond,
            float sprintRestartStamina,
            Animator playerAnimator,
            float animationSmoothTime,
            float rollDistance,
            float sprintRollDistance,
            AnimationCurve rollMovementCurve,
            float rollCompleteNormalizedTime,
            PlayerAttackData[] attackData,
            float actionInputBufferDuration,
            PlayerTargetFinder targetFinder,
            PlayerTargetCamera targetCamera,
            float targetBreakDistance,
            float targetHiddenGraceDuration,
            float guardAngle,
            float guardRaiseDuration,
            PlayerGuardHitBox guardHitBox,
            float guardBreakControlLockDuration,
            float hitPushDuration,
            AnimationCurve hitPushCurve,
            Transform attackerRoot,
            Transform weaponHitStart,
            Transform weaponHitEnd,
            LayerMask attackLayers,
            float weaponHitRadius,
            CombatHitStop hitStop,
            CombatHitEffectPlayer hitEffectPlayer,
            PlayerAttackEffectPlayer attackEffectPlayer)
        {
            this.playerInput = playerInput;
            this.playerMovement = playerMovement;
            this.playerStamina = playerStamina;
            this.guardStaminaRecoveryRate = Mathf.Clamp01(guardStaminaRecoveryRate);
            this.rollStaminaCost = Mathf.Max(0f, rollStaminaCost);
            this.sprintStaminaCostPerSecond = Mathf.Max(0f, sprintStaminaCostPerSecond);
            this.sprintRestartStamina = Mathf.Clamp(sprintRestartStamina, 0f, playerStamina.MaxStamina);
            this.attackEffectPlayer = attackEffectPlayer;
            animationController = new PlayerAnimationController(playerAnimator, animationSmoothTime);
            minimumGuardDot = Mathf.Cos(Mathf.Clamp(guardAngle, 0f, 180f) * 0.5f * Mathf.Deg2Rad);

            var moveState = new PlayerMoveState(this, animationController);
            var blockState = new PlayerBlockState(
                this,
                animationController,
                guardHitBox,
                guardRaiseDuration);
            var rollState = new PlayerRollState(
                this,
                animationController,
                rollDistance,
                sprintRollDistance,
                rollMovementCurve,
                rollCompleteNormalizedTime);
            attackState = new PlayerAttackState(
                this,
                animationController,
                attackData);
            actionStateMachine = new PlayerActionStateMachine(
                this,
                moveState,
                blockState,
                rollState,
                attackState,
                Mathf.Max(0f, actionInputBufferDuration));
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
                targetBreakDistance,
                targetHiddenGraceDuration);

            attackRangeDetector = new PlayerAttackRangeDetector(
                attackerRoot,
                weaponHitStart,
                weaponHitEnd,
                attackLayers,
                weaponHitRadius,
                hitStop,
                hitEffectPlayer,
                attackEffectPlayer);
            hitState = new PlayerHitState(
                this,
                animationController,
                hitPushDuration,
                hitPushCurve,
                guardBreakControlLockDuration);
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
                attackState.AttackDamage,
                attackState.AttackStaggerDamage,
                attackState.CurrentAttackStrength,
                attackState.AttackPushDistance,
                attackState.AttackHitStopDuration);
            attackEffectPlayer?.BeginTrail();
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
