using rudIsland.RPG3D.Characters;
using Cinemachine;
using rudIsland.RPG3D.Combat.Attack;
using rudIsland.RPG3D.Combat.Detection;
using rudIsland.RPG3D.Combat.Resolution;
using rudIsland.RPG3D.Combat.Result;
using rudIsland.RPG3D.Player.Camera;
using rudIsland.RPG3D.Player.Input;
using rudIsland.RPG3D.Player.Movement;
using rudIsland.RPG3D.Player.States;
using rudIsland.RPG3D.Player.States.Target;
using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Player
{
    [RequireComponent(typeof(CharacterController))]
    // Unity 생명주기에서 플레이어 입력, 이동, Animator를 연결한다.
    public sealed class PlayerController : MonoBehaviour, IAttackHitReceiver
    {
        private const int ActiveCameraPriority = 20;
        private const int InactiveCameraPriority = 10;

        [Header("필수 연결")]
        [SerializeField] private WorldObjectManager worldObjectManager; // 씬 또는 시스템 참조
        [SerializeField] private Transform moveCamera; // 이동 정보
        [SerializeField] private Animator playerAnimator; // 애니메이터 참조

        [Header("타깃 전환")]
        [SerializeField] private CinemachineFreeLook playerFreeLookCamera;
        [SerializeField] private CinemachineFreeLook playerTargetLookCamera;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField, Min(0f)] private float targetRange = 12f;
        [SerializeField, Min(0f)] private float targetBreakDistance = 15f;
        [SerializeField, Range(0f, 180f)] private float targetMaximumAngle = 70f;
        [SerializeField, Min(0f)] private float targetCameraTurnSpeed = 540f;
        [SerializeField, Range(0f, 1f)] private float targetCameraVerticalValue = 0.5f;

        [Header("생명")]
        [SerializeField, Min(1f)] private float maxHealth = 100f; // 최대 체력
        [Header("공격별 타격")]
        [SerializeField] private AttackHitSettings[] attackHitSettings; // 행동 설정 참조

        [Header("경직")]
        [SerializeField, Min(0.01f)] private float staggerLimit = 10f; // 피격 동작이 나올 경직 한계
        [SerializeField, Min(0f)] private float staggerRecoverDelay = 1f; // 경직 회복을 기다리는 시간
        [SerializeField, Min(0f)] private float staggerRecoverSpeed = 20f; // 1초에 회복할 경직 수치

        [Header("가드")]
        [SerializeField, Range(0f, 180f)] private float guardAngle = 120f;

        [Header("피격 밀림")]
        [SerializeField, Min(0.01f)] private float hitPushTime = 0.18f; // 피격 또는 피해 관련 값

        [Header("Unit 간격")]
        [SerializeField] private LayerMask unitCollisionLayers =
            (1 << 6) | (1 << 7);
        [SerializeField, Min(0f)]
        private float minimumUnitSeparation = 0.2f;

#if UNITY_EDITOR
        [Header("체력 확인")]
        [SerializeField, Min(0f)] private float testDamage = 10f; // 피격 또는 피해 관련 값
#endif

        [Header("회전")]
        [SerializeField] private float turnSpeed = 720f; // 회전 속도

        [Header("입력 이동")]
        [SerializeField, Min(0f)] private float walkSpeed = 2.5f; // 걷기 속도
        [SerializeField, Min(0f)] private float sprintSpeed = 5f; // 달리기 속도

        [Header("이동 애니메이션")]
        [SerializeField] private float animationSmoothTime = 0.12f; // 시간 설정

        [Header("구르기 이동 거리")]
        [Tooltip("1이면 원래 거리, 0.5면 절반, 1.5면 1.5배 이동합니다.")]
        [SerializeField, Min(0f)] private float rollDistanceScale = 0.1f; // 거리 설정

        [Header("콤보 연결 시점 (0~1)")]
        [Tooltip("1타 재생이 이 비율을 넘은 뒤부터 2타 입력을 받습니다.")]
        [SerializeField, Range(0f, 1f)] private float attack01NextInputTime = 0.46f; // 공격 관련 설정 또는 상태
        [Tooltip("2타 재생이 이 비율을 넘은 뒤부터 3타 입력을 받습니다.")]
        [SerializeField, Range(0f, 1f)] private float attack02NextInputTime = 0.58f; // 공격 관련 설정 또는 상태
        [Tooltip("3타 재생이 이 비율을 넘은 뒤부터 4타 입력을 받습니다.")]
        [SerializeField, Range(0f, 1f)] private float attack03NextInputTime = 0.52f; // 공격 관련 설정 또는 상태
        [Tooltip("4타 재생이 이 비율을 넘은 뒤부터 5타 입력을 받습니다.")]
        [SerializeField, Range(0f, 1f)] private float attack04NextInputTime = 0.50f; // 공격 관련 설정 또는 상태

        [Header("콤보 입력 버퍼")]
        [Tooltip("콤보 연결 시점 전에 누른 공격 입력을 보관하는 시간입니다.")]
        [SerializeField, Min(0f)] private float comboInputBufferDuration = 0.25f; // 시간 설정

        [Header("공격 전진 거리 비율 (0~1)")]
        [SerializeField, Range(0f, 1f)] private float attack01MoveScale = 0.35f; // 공격 관련 설정 또는 상태
        [SerializeField, Range(0f, 1f)] private float attack02MoveScale = 0.50f; // 공격 관련 설정 또는 상태
        [SerializeField, Range(0f, 1f)] private float attack03MoveScale = 0.45f; // 공격 관련 설정 또는 상태
        [SerializeField, Range(0f, 1f)] private float attack04MoveScale = 0.40f; // 공격 관련 설정 또는 상태
        [SerializeField, Range(0f, 1f)] private float attack05MoveScale = 0.30f; // 공격 관련 설정 또는 상태
        [SerializeField, Range(0f, 1f)] private float runAttackMoveScale = 0.45f; // 공격 관련 설정 또는 상태

        [Header("중력")]
        [SerializeField] private float gravity = -22f; // Inspector 설정 값
        [SerializeField] private float groundPull = -2f; // Inspector 설정 값

        private CharacterController characterController; // 씬 또는 시스템 참조
        private PlayerInputReader playerInput; // 입력 또는 행동 여부
        private PlayerStateMachine playerStateMachine; // 현재 행동 상태
        private PlayerMovement playerMovement; // 이동 정보
        private PlayerWorldUnit playerWorldUnit; // 씬 또는 시스템 참조
        private MeleeHitDetector activeHitDetector; // 피격 또는 피해 관련 값

        public bool IsAttackHitActive =>
            activeHitDetector != null;
        public HitReaction LastHitReaction =>
            playerStateMachine != null
                ? playerStateMachine.LastHitReaction
                : default;

        private void Awake()
        {
            if (worldObjectManager == null ||
                moveCamera == null ||
                playerFreeLookCamera == null ||
                playerTargetLookCamera == null)
            {
                Debug.LogError(
                    "PlayerController에 WorldObjectManager와 이동 기준 카메라가 필요합니다.",
                    this);
                enabled = false;
                return;
            }

            characterController = GetComponent<CharacterController>();
            EnsureAttackHitSettings(
                GetComponentInChildren<MeleeHitDetector>(true));
            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>();
            }

            if (playerAnimator != null)
            {
                playerAnimator.applyRootMotion = true;
            }

            playerInput = new PlayerInputReader();
            var movementSeparation = new UnitMovementSeparation(
                characterController,
                unitCollisionLayers,
                minimumUnitSeparation);
            playerMovement = new PlayerMovement(
                transform,
                moveCamera,
                characterController,
                movementSeparation,
                playerInput,
                turnSpeed,
                walkSpeed,
                sprintSpeed,
                gravity,
                groundPull,
                hitPushTime);
            var targetFinder = new PlayerTargetFinder(
                transform,
                moveCamera,
                targetLayers,
                targetRange,
                targetMaximumAngle);
            var targetCamera = new PlayerTargetCamera(
                transform,
                playerFreeLookCamera,
                playerTargetLookCamera,
                ActiveCameraPriority,
                InactiveCameraPriority,
                targetCameraTurnSpeed,
                targetCameraVerticalValue);
            playerStateMachine = new PlayerStateMachine(
                playerInput,
                playerMovement,
                playerAnimator,
                animationSmoothTime,
                rollDistanceScale,
                attack01NextInputTime,
                attack02NextInputTime,
                attack03NextInputTime,
                attack04NextInputTime,
                comboInputBufferDuration,
                attack01MoveScale,
                attack02MoveScale,
                attack03MoveScale,
                attack04MoveScale,
                attack05MoveScale,
                runAttackMoveScale,
                targetFinder,
                targetCamera,
                Mathf.Max(targetRange, targetBreakDistance),
                EndAttackHit,
                StartGuard,
                StopGuard);
            playerWorldUnit = new PlayerWorldUnit(
                maxHealth,
                staggerLimit,
                staggerRecoverDelay,
                staggerRecoverSpeed,
                guardAngle,
                playerInput,
                playerStateMachine);
            worldObjectManager.Register(playerWorldUnit);
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerWorldUnit != null)
            {
                worldObjectManager.Enable(playerWorldUnit);
            }
        }

        // 구르기·방어·공격·사망 애니메이션의 루트 모션을 플레이어 루트에 적용한다.
        public void ApplyRootMotion(
            Vector3 deltaPosition,
            Quaternion deltaRotation)
        {
            playerStateMachine?.ApplyRootMotion(deltaPosition, deltaRotation);
        }

        public void TakeDamage(float damage)
        {
            playerWorldUnit?.TakeDamage(damage);
        }

        public bool CanTakeHit =>
            playerWorldUnit != null && playerWorldUnit.CanTakeHit;

        public int ActivationSequence =>
            playerWorldUnit != null ? playerWorldUnit.ActivationSequence : 0;

        public AttackHitResult ReceiveAttackHit(in AttackHitInput hit)
        {
            if (!CanTakeHit)
            {
                return AttackHitResult.Ignored;
            }

            return playerWorldUnit.ReceiveAttackHit(
                in hit,
                transform.forward);
        }

        public void StartAttackHit(int attackNumber)
        {
            EndAttackHit();

            if (!AttackHitSettings.TryFind(attackHitSettings,attackNumber, out AttackHitSettings hitSettings) 
            || hitSettings.HitDetector == null)
            {
                return;
            }

            var hit = new AttackHitInput(
                hitSettings.Damage,
                UnitTeam.Player,
                attackNumber,
                hitSettings.Strength,
                hitSettings.StaggerDamage,
                hitSettings.BlockStaminaDamage,
                hitSettings.CanBeBlocked,
                hitSettings.CanBeParried,
                hitSettings.PushDistance,
                hitSettings.HitStopTime,
                default);
            activeHitDetector = hitSettings.HitDetector;
            activeHitDetector.StartHit(in hit);
        }

        public void EndAttackHit()
        {
            activeHitDetector?.EndHit();
            activeHitDetector = null;
        }

        private void StartGuard()
        {
            playerWorldUnit?.StartGuard();
        }

        private void StopGuard()
        {
            playerWorldUnit?.StopGuard();
        }

        public void NotifyAttackHitEnded()
        {
            EndAttackHit();
            playerStateMachine?.NotifyAttackHitEnded();
        }

        internal void NotifyAttackAnimationEnded()
        {
            EndAttackHit();
            playerStateMachine?.NotifyAttackAnimationEnded();
        }

        private void OnDisable()
        {
            EndAttackHit();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playerWorldUnit != null && worldObjectManager != null)
            {
                worldObjectManager.Disable(playerWorldUnit);
            }
        }

        private void OnDestroy()
        {
            if (playerWorldUnit == null)
            {
                return;
            }

            if (worldObjectManager != null)
            {
                worldObjectManager.Unregister(playerWorldUnit);
                return;
            }

            playerWorldUnit.Dispose();
        }

        private void EnsureAttackHitSettings(
            MeleeHitDetector defaultHitDetector)
        {
            if (attackHitSettings != null &&
                attackHitSettings.Length > 0)
            {
                return;
            }

            attackHitSettings =
                CreateDefaultAttackHitSettings(defaultHitDetector);
        }

        private static AttackHitSettings[]
            CreateDefaultAttackHitSettings(
                MeleeHitDetector defaultHitDetector)
        {
            return new[]
            {
                new AttackHitSettings(
                    1, defaultHitDetector, new AttackDamage(10f), 10f, 0.40f),
                new AttackHitSettings(
                    2, defaultHitDetector, new AttackDamage(12f), 10f, 0.40f),
                new AttackHitSettings(
                    3, defaultHitDetector, new AttackDamage(14f), 10f, 0.40f),
                new AttackHitSettings(
                    4, defaultHitDetector, new AttackDamage(17f), 10f, 0.45f),
                new AttackHitSettings(
                    5,
                    defaultHitDetector,
                    new AttackDamage(25f),
                    HitStrength.Heavy,
                    10f,
                    0.55f),
                new AttackHitSettings(
                    6,
                    defaultHitDetector,
                    new AttackDamage(18f),
                    HitStrength.Heavy,
                    10f,
                    0.50f)
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (AttackHitSettings.HasDuplicateAttackNumber(
                    attackHitSettings))
            {
                Debug.LogError(
                    "PlayerController의 공격 번호가 중복되었습니다.",
                    this);
            }
        }

        [ContextMenu("Test Damage")]
        private void TestDamage()
        {
            if (!Application.isPlaying || playerWorldUnit == null)
            {
                Debug.LogWarning(
                    "Test Damage는 Play 중이고 플레이어 준비가 끝난 뒤 사용할 수 있습니다.",
                    this);
                return;
            }

            float healthBeforeDamage = playerWorldUnit.CurrentHealth;
            playerWorldUnit.TakeDamage(testDamage);

            Debug.Log(
                $"플레이어 체력: {healthBeforeDamage} → {playerWorldUnit.CurrentHealth}",
                this);
        }
#endif
    }
}
