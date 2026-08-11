using System;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Player;
using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(CharacterController),
        typeof(ZombieAnimationController))]
    // Unity 씬과 일반 C# Zombie AI를 연결한다.
    public sealed class ZombieController :
        WorldObjectView,
        IUnitDeathState,
        IEnemyDamageReceiver
    {
        [Header("필수 연결")]
        [SerializeField] private Transform target; // 대상 참조
        [SerializeField] private Animator zombieAnimator; // 애니메이터 참조

        [Header("생명")]
        [SerializeField, Min(1f)] private float maxHealth = 100f; // 최대 체력

        [Header("사망 후 정리")]
        [SerializeField, Min(0f)] private float deadBodyKeepTime = 2f; // 시간 설정

#if UNITY_EDITOR
        [Header("체력 확인")]
        [SerializeField, Min(0f)] private float testDamage = 10f; // 피격 또는 피해 관련 값
#endif

        [Header("찾기와 공격 거리")]
        [SerializeField, Min(0.1f)] private float findRange = 30f; // 거리 설정
        [SerializeField, Min(0.01f)]
        private float idleTargetCheckInterval = 0.1f; // 대상 참조
        [SerializeField, Min(0.1f)] private float attackRange = 1.8f; // 공격 관련 설정 또는 상태
        [SerializeField, Range(0f, 180f)]
        private float attackFacingAngle = 10f; // 공격 관련 설정 또는 상태

        [Header("이동")]
        [SerializeField, Min(0.1f)] private float chaseSpeed = 3.5f; // 이동 속도
        [SerializeField, Min(1f)] private float turnSpeed = 360f; // 이동 속도
        [SerializeField] private float gravity = -22f; // Inspector 설정 값
        [SerializeField] private float groundPull = -2f; // Inspector 설정 값

        private CharacterController characterController; // 씬 또는 시스템 참조
        private ZombieAnimationController zombieAnimation; // 씬 또는 시스템 참조
        private ZombieWorldUnit zombieWorldUnit; // 씬 또는 시스템 참조

        public bool IsDead =>
            zombieWorldUnit != null && zombieWorldUnit.IsDead;
        internal bool CanTurnDuringAttack =>
            zombieWorldUnit != null && zombieWorldUnit.CanTurnDuringAttack();

        protected override IWorldObject CreateRuntimeObject()
        {
            FindSceneReferences();
            FindUnityComponents();

            if (target == null || zombieAnimator == null)
            {
                throw new InvalidOperationException(
                    "ZombieController에 Target과 자식 Animator가 필요합니다.");
            }

            zombieAnimator.applyRootMotion = true;
            zombieAnimation.ConnectAnimator(zombieAnimator);

            var movement = new ZombieMovement(
                transform,
                characterController,
                gravity,
                groundPull);
            var stateMachine = new ZombieStateMachine(
                target,
                movement,
                zombieAnimation,
                findRange,
                idleTargetCheckInterval,
                attackRange,
                attackFacingAngle,
                chaseSpeed,
                turnSpeed,
                deadBodyKeepTime,
                RequestDeadZombieRelease,
                EndAttackHit);

            zombieWorldUnit = new ZombieWorldUnit(
                maxHealth,
                stateMachine);
            return zombieWorldUnit;
        }

        private void RequestDeadZombieRelease()
        {
            RequestDespawn();
        }
        public void StartAttackHit(int attackNumber)
        {
            zombieWorldUnit?.BeginAttackHit();
        }
        public void EndAttackHitAnimationEvent()
        {
            if (zombieWorldUnit?.BeginAttackRecovery() == true)
            {
                EndAttackHit();
            }
        }

        public void EndAttackHit()
        {
        }

        internal void NotifyAttackAnimationEnded()
        {
            zombieWorldUnit?.BeginAttackRecovery();
            EndAttackHit();
            zombieWorldUnit?.NotifyAttackAnimationEnded();
        }

        internal void NotifyAlertAnimationEnded()
        {
            zombieWorldUnit?.NotifyAlertAnimationEnded();
        }

#if UNITY_EDITOR
        [ContextMenu("Test Damage")]
        private void TestDamage()
        {
            if (!Application.isPlaying || zombieWorldUnit == null)
            {
                Debug.LogWarning(
                    "Test Damage는 Play 중이고 좀비 준비가 끝난 뒤 사용할 수 있습니다.",
                    this);
                return;
            }

            float healthBeforeDamage = zombieWorldUnit.CurrentHealth;

            zombieWorldUnit.TakeDamage(testDamage, transform.position);

            Debug.Log(
                $"좀비 체력: {healthBeforeDamage} → {zombieWorldUnit.CurrentHealth}",
                this);
        }
#endif

        private void FindSceneReferences()
        {
            if (target != null)
            {
                return;
            }

            PlayerController player =
                FindFirstObjectByType<PlayerController>();
            target = player != null ? player.transform : null;
        }

        private void FindUnityComponents()
        {
            characterController = GetComponent<CharacterController>();
            zombieAnimation = GetComponent<ZombieAnimationController>();

            if (zombieAnimator == null)
            {
                zombieAnimator = GetComponentInChildren<Animator>(true);
            }
        }

        protected override void OnResetForPool()
        {
            EndAttackHit();
            zombieAnimation?.ResetAnimation();
        }

        public void TakeDamage(float damage, Vector3 hitPosition)
        {
            zombieWorldUnit?.TakeDamage(damage, hitPosition);
        }



#if UNITY_EDITOR
        private void OnValidate()
        {
            FindUnityComponents();
            findRange = Mathf.Max(0.1f, findRange);
            idleTargetCheckInterval =
                Mathf.Max(0.01f, idleTargetCheckInterval);
            attackRange = Mathf.Clamp(attackRange, 0.1f, findRange);
            attackFacingAngle = Mathf.Clamp(attackFacingAngle, 0f, 180f);
            deadBodyKeepTime = Mathf.Max(0f, deadBodyKeepTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, findRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
#endif
    }
}
