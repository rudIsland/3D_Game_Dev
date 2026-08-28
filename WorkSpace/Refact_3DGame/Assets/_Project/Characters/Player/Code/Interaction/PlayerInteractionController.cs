using System;
using Characters.Player.Lifecycle;
using UnityEngine;
using World.Interaction;

namespace Characters.Player.Interaction
{
    // 플레이어 입력을 현재 상호작용 대상의 실행 요청으로 전달한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        private const int DefaultLayerMask = 1;
        private const int InteractionTargetLayerMask = 1 << 22;

        [Header("상호작용 감지")]
        [SerializeField, Min(0.1f)]
        private float interactionRange = 1.5f;
        [SerializeField]
        private LayerMask interactableLayers = InteractionTargetLayerMask;

        private PlayerController playerController;
        private PlayerInteractionDetector interactionDetector;

        public bool HasCurrentInteractable =>
            interactionDetector != null &&
            interactionDetector.HasCurrentTarget;

        public event Action<bool> AvailableInteractableChanged;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            SetDefaultInteractionTargetLayer();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SetDefaultInteractionTargetLayer();
        }
#endif

        private void SetDefaultInteractionTargetLayer()
        {
            if (interactableLayers.value == DefaultLayerMask)
            {
                interactableLayers = InteractionTargetLayerMask;
            }
        }

        private void Start()
        {
            if (playerController == null ||
                playerController.ViewTransform == null)
            {
                Debug.LogError("PlayerInteractionController에 플레이어 시점 연결이 필요합니다.", this);
                enabled = false;
                return;
            }

            interactionDetector = new PlayerInteractionDetector(
                transform,
                playerController.ViewTransform,
                interactableLayers,
                playerController.ObstructionLayers,
                interactionRange);
            interactionDetector.CurrentTargetChanged += HandleCurrentTargetChanged;
            interactionDetector.RefreshCurrentTarget(playerController);
        }

        // 플레이어 커스텀 생명주기에서 상호작용 대상을 갱신한다.
        internal void RefreshCurrentTarget()
        {
            if (!isActiveAndEnabled)
            {
                interactionDetector?.ClearCurrentTarget();
                return;
            }

            interactionDetector?.RefreshCurrentTarget(playerController);
        }

        private void OnDisable()
        {
            interactionDetector?.ClearCurrentTarget();
        }

        public bool Interact()
        {
            if (!enabled ||
                interactionDetector == null ||
                interactionDetector.CurrentTarget == null ||
                playerController == null)
            {
                return false;
            }

            bool interacted = interactionDetector.CurrentTarget.TryInteract(playerController);
            interactionDetector.RefreshCurrentTarget(playerController);
            return interacted;
        }

        // 플레이어 생명주기가 멈출 때 남아 있는 상호작용 대상을 지운다.
        internal void ClearCurrentTarget()
        {
            interactionDetector?.ClearCurrentTarget();
        }

        private void HandleCurrentTargetChanged(bool hasCurrentTarget)
        {
            AvailableInteractableChanged?.Invoke(hasCurrentTarget);
        }
    }
}
