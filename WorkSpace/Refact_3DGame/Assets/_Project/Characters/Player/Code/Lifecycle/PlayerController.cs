using rudIsland.RPG3D.Characters;
using Cinemachine;
using rudIsland.RPG3D.Player.Camera;
using rudIsland.RPG3D.Player.Input;
using rudIsland.RPG3D.Player.Movement;
using rudIsland.RPG3D.Player.States;
using rudIsland.RPG3D.Player.States.Attack;
using rudIsland.RPG3D.Player.States.Target;
using rudIsland.RPG3D.World;
using rudIsland.RPG3D.Player.Runtime.Hit;
using rudIsland.RPG3D.Characters.Combat.AttackData;
using UnityEngine;

namespace rudIsland.RPG3D.Player
{
    [RequireComponent(typeof(CharacterController))]
    // Unity 생명주기에서 플레이어 입력, 이동, Animator를 연결한다.
    public sealed class PlayerController : WorldObjectView, IPlayerDamageReceiver
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

        [Header("공격 데이터")]
        [SerializeField] private PlayerAttackData[] attackData =
            new PlayerAttackData[6];

        [Header("콤보 입력 버퍼")]
        [Tooltip("콤보 연결 시점 전에 누른 공격 입력을 보관하는 시간입니다.")]
        [SerializeField, Min(0f)] private float comboInputBufferDuration = 0.25f; // 시간 설정

        [Header("중력")]
        [SerializeField] private float gravity = -22f; // Inspector 설정 값
        [SerializeField] private float groundPull = -2f; // Inspector 설정 값

        [Header("공격 범위 확인")]
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private LayerMask attackLayers;
        [SerializeField, Min(0f)] private float attackRadius = 1.1f;
        [SerializeField] private float attackForwardOffset = 1f;

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

            if (!HasValidAttackData())
            {
                Debug.LogError(
                    "PlayerController에 공격 1~6 PlayerAttackData가 필요합니다.",
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

            //공격자의 현재 위치가 null이면 PlayerController의 transform을 공격 시작 위치로 사용한다.
            if(attackOrigin==null) attackOrigin = transform;
            LayerMask attackMask =
            attackLayers.value != 0
                ? attackLayers
                : targetLayers;

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
                groundPull);
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
                attackData,
                comboInputBufferDuration,
                targetFinder,
                targetCamera,
                Mathf.Max(targetRange, targetBreakDistance),
                attackOrigin,
                attackMask,
                attackRadius,
                attackForwardOffset
                );
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

        public bool TryTakeDamage(AttackDamage attackDamage)
        {
            return playerWorldUnit != null &&
                playerWorldUnit.TryTakeDamage(attackDamage);
        }


        public void StartAttackHit(int attackNumber)
        {
            playerStateMachine?.BeginAttackHit(attackNumber);
        }

        public void EndAttackHit()
        {
            playerStateMachine?.EndAttackHit();
        }

        public void NotifyAttackHitEnded()
        {
            playerStateMachine?.EndAttackHit(); //공격 판정 윈도우 종료
            playerStateMachine?.NotifyAttackHitEnded(); //콤보 공격 입력 가능 상태로 전환
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

        private bool HasValidAttackData()
        {
            if (attackData == null || attackData.Length != 6)
            {
                return false;
            }

            for (int index = 0; index < attackData.Length; index++)
            {
                if (attackData[index] == null ||
                    attackData[index].AttackNumber != index + 1)
                {
                    return false;
                }
            }

            return true;
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

        protected override IWorldObject CreateRuntimeObject()
        {
            throw new System.NotImplementedException();
        }
#endif
    }
}
