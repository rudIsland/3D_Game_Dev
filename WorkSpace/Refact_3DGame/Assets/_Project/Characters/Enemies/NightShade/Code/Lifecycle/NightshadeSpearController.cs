using System;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Characters.Combat.AttackData;
using rudIsland.RPG3D.Player;
using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(CharacterController),
        typeof(NightshadeSpearAnimationController))]
    [RequireComponent(typeof(AudioSource))]
    // Unity 입력과 일반 C# Nightshade 전투 로직을 연결한다.
    public sealed class NightshadeSpearController : WorldObjectView, IUnitDeathState, IEnemyDamageReceiver
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
        [SerializeField, Min(0.1f)] private float walkSpeed = 1.6f; // 걷기 속도
        [SerializeField, Min(0.1f)] private float runSpeed = 3.8f; // 달리기 속도
        [SerializeField, Min(1f)] private float turnSpeed = 300f; // 회전 속도
        [SerializeField, Min(0f)] private float maximumAttackRootMotionSpeed = 6f;
        [SerializeField, Min(0f)] private float maximumAttackRootMotionTurnSpeed = 360f;
        [SerializeField] private float gravity = -22f; // Inspector 설정 값
        [SerializeField] private float groundPull = -2f; // Inspector 설정 값

        [Header("공격 판정")]
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField, Min(0f)] private float attackRange = 1.1f;
        [SerializeField] private float attackForwardOffset = 1f;

        [Header("공격 예고 음향")]
        [SerializeField] private AudioSource attackAudioSource;
        [SerializeField] private AudioClip attackReadyClip;
        [SerializeField] private AudioClip lightAttackClip;
        [SerializeField] private AudioClip strongAttackClip;

        private NightShadeSpearAttackRangeDetector attackRangeDetector;

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
            attackRangeDetector?.Tick();
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
                maximumAttackRootMotionSpeed,
                maximumAttackRootMotionTurnSpeed);
            animationController.ConnectAttackRootMotion(
                movement.ApplyAttackRootMotion);
            var stateMachine = new NightshadeSpearStateMachine(
                target,
                movement,
                animationController,
                findRange,
                runStartRange,
                walkSpeed,
                runSpeed,
                turnSpeed,
                deadBodyKeepTime,
                StartAttackHit,
                EndAttackHit,
                WasAttackDamageApplied,
                PlayAttackReadyCue,
                PlayAttackHitCue,
                RequestDeadNightshadeRelease,
                canTrackTarget,
                phaseTwoHealthRate);

            nightshadeWorldUnit = new NightshadeSpearWorldUnit(
                maxHealth,
                stateMachine);

            if (attackOrigin == null)
            {
                attackOrigin = transform;
            }

            attackRangeDetector = new NightShadeSpearAttackRangeDetector(
                attackOrigin,
                targetLayers,
                attackRange,
                attackForwardOffset);
            return nightshadeWorldUnit;
        }

        private void StartAttackHit(AttackDamage damage, int attackNumber)
        {
            attackRangeDetector?.Open(damage);
        }

        private void EndAttackHit()
        {
            attackRangeDetector?.Close();
        }

        private bool WasAttackDamageApplied()
        {
            return attackRangeDetector != null &&
                attackRangeDetector.WasDamageApplied;
        }

        private void PlayAttackReadyCue(int attackNumber)
        {
            if (attackAudioSource != null && attackReadyClip != null)
            {
                attackAudioSource.PlayOneShot(attackReadyClip);
            }
        }

        private void PlayAttackHitCue(
            int attackNumber,
            bool isStrongAttack)
        {
            AudioClip attackClip = isStrongAttack
                ? strongAttackClip
                : lightAttackClip;
            if (attackAudioSource != null && attackClip != null)
            {
                attackAudioSource.PlayOneShot(attackClip);
            }
        }

        private void ConfigureAttackAudioSource()
        {
            if (attackAudioSource == null)
            {
                return;
            }

            attackAudioSource.playOnAwake = false;
            attackAudioSource.spatialBlend = 1f;
            attackAudioSource.dopplerLevel = 0f;
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
            if (attackAudioSource == null)
            {
                attackAudioSource = GetComponent<AudioSource>();
            }

            ConfigureAttackAudioSource();
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

        public void TakeDamage(float damage, Vector3 hitPosition)
        {
            nightshadeWorldUnit?.TakeDamage(damage, hitPosition);
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

            nightshadeWorldUnit.TakeDamage(
                testDamage,
                transform.position);
        }

        private void OnValidate()
        {
            FindUnityComponents();
            findRange = Mathf.Max(0.1f, findRange);
            runStartRange = Mathf.Clamp(runStartRange, 0.1f, findRange);
            walkSpeed = Mathf.Max(0.1f, walkSpeed);
            runSpeed = Mathf.Max(walkSpeed, runSpeed);
            turnSpeed = Mathf.Max(1f, turnSpeed);
            maximumAttackRootMotionSpeed = Mathf.Max(
                0f,
                maximumAttackRootMotionSpeed);
            maximumAttackRootMotionTurnSpeed = Mathf.Max(
                0f,
                maximumAttackRootMotionTurnSpeed);
            deadBodyKeepTime = Mathf.Max(0f, deadBodyKeepTime);
            phaseTwoHealthRate = Mathf.Clamp(
                phaseTwoHealthRate,
                0.1f,
                0.9f);

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
