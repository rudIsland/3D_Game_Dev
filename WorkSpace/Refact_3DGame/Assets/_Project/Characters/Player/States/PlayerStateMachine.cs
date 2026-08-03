using System;
using rudIsland.RPG3D.Combat;
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
        private readonly Action endAttackHit;
        private readonly float rollDistanceScale;

        private IPlayerState currentState;
        private IPlayerState returnLookState;
        private bool isEnabled;

        internal PlayerMovement Movement => playerMovement;
        internal PlayerInputReader Input => playerInput;
        public bool IsBlocking => actionStateMachine.IsBlocking;
        public bool IsRolling => actionStateMachine.IsRolling;
        public bool IsAttacking => actionStateMachine.IsAttacking;
        public bool IsTargeting => ReferenceEquals(currentState, targetLookState);
        public bool IsDead => ReferenceEquals(currentState, deadState);
        public bool IsHit => ReferenceEquals(currentState, hitState);
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
            PlayerTargetFinder targetFinder,
            PlayerTargetCamera targetCamera,
            float targetBreakDistance,
            Action endAttackHit)
        {
            this.playerInput = playerInput;
            this.playerMovement = playerMovement;
            animationController = new PlayerAnimationController(
                playerAnimator,
                animationSmoothTime);
            this.rollDistanceScale = Mathf.Max(0f, rollDistanceScale);
            this.endAttackHit = endAttackHit;

            var moveState = new PlayerMoveState(this, animationController);
            var blockState = new PlayerBlockState(this, animationController);
            var rollState = new PlayerRollState(this, animationController);
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
            actionStateMachine = new PlayerActionStateMachine(
                this,
                moveState,
                blockState,
                rollState,
                attackState);
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
                targetBreakDistance);
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

            currentState.Update(deltaTime, new PlayerStateInput(
                rollPressed,
                attackPressed,
                targetTogglePressed,
                playerInput.IsBlocking));
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

            if (ReferenceEquals(returnLookState, targetLookState) &&
                targetLookState.IsTargetAvailable())
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

        internal bool CanStartAttack()
        {
            return playerMovement.IsGrounded;
        }

        internal bool ShouldStartRunAttack()
        {
            return playerInput.IsSprinting &&
                playerInput.MoveValue.sqrMagnitude >= 0.95f;
        }

        internal void ApplyRootMotion(
            Vector3 deltaPosition,
            Quaternion deltaRotation)
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

            actionStateMachine.Disable();
            targetLookState.ReleaseTarget();
            returnLookState = freeLookState;
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

            actionStateMachine.Disable();
            ChangeState(hitState);
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

        internal void NotifyAttackHitEnded()
        {
            if (!isEnabled || !IsAttacking)
            {
                return;
            }

            attackState.OpenComboTurnWindow();
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
