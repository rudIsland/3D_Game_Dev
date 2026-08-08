using System;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Player;
using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.MummyWarrior
{
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(CharacterController),
        typeof(MummyWarriorAnimationController))]
    // Unity 입력과 일반 C# Mummy Warrior AI를 연결한다.
    public sealed class MummyWarriorController :
        WorldObjectView,
        IUnitDeathState
    {
        [Header("필수 연결")]
        [SerializeField] private Transform target;
        [SerializeField] private Animator mummyAnimator;

        [Header("추적 설정")]
        [SerializeField] private bool canTrackTarget = false;

        [Header("생명")]
        [SerializeField, Min(1f)] private float maxHealth = 120f;
        [SerializeField, Min(0f)] private float deadBodyKeepTime = 2f;
        [SerializeField, Range(0.1f, 0.9f)] private float phaseTwoHealthRate = 0.6f;

        [Header("탐지와 이동")]
        [SerializeField, Min(0.1f)] private float findRange = 25f;
        [SerializeField, Min(0.1f)] private float runStartRange = 6f;
        [SerializeField, Min(0.1f)] private float walkSpeed = 1.6f;
        [SerializeField, Min(0.1f)] private float runSpeed = 3.8f;
        [SerializeField, Min(1f)] private float turnSpeed = 300f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float groundPull = -2f;

        [Header("공격 목록")]
        [SerializeField] private MummyWarriorAttackPattern[] attackPatterns =
            { new MummyWarriorAttackPattern() };

#if UNITY_EDITOR
        [Header("체력 확인")]
        [SerializeField, Min(0f)] private float testDamage = 10f;
#endif

        private CharacterController characterController;
        private MummyWarriorAnimationController animationController;
        private MummyWarriorWorldUnit mummyWorldUnit;
        private MummyWarriorWorldUnit standaloneWorldUnit;

        public bool IsDead =>
            mummyWorldUnit != null && mummyWorldUnit.IsDead;

        private void Awake()
        {
            FindSceneReferences();
            FindUnityComponents();
        }

        private void Start()
        {
            if (RuntimeObject != null)
            {
                return;
            }

            standaloneWorldUnit = (MummyWarriorWorldUnit)CreateRuntimeObject();
            standaloneWorldUnit.Create();
            standaloneWorldUnit.Enable();
        }

        private void Update()
        {
            standaloneWorldUnit?.Tick(Time.deltaTime);
        }

        private void OnEnable()
        {
            if (standaloneWorldUnit != null && !standaloneWorldUnit.IsEnabled)
            {
                standaloneWorldUnit.Enable();
            }
        }

        private void OnDisable()
        {
            standaloneWorldUnit?.Disable();
            EndAttackHit();
        }

        private void OnDestroy()
        {
            standaloneWorldUnit?.Dispose();
        }

        protected override IWorldObject CreateRuntimeObject()
        {
            FindSceneReferences();
            FindUnityComponents();

            if (target == null || mummyAnimator == null ||
                characterController == null)
            {
                throw new InvalidOperationException(
                    "MummyWarriorController에 Target, Animator, CharacterController가 필요합니다.");
            }

            animationController.ConnectAnimator(mummyAnimator);
            var movement = new MummyWarriorMovement(
                transform,
                characterController,
                gravity,
                groundPull);
            var stateMachine = new MummyWarriorStateMachine(
                target,
                movement,
                animationController,
                attackPatterns,
                findRange,
                runStartRange,
                walkSpeed,
                runSpeed,
                turnSpeed,
                deadBodyKeepTime,
                StartAttackHit,
                EndAttackHit,
                RequestDeadMummyRelease,
                canTrackTarget,
                phaseTwoHealthRate);

            mummyWorldUnit = new MummyWarriorWorldUnit(maxHealth, stateMachine);
            return mummyWorldUnit;
        }

        private void StartAttackHit(
            MummyWarriorAttackPattern pattern,
            int attackNumber)
        {
            // 적 공격 판정은 플레이어 기본 공격 완성 후 다시 구현한다.
        }

        private void EndAttackHit()
        {
        }

        private void RequestDeadMummyRelease()
        {
            if (standaloneWorldUnit != null)
            {
                gameObject.SetActive(false);
                return;
            }

            RequestDespawn();
        }

        private void FindSceneReferences()
        {
            if (target != null)
            {
                return;
            }

            PlayerController player = FindFirstObjectByType<PlayerController>();
            target = player != null ? player.transform : null;
        }

        private void FindUnityComponents()
        {
            characterController = GetComponent<CharacterController>();
            animationController = GetComponent<MummyWarriorAnimationController>();
            if (mummyAnimator == null)
            {
                mummyAnimator = GetComponentInChildren<Animator>(true);
            }
        }

        protected override void OnResetForPool()
        {
            EndAttackHit();
            animationController?.ResetAnimation();
        }

#if UNITY_EDITOR
        [ContextMenu("Test Damage")]
        private void TestDamage()
        {
            if (!Application.isPlaying || mummyWorldUnit == null)
            {
                Debug.LogWarning(
                    "Test Damage는 Play 중이고 Mummy 준비가 끝난 뒤 사용할 수 있습니다.",
                    this);
                return;
            }

            mummyWorldUnit.TakeDamage(testDamage);
        }

        private void OnValidate()
        {
            FindUnityComponents();
            findRange = Mathf.Max(0.1f, findRange);
            runStartRange = Mathf.Clamp(runStartRange, 0.1f, findRange);
            walkSpeed = Mathf.Max(0.1f, walkSpeed);
            runSpeed = Mathf.Max(walkSpeed, runSpeed);
            deadBodyKeepTime = Mathf.Max(0f, deadBodyKeepTime);
            phaseTwoHealthRate = Mathf.Clamp(
                phaseTwoHealthRate,
                0.1f,
                0.9f);

            if (attackPatterns == null)
            {
                return;
            }

            for (int index = 0; index < attackPatterns.Length; index++)
            {
                attackPatterns[index]?.ClampValues();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, findRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, runStartRange);
        }
#endif
    }
}