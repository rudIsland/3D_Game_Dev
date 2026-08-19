using UnityEngine;

namespace rudIsland.RPG3D.Characters.Combat
{
    //연출을 위해 피격과 가드시 속성의 시간만큼 연출을 멈춤.
    // 공격 데이터가 요청한 시간만큼 한 유닛의 애니메이션과 상태 갱신을 정지시킨다.
    public sealed class CombatHitStop
    {
        public const float DefaultDamageDuration = 0.04f;
        public const float GuardDuration = 0.03f;

        private readonly Animator animator;

        private float remainingDuration;
        private float resumeAnimatorSpeed = 1f;
        private bool isActive;

        public CombatHitStop(Animator animator)
        {
            this.animator = animator;
        }

        public void Request(float duration)
        {
            duration = Mathf.Max(0f, duration);
            if (duration <= 0f || animator == null || !animator.isActiveAndEnabled)
            {
                return;
            }

            if (!isActive)
            {
                resumeAnimatorSpeed = animator.speed;
                animator.speed = 0f;
                isActive = true;
            }

            remainingDuration = Mathf.Max(remainingDuration, duration);
        }

        public bool Update(float deltaTime)
        {
            if (!isActive)
            {
                return false;
            }

            remainingDuration -= Mathf.Max(0f, deltaTime);
            if (remainingDuration <= 0f)
            {
                RestoreAnimatorSpeed();
            }

            return true;
        }

        public void Reset()
        {
            RestoreAnimatorSpeed();
        }

        private void RestoreAnimatorSpeed()
        {
            if (isActive && animator != null)
            {
                animator.speed = resumeAnimatorSpeed;
            }

            remainingDuration = 0f;
            resumeAnimatorSpeed = 1f;
            isActive = false;
        }
    }
}
