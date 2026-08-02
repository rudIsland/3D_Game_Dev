using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Combat;

namespace rudIsland.RPG3D.Player.States
{
    // 피격 중에는 조작을 무시하고 공격 방향으로 밀린다.
    internal sealed class PlayerHitState : IPlayerState
    {
        private const float ControlReturnNormalizedTime = 0.9f; // 시간 설정

        private readonly PlayerStateMachine stateMachine; // 현재 행동 상태
        private readonly PlayerAnimationController animationController; // 씬 또는 시스템 참조

        private HitReaction hitReaction; // 이번 피격의 방향, 세기와 신체 부위

        public PlayerHitState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
        }

        public void Enter()
        {
            Restart();
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            stateMachine.Movement.UpdateHitPush(deltaTime);
            animationController.StopMove();

            if (animationController.TryGetHitTime(
                    out float normalizedTime) &&
                normalizedTime >= ControlReturnNormalizedTime)
            {
                stateMachine.ChangeToControlState();
            }
        }

        public void Exit()
        {
            stateMachine.Movement.StopHitPush();
        }

        internal void Restart()
        {
            stateMachine.EndAttackHit();
            stateMachine.Movement.StartHitPush(
                hitReaction.PushDirection,
                hitReaction.PushDistance);
            animationController.PlayHitFromStart();
        }

        internal void SetHitReaction(in HitReaction reaction)
        {
            hitReaction = reaction;
        }
    }
}
