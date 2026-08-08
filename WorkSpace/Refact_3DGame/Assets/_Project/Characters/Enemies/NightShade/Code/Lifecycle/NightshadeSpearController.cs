using System;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Player;
using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(CharacterController),
        typeof(NightshadeSpearAnimationController))]
    // Unity 입력과 일반 C# Nightshade 전투 로직을 연결한다.
    public sealed class NightshadeSpearController :
        WorldObjectView,
        IUnitDeathState
    {
        [Header("필수 연결")]
        [SerializeField] private Transform target; // 대상 참조
        [SerializeField] private Animator nightshadeAnimator; // 애니메이터 참조

        [Header("추적 설정")]
        [SerializeField] private bool canTrackTarget = false; // 기능 사용 여부

        [Header("생명")]
        [SerializeField, Min(1f)] private float maxHealth = 120f; // 최대 체력
        [SerializeField, Min(0f)] private float deadBodyKeepTime = 2f; // 시간 설정
        [SerializeField, Range(0.1f, 0.9f)] private float phaseTwoHealthRate = 0.6f;

        [Header("탐지와 이동")]
        [SerializeField, Min(0.1f)] private float findRange = 25f; // 거리 설정
        [SerializeField, Min(0.1f)] private float runStartRange = 6f; // 거리 설정
        [SerializeField, Min(0.1f)] private float walkSpeed = 1.6f; // 이동 속도
        [SerializeField, Min(0.1f)] private float runSpeed = 3.8f; // 이동 속도
        [SerializeField, Min(1f)] private float turnSpeed = 300f; // 이동 속도
        [SerializeField] private float gravity = -22f; // Inspector 설정 값
        [SerializeField] private float groundPull = -2f; // Inspector 설정 값

        [Header("공격 목록")]
        [SerializeField] private NightshadeSpearAttackPattern[] attackPatterns = // 행동 설정 참조
            { new NightshadeSpearAttackPattern() };

#if UNITY_EDITOR
        [Header("체력 확인")]
        [SerializeField, Min(0f)] private float testDamage = 10f; // 피격 또는 피해 관련 값
#endif

        private CharacterController characterController; // 씬 또는 시스템 참조
        private NightshadeSpearAnimationController animationController; // 씬 또는 시스템 참조
        private NightshadeSpearWorldUnit nightshadeWorldUnit; // 씬 또는 시스템 참조
        private NightshadeSpearWorldUnit standaloneWorldUnit; // 씬 또는 시스템 참조

        public bool IsDead =>
            nightshadeWorldUnit != null && nightshadeWorldUnit.IsDead;

        private void Awake()
        {
            FindSceneReferences();
            FindUnityComponents();
        }

        // 풀을 거치지 않고 CharacterTestScene에 직접 놓인 객체도 같은 로직으로 실행한다.
        private void Start()
        {
            if (RuntimeObject != null) return;

            standaloneWorldUnit = (NightshadeSpearWorldUnit)CreateRuntimeObject();
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

            if (target == null || nightshadeAnimator == null ||
                characterController == null)
            {
                throw new InvalidOperationException(
                    "NightshadeSpearController에 Target, Animator, CharacterController가 필요합니다.");
            }

            animationController.ConnectAnimator(nightshadeAnimator);
            var movement = new NightshadeSpearMovement(
                transform,
                characterController,
                gravity,
                groundPull);
            var stateMachine = new NightshadeSpearStateMachine(
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
                RequestDeadNightshadeRelease,
                canTrackTarget,
                phaseTwoHealthRate);

            nightshadeWorldUnit = new NightshadeSpearWorldUnit(
                maxHealth,
                stateMachine);
            return nightshadeWorldUnit;
        }
        private void StartAttackHit(
            NightshadeSpearAttackPattern pattern,
            int attackNumber)
        {
        }

        private void EndAttackHit()
        {
        }        private void RequestDeadNightshadeRelease()
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
            if (target != null) return;
            PlayerController player = FindFirstObjectByType<PlayerController>();
            target = player != null ? player.transform : null;
        }

        private void FindUnityComponents()
        {
            characterController = GetComponent<CharacterController>();
            animationController = GetComponent<NightshadeSpearAnimationController>();
            if (nightshadeAnimator == null)
            {
                nightshadeAnimator = GetComponentInChildren<Animator>(true);
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
            if (!Application.isPlaying || nightshadeWorldUnit == null)
            {
                Debug.LogWarning("Test Damage는 Play 중이고 Nightshade 준비가 끝난 뒤 사용할 수 있습니다.", this);
                return;
            }

            nightshadeWorldUnit.TakeDamage(testDamage);
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

            if (attackPatterns == null) return;
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
