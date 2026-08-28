using System;
using Characters.Player.Lifecycle;
using UnityEngine;
using World.Interaction;

namespace Characters.Player.Interaction
{
    // 주변 상호작용 물체 중 플레이어가 정면으로 바라보는 대상을 찾는다.
    internal sealed class PlayerInteractionDetector
    {
        private const int MaximumDetectedColliderCount = 16;
        private const float MinimumRayDistance = 0.0001f;

        private readonly Collider[] detectedColliders =
            new Collider[MaximumDetectedColliderCount];
        private readonly Transform playerTransform;
        private readonly Transform viewTransform;
        private readonly LayerMask interactableLayers;
        private readonly LayerMask obstructionLayers;
        private readonly float interactionRange;
        private IPlayerInteractable currentTarget;

        public IPlayerInteractable CurrentTarget => currentTarget;
        public bool HasCurrentTarget => currentTarget != null;

        public event Action<bool> CurrentTargetChanged;

        public PlayerInteractionDetector(
            Transform playerTransform,
            Transform viewTransform,
            LayerMask interactableLayers,
            LayerMask obstructionLayers,
            float interactionRange)
        {
            this.playerTransform = playerTransform;
            this.viewTransform = viewTransform;
            this.interactableLayers = interactableLayers;
            this.obstructionLayers = obstructionLayers;
            this.interactionRange = Mathf.Max(0f, interactionRange);
        }

        public void RefreshCurrentTarget(PlayerController player)
        {
            IPlayerInteractable nextTarget = FindNearbyInteractable(player);
            if (ReferenceEquals(currentTarget, nextTarget))
            {
                return;
            }

            currentTarget = nextTarget;
            CurrentTargetChanged?.Invoke(currentTarget != null);
        }

        public void ClearCurrentTarget()
        {
            if (currentTarget == null)
            {
                return;
            }

            currentTarget = null;
            CurrentTargetChanged?.Invoke(false);
        }

        private IPlayerInteractable FindNearbyInteractable(PlayerController player)
        {
            if (!CanDetect(player))
            {
                return null;
            }

            int detectedCount = DetectNearbyInteractableColliders();
            if (detectedCount == 0)
            {
                return null;
            }

            for (int index = 0; index < detectedCount; index++)
            {
                if (!TryGetInteractable(
                        detectedColliders[index],
                        out IPlayerInteractable interactable) ||
                    !interactable.CanInteract(player) ||
                    !IsVisible(detectedColliders[index], interactable))
                {
                    continue;
                }

                return interactable;
            }

            return null;
        }

        private bool CanDetect(PlayerController player)
        {
            return player != null &&
                playerTransform != null &&
                viewTransform != null &&
                interactionRange > 0f &&
                interactableLayers.value != 0;
        }

        private int DetectNearbyInteractableColliders()
        {
            return Physics.OverlapSphereNonAlloc(
                playerTransform.position,
                interactionRange,
                detectedColliders,
                interactableLayers,
                QueryTriggerInteraction.Collide);
        }

        private bool IsVisible(
            Collider candidateCollider,
            IPlayerInteractable candidate)
        {
            if (obstructionLayers.value == 0)
            {
                return true;
            }

            Vector3 rayOrigin = viewTransform.position;
            Vector3 toCandidate = candidateCollider.bounds.center - rayOrigin;
            float distance = toCandidate.magnitude;
            if (distance <= MinimumRayDistance ||
                !Physics.Raycast(
                    rayOrigin,
                    toCandidate / distance,
                    out RaycastHit hit,
                    distance,
                    obstructionLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            if (!TryGetInteractable(
                    hit.collider,
                    out IPlayerInteractable hitInteractable))
            {
                return false;
            }

            return ReferenceEquals(candidate, hitInteractable);
        }

        private static bool TryGetInteractable(Collider collider, out IPlayerInteractable interactable)
        {
            interactable = null;
            if (collider == null)
            {
                return false;
            }

            Component component = collider.GetComponentInParent(typeof(IPlayerInteractable));
            interactable = component as IPlayerInteractable;
            return interactable != null;
        }
    }
}
