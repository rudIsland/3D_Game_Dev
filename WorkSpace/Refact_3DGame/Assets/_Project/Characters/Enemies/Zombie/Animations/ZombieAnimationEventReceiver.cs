using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    // 좀비 클립의 AnimationEvent와 루트 회전을 게임 코드에 전달한다.
    public sealed class ZombieAnimationEventReceiver : MonoBehaviour
    {
        private Animator zombieAnimator; // 애니메이터 참조
        private ZombieAnimationController animationController; // 씬 또는 시스템 참조
        private ZombieController zombieController; // 씬 또는 시스템 참조

        private void Awake()
        {
            zombieAnimator = GetComponent<Animator>();
            animationController =
                GetComponentInParent<ZombieAnimationController>();
            zombieController = GetComponentInParent<ZombieController>();
        }

        internal void Initialize(
            ZombieAnimationController controller)
        {
            animationController = controller;
            zombieController = GetComponentInParent<ZombieController>();

            if (zombieAnimator == null)
            {
                zombieAnimator = GetComponent<Animator>();
            }
        }

        public void StartAttackHitAnimationEvent(int attackNumber)
        {
            zombieController?.StartAttackHit(attackNumber);
        }

        public void EndAttackHitAnimationEvent()
        {
            zombieController?.EndAttackHit();
        }

        public void EndAttackAnimationEvent()
        {
            zombieController?.EndAttackHit();
            zombieController?.NotifyAttackAnimationEnded();
        }

        public void EndAlert()
        {
            zombieController?.NotifyAlertAnimationEnded();
        }

        private void OnAnimatorMove()
        {
            if (zombieAnimator == null)
            {
                return;
            }

            animationController?.ApplyAttackRootRotation(
                zombieAnimator.deltaRotation);
        }

        private void OnDisable()
        {
            zombieController?.EndAttackHit();
        }
    }
}
