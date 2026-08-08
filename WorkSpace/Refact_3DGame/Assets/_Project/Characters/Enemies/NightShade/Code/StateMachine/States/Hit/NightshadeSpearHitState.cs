
namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 방향별 피격 애니메이션을 재생하고, 종료 후 추적으로 복귀한다.
    internal sealed class NightshadeSpearHitState : INightshadeSpearState
    {
        private readonly NightshadeSpearStateMachine stateMachine;
        private bool hasEnteredHit;

        public string Name => nameof(NightshadeSpearHitState);

        internal NightshadeSpearHitState(
            NightshadeSpearStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        internal void Restart()
        {
            stateMachine.Animation.SetMovement(0f, 0f);
            hasEnteredHit = false;
            stateMachine.Animation.PlayHit(NightshadeSpearHitDirection.Forward);
        }

        public void Enter()
        {
            Restart();
        }

        public void Update(float deltaTime)
        {
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
            stateMachine.Animation.ResetActionSpeed();
        }

    }
}
