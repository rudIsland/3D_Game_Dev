using rudIsland.RPG3D.Characters;
using Cinemachine;
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
    public sealed class PlayerController : MonoBehaviour
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

#if UNITY_EDITOR
        [Header("체력 확인")]
        [SerializeField, Min(0f)] private float testDamage = 10f; // 피격 또는 피해 관련 값
#endif

        [Header("회전")]
        [SerializeField] private float turnSpeed = 360f; // 이동 속도

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
            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>();
            }

            if (playerAnimator != null)
            {
                playerAnimator.applyRootMotion = true;
            }

            playerInput = new PlayerInputReader();
            playerMovement = new PlayerMovement(
                transform,
                moveCamera,
                characterController,
                playerInput,
                turnSpeed,
                walkSpeed,
                sprintSpeed,
                gravity,
                groundPull);            var targetFinder = new PlayerTargetFinder(
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
                Mathf.Max(targetRange, targetBreakDistance));
            playerWorldUnit = new PlayerWorldUnit(
                maxHealth,
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


        public void StartAttackHit(int attackNumber)
        {
            // 기존 애니메이션 이벤트 이름은 유지하고, 새 공격 판정은 상태머신에서 구현한다.
        }

        public void EndAttackHit()
        {
        }

        public void NotifyAttackHitEnded()
        {
            playerStateMachine?.NotifyAttackHitEnded();
        }

        internal void NotifyAttackAnimationEnded()
        {
            playerStateMachine?.NotifyAttackAnimationEnded();
        }

        private void OnDisable()
        {
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


#if UNITY_EDITOR
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
