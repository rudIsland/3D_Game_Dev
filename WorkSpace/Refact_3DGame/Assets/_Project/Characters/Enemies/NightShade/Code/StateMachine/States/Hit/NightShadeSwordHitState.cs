using Characters.Combat;
using UnityEngine;

namespace Characters.Enemies.NightShade
{
    internal enum NightShadeHitStep
    {
        Reaction = 0,
        StayDown = 1,
        GetUp = 2,
        StaggerEnter = 3,
        StaggerStart = 4,
        StaggerIdle = 5,
        StaggerEnd = 6
    }

    // 경직 애니메이션 동안 피격 밀림 곡선을 누적 적용한다.
    internal sealed class NightShadeSwordHitState : INightShadeSwordState
    {
        private readonly NightShadeSwordBehaviorContext context;
        private readonly NightShadeSwordHitReactionRuntimeConfig settings;

        private EnemyHitRequest hitRequest;
        private HitReaction reaction;
        private NightShadeHitStep hitStep;
        private float elapsedPushTime;
        private float previousPushProgress;
        private float remainingDownTime;
        private float elapsedReactionTime;
        private float pushDistance;

        internal NightShadeSwordHitState(
            NightShadeSwordBehaviorContext context,
            NightShadeSwordHitReactionRuntimeConfig settings)
        {
            this.context = context;
            this.settings = settings;
        }

        internal void SetHitRequest(
            HitReaction nextReaction,
            in EnemyHitRequest nextHitRequest)
        {
            reaction = nextReaction;
            hitRequest = nextHitRequest;
            pushDistance = HitPushDistance.GetDistance(
                hitRequest.PushDistance,
                reaction);
        }

        public void Enter()
        {
            StartReaction();
        }

        internal bool TryRestart(
            HitReaction nextReaction,
            in EnemyHitRequest nextHitRequest)
        {
            if (reaction == HitReaction.StaggerBreak ||
                !HitReactionPlayback.CanStart(
                    reaction,
                    nextReaction,
                    elapsedReactionTime))
            {
                return false;
            }

            SetHitRequest(nextReaction, in nextHitRequest);
            StartReaction();
            return true;
        }

        private void StartReaction()
        {
            elapsedPushTime = 0f;
            previousPushProgress = settings.EvaluatePushProgress(0f);
            remainingDownTime = 0f;
            elapsedReactionTime = 0f;
            hitStep = reaction == HitReaction.StaggerBreak
                ? NightShadeHitStep.StaggerEnter
                : NightShadeHitStep.Reaction;
            context.Animation.ResetAttackPlaybackSpeed();
            PlayReactionAnimation();
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            elapsedReactionTime += safeDeltaTime;
            ApplyHitMovement(deltaTime);
            if (hitStep == NightShadeHitStep.StaggerIdle)
            {
                remainingDownTime -= safeDeltaTime;
                if (remainingDownTime > 0f)
                {
                    return null;
                }

                hitStep = NightShadeHitStep.StaggerEnd;
                context.Animation.PlayStaggerEndFromStart();
                return null;
            }

            if (hitStep == NightShadeHitStep.StayDown)
            {
                remainingDownTime -= safeDeltaTime;
                if (remainingDownTime > 0f)
                {
                    return null;
                }

                hitStep = NightShadeHitStep.GetUp;
                context.Animation.PlayGetUpFromStart();
                return null;
            }

            if (!context.Animation.TryGetRequestedAnimationTime(out float normalizedTime) ||
                context.Animation.IsTransitioning())
            {
                return null;
            }

            if (normalizedTime < 1f)
            {
                return null;
            }

            if (hitStep == NightShadeHitStep.StaggerEnter)
            {
                hitStep = NightShadeHitStep.StaggerStart;
                context.Animation.PlayStaggerStartFromStart();
                return null;
            }

            if (hitStep == NightShadeHitStep.StaggerStart)
            {
                hitStep = NightShadeHitStep.StaggerIdle;
                remainingDownTime = settings.StaggerBreakStayDuration;
                context.Animation.PlayStaggerIdleFromStart();
                return null;
            }

            if (reaction == HitReaction.Knockdown &&
                hitStep == NightShadeHitStep.Reaction)
            {
                hitStep = NightShadeHitStep.StayDown;
                remainingDownTime = settings.KnockdownStayDuration;
                return null;
            }

            return context.TargetStatus.IsDetected
                ? NightShadeSwordStateId.Combat
                : NightShadeSwordStateId.Idle;
        }

        public void Exit()
        {
        }

        private void PlayReactionAnimation()
        {
            switch (reaction)
            {
                case HitReaction.SmallHit:
                    context.Animation.PlaySmallHitFromStart(
                        hitRequest.HitDirection);
                    break;
                case HitReaction.BigHit:
                    context.Animation.PlayBigHitFromStart(
                        hitRequest.HitDirection);
                    break;
                case HitReaction.Knockback:
                    context.Animation.PlayKnockbackFromStart();
                    break;
                case HitReaction.Knockdown:
                    context.Animation.PlayKnockdownFromStart();
                    break;
                case HitReaction.StaggerBreak:
                    context.Animation.PlayStaggerEnterFromStart();
                    break;
            }
        }

        private void ApplyHitMovement(float deltaTime)
        {
            float pushDuration =
                settings.GetPushDuration(reaction);
            elapsedPushTime = Mathf.Min(
                elapsedPushTime + Mathf.Max(0f, deltaTime), pushDuration);
            float pushProgress = Mathf.Max(
                previousPushProgress,
                settings.EvaluatePushProgress(elapsedPushTime / pushDuration));
            float deltaProgress = pushProgress - previousPushProgress;
            Vector3 movementAmount = hitRequest.PushDirection *
                (pushDistance * deltaProgress);

            previousPushProgress = pushProgress;
            context.Movement.ApplyHitMovement(movementAmount, deltaTime);
        }
    }
}
