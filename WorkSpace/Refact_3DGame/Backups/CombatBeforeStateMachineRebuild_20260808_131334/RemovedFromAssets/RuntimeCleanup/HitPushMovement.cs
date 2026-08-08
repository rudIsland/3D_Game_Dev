using UnityEngine;

namespace rudIsland.RPG3D.Characters
{
    // 피격 방향과 전체 거리를 프레임별 이동량으로 나눈다.
    public sealed class HitPushMovement
    {
        private const float MinimumPushTime = 0.01f; // 시간 설정
        private const float MinimumDirectionLength = 0.0001f; // 이동 정보

        private readonly float pushTime; // 시간 설정

        private Vector3 pushDirection; // 이동 정보
        private float pushSpeed; // 이동 속도
        private float remainingDistance; // 거리 설정

        public bool IsMoving => remainingDistance > 0f; // 기능 사용 여부

        public HitPushMovement(float pushTime)
        {
            this.pushTime =
                pushTime >= MinimumPushTime &&
                !float.IsNaN(pushTime) &&
                !float.IsInfinity(pushTime)
                    ? pushTime
                    : MinimumPushTime;
        }

        public void StartPush(
            Vector3 hitDirection,
            float pushDistance)
        {
            hitDirection.y = 0f;
            if (hitDirection.sqrMagnitude <=
                    MinimumDirectionLength * MinimumDirectionLength ||
                pushDistance <= 0f ||
                float.IsNaN(pushDistance) ||
                float.IsInfinity(pushDistance))
            {
                StopPush();
                return;
            }

            pushDirection = hitDirection.normalized;
            remainingDistance = pushDistance;
            pushSpeed = pushDistance / pushTime;
        }

        // 이번 프레임에 적용할 이동량을 꺼내고 남은 거리를 줄인다.
        public Vector3 GetNextMove(float deltaTime)
        {
            if (!IsMoving ||
                deltaTime <= 0f ||
                float.IsNaN(deltaTime) ||
                float.IsInfinity(deltaTime))
            {
                return Vector3.zero;
            }

            float moveDistance = Mathf.Min(
                remainingDistance,
                pushSpeed * deltaTime);
            remainingDistance -= moveDistance;
            Vector3 nextMove = pushDirection * moveDistance;

            if (remainingDistance <= 0f)
            {
                StopPush();
            }

            return nextMove;
        }

        public void StopPush()
        {
            pushDirection = Vector3.zero;
            pushSpeed = 0f;
            remainingDistance = 0f;
        }
    }
}
