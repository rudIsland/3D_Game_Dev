using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    [DisallowMultipleComponent]
    // 기존 Zombie 공격 클립의 AnimationEvent를 공격 판정 값으로 바꾼다.
    public sealed class ZombieAnimationEventReceiver : MonoBehaviour
    {
        public bool IsAttackDamageActive { get; private set; }
        public int CurrentHitNumber { get; private set; }

        // 아래 메서드 이름은 기존 AnimationClip 이벤트 이름과 같아야 한다.
        public void AttackStart()
        {
            IsAttackDamageActive = false;
            CurrentHitNumber = 0;
        }

        public void ActiveWeapon()
        {
            IsAttackDamageActive = true;
        }

        public void SetHitIndex()
        {
            CurrentHitNumber++;
        }

        public void DisActiveWeapon()
        {
            IsAttackDamageActive = false;
        }

        public void EndAttack()
        {
            IsAttackDamageActive = false;
        }

        private void OnDisable()
        {
            IsAttackDamageActive = false;
            CurrentHitNumber = 0;
        }
    }
}
