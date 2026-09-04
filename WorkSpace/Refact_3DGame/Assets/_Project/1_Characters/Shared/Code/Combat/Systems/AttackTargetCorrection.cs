using UnityEngine;

namespace Characters.Combat
{
    // 공격 시작 때 확정한 거리와 시작 방향을 기준으로 제한 회전과 전진량을 계산한다.
    internal sealed class AttackTargetCorrection
    {
        private const float MinimumDirectionSquared = 0.000001f;
        private const float MinimumCurveRange = 0.000001f;

        private AnimationCurve movementCurve;
        private Vector3 startDirection;
        private Vector3 turnDirection;
        private float maximumTurnAngle;
        private float plannedMoveDistance;
        private float curveStart;
        private float curveRange;
        private float previousProgress;
        private bool isActive;
        private bool hasTarget;

        internal bool IsActive => isActive;
        internal bool HasTarget => hasTarget;
        internal float PlannedMoveDistance => plannedMoveDistance;
        internal Vector3 TurnDirection => turnDirection;

        internal void Begin(
            Vector3 attackerPosition,
            Vector3 attackerForward,
            bool hasAttackTarget,
            Vector3 targetPosition,
            float baseMoveDistance,
            float targetStopDistance,
            float maximumAddedMoveDistance,
            float maximumTurnAngle,
            AnimationCurve movementCurve)
        {
            Reset();

            startDirection = GetFlatDirection(attackerForward);
            if (startDirection.sqrMagnitude <= MinimumDirectionSquared)
            {
                startDirection = Vector3.forward;
            }

            turnDirection = startDirection;
            this.maximumTurnAngle = Mathf.Clamp(maximumTurnAngle, 0f, 180f);
            this.movementCurve = movementCurve;
            hasTarget = hasAttackTarget;
            isActive = true;

            float safeBaseMoveDistance = Mathf.Max(0f, baseMoveDistance);
            if (hasTarget)
            {
                Vector3 targetOffset = targetPosition - attackerPosition;
                targetOffset.y = 0f;
                float availableMoveDistance = Mathf.Max(
                    0f,
                    targetOffset.magnitude - Mathf.Max(0f, targetStopDistance));
                plannedMoveDistance = Mathf.Min(
                    safeBaseMoveDistance + Mathf.Max(0f, maximumAddedMoveDistance),
                    availableMoveDistance);
                UpdateTargetDirection(attackerPosition, targetPosition);
            }
            else
            {
                plannedMoveDistance = safeBaseMoveDistance;
            }

            if (this.movementCurve == null || this.movementCurve.length < 2)
            {
                curveStart = 0f;
                curveRange = 0f;
            }
            else
            {
                curveStart = this.movementCurve.Evaluate(0f);
                curveRange = this.movementCurve.Evaluate(1f) - curveStart;
            }

            previousProgress = EvaluateProgress(0f);
        }

        internal void UpdateTargetDirection(
            Vector3 attackerPosition,
            Vector3 targetPosition)
        {
            if (!isActive || !hasTarget)
            {
                return;
            }

            Vector3 wantedDirection = GetFlatDirection(
                targetPosition - attackerPosition);
            if (wantedDirection.sqrMagnitude <= MinimumDirectionSquared)
            {
                return;
            }

            float signedAngle = Vector3.SignedAngle(
                startDirection,
                wantedDirection,
                Vector3.up);
            float limitedAngle = Mathf.Clamp(
                signedAngle,
                -maximumTurnAngle,
                maximumTurnAngle);
            turnDirection = Quaternion.AngleAxis(
                limitedAngle,
                Vector3.up) * startDirection;
        }

        internal float EvaluateDeltaDistance(float normalizedTime)
        {
            if (!isActive)
            {
                return 0f;
            }

            float progress = EvaluateProgress(normalizedTime);
            float deltaDistance =
                (progress - previousProgress) * plannedMoveDistance;
            previousProgress = progress;
            return deltaDistance;
        }

        internal void Reset()
        {
            movementCurve = null;
            startDirection = Vector3.zero;
            turnDirection = Vector3.zero;
            maximumTurnAngle = 0f;
            plannedMoveDistance = 0f;
            curveStart = 0f;
            curveRange = 0f;
            previousProgress = 0f;
            isActive = false;
            hasTarget = false;
        }

        private float EvaluateProgress(float normalizedTime)
        {
            float safeNormalizedTime = Mathf.Clamp01(normalizedTime);
            if (movementCurve == null ||
                movementCurve.length < 2 ||
                Mathf.Abs(curveRange) <= MinimumCurveRange)
            {
                return safeNormalizedTime;
            }

            return (movementCurve.Evaluate(safeNormalizedTime) - curveStart) /
                curveRange;
        }

        private static Vector3 GetFlatDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > MinimumDirectionSquared)
            {
                direction.Normalize();
            }

            return direction;
        }
    }
}
