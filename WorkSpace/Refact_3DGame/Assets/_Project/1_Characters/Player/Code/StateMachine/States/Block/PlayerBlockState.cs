using Characters.Player.Animation;
using Characters.Player.Combat.Hit;
using Characters.Player.StateMachine;
using UnityEngine;

namespace Characters.Player.StateMachine.States.Block
{
    // 방어 입력이 유지되는 동안 방패 걷기 Blend Tree와 방어 상태를 유지한다.
    internal sealed class PlayerBlockState : IPlayerState
    {
        private readonly PlayerStateMachine stateMachine; // 현재 행동 상태
        private readonly PlayerAnimationController animationController; // 씬 또는 시스템 참조
        private readonly PlayerGuardHitBox guardHitBox;
        private readonly float guardRaiseDuration;
        private float guardRaiseElapsedTime;

        internal bool IsGuardReady { get; private set; }

        public PlayerBlockState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController,
            PlayerGuardHitBox guardHitBox,
            float guardRaiseDuration)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
            this.guardHitBox = guardHitBox;
            this.guardRaiseDuration = Mathf.Max(0f, guardRaiseDuration);
        }

        public void Enter()
        {
            guardRaiseElapsedTime = 0f;
            IsGuardReady = guardRaiseDuration <= 0f;
            animationController.StopMove();
            stateMachine.SetAttackDirection(true);
            animationController.SetBlocking(true);
            guardHitBox?.SetGuardActive(IsGuardReady);
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            UpdateGuardReady(deltaTime);

            bool canGuardMove =
                IsGuardReady &&
                animationController.IsPlayingBlockIdle();
            if (canGuardMove)
            {
                stateMachine.UpdateAttackDirection();
                stateMachine.UpdateAttackTurn(deltaTime);
                stateMachine.Movement.UpdateGuardMove(deltaTime);
            }
            else
            {
                stateMachine.Movement.UpdateStoppedMove(deltaTime);
            }

            animationController.UpdateBlockMove(stateMachine.Movement.GetLocalMoveInput(), deltaTime);
            guardHitBox?.SetGuardActive(IsGuardReady);
        }

        public void Exit()
        {
            guardRaiseElapsedTime = 0f;
            IsGuardReady = false;
            animationController.StopMove();
            animationController.SetBlocking(false);
            guardHitBox?.SetGuardActive(false);
            stateMachine.ClearAttackDirection();
        }

        private void UpdateGuardReady(float deltaTime)
        {
            if (IsGuardReady)
            {
                return;
            }

            guardRaiseElapsedTime += Mathf.Max(0f, deltaTime);
            IsGuardReady = guardRaiseElapsedTime >= guardRaiseDuration;
        }
    }
}
