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
        private readonly float rollStaminaCost;
        private readonly float sprintStaminaCostPerSecond;
        private readonly float minimumGuardDot;
        private IPlayerState currentState;
        private IPlayerState returnLookState;
        private bool isEnabled;


        private readonly PlayerAttackRangeDetector attackRangeDetector;

        internal PlayerMovement Movement => playerMovement;
        internal PlayerInputReader Input => playerInput;
        public bool IsBlocking => actionStateMachine.IsBlocking;
        public bool IsRolling => actionStateMachine.IsRolling;
        public bool IsAttacking => actionStateMachine.IsAttacking;
        public bool IsTargeting => ReferenceEquals(currentState, targetLookState);
        public bool IsDead => ReferenceEquals(currentState, deadState);
        public bool IsHit => ReferenceEquals(currentState, hitState);
        public bool IsWalking =>
            actionStateMachine.IsMoving &&
            !playerInput.IsSprinting &&
            playerInput.MoveValue.sqrMagnitude >= 0.01f;

        public PlayerStateMachine(
            PlayerInputReader playerInput,
            PlayerMovement playerMovement,
            PlayerStamina playerStamina,
            float rollStaminaCost,
            float sprintStaminaCostPerSecond,
            Animator playerAnimator,
            float animationSmoothTime,
            float rollDistance,
            float sprintRollDistance,
            AnimationCurve rollMovementCurve,
            PlayerAttackData[] attackData,
            float comboInputBufferDuration,
            PlayerTargetFinder targetFinder,
            PlayerTargetCamera targetCamera,
            float targetBreakDistance,
            float guardAngle,
            PlayerGuardHitBox guardHitBox,
            float hitPushDuration,
            AnimationCurve hitPushCurve,
            Transform attackerRoot,
            Transform weaponHitStart,
            Transform weaponHitEnd,
            LayerMask attackLayers,
            float weaponHitRadius,
            CombatHitStop hitStop,
            CombatHitEffectPlayer hitEffectPlayer)
        {
            this.playerInput = playerInput;
            this.playerMovement = playerMovement;
            this.playerStamina = playerStamina;
            this.rollStaminaCost = Mathf.Max(0f, rollStaminaCost);
            this.sprintStaminaCostPerSecond =
                Mathf.Max(0f, sprintStaminaCostPerSecond);
            animationController = new PlayerAnimationController(
                playerAnimator,
                animationSmoothTime);
            minimumGuardDot = Mathf.Cos(
                Mathf.Clamp(guardAngle, 0f, 180f) *
                0.5f *
                Mathf.Deg2Rad);

            var moveState = new PlayerMoveState(this, animationController);
            var blockState = new PlayerBlockState(
                this,
                animationController,
                guardHitBox);
            var rollState = new PlayerRollState(
                this,
                animationController,
                rollDistance,
                sprintRollDistance,
                rollMovementCurve);
            attackState = new PlayerAttackState(
                this,
                animationController,
                attackData,
                Mathf.Max(0f, comboInputBufferDuration));
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

            attackRangeDetector = new PlayerAttackRangeDetector(
                attackerRoot,
                weaponHitStart,
                weaponHitEnd,
                attackLayers,
                weaponHitRadius,
                hitStop,
                hitEffectPlayer);
            hitState = new PlayerHitState(
                this,
                animationController,
                hitPushDuration,
                hitPushCurve);
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

        internal bool TryPrepareAttack()
        {
            if (!playerMovement.IsGrounded)
            {
                return false;
            }

            bool startAsRunAttack = ShouldStartRunAttack();
            if (!playerStamina.TryConsume(
                    attackState.GetInitialStaminaCost(startAsRunAttack)))
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
            if (!playerStamina.CanConsume(rollStaminaCost) ||
                !playerMovement.TryStartRoll())
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
            if (!playerInput.IsSprinting ||
                playerInput.MoveValue.sqrMagnitude < 0.01f)
            {
                return false;
            }

            return playerStamina.TryConsume(
                sprintStaminaCostPerSecond * deltaTime);
        }

        private bool ShouldStartRunAttack()
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
                float horizontalRootMotionScale =
                    IsRolling || IsAttacking ? 0f : 1f;
                playerMovement.ApplyRootMotion(
                    deltaPosition,
                    deltaRotation,
                    horizontalRootMotionScale);
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

        internal void ChangeToHitState(
            in PlayerHitRequest hitRequest)
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            hitState.SetHitRequest(in hitRequest);
            if (ReferenceEquals(currentState, hitState))
            {
                hitState.Restart();
                return;
            }

            actionStateMachine.Disable();
            ChangeState(hitState);
        }


        internal void BeginAttackHit(int attackNumber) //공격 윈도우 시작
        {
            if(!isEnabled || !IsAttacking)
                return;

            attackRangeDetector.Open(
                attackState.AttackDamage,
                attackState.AttackStaggerDamage,
                attackState.AttackPushDistance,
                attackState.AttackHitStopDuration);
        }

        internal void EndAttackHit() //공격 윈도우 종료
        {
            attackRangeDetector.Close();
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
            if (!isEnabled || !IsBlocking)
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

            return Vector3.Dot(
                    guardForward.normalized,
                    attackerDirection) >= minimumGuardDot;
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
