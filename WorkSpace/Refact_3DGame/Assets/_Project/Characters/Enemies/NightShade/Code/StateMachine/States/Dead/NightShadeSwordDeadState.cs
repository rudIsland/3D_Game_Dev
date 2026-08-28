using UnityEngine;

namespace Characters.Enemies.NightShade
{
    // 사망 애니메이션과 시체 유지 시간이 끝나면 풀 반환을 한 번 요청한다.
    internal sealed class NightShadeSwordDeadState : INightShadeSwordState
    {
        private readonly NightShadeSwordBehaviorContext context;
        private readonly NightShadeSwordLifeRuntimeConfig settings;
        private readonly NightShadeSwordCombatOutput combatOutput;

        private bool isAnimationFinished;
        private bool isReleaseRequested;
        private float remainingBodyKeepTime;

        internal NightShadeSwordDeadState(
            NightShadeSwordBehaviorContext context,
            NightShadeSwordLifeRuntimeConfig settings,
            NightShadeSwordCombatOutput combatOutput)
        {
            this.context = context;
            this.settings = settings;
            this.combatOutput = combatOutput;
        }

        public void Enter()
        {
            isAnimationFinished = false;
            isReleaseRequested = false;
            remainingBodyKeepTime = 0f;
            context.Animation.ResetAttackPlaybackSpeed();
            context.Animation.PlayDead();
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            context.Movement.StayOnGround(deltaTime);
            if (!isAnimationFinished)
            {
                if (!context.Animation.TryGetRequestedAnimationTime(out float normalizedTime) ||
                    context.Animation.IsTransitioning() ||
                    normalizedTime < 1f)
                {
                    return null;
                }

                isAnimationFinished = true;
                remainingBodyKeepTime = settings.DeadBodyKeepTime;
            }

            remainingBodyKeepTime -= deltaTime;
            if (!isReleaseRequested && remainingBodyKeepTime <= 0f)
            {
                isReleaseRequested = true;
                combatOutput.RequestRelease();
            }

            return null;
        }

        public void Exit()
        {
        }
    }
}
