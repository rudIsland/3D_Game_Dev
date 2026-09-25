using UnityEngine;

namespace Player
{
    // 방어 입력이 유지되는 동안 방패 걷기 Blend Tree와 방어 상태를 유지한다.
    internal sealed class PlayerBlockState : IPlayerState
    {
        private readonly PlayerMovement movement;
        private readonly PlayerAnimationController animationController; // 씬 또는 시스템 참조
        private readonly PlayerGuardHitBox guardHitBox;
        private readonly float guardRaiseDuration;
        private float guardRaiseElapsedTime;

        internal bool IsGuardReady { get; private set; }

        /// <summary>방어 회전·애니메이션·방패 판정과 준비 시간을 연결한다.</summary>
        public PlayerBlockState(
            PlayerMovement movement,
            PlayerAnimationController animationController,
            PlayerGuardHitBox guardHitBox,
            float guardRaiseDuration)
        {
            this.movement = movement;
            this.animationController = animationController;
            this.guardHitBox = guardHitBox;
            this.guardRaiseDuration = Mathf.Max(0f, guardRaiseDuration);
        }

        /// <summary>방어 방향을 정하고 준비 시간에 맞춰 방패 판정을 시작한다.</summary>
        public void Enter()
        {
            guardRaiseElapsedTime = 0f;
            IsGuardReady = guardRaiseDuration <= 0f;
            animationController.StopMove();
            movement.SetAttackDirection(false);
            movement.BeginGuardTurn();
            if (IsGuardReady)
                movement.UpdateGuardTurn(1f);
            animationController.SetBlocking(true);
            guardHitBox?.SetGuardActive(IsGuardReady);
        }

        /// <summary>준비와 대기 애니메이션이 끝난 뒤 이동을 허용하고 방패 판정을 갱신한다.</summary>
        public void Update(float deltaTime, PlayerStateInput input)
        {
            UpdateGuardReady(deltaTime);

            bool canGuardMove =
                IsGuardReady &&
                animationController.IsPlayingBlockIdle();
            if (canGuardMove)
            {
                movement.UpdateAttackDirection();
                movement.UpdateAttackTurn(deltaTime);
                movement.UpdateGuardMove(deltaTime);
            }
            else
            {
                movement.UpdateStoppedMove(deltaTime);
            }

            animationController.UpdateBlockMove(movement.GetLocalMoveVelocity(),
                movement.GuardMoveSpeed, deltaTime);
            guardHitBox?.SetGuardActive(IsGuardReady);
        }

        /// <summary>방어 표시와 방패 판정을 끄고 사용하던 방향을 해제한다.</summary>
        public void Exit()
        {
            guardRaiseElapsedTime = 0f;
            IsGuardReady = false;
            animationController.StopMove();
            animationController.SetBlocking(false);
            guardHitBox?.SetGuardActive(false);
            movement.ClearAttackDirection();
        }

        // 준비가 끝나는 프레임까지 시작·종료 방향 사이의 회전을 갱신한다.
        private void UpdateGuardReady(float deltaTime)
        {
            if (IsGuardReady)
            {
                return;
            }

            guardRaiseElapsedTime += Mathf.Max(0f, deltaTime);
            movement.UpdateGuardTurn(guardRaiseElapsedTime / guardRaiseDuration);
            IsGuardReady = guardRaiseElapsedTime >= guardRaiseDuration;
        }
    }
}
