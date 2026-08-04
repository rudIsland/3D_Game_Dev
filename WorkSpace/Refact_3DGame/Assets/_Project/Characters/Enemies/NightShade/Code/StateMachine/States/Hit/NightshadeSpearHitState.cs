using rudIsland.RPG3D.Combat;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 방향별 피격 애니메이션을 재생하고, 종료 후 추적으로 복귀한다.
    internal sealed class NightshadeSpearHitState : INightshadeSpearState
    {
        private readonly NightshadeSpearStateMachine stateMachine;
        private bool hasEnteredHit;
        private HitReaction hitReaction;

        public string Name => nameof(NightshadeSpearHitState);

        internal NightshadeSpearHitState(
            NightshadeSpearStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        internal void SetHitReaction(in HitReaction nextHitReaction)
        {
            hitReaction = nextHitReaction;
        }

        internal void Restart()
        {
            stateMachine.Animation.SetMovement(0f, 0f);
            hasEnteredHit = false;
            stateMachine.StartHitPush(
                hitReaction.PushDirection,
                hitReaction.PushDistance);
            stateMachine.Animation.PlayHit(
                GetHitDirection(hitReaction.Direction));
        }

        public void Enter()
        {
            Restart();
        }

        public void Update(float deltaTime)
        {
            stateMachine.UpdateHitPush(deltaTime);
            bool hasActionTime = stateMachine.TryGetCurrentActionTime(
                out float normalizedTime);
            if (hasActionTime)
            {
                hasEnteredHit = true;
            }

            if (hasEnteredHit &&
                !stateMachine.IsActionTransitioning() &&
                hasActionTime &&
                normalizedTime >= 1f)
            {
                stateMachine.ChangeToAliveState();
            }
        }

        public void Exit()
        {
            stateMachine.StopHitPush();
            stateMachine.Animation.ResetActionSpeed();
        }

        private static NightshadeSpearHitDirection GetHitDirection(
            HitReactionDirection direction)
        {
            switch (direction)
            {
                case HitReactionDirection.Back:
                    return NightshadeSpearHitDirection.Backward;
                case HitReactionDirection.Left:
                    return NightshadeSpearHitDirection.Left;
                case HitReactionDirection.Right:
                    return NightshadeSpearHitDirection.Right;
                default:
                    return NightshadeSpearHitDirection.Forward;
            }
        }
    }
}
