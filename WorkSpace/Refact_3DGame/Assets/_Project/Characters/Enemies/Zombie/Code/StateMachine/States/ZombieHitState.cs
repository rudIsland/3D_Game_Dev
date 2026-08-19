using rudIsland.RPG3D.Characters.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 피격 중에는 공격 방향으로 밀리며 Hit 애니메이션 종료를 기다린다.
    internal sealed class ZombieHitState : IZombieState
    {
        private readonly ZombieStateMachine stateMachine;

        private EnemyHitRequest hitRequest;
        private HitReaction reaction;
        private float elapsedPushTime;
        private float previousPushProgress;
        private float elapsedReactionTime;
        private float pushDistance;

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
            elapsedReactionTime += Mathf.Max(0f, deltaTime);
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
            previousPushProgress = stateMachine.EvaluateHitPushProgress(0f);
            elapsedReactionTime = 0f;
            stateMachine.PlayHitFromStart(reaction);
        }

        internal bool TryRestart(
            HitReaction nextReaction,
            in EnemyHitRequest nextHitRequest)
        {
            if (!HitReactionPlayback.CanStart(
                    reaction,
                    nextReaction,
                    elapsedReactionTime))
            {
                return false;
            }

            SetHitRequest(nextReaction, in nextHitRequest);
            Restart();
            return true;
        }

        internal void SetHitRequest(HitReaction nextReaction, in EnemyHitRequest nextHitRequest)
        {
            reaction = nextReaction;
            hitRequest = nextHitRequest;
            pushDistance = HitPushDistance.GetDistance(
                hitRequest.PushDistance,
                reaction);
        }

        private void ApplyHitMovement(float deltaTime)
        {
            float pushDuration = stateMachine.GetHitPushDuration(reaction);
            elapsedPushTime = Mathf.Min(elapsedPushTime + Mathf.Max(0f, deltaTime), pushDuration);
            float normalizedTime = elapsedPushTime / pushDuration;
            float pushProgress = Mathf.Max(previousPushProgress, stateMachine.EvaluateHitPushProgress(normalizedTime));
            float deltaProgress = pushProgress - previousPushProgress;
            Vector3 horizontalMovement = hitRequest.PushDirection *
                (pushDistance * deltaProgress);

            previousPushProgress = pushProgress;
            stateMachine.ApplyHitMovement(horizontalMovement, deltaTime);
        }
    }
}
