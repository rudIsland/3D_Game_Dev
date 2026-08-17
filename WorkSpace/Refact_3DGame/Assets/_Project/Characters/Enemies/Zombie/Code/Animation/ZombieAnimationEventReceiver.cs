using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    // 좀비 클립의 공격 AnimationEvent를 게임 코드에 전달한다.
    public sealed class ZombieAnimationEventReceiver : MonoBehaviour
    {
        private ZombieController zombieController; // 씬 또는 시스템 참조

        private void Awake()
        {
            Initialize();
        }

        internal void Initialize()
        {
            zombieController = GetComponentInParent<ZombieController>();
        }
        public void StartAttackHitAnimationEvent(int attackNumber)
        {
            zombieController?.StartAttackHit(attackNumber);
        }

        public void EndAttackHitAnimationEvent()
        {
            zombieController?.EndAttackHitAnimationEvent();
        }

        public void EndAttackAnimationEvent()
        {
            zombieController?.NotifyAttackAnimationEnded();
        }

        public void EndAlert()
        {
            zombieController?.NotifyAlertAnimationEnded();
        }

        private void OnDisable()
        {
            zombieController?.EndAttackHit();
        }
    }
}
