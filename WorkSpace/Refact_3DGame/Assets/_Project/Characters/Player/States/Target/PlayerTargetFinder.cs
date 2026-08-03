using rudIsland.RPG3D.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Player.States.Target
{
    // 카메라 전방 범위에서 가장 가까운 공격 가능 대상을 찾는다.
    public sealed class PlayerTargetFinder
    {
        private const int MaximumDetectedTargetCount = 32;
        private const float MinimumDirectionSqrMagnitude = 0.01f;

        private readonly Collider[] detectedTargets =
            new Collider[MaximumDetectedTargetCount];
        private readonly Transform playerTransform;
        private readonly Transform viewTransform;
        private readonly LayerMask targetLayers;
        private readonly float targetRange;
        private readonly float minimumFacingDot;

        public PlayerTargetFinder(
            Transform playerTransform,
            Transform viewTransform,
            LayerMask targetLayers,
            float targetRange,
            float maximumTargetAngle)
        {
            this.playerTransform = playerTransform;
            this.viewTransform = viewTransform;
            this.targetLayers = targetLayers;
            this.targetRange = Mathf.Max(0f, targetRange);
            minimumFacingDot = Mathf.Cos(
                Mathf.Clamp(maximumTargetAngle, 0f, 180f) *
                Mathf.Deg2Rad);
        }

        public bool TryFindTarget(out Transform target)
        {
            target = null;
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
            viewForward.y = 0f;
            if (viewForward.sqrMagnitude < MinimumDirectionSqrMagnitude)
            {
                viewForward = playerTransform.forward;
            }
            else
            {
                viewForward.Normalize();
            }

            float nearestDistanceSquared = float.PositiveInfinity;
            for (int index = 0; index < detectedCount; index++)
            {
                if (!TryGetTargetTransform(
                        detectedTargets[index],
                        out Transform candidate))
                {
                    continue;
                }

                Vector3 toCandidate =
                    candidate.position - playerTransform.position;
                toCandidate.y = 0f;
                float distanceSquared = toCandidate.sqrMagnitude;
                if (distanceSquared < MinimumDirectionSqrMagnitude ||
                    distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                float facingDot = Vector3.Dot(
                    viewForward,
                    toCandidate / Mathf.Sqrt(distanceSquared));
                if (facingDot < minimumFacingDot)
                {
                    continue;
                }

                nearestDistanceSquared = distanceSquared;
                target = candidate;
            }

            return target != null;
        }

        public bool IsTargetAvailable(
            Transform target,
            float maximumDistance)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            Vector3 difference = target.position - playerTransform.position;
            difference.y = 0f;
            float clampedMaximumDistance = Mathf.Max(0f, maximumDistance);
            return difference.sqrMagnitude <=
                clampedMaximumDistance * clampedMaximumDistance;
        }

        private bool TryGetTargetTransform(
            Collider targetCollider,
            out Transform target)
        {
            target = null;
            if (targetCollider == null)
            {
                return false;
            }

            IAttackHitReceiver receiver =
                targetCollider.GetComponentInParent<IAttackHitReceiver>();
            Component receiverComponent = receiver as Component;
            if (receiverComponent == null ||
                receiverComponent.transform == playerTransform ||
                !receiverComponent.gameObject.activeInHierarchy)
            {
                return false;
            }

            target = receiverComponent.transform;
            return true;
        }
    }
}
