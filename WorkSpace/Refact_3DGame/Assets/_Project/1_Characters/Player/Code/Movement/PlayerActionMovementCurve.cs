using UnityEngine;

namespace Characters.Player.Movement
{
    // 동작의 정규화 시간과 누적 Curve를 프레임 이동 거리로 변환한다.
    internal sealed class PlayerActionMovementCurve
    {
        private AnimationCurve movementCurve;
        private float movementDistance;
        private float curveStart;
        private float curveRange;
        private float previousProgress;

        public void Begin(float distance, AnimationCurve curve)
        {
            movementDistance = Mathf.Max(0f, distance);
            movementCurve = curve;

            if (movementCurve == null || movementCurve.length < 2)
            {
                curveStart = 0f;
                curveRange = 0f;
                previousProgress = 0f;
                return;
            }

            curveStart = movementCurve.Evaluate(0f);
            curveRange = movementCurve.Evaluate(1f) - curveStart;
            previousProgress = EvaluateProgress(0f);
        }

        public float EvaluateDeltaDistance(float normalizedTime)
        {
            float progress = EvaluateProgress(normalizedTime);
            float deltaDistance = (progress - previousProgress) * movementDistance;
            previousProgress = progress;
            return deltaDistance;
        }

        public void Reset()
        {
            movementDistance = 0f;
            movementCurve = null;
            curveStart = 0f;
            curveRange = 0f;
            previousProgress = 0f;
        }

        private float EvaluateProgress(float normalizedTime)
        {
            normalizedTime = Mathf.Clamp01(normalizedTime);
            if (movementCurve == null ||
                movementCurve.length < 2 ||
                Mathf.Abs(curveRange) <= 0.000001f)
            {
                return normalizedTime;
            }

            return (movementCurve.Evaluate(normalizedTime) - curveStart) /
                curveRange;
        }
    }
}
