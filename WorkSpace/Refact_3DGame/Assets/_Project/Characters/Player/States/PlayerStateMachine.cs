using System;
using rudIsland.RPG3D.Combat;
using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Player.Input;
using rudIsland.RPG3D.Player.Movement;
using rudIsland.RPG3D.Player.States.Attack;
using rudIsland.RPG3D.Player.States.Block;
using rudIsland.RPG3D.Player.States.Movement;
using UnityEngine;

namespace rudIsland.RPG3D.Player.States
{
    // 플레이어 상태 전환, 입력 판단, 루트 모션 전달을 관리한다.
    public sealed class PlayerStateMachine
    {
        private readonly PlayerInputReader playerInput; // 입력 또는 행동 여부
        private readonly PlayerMovement playerMovement; // 이동 정보
        private readonly PlayerAnimationController animationController; // 씬 또는 시스템 참조
        private readonly PlayerAttackState attackState; // 공격 관련 설정 또는 상태
        private readonly PlayerControlState controlState; // 현재 행동 상태
        private readonly PlayerHitState hitState; // 피격 또는 피해 관련 값
        private readonly PlayerDeadState deadState; // 현재 행동 상태
        private readonly Action endAttackHit; // 공격 관련 설정 또는 상태
        private readonly float rollDistanceScale; // 거리 설정

        private IPlayerState currentState; // 현재 행동 상태
        private bool isEnabled; // 기능 사용 여부

        internal PlayerMovement Movement => playerMovement; // 이동 정보
        internal PlayerInputReader Input => playerInput; // 입력 또는 행동 여부
        public bool IsBlocking => ReferenceEquals(currentState, controlState) && // 기능 사용 여부
            controlState.IsBlocking;
        public bool IsRolling => ReferenceEquals(currentState, controlState) && // 기능 사용 여부
            controlState.IsRolling;
        public bool IsAttacking => ReferenceEquals(currentState, controlState) && // 기능 사용 여부
            controlState.IsAttacking;
        public bool IsDead => ReferenceEquals(currentState, deadState); // 기능 사용 여부
        public bool IsHit => ReferenceEquals(currentState, hitState); // 기능 사용 여부
        public HitReaction LastHitReaction { get; private set; }

        public PlayerStateMachine(
            PlayerInputReader playerInput,
            PlayerMovement playerMovement,
            Animator playerAnimator,
            float animationSmoothTime,
            float rollDistanceScale,
            float attack01NextInputTime,
            float attack02NextInputTime,
            float attack03NextInputTime,
            float attack04NextInputTime,
            float comboInputBufferDuration,
            float attack01MoveScale,
            float attack02MoveScale,
            float attack03MoveScale,
            float attack04MoveScale,
            float attack05MoveScale,
            float runAttackMoveScale,
            Action endAttackHit)
        {
            this.playerInput = playerInput;
            this.playerMovement = playerMovement;
            animationController = new PlayerAnimationController(
                playerAnimator,
                animationSmoothTime);
            this.rollDistanceScale = Mathf.Max(0f, rollDistanceScale);
            this.endAttackHit = endAttackHit;

            PlayerMoveState moveState = new PlayerMoveState(this, animationController);
            PlayerBlockState blockState = new PlayerBlockState(this, animationController);
            PlayerRollState rollState = new PlayerRollState(this, animationController);
            attackState = new PlayerAttackState(
                this,
                animationController,
                Mathf.Clamp01(attack01NextInputTime),
                Mathf.Clamp01(attack02NextInputTime),
                Mathf.Clamp01(attack03NextInputTime),
                Mathf.Clamp01(attack04NextInputTime),
                Mathf.Max(0f, comboInputBufferDuration),
                Mathf.Clamp01(attack01MoveScale),
                Mathf.Clamp01(attack02MoveScale),
                Mathf.Clamp01(attack03MoveScale),
                Mathf.Clamp01(attack04MoveScale),
                Mathf.Clamp01(attack05MoveScale),
                Mathf.Clamp01(runAttackMoveScale));
            controlState = new PlayerControlState(
                this, moveState, blockState, rollState, attackState);
            hitState = new PlayerHitState(this, animationController);
            deadState = new PlayerDeadState(this, animationController);
        }

        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            isEnabled = true;
            ChangeState(controlState);
        }

        public void Update(float deltaTime, bool rollPressed, bool attackPressed)
        {
            if (!isEnabled || currentState == null)
            {
                return;
            }

            currentState.Update(deltaTime, new PlayerStateInput(
                rollPressed,
                attackPressed,
                playerInput.IsBlocking));
        }

        public void Disable()
        {
            if (!isEnabled)
            {
                return;
            }

            currentState?.Exit();
            EndAttackHit();
            currentState = null;
            isEnabled = false;
            animationController.Reset();
        }

        internal void SetAttackDirection()
        {
            playerMovement.SetAttackDirection();
        }

        internal void UpdateAttackTurn(float deltaTime)
        {
            playerMovement.UpdateAttackTurn(deltaTime);
        }

        internal void ClearAttackDirection()
        {
            playerMovement.ClearAttackDirection();
        }

        internal bool CanStartAttack()
        {
            return playerMovement.IsGrounded;
        }

        internal bool ShouldStartRunAttack()
        {
            return playerInput.IsSprinting &&
                playerInput.MoveValue.sqrMagnitude >= 0.95f;
        }

        internal void ApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            if (IsRolling || IsBlocking || IsAttacking || IsDead)
            {
                float horizontalMoveScale = IsRolling
                    ? rollDistanceScale
                    : IsAttacking
                        ? attackState.CurrentMoveScale
                        : 1f;
                playerMovement.ApplyRootMotion(
                    deltaPosition,
                    deltaRotation,
                    horizontalMoveScale);
            }
        }

        internal void ChangeToDeadState()
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            EndAttackHit();
            ChangeState(deadState);
        }

        internal void ChangeToHitState()
        {
            HitReaction reaction = default;
            ChangeToHitState(in reaction);
        }

        internal void ChangeToHitState(in AttackHitData hit)
        {
            HitReaction reaction = HitReaction.Create(
                in hit,
                playerMovement.Forward,
                playerMovement.Right);
            ChangeToHitState(in reaction);
        }

        private void ChangeToHitState(in HitReaction reaction)
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            LastHitReaction = reaction;
            hitState.SetHitReaction(in reaction);
            EndAttackHit();
            if (ReferenceEquals(currentState, hitState))
            {
                hitState.Restart();
                return;
            }

            ChangeState(hitState);
        }

        internal void ChangeToControlState()
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            ChangeState(controlState);
        }

        internal void EndAttackHit()
        {
            endAttackHit?.Invoke();
        }

        internal void NotifyAttackAnimationEnded()
        {
            if (!isEnabled || !IsAttacking)
            {
                return;
            }

            attackState.NotifyAnimationEnded();
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
