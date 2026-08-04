using rudIsland.RPG3D.Combat;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 피격 중에는 공격 방향으로 밀리며 Hit 애니메이션 종료를 기다린다.
    internal sealed class ZombieHitState : IZombieState
    {
        private readonly ZombieStateMachine stateMachine; // 현재 행동 상태

        private HitReaction hitReaction; // 이번 피격의 방향, 세기와 신체 부위

        public ZombieHitState(ZombieStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            Restart();
        }

        public void Update(float deltaTime)
        {
            stateMachine.UpdateHitPush(deltaTime);

            if (stateMachine.TryGetCurrentAnimationTime(
                    out float normalizedTime) &&
                !stateMachine.IsAnimationTransitioning() &&
                normalizedTime >= 1f)
            {
                stateMachine.ChangeToAliveState();
            }
        }

        public void Exit()
        {
            stateMachine.StopHitPush();
        }

        public void Restart()
        {
            stateMachine.StartHitPush(
                hitReaction.PushDirection,
                hitReaction.PushDistance);
            stateMachine.PlayHitFromStart();
        }

        public void SetHitReaction(in HitReaction reaction)
        {
            hitReaction = reaction;
        }
    }
}
