using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 경직 애니메이션 동안 피격 밀림 곡선을 누적 적용한다.
    internal sealed class NightShadeSwordHitState : INightShadeSwordState
    {
        private readonly NightShadeSwordTargetReader targetReader;
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;
        private readonly NightShadeSwordSettings settings;
        private readonly NightShadeSwordFightMemory fightMemory;

        private EnemyHitRequest hitRequest;
        private float elapsedPushTime;
        private float previousPushProgress;

        internal NightShadeSwordHitState(
            NightShadeSwordTargetReader targetReader,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings,
            NightShadeSwordFightMemory fightMemory)
        {
            this.targetReader = targetReader;
            this.movement = movement;
            this.animation = animation;
            this.settings = settings;
            this.fightMemory = fightMemory;
        }

        internal void SetHitRequest(in EnemyHitRequest nextHitRequest)
        {
            hitRequest = nextHitRequest;
        }

        public void Enter()
        {
            fightMemory.CancelComboSecond();
            fightMemory.ResetCompletedAttackCount();
            elapsedPushTime = 0f;
            previousPushProgress = settings.EvaluateHitPushProgress(0f);
            animation.ResetAttackPlaybackSpeed();
            animation.PlayHitFromStart();
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            ApplyHitMovement(deltaTime);
            if (!animation.TryGetRequestedAnimationTime(out float normalizedTime) ||
                animation.IsTransitioning() ||
                normalizedTime < 1f)
            {
                return null;
            }

            return targetReader.IsFound(settings.FindRangeSquared)
                ? settings.GetApproachState(targetReader.DistanceSquared)
                : NightShadeSwordStateId.Idle;
        }

        public void Exit()
        {
        }

        private void ApplyHitMovement(float deltaTime)
        {
            elapsedPushTime = Mathf.Min(elapsedPushTime + Mathf.Max(0f, deltaTime), settings.HitPushDuration);
            float pushProgress = Mathf.Max(previousPushProgress, settings.EvaluateHitPushProgress(elapsedPushTime / settings.HitPushDuration));
            float deltaProgress = pushProgress - previousPushProgress;
            Vector3 movementAmount = hitRequest.PushDirection *
                (hitRequest.PushDistance * deltaProgress);

            previousPushProgress = pushProgress;
            movement.ApplyHitMovement(movementAmount, deltaTime);
        }
    }
}
