using rudIsland.RPG3D.Combat;
using rudIsland.RPG3D.Player.Animations;

namespace rudIsland.RPG3D.Player.States.Hit
{
    // 피격 중에는 조작을 무시하고 공격 방향으로 밀린다.
    internal sealed class PlayerHitState : IPlayerState
    {
        private const float ControlReturnNormalizedTime = 0.9f;

        private readonly PlayerStateMachine stateMachine;
        private readonly PlayerAnimationController animationController;

        private HitReaction hitReaction;

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
                stateMachine.ChangeToLookState();
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
