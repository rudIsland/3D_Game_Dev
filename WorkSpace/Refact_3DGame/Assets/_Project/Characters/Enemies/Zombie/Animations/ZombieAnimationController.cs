using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    [DisallowMultipleComponent]
    // Zombie의 게임 상태를 Animator 값으로 바꾸는 Unity 경계다.
    public sealed class ZombieAnimationController : MonoBehaviour
    {
        private const int SwingAttackNumber = 0;
        private const int KickAttackNumber = 1;
        private const int UpDownAttackNumber = 2;

        private static readonly int MoveSpeedId =
            Animator.StringToHash("MoveSpeed");
        private static readonly int AttackId =
            Animator.StringToHash("Attack");
        private static readonly int AttackNumberId =
            Animator.StringToHash("AttackNumber");
        private static readonly int ScreamId =
            Animator.StringToHash("Scream");
        private static readonly int HitId =
            Animator.StringToHash("Hit");
        private static readonly int DieId =
            Animator.StringToHash("Die");

        [SerializeField] private Animator zombieAnimator;

        private bool isDead;

        private void Awake()
        {
            FindZombieAnimator();
        }

        // 0은 대기, 0보다 크면 걷기와 달리기 애니메이션으로 이어진다.
        public void SetMoveSpeed(float moveSpeed)
        {
            if (isDead || zombieAnimator == null)
            {
                return;
            }

            zombieAnimator.SetFloat(MoveSpeedId, Mathf.Clamp01(moveSpeed));
        }

        public void PlaySwingAttack()
        {
            PlayAttack(SwingAttackNumber);
        }

        public void PlayKickAttack()
        {
            PlayAttack(KickAttackNumber);
        }

        public void PlayUpDownAttack()
        {
            PlayAttack(UpDownAttackNumber);
        }

        public void PlayScream()
        {
            PlayTrigger(ScreamId);
        }

        public void PlayHit()
        {
            PlayTrigger(HitId);
        }

        public void PlayDeath()
        {
            if (isDead || zombieAnimator == null)
            {
                return;
            }

            isDead = true;
            zombieAnimator.SetFloat(MoveSpeedId, 0f);
            zombieAnimator.ResetTrigger(AttackId);
            zombieAnimator.ResetTrigger(ScreamId);
            zombieAnimator.ResetTrigger(HitId);
            zombieAnimator.SetTrigger(DieId);
        }

        // 풀에서 다시 꺼낸 Zombie를 처음 대기 상태로 되돌린다.
        public void ResetAnimation()
        {
            isDead = false;
            FindZombieAnimator();

            if (zombieAnimator == null)
            {
                return;
            }

            zombieAnimator.Rebind();

            if (zombieAnimator.isActiveAndEnabled)
            {
                zombieAnimator.Update(0f);
            }

            zombieAnimator.SetFloat(MoveSpeedId, 0f);
            zombieAnimator.SetInteger(AttackNumberId, SwingAttackNumber);
        }

        private void PlayAttack(int attackNumber)
        {
            if (isDead || zombieAnimator == null)
            {
                return;
            }

            zombieAnimator.SetFloat(MoveSpeedId, 0f);
            zombieAnimator.SetInteger(AttackNumberId, attackNumber);
            RestartTrigger(AttackId);
        }

        private void PlayTrigger(int triggerId)
        {
            if (isDead || zombieAnimator == null)
            {
                return;
            }

            zombieAnimator.SetFloat(MoveSpeedId, 0f);
            RestartTrigger(triggerId);
        }

        private void RestartTrigger(int triggerId)
        {
            zombieAnimator.ResetTrigger(triggerId);
            zombieAnimator.SetTrigger(triggerId);
        }

        private void FindZombieAnimator()
        {
            if (zombieAnimator == null)
            {
                zombieAnimator = GetComponentInChildren<Animator>(true);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            FindZombieAnimator();
        }
#endif
    }
}
