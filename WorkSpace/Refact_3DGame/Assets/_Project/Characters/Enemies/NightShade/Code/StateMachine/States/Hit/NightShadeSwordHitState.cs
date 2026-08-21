using rudIsland.RPG3D.Characters.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
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
        private readonly NightShadeSwordSituationReader situation;
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;
        private readonly NightShadeSwordSettings settings;
        private readonly NightShadeSwordFightMemory fightMemory;

        private EnemyHitRequest hitRequest;
        private HitReaction reaction;
        private NightShadeHitStep hitStep;
        private float elapsedPushTime;
        private float previousPushProgress;
        private float remainingDownTime;
        private float elapsedReactionTime;
        private float pushDistance;

        internal NightShadeSwordHitState(
            NightShadeSwordSituationReader situation,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings,
            NightShadeSwordFightMemory fightMemory)
        {
            this.situation = situation;
            this.movement = movement;
            this.animation = animation;
            this.settings = settings;
            this.fightMemory = fightMemory;
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
            fightMemory.ClearCombo();
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
            previousPushProgress = settings.EvaluateHitPushProgress(0f);
            remainingDownTime = 0f;
            elapsedReactionTime = 0f;
            hitStep = reaction == HitReaction.StaggerBreak
                ? NightShadeHitStep.StaggerEnter
                : NightShadeHitStep.Reaction;
            animation.ResetAttackPlaybackSpeed();
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
                animation.PlayStaggerEndFromStart();
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
                animation.PlayGetUpFromStart();
                return null;
            }

            if (!animation.TryGetRequestedAnimationTime(out float normalizedTime) ||
                animation.IsTransitioning())
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
                animation.PlayStaggerStartFromStart();
                return null;
            }

            if (hitStep == NightShadeHitStep.StaggerStart)
            {
                hitStep = NightShadeHitStep.StaggerIdle;
                remainingDownTime = settings.StaggerBreakStayDuration;
                animation.PlayStaggerIdleFromStart();
                return null;
            }

            if (reaction == HitReaction.Knockdown &&
                hitStep == NightShadeHitStep.Reaction)
            {
                hitStep = NightShadeHitStep.StayDown;
                remainingDownTime = settings.KnockdownStayDuration;
                return null;
            }

            return situation.IsTargetDetected
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
                    animation.PlaySmallHitFromStart(
                        hitRequest.HitDirection);
                    break;
                case HitReaction.BigHit:
                    animation.PlayBigHitFromStart(
                        hitRequest.HitDirection);
                    break;
                case HitReaction.Knockback:
                    animation.PlayKnockbackFromStart();
                    break;
                case HitReaction.Knockdown:
                    animation.PlayKnockdownFromStart();
                    break;
                case HitReaction.StaggerBreak:
                    animation.PlayStaggerEnterFromStart();
                    break;
            }
        }

        private void ApplyHitMovement(float deltaTime)
        {
            float pushDuration =
                settings.GetHitPushDuration(reaction);
            elapsedPushTime = Mathf.Min(
                elapsedPushTime + Mathf.Max(0f, deltaTime), pushDuration);
            float pushProgress = Mathf.Max(
                previousPushProgress,
                settings.EvaluateHitPushProgress( elapsedPushTime / pushDuration));
            float deltaProgress = pushProgress - previousPushProgress;
            Vector3 movementAmount = hitRequest.PushDirection *
                (pushDistance * deltaProgress);

            previousPushProgress = pushProgress;
            movement.ApplyHitMovement(movementAmount, deltaTime);
        }
    }
}
