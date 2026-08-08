using System;
using UnityEngine;

namespace rudIsland.RPG3D.Characters
{
    // Unit 사이 최소 간격을 침범하는 수평 접근 이동만 제한한다.
    public sealed class UnitMovementSeparation
    {
        private const int MaximumQueryResults = 16;
        private const float DirectionThreshold = 0.000001f;
        private const float ContactClearance = 0.001f;

        private readonly CharacterController characterController;
        private readonly Transform ownerTransform;
        private readonly int unitCollisionMask;
        private readonly float minimumSeparation;
        private readonly RaycastHit[] castResults =
            new RaycastHit[MaximumQueryResults];
        private readonly Collider[] overlapResults =
            new Collider[MaximumQueryResults];

        public UnitMovementSeparation(
            CharacterController characterController,
            LayerMask unitCollisionLayers,
            float minimumSeparation)
        {
            this.characterController = characterController != null
                ? characterController
                : throw new ArgumentNullException(
                    nameof(characterController));
            ownerTransform = characterController.transform;
            unitCollisionMask = unitCollisionLayers.value;
            this.minimumSeparation = Mathf.Max(
                0f,
                minimumSeparation);
        }

        public Vector3 LimitApproachMovement(
            Vector3 requestedMovement)
        {
            if (minimumSeparation <= 0f || unitCollisionMask == 0)
            {
                return requestedMovement;
            }

            Vector3 verticalMovement =
                Vector3.up * requestedMovement.y;
            Vector3 horizontalMovement = requestedMovement;
            horizontalMovement.y = 0f;

            if (horizontalMovement.sqrMagnitude <= DirectionThreshold)
            {
                return requestedMovement;
            }

            GetWorldCapsule(
                out Vector3 capsuleTop,
                out Vector3 capsuleBottom,
                out Vector3 capsuleCenter,
                out float capsuleRadius);

            float castRadius =
                capsuleRadius + minimumSeparation;
            float overlapRadius = castRadius +
                GetWorldSkinWidth() + Physics.defaultContactOffset;
            int overlapCount = Physics.OverlapCapsuleNonAlloc(
                capsuleTop,
                capsuleBottom,
                overlapRadius,
                overlapResults,
                unitCollisionMask,
                QueryTriggerInteraction.Ignore);

            horizontalMovement = LimitOverlappingApproach(
                horizontalMovement,
                capsuleCenter,
                capsuleRadius,
                overlapCount);
            if (horizontalMovement.sqrMagnitude <= DirectionThreshold)
            {
                return verticalMovement;
            }

            float horizontalDistance = horizontalMovement.magnitude;
            Vector3 moveDirection =
                horizontalMovement / horizontalDistance;
            int castCount = Physics.CapsuleCastNonAlloc(
                capsuleTop,
                capsuleBottom,
                castRadius,
                moveDirection,
                castResults,
                horizontalDistance,
                unitCollisionMask,
                QueryTriggerInteraction.Ignore);

            Vector3 limitedHorizontalMovement = LimitCastApproach(
                horizontalMovement,
                capsuleCenter,
                capsuleRadius,
                horizontalDistance,
                castCount);
            return limitedHorizontalMovement + verticalMovement;
        }

        private Vector3 LimitOverlappingApproach(
            Vector3 horizontalMovement,
            Vector3 capsuleCenter,
            float capsuleRadius,
            int overlapCount)
        {
            float closestSurfaceDistance = float.PositiveInfinity;
            float closestApproachDistance = 0f;
            int closestColliderId = int.MaxValue;
            Vector3 closestApproachDirection = Vector3.zero;

            for (int index = 0; index < overlapCount; index++)
            {
                Collider otherCollider = overlapResults[index];
                if (IsSelfCollider(otherCollider))
                {
                    continue;
                }

                if (!TryGetDirectionToUnit(
                        otherCollider,
                        capsuleCenter,
                        out Vector3 approachDirection,
                        out float centerDistance))
                {
                    continue;
                }

                float requiredCenterDistance = capsuleRadius +
                    GetHorizontalRadius(otherCollider) +
                    minimumSeparation;
                if (centerDistance >
                    requiredCenterDistance + ContactClearance)
                {
                    continue;
                }

                float approachDistance = Vector3.Dot(
                    horizontalMovement,
                    approachDirection);
                if (approachDistance <= 0f)
                {
                    continue;
                }

                float surfaceDistance =
                    centerDistance - requiredCenterDistance;
                int colliderId = otherCollider.GetInstanceID();
                bool isSameDistance = Mathf.Abs(
                    surfaceDistance - closestSurfaceDistance) <=
                    DirectionThreshold;
                if (surfaceDistance > closestSurfaceDistance ||
                    (isSameDistance &&
                     colliderId >= closestColliderId))
                {
                    continue;
                }

                closestSurfaceDistance = surfaceDistance;
                closestApproachDistance = approachDistance;
                closestColliderId = colliderId;
                closestApproachDirection = approachDirection;
            }

            return closestColliderId == int.MaxValue
                ? horizontalMovement
                : horizontalMovement -
                    closestApproachDirection *
                    closestApproachDistance;
        }

        private Vector3 LimitCastApproach(
            Vector3 horizontalMovement,
            Vector3 capsuleCenter,
            float capsuleRadius,
            float horizontalDistance,
            int castCount)
        {
            float earliestProgress = 1f;
            int earliestColliderId = int.MaxValue;
            Vector3 earliestApproachDirection = Vector3.zero;

            for (int index = 0; index < castCount; index++)
            {
                RaycastHit hit = castResults[index];
                if (IsSelfCollider(hit.collider) ||
                    !TryGetApproachDirection(
                        in hit,
                        capsuleCenter,
                        out Vector3 approachDirection) ||
                    Vector3.Dot(
                        horizontalMovement,
                        approachDirection) <= 0f)
                {
                    continue;
                }

                float castAllowedDistance = Mathf.Max(
                    0f,
                    hit.distance - ContactClearance);
                float castProgress = Mathf.Clamp01(
                    castAllowedDistance / horizontalDistance);

                Vector3 targetOffset =
                    hit.collider.bounds.center - capsuleCenter;
                targetOffset.y = 0f;
                float currentCenterDistance = Vector3.Dot(
                    targetOffset,
                    approachDirection);
                float requiredCenterDistance = capsuleRadius +
                    GetHorizontalRadius(hit.collider) +
                    minimumSeparation;
                float separationAllowedDistance = Mathf.Max(
                    0f,
                    currentCenterDistance -
                    requiredCenterDistance -
                    ContactClearance);
                float approachMovementDistance = Vector3.Dot(
                    horizontalMovement,
                    approachDirection);
                float separationProgress = Mathf.Clamp01(
                    separationAllowedDistance /
                    approachMovementDistance);
                float progress = Mathf.Min(
                    castProgress,
                    separationProgress);
                int colliderId = hit.collider.GetInstanceID();
                bool isSameProgress = Mathf.Abs(
                    progress - earliestProgress) <=
                    DirectionThreshold;
                if (progress > earliestProgress ||
                    (isSameProgress &&
                     colliderId >= earliestColliderId))
                {
                    continue;
                }

                earliestProgress = progress;
                earliestColliderId = colliderId;
                earliestApproachDirection = approachDirection;
            }

            if (earliestProgress >= 1f)
            {
                return horizontalMovement;
            }

            float approachDistance = Vector3.Dot(
                horizontalMovement,
                earliestApproachDirection);
            float blockedDistance =
                approachDistance * (1f - earliestProgress);
            return horizontalMovement -
                earliestApproachDirection * blockedDistance;
        }

        private bool IsSelfCollider(Collider candidate)
        {
            if (candidate == null)
            {
                return true;
            }

            Transform candidateTransform = candidate.transform;
            return candidate == characterController ||
                candidateTransform == ownerTransform ||
                candidateTransform.IsChildOf(ownerTransform);
        }

        private static bool TryGetApproachDirection(
            in RaycastHit hit,
            Vector3 capsuleCenter,
            out Vector3 approachDirection)
        {
            approachDirection = -hit.normal;
            approachDirection.y = 0f;
            if (approachDirection.sqrMagnitude > DirectionThreshold)
            {
                approachDirection.Normalize();
                return true;
            }

            return TryGetDirectionToUnit(
                hit.collider,
                capsuleCenter,
                out approachDirection,
                out _);
        }

        private static bool TryGetDirectionToUnit(
            Collider otherCollider,
            Vector3 capsuleCenter,
            out Vector3 direction,
            out float centerDistance)
        {
            direction = otherCollider.bounds.center - capsuleCenter;
            direction.y = 0f;
            centerDistance = direction.magnitude;
            if (centerDistance <= DirectionThreshold)
            {
                return false;
            }

            direction /= centerDistance;
            return true;
        }

        private static float GetHorizontalRadius(Collider unitCollider)
        {
            if (unitCollider is CharacterController unitController)
            {
                Vector3 scale = unitController.transform.lossyScale;
                float radiusScale = Mathf.Max(
                    Mathf.Abs(scale.x),
                    Mathf.Abs(scale.z));
                return unitController.radius * radiusScale;
            }

            Bounds bounds = unitCollider.bounds;
            return Mathf.Max(bounds.extents.x, bounds.extents.z);
        }

        private float GetWorldSkinWidth()
        {
            Vector3 scale = ownerTransform.lossyScale;
            float radiusScale = Mathf.Max(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.z));
            return characterController.skinWidth * radiusScale;
        }

        private void GetWorldCapsule(
            out Vector3 capsuleTop,
            out Vector3 capsuleBottom,
            out Vector3 capsuleCenter,
            out float capsuleRadius)
        {
            Vector3 scale = ownerTransform.lossyScale;
            float radiusScale = Mathf.Max(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.z));
            float heightScale = Mathf.Abs(scale.y);

            capsuleRadius =
                characterController.radius * radiusScale;
            float capsuleHeight = Mathf.Max(
                characterController.height * heightScale,
                capsuleRadius * 2f);
            float halfLineHeight =
                capsuleHeight * 0.5f - capsuleRadius;

            capsuleCenter = ownerTransform.TransformPoint(
                characterController.center);
            capsuleTop =
                capsuleCenter + Vector3.up * halfLineHeight;
            capsuleBottom =
                capsuleCenter - Vector3.up * halfLineHeight;
        }
    }
}
