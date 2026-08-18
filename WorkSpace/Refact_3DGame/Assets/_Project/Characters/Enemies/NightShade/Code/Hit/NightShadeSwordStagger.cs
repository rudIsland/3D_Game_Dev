using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // NightShade가 받은 경직 피해의 누적과 시간 회복을 관리한다.
    internal sealed class NightShadeSwordStagger
    {
        private readonly float staggerLimit;
        private readonly float recoverDelay;
        private readonly float recoverSpeed;

        private float currentStagger;
        private float recoverElapsedTime;

        internal float CurrentStagger => currentStagger;
        internal float StaggerLimit => staggerLimit;

        internal NightShadeSwordStagger(
            float staggerLimit,
            float recoverDelay,
            float recoverSpeed)
        {
            this.staggerLimit = Mathf.Max(1f, staggerLimit);
            this.recoverDelay = Mathf.Max(0f, recoverDelay);
            this.recoverSpeed = Mathf.Max(0f, recoverSpeed);
        }

        internal bool TryAccumulate(float staggerDamage)
        {
            staggerDamage = Mathf.Max(0f, staggerDamage);
            if (staggerDamage <= 0f)
            {
                return false;
            }

            recoverElapsedTime = 0f;
            currentStagger = Mathf.Min(staggerLimit, currentStagger + staggerDamage);
            if (currentStagger < staggerLimit)
            {
                return false;
            }

            currentStagger = 0f;
            return true;
        }

        internal bool UpdateRecovery(float deltaTime)
        {
            if (deltaTime <= 0f || currentStagger <= 0f)
            {
                return false;
            }

            recoverElapsedTime += deltaTime;
            if (recoverElapsedTime < recoverDelay)
            {
                return false;
            }

            float staggerBeforeRecovery = currentStagger;
            currentStagger = Mathf.Max(0f, currentStagger - recoverSpeed * deltaTime);
            return currentStagger < staggerBeforeRecovery;
        }

        internal void Reset()
        {
            currentStagger = 0f;
            recoverElapsedTime = 0f;
        }
    }
}
