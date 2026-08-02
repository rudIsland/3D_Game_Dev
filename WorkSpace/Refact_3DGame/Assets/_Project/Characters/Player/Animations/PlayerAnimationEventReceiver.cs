using UnityEngine;

namespace rudIsland.RPG3D.Player.Animations
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    // Animator의 루트 이동을 전달하며, 상태 머신이 행동 루트 모션만 적용한다.
    public sealed class PlayerAnimationEventReceiver : MonoBehaviour
    {
        private Animator playerAnimator;
        private PlayerController playerController;

        private void Awake()
        {
            playerAnimator = GetComponent<Animator>();
            playerController = GetComponentInParent<PlayerController>();

            if (playerController == null)
            {
                Debug.LogError(
                    "PlayerAnimationEventReceiver가 PlayerController를 찾지 못했습니다.",
                    this);
                enabled = false;
            }
        }

        private void OnAnimatorMove()
        {
            if (playerController == null)
            {
                return;
            }

            playerController.ApplyRootMotion(
                playerAnimator.deltaPosition,
                playerAnimator.deltaRotation);
        }

        public void StartAttackHitAnimationEvent(int attackNumber)
        {
            playerController?.StartAttackHit(attackNumber);
        }

        public void EndAttackHitAnimationEvent()
        {
            playerController?.EndAttackHit();
        }

        public void EndAttackAnimationEvent()
        {
            playerController?.NotifyAttackAnimationEnded();
        }

        private void OnDisable()
        {
            playerController?.EndAttackHit();
        }
    }
}
