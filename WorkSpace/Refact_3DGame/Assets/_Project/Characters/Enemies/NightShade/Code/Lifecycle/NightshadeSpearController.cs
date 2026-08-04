using System;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Combat;
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
        IAttackHitReceiver,
        IUnitDeathState
    {
        [Header("필수 연결")]
        [SerializeField] private Transform target; // 대상 참조
        [SerializeField] private Animator nightshadeAnimator; // 애니메이터 참조
        [SerializeField] private MeleeHitDetector attackHitDetector; // 피격 또는 피해 관련 값

        [Header("추적 설정")]
        [SerializeField] private bool canTrackTarget = false; // 기능 사용 여부

        [Header("생명")]
        [SerializeField, Min(1f)] private float maxHealth = 120f; // 최대 체력
        [SerializeField, Min(0f)] private float deadBodyKeepTime = 2f; // 시간 설정
        [SerializeField, Range(0.1f, 0.9f)] private float phaseTwoHealthRate = 0.6f;

        [Header("경직")]
        [SerializeField, Min(1f)] private float staggerLimit = 60f;
        [SerializeField, Min(0f)] private float staggerRecoverDelay = 1.3f;
        [SerializeField, Min(0f)] private float staggerRecoverSpeed = 18f;

        [Header("탐지와 이동")]
        [SerializeField, Min(0.1f)] private float findRange = 25f; // 거리 설정
        [SerializeField, Min(0.1f)] private float runStartRange = 6f; // 거리 설정
        [SerializeField, Min(0.1f)] private float walkSpeed = 1.6f; // 이동 속도
        [SerializeField, Min(0.1f)] private float runSpeed = 3.8f; // 이동 속도
        [SerializeField, Min(1f)] private float turnSpeed = 300f; // 이동 속도
        [SerializeField] private float gravity = -22f; // Inspector 설정 값
        [SerializeField] private float groundPull = -2f; // Inspector 설정 값
        [SerializeField, Min(0.01f)] private float hitPushTime = 0.18f;

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
                groundPull,
                hitPushTime);
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
                stateMachine,
                staggerLimit,
                staggerRecoverDelay,
                staggerRecoverSpeed);
            return nightshadeWorldUnit;
        }

        public bool CanTakeHit =>
            nightshadeWorldUnit != null &&
            nightshadeWorldUnit.CanTakeHit;

        public int ActivationSequence =>
            nightshadeWorldUnit != null
                ? nightshadeWorldUnit.ActivationSequence
                : 0;

        public AttackHitResult ReceiveAttackHit(in AttackHitInput hit)
        {
            if (!CanTakeHit)
            {
                return AttackHitResult.Ignored;
            }

            return nightshadeWorldUnit.ReceiveAttackHit(
                in hit,
                transform.forward);
        }

        private void StartAttackHit(
            NightshadeSpearAttackPattern pattern,
            int attackNumber)
        {
            if (pattern == null || !pattern.Damage.IsValid || attackHitDetector == null) return;

            var hit = new AttackHitInput(
                pattern.Damage,
                UnitTeam.Enemy,
                attackNumber,
                pattern.Strength,
                pattern.StaggerDamage,
                0f,
                true,
                true,
                pattern.PushDistance,
                0f,
                default);
            attackHitDetector.StartHit(in hit);
        }

        private void EndAttackHit()
        {
            attackHitDetector?.EndHit();
        }

        private void RequestDeadNightshadeRelease()
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

            if (attackHitDetector == null)
            {
                attackHitDetector = GetComponentInChildren<MeleeHitDetector>(true);
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
            staggerLimit = Mathf.Max(1f, staggerLimit);
            staggerRecoverDelay = Mathf.Max(0f, staggerRecoverDelay);
            staggerRecoverSpeed = Mathf.Max(0f, staggerRecoverSpeed);
            hitPushTime = Mathf.Max(0.01f, hitPushTime);

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
