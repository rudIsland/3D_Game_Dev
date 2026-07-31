using UnityEngine;

namespace rudIsland.RPG3D.Player.Animations
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    // Animator의 이동량을 PlayerRoot의 CharacterController로 전달한다.
    public sealed class PlayerAnimationMoveReceiver : MonoBehaviour
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
                    "PlayerAnimationMoveReceiver가 PlayerController를 찾지 못했습니다.",
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

            playerController.ApplyAttackAnimationMove(
                playerAnimator.deltaPosition);
        }
    }
}
