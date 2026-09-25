using UnityEngine;
using Core;

namespace Player
{
    // 상태들을 소유하고 시점·행동·피격·사망 전환과 애니메이션 이벤트의 유효성을 관리한다.
    public sealed class PlayerStateMachine : IPlayerLookStateChange, IPlayerAttackTarget
    {
        private readonly PlayerInputReader playerInput;
        private readonly PlayerMovement playerMovement;
        private readonly PlayerAnimationController animationController;
        private readonly PlayerAttackState attackState;
        private readonly PlayerRollState rollState;
        private readonly PlayerActionStateMachine actionStateMachine;
        private readonly PlayerFreeLookState freeLookState;
        private readonly PlayerTargetLookState targetLookState;
        private readonly PlayerHitState hitState;
        private readonly PlayerDeadState deadState;
        private readonly PlayerActionStamina actionStamina;
        private readonly float minimumGuardDot;
        private readonly PlayerAttackHit attackHit;
        private IPlayerState currentState;
        private IPlayerState returnLookState;
        private bool isEnabled;

        public bool IsBlocking => actionStateMachine.IsBlocking;
        public bool IsRolling => actionStateMachine.IsRolling;
        /// <summary>구르기 상태가 보관하는 현재 무적 여부를 반환한다.</summary>
        public bool IsRollInvulnerable => rollState.IsInvulnerable;
        public bool IsAttacking => actionStateMachine.IsAttacking;
        public bool IsDead => ReferenceEquals(currentState, deadState);
        public bool IsHit => ReferenceEquals(currentState, hitState);
        internal bool ProtectsSmallHit =>
            IsAttacking && attackState.ProtectsSmallHit;
        /// <summary>현재 행동과 달리기 기록에 따른 스태미나 회복 배율을 반환한다.</summary>
        public float StaminaRecoveryRate => actionStamina.GetRecoveryRate(
            IsDead, IsHit, IsAttacking, IsRolling, IsBlocking);

        // 외부에서 조립한 실행 객체를 공유하고 플레이어 상태들만 생성·연결한다.
        internal PlayerStateMachine(
            PlayerInputReader playerInput,
            PlayerMovement playerMovement,
            PlayerActionStamina actionStamina,
            PlayerAnimationController animationController,
            PlayerAttackHit attackHit,
            PlayerCharacterRuntimeConfig config,
            PlayerTargetFinder targetFinder,
            PlayerTargetCamera targetCamera,
            PlayerGuardHitBox guardHitBox)
        {
            this.playerInput = playerInput;
            this.playerMovement = playerMovement;
            PlayerCombatRuntimeConfig combat = config.Combat;
            PlayerMovementRuntimeConfig movement = config.Movement;
            this.actionStamina = actionStamina;
            this.animationController = animationController;
            this.attackHit = attackHit;
            minimumGuardDot = combat.MinimumGuardDot;

            var moveState = new PlayerMoveState(playerMovement, actionStamina, animationController);
            var blockState = new PlayerBlockState(
                playerMovement,
                animationController,
                guardHitBox,
                combat.GuardRaiseDuration);
            rollState = new PlayerRollState(
                playerMovement,
                playerInput,
                actionStamina,
                animationController,
                movement.RollDistance,
                movement.SprintRollDistance,
                movement.RollMovementCurve,
                movement.RollCompleteNormalizedTime);
            attackState = new PlayerAttackState(
                playerMovement,
                actionStamina,
                attackHit,
                this,
                animationController,
                config.Attacks);
            actionStateMachine = new PlayerActionStateMachine(
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

            hitState = new PlayerHitState(
                this,
                playerMovement,
                attackHit,
                animationController,
                combat.HitPushDuration,
                combat.HitPushCurve,
                combat.GuardBreakControlLockDuration);
            deadState = new PlayerDeadState(playerMovement, animationController);
        }

        /// <summary>비활성 상태에서만 무적·달리기를 초기화하고 자유 시점으로 시작한다.</summary>
        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            isEnabled = true;
            rollState.EndInvulnerability();
            actionStamina.ResetSprint();
            returnLookState = freeLookState;
            ChangeState(freeLookState);
        }

        /// <summary>입력과 프레임 시간을 현재 상태에 전달하고 마지막에 공격 판정을 갱신한다.</summary>
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

            actionStamina.BeginFrame();
            currentState.Update(deltaTime, new PlayerStateInput(rollPressed, attackPressed, targetTogglePressed, playerInput.IsBlocking));


            // 현재 상태의 이동·전환을 마친 뒤 열린 공격 판정을 검사한다.
            attackHit.Tick();
        }

        /// <summary>행동·대상·현재 상태·판정을 순서대로 종료하고 재활성화할 상태를 정리한다.</summary>
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
            rollState.EndInvulnerability();
            actionStamina.ResetSprint();
            isEnabled = false;
            animationController.Reset();
        }

        // 대상 선택에 성공하면 현재 프레임 안에서 락온 시점으로 전환한다.
        void IPlayerLookStateChange.TryChangeToTargetLookState()
        {
            if (!isEnabled || !targetLookState.TrySelectTarget())
            {
                return;
            }

            returnLookState = targetLookState;
            ChangeState(targetLookState);
        }

        // 락온 대상을 해제하고 자유 시점으로 전환한다.
        void IPlayerLookStateChange.ChangeToFreeLookState()
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            targetLookState.ReleaseTarget();
            returnLookState = freeLookState;
            ChangeState(freeLookState);
        }

        // 피격 전 시점을 복구하며 대상이 유효하지 않으면 자유 시점으로 돌아간다.
        void IPlayerLookStateChange.ChangeToLookState()
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

        // 현재 상위 상태가 락온일 때만 공격 보정 대상을 제공한다.
        bool IPlayerAttackTarget.TryGetCurrentAttackTarget(out Transform target)
        {
            if (!ReferenceEquals(currentState, targetLookState))
            {
                target = null;
                return false;
            }

            return targetLookState.TryGetCurrentTarget(out target);
        }

        // 공격 시작에 고른 대상과 현재 락온 대상이 같은지 확인한다.
        bool IPlayerAttackTarget.IsAttackTargetAvailable(Transform target)
        {
            return ReferenceEquals(currentState, targetLookState) &&
                targetLookState.IsCurrentTargetAvailable(target);
        }

        // 행동과 대상을 종료하는 기존 순서로 사망 상태에 진입한다.
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
            actionStamina.StopSprint();
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

        // 이미 피격 중이면 재시작 조건을 확인하고, 새 피격이면 행동 종료 후 즉시 전환한다.
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
            actionStamina.StopSprint();
            ChangeState(hitState);
        }


        // 현재 공격 번호의 이벤트만 소리 실행으로 전달한다.
        internal void PlayAttackSound(int attackNumber)
        {
            if (!IsCurrentAttack(attackNumber))
            {
                return;
            }

            attackHit.PlaySound(attackState.CurrentAttackData);
        }

        // 현재 공격 번호를 확인한 뒤 판정과 궤적을 함께 연다.
        internal void BeginAttackHit(int attackNumber)
        {
            if (!IsCurrentAttack(attackNumber))
            {
                return;
            }

            attackHit.Open(attackState.CurrentAttackData);
        }

        // 기존 강화 호출을 공격 판정이 보관하는 배율에 연결한다.
        internal void SetAttackDamageMultiplier(float multiplier)
        {
            attackHit.SetDamageMultiplier(multiplier);
        }

        // 기존 외부 종료 요청과 비활성화 경로를 같은 판정 종료에 연결한다.
        internal void EndAttackHit()
        {
            attackHit.Close();
        }

        private bool IsCurrentAttack(int attackNumber)
        {
            return isEnabled &&
                IsAttacking &&
                attackState.CurrentAttackData != null &&
                attackState.CurrentAttackData.AttackNumber == attackNumber;
        }

        // 구르기 중인 경우에만 시작 이벤트를 무적 값에 반영한다.
        internal void BeginRollInvulnerability()
        {
            if (!isEnabled || !IsRolling)
            {
                return;
            }

            rollState.BeginInvulnerability();
        }

        // 상태와 관계없이 종료 이벤트가 남은 무적 값을 해제하게 한다.
        internal void EndRollInvulnerability()
        {
            rollState.EndInvulnerability();
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
