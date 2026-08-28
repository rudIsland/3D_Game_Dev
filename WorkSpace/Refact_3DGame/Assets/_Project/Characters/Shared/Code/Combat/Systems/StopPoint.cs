using UnityEngine;

namespace Characters.Combat
{
    // 행동 중단 피해 누적, 한계 판정과 지연 회복만 관리한다.
    internal sealed class StopPoint
    {
        private readonly float maxPoint;
        private readonly float recoverDelay;
        private readonly float recoverSpeed;

        private float currentPoint;
        private float recoverElapsedTime;

        internal float CurrentPoint => currentPoint;
        internal float MaxPoint => maxPoint;

        internal StopPoint(
            float maxPoint,
            float recoverDelay,
            float recoverSpeed)
        {
            this.maxPoint = Mathf.Max(1f, maxPoint);
            this.recoverDelay = Mathf.Max(0f, recoverDelay);
            this.recoverSpeed = Mathf.Max(0f, recoverSpeed);
        }

        internal bool TryAccumulate(float stopDamage)
        {
            stopDamage = Mathf.Max(0f, stopDamage);
            if (stopDamage <= 0f)
            {
                return false;
            }

            recoverElapsedTime = 0f;
            currentPoint = Mathf.Min(maxPoint, currentPoint + stopDamage);
            if (currentPoint < maxPoint)
            {
                return false;
            }

            currentPoint = 0f;
            return true;
        }

        internal bool UpdateRecovery(float deltaTime)
        {
            if (deltaTime <= 0f || currentPoint <= 0f)
            {
                return false;
            }

            recoverElapsedTime += deltaTime;
            if (recoverElapsedTime < recoverDelay)
            {
                return false;
            }

            float pointBeforeRecovery = currentPoint;
            currentPoint = Mathf.Max(
                0f,
                currentPoint - recoverSpeed * deltaTime);
            return currentPoint < pointBeforeRecovery;
        }

        // 호출자가 정한 회복량만큼 경직 누적값을 직접 줄인다.
        internal bool Recover(float amount)
        {
            amount = Mathf.Max(0f, amount);
            if (amount <= 0f || currentPoint <= 0f)
            {
                return false;
            }

            float pointBeforeRecovery = currentPoint;
            currentPoint = Mathf.Max(0f, currentPoint - amount);
            return currentPoint < pointBeforeRecovery;
        }

        internal void Reset()
        {
            currentPoint = 0f;
            recoverElapsedTime = 0f;
        }
    }
}
