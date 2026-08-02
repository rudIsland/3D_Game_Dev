using System;
using rudIsland.RPG3D.Combat;
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
    public sealed class ZombieController : WorldObjectView, IAttackHitReceiver
    {
        [Header("필수 연결")]
        [SerializeField] private Transform target;
        [SerializeField] private Animator zombieAnimator;

        [Header("생명")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        [Header("공격 판정")]
        [SerializeField] private MeleeHitDetector swingHitDetector;
        [SerializeField] private MeleeHitDetector kickHitDetector;
        [SerializeField] private MeleeHitDetector upDownHitDetector;
        [SerializeField] private AttackDamage swingDamage =
            new AttackDamage(10f);
        [SerializeField] private AttackDamage kickDamage =
            new AttackDamage(10f);
        [SerializeField] private AttackDamage upDownDamage =
            new AttackDamage(10f);

        [Header("사망 후 정리")]
        [SerializeField, Min(0f)] private float deadBodyKeepTime = 2f;

#if UNITY_EDITOR
        [Header("체력 확인")]
        [SerializeField, Min(0f)] private float testDamage = 10f;
#endif

        [Header("찾기와 공격 거리")]
        [SerializeField, Min(0.1f)] private float findRange = 30f;
        [SerializeField, Min(0.01f)]
        private float idleTargetCheckInterval = 0.1f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.8f;
        [SerializeField, Range(0f, 180f)]
        private float attackFacingAngle = 30f;

        [Header("이동")]
        [SerializeField, Min(0.1f)] private float chaseSpeed = 3.5f;
        [SerializeField, Min(1f)] private float turnSpeed = 360f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float groundPull = -2f;

        private CharacterController characterController;
        private ZombieAnimationController zombieAnimation;
        private ZombieWorldUnit zombieWorldUnit;
        private MeleeHitDetector activeHitDetector;

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

        public void ReceiveHit(in AttackHitData hit)
        {
            zombieWorldUnit?.ApplyHit(in hit);
        }

        public void StartAttackHit(int attackNumber)
        {
            EndAttackHit();

            GetAttackHitSettings(
                attackNumber,
                out MeleeHitDetector detector,
                out AttackDamage damage);
            if (detector == null || !damage.IsValid)
            {
                return;
            }

            var hit = new AttackHitData(
                damage,
                UnitTeam.Enemy,
                attackNumber);
            activeHitDetector = detector;
            activeHitDetector.StartHit(in hit);
        }

        public void EndAttackHit()
        {
            activeHitDetector?.EndHit();
            activeHitDetector = null;
        }

        internal void NotifyAttackAnimationEnded()
        {
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

            zombieWorldUnit.TakeDamage(testDamage);

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

        private void GetAttackHitSettings(
            int attackNumber,
            out MeleeHitDetector detector,
            out AttackDamage damage)
        {
            switch (attackNumber)
            {
                case 1:
                    detector = swingHitDetector;
                    damage = swingDamage;
                    return;
                case 2:
                    detector = kickHitDetector;
                    damage = kickDamage;
                    return;
                case 3:
                    detector = upDownHitDetector;
                    damage = upDownDamage;
                    return;
                default:
                    detector = null;
                    damage = default;
                    return;
            }
        }

        protected override void OnResetForPool()
        {
            EndAttackHit();
            zombieAnimation?.ResetAnimation();
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
