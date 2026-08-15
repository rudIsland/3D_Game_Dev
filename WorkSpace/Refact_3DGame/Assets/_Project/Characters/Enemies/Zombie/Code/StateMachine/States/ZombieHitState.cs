using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 피격 중에는 공격 방향으로 밀리며 Hit 애니메이션 종료를 기다린다.
    internal sealed class ZombieHitState : IZombieState
    {
        private readonly ZombieStateMachine stateMachine;

        private EnemyHitRequest hitRequest;
        private float elapsedPushTime;
        private float previousPushProgress;

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
            ApplyHitMovement(deltaTime);

            if (stateMachine.TryGetCurrentAnimationTime(out float normalizedTime)
                && !stateMachine.IsAnimationTransitioning()
                && normalizedTime >= 1f)
            {
                stateMachine.ChangeToAliveState();
            }
        }

        public void Exit()
        {
            elapsedPushTime = 0f;
            previousPushProgress = 0f;
        }

        public void Restart()
        {
            elapsedPushTime = 0f;
            previousPushProgress =
                stateMachine.EvaluateHitPushProgress(0f);
            stateMachine.PlayHitFromStart();
        }

        internal void SetHitRequest( in EnemyHitRequest nextHitRequest)
        {
            hitRequest = nextHitRequest;
        }

        private void ApplyHitMovement(float deltaTime)
        {
            elapsedPushTime = Mathf.Min(
                elapsedPushTime + Mathf.Max(0f, deltaTime),
                stateMachine.HitPushDuration);
            float normalizedTime =
                elapsedPushTime / stateMachine.HitPushDuration;
            float pushProgress = Mathf.Max(
                previousPushProgress,
                stateMachine.EvaluateHitPushProgress(normalizedTime));
            float deltaProgress =
                pushProgress - previousPushProgress;
            Vector3 horizontalMovement =
                hitRequest.PushDirection *
                (hitRequest.PushDistance * deltaProgress);

            previousPushProgress = pushProgress;
            stateMachine.ApplyHitMovement(
                horizontalMovement,
                deltaTime);
        }
    }
}
