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
    public sealed class ZombieController : MonoBehaviour
    {
        [Header("필수 연결")]
        [SerializeField] private WorldObjectManager worldObjectManager;
        [SerializeField] private Transform target;
        [SerializeField] private Animator zombieAnimator;

        [Header("생명")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        [Header("찾기와 공격 거리")]
        [SerializeField, Min(0.1f)] private float findRange = 30f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.8f;

        [Header("이동")]
        [SerializeField, Min(0.1f)] private float chaseSpeed = 3.5f;
        [SerializeField, Min(1f)] private float turnSpeed = 360f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float groundPull = -2f;

        [Header("상태 시간")]
        [SerializeField, Min(0.01f)] private float alertTime = 1.5f;
        [SerializeField, Min(0.01f)] private float attackInterval = 1.5f;
        [SerializeField, Min(0.01f)] private float hitTime = 0.7f;

        private CharacterController characterController;
        private ZombieAnimationController zombieAnimation;
        private ZombieWorldUnit zombieWorldUnit;

        public string CurrentStateName =>
            zombieWorldUnit?.CurrentStateName ?? "Not Ready";

        private void Awake()
        {
            FindSceneReferences();

            if (worldObjectManager == null || target == null)
            {
                Debug.LogError(
                    "ZombieController에 WorldObjectManager와 Target이 필요합니다.",
                    this);
                enabled = false;
                return;
            }

            characterController = GetComponent<CharacterController>();
            zombieAnimation = GetComponent<ZombieAnimationController>();

            if (zombieAnimator == null)
            {
                zombieAnimator = GetComponentInChildren<Animator>(true);
            }

            if (zombieAnimator == null)
            {
                Debug.LogError(
                    "ZombieController가 자식 Animator를 찾지 못했습니다.",
                    this);
                enabled = false;
                return;
            }

            zombieAnimator.applyRootMotion = false;

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
                attackRange,
                chaseSpeed,
                turnSpeed,
                alertTime,
                attackInterval,
                hitTime);

            zombieWorldUnit = new ZombieWorldUnit(
                maxHealth,
                stateMachine);
            worldObjectManager.Register(zombieWorldUnit);
        }

        private void OnEnable()
        {
            if (zombieWorldUnit != null)
            {
                worldObjectManager.Enable(zombieWorldUnit);
            }
        }

        private void OnDisable()
        {
            if (zombieWorldUnit != null && worldObjectManager != null)
            {
                worldObjectManager.Disable(zombieWorldUnit);
            }
        }

        private void OnDestroy()
        {
            if (zombieWorldUnit == null)
            {
                return;
            }

            if (worldObjectManager != null)
            {
                worldObjectManager.Unregister(zombieWorldUnit);
                return;
            }

            zombieWorldUnit.Dispose();
        }

        // 전투 시스템이 완성되면 이 진입점으로 피해를 전달한다.
        public void TakeDamage(float damage)
        {
            zombieWorldUnit?.TakeDamage(damage);
        }

        private void FindSceneReferences()
        {
            if (worldObjectManager == null)
            {
                worldObjectManager =
                    FindFirstObjectByType<WorldObjectManager>();
            }

            if (target != null)
            {
                return;
            }

            PlayerController player =
                FindFirstObjectByType<PlayerController>();
            target = player != null ? player.transform : null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (zombieAnimator == null)
            {
                zombieAnimator = GetComponentInChildren<Animator>(true);
            }

            findRange = Mathf.Max(0.1f, findRange);
            attackRange = Mathf.Clamp(attackRange, 0.1f, findRange);
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
