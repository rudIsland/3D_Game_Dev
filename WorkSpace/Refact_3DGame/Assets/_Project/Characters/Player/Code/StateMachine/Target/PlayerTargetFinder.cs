using rudIsland.RPG3D.Characters;
using UnityEngine;

namespace rudIsland.RPG3D.Player.States.Target
{
    // 카메라 중심에 가까운 공격 가능 대상을 우선해 찾고 시야를 검사한다.
    public sealed class PlayerTargetFinder
    {
        private const int MaximumDetectedTargetCount = 32;
        private const float MinimumDirectionSqrMagnitude = 0.01f;

        private readonly Collider[] detectedTargets = new Collider[MaximumDetectedTargetCount];
        private readonly Transform playerTransform;
        private readonly Transform viewTransform;
        private readonly LayerMask targetLayers;
        private readonly LayerMask obstructionLayers;
        private readonly float targetRange;
        private readonly float targetHeightOffset;
        private readonly float minimumFacingDot;
        private Transform selectedTarget;
        private IUnitDeathState selectedTargetDeathState;

        public PlayerTargetFinder(
            Transform playerTransform,
            Transform viewTransform,
            LayerMask targetLayers,
            float targetRange,
            float maximumTargetAngle,
            LayerMask obstructionLayers,
            float targetHeightOffset)
        {
            this.playerTransform = playerTransform;
            this.viewTransform = viewTransform;
            this.targetLayers = targetLayers;
            this.obstructionLayers = obstructionLayers;
            this.targetRange = Mathf.Max(0f, targetRange);
            this.targetHeightOffset = Mathf.Max(0f, targetHeightOffset);
            minimumFacingDot = Mathf.Cos(Mathf.Clamp(maximumTargetAngle, 0f, 180f) * Mathf.Deg2Rad);
        }

        public bool TryFindTarget(out Transform target)
        {
            target = null;
            selectedTarget = null;
            selectedTargetDeathState = null;
            if (targetRange <= 0f || targetLayers.value == 0)
            {
                return false;
            }

            int detectedCount = Physics.OverlapSphereNonAlloc(
                playerTransform.position,
                targetRange,
                detectedTargets,
                targetLayers.value,
                QueryTriggerInteraction.Collide);

            Vector3 viewForward = viewTransform.forward;
            if (viewForward.sqrMagnitude < MinimumDirectionSqrMagnitude)
            {
                viewForward = playerTransform.forward;
            }
            else
            {
                viewForward.Normalize();
            }

            float bestScore = float.PositiveInfinity;
            for (int index = 0; index < detectedCount; index++)
            {
                if (!TryGetTargetTransform(
                        detectedTargets[index],
                        out Transform candidate,
                        out IUnitDeathState candidateDeathState) ||
                    candidateDeathState?.IsDead == true)
                {
                    continue;
                }

                Vector3 playerToCandidate = candidate.position - playerTransform.position;
                playerToCandidate.y = 0f;
                float distanceSquared = playerToCandidate.sqrMagnitude;
                if (distanceSquared < MinimumDirectionSqrMagnitude ||
                    !IsTargetVisible(candidate))
                {
                    continue;
                }

                Vector3 viewToCandidate = GetTargetPoint(candidate) - viewTransform.position;
                if (viewToCandidate.sqrMagnitude <
                    MinimumDirectionSqrMagnitude)
                {
                    continue;
                }

                float facingDot = Vector3.Dot(
                    viewForward,
                    viewToCandidate.normalized);
                if (facingDot < minimumFacingDot)
                {
                    continue;
                }

                float score = CalculateTargetScore(
                    facingDot,
                    minimumFacingDot,
                    Mathf.Sqrt(distanceSquared),
                    targetRange);
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                target = candidate;
                selectedTarget = candidate;
                selectedTargetDeathState = candidateDeathState;
            }

            return target != null;
        }

        public bool IsTargetAliveAndInRange(
            Transform target,
            float maximumDistance)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (target == selectedTarget &&
                selectedTargetDeathState?.IsDead == true)
            {
                return false;
            }

            Vector3 difference = target.position - playerTransform.position;
            difference.y = 0f;
            float clampedMaximumDistance = Mathf.Max(0f, maximumDistance);
            return difference.sqrMagnitude <=
                clampedMaximumDistance * clampedMaximumDistance;
        }

        public bool IsTargetVisible(Transform target)
        {
            if (target == null || obstructionLayers.value == 0)
            {
                return target != null;
            }

            Vector3 start = viewTransform.position;
            Vector3 toTarget = GetTargetPoint(target) - start;
            float distance = toTarget.magnitude;
            if (distance <= 0.0001f ||
                !Physics.Raycast(
                    start,
                    toTarget / distance,
                    out RaycastHit hit,
                    distance,
                    obstructionLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            Transform hitTransform = hit.collider.transform;
            return hitTransform == target ||
                hitTransform.IsChildOf(target) ||
                target.IsChildOf(hitTransform);
        }

        internal static float CalculateTargetScore(
            float facingDot,
            float minimumFacingDot,
            float distance,
            float maximumDistance)
        {
            float angleRange = Mathf.Max(
                0.0001f,
                1f - minimumFacingDot);
            float angleScore = Mathf.Clamp01(
                (1f - facingDot) / angleRange);
            float distanceScore = maximumDistance > 0f
                ? Mathf.Clamp01(distance / maximumDistance)
                : 1f;
            return angleScore * 0.8f + distanceScore * 0.2f;
        }

        private Vector3 GetTargetPoint(Transform target)
        {
            return target.position + Vector3.up * targetHeightOffset;
        }

        private bool TryGetTargetTransform(
            Collider targetCollider,
            out Transform target,
            out IUnitDeathState targetDeathState)
        {
            target = null;
            targetDeathState = null;
            if (targetCollider == null)
            {
                return false;
            }

            IUnitDeathState deathState = targetCollider.GetComponentInParent<IUnitDeathState>();
            Component deathStateComponent = deathState as Component;
            if (deathStateComponent == null ||
                deathStateComponent.transform == playerTransform ||
                !deathStateComponent.gameObject.activeInHierarchy)
            {
                return false;
            }

            target = deathStateComponent.transform;
            targetDeathState = deathState;
            return true;
        }
    }
}
