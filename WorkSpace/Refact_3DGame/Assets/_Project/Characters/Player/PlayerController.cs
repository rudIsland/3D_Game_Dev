using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Player.Input;
using rudIsland.RPG3D.Player.Movement;
using rudIsland.RPG3D.Player.States;
using rudIsland.RPG3D.World;
using UnityEngine;
using UnityEngine.Serialization;

namespace rudIsland.RPG3D.Player
{
    [RequireComponent(typeof(CharacterController))]
    // Unity 생명주기에서 입력, 이동, 애니메이션을 연결한다.
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("필수 연결")]
        [SerializeField] private WorldObjectManager worldObjectManager;
        [SerializeField] private Transform moveCamera;
        [SerializeField] private Animator playerAnimator;

        [Header("생명")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        [Header("이동")]
        [SerializeField] private float walkSpeed = 2.8f;
        [SerializeField] private float sprintSpeed = 5.5f;
        [SerializeField] private float turnSpeed = 720f;
        [SerializeField, Min(0.01f)] private float moveAcceleration = 28f;
        [SerializeField, Min(0.01f)] private float moveDeceleration = 36f;

        [Header("Attack Direction")]
        [SerializeField] private Transform lockOnTarget;
        [SerializeField, Min(0f)] private float attackTurnSpeed = 360f;

        [Header("이동 애니메이션")]
        [SerializeField] private float animationSmoothTime = 0.12f;

        [Header("회피 구르기")]
        [SerializeField]
        [Tooltip("X축은 롤 시작 속도 비율, Y축은 총 구르기 거리입니다.")]
        private AnimationCurve rollDistanceByStartSpeed =
            CreateDefaultRollDistanceByStartSpeed();

        [SerializeField]
        [Tooltip("X축은 롤 재생 시간 비율, Y축은 누적 이동 거리 비율입니다.")]
        private AnimationCurve rollMoveProgressByTime =
            CreateDefaultRollMoveProgressByTime();

        [SerializeField, Range(0f, 1f)]
        [Tooltip("이 속도 비율 이상으로 롤을 시작하면 달리기 롤을 재생합니다.")]
        private float sprintRollStartSpeedRatio = 0.85f;
        [FormerlySerializedAs("rollTotalTime")]
        [SerializeField, Min(0.01f)] private float normalRollTotalTime = 0.97f;
        [SerializeField, Min(0.01f)] private float sprintRollTotalTime = 0.73f;
        [SerializeField, Min(0.01f)] private float rollTurnTime = 0.1f;

        [Header("공격")]
        [SerializeField, Min(0.01f)] private float attack01TotalTime = 0.97f;
        [SerializeField, Min(0.01f)] private float attack02TotalTime = 1.13f;
        [SerializeField, Min(0.01f)] private float attack03TotalTime = 0.98f;
        [SerializeField, Min(0.01f)] private float attack04TotalTime = 1.18f;
        [SerializeField, Min(0.01f)] private float attack05TotalTime = 1.47f;
        [SerializeField, Min(0f)] private float attack01NextTime = 0.65f;
        [SerializeField, Min(0f)] private float attack02NextTime = 0.72f;
        [SerializeField, Min(0f)] private float attack03NextTime = 0.65f;
        [SerializeField, Min(0f)] private float attack04NextTime = 0.65f;
        [SerializeField, Min(0f)] private float attack01MoveScale = 0.4f;
        [SerializeField, Min(0f)] private float attack02MoveScale = 0.4f;
        [SerializeField, Min(0f)] private float attack03MoveScale = 0.5f;
        [SerializeField, Min(0f)] private float attack04MoveScale = 0.5f;
        [SerializeField, Min(0f)] private float attack05MoveScale = 0.5f;
        [SerializeField, Min(0.01f)] private float runAttackTotalTime = 0.94f;
        [SerializeField, Min(0f)]
        [Tooltip("이동하면서 달리기를 이 시간 이상 유지해야 달리기 공격을 사용합니다.")]
        private float runAttackMinimumSprintTime = 0.35f;
        [SerializeField, Range(0f, 1f)]
        private float runAttackStartSpeedRatio = 0.95f;
        [SerializeField, Min(0f)] private float runAttackMoveScale = 1f;

        [Header("중력")]
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float groundPull = -2f;

        private CharacterController characterController;
        private PlayerInputReader playerInput;
        private PlayerStateMachine playerStateMachine;
        private PlayerMovement playerMovement;
        private PlayerWorldUnit playerWorldUnit;

        public bool IsBlocking => playerWorldUnit?.IsBlocking == true;

        // Inspector에서 커브가 비어 있으면 기본값을 채운다.
        private void OnValidate()
        {
            EnsureRollCurves();
        }

        // 필수 컴포넌트와 입력·이동 객체를 한 번 생성한다.
        private void Awake()
        {
            if (worldObjectManager == null)
            {
                Debug.LogError("PlayerController에 WorldObjectManager가 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }

            if (moveCamera == null)
            {
                Debug.LogError("PlayerController에 이동 기준 카메라가 연결되지 않았습니다.", this);
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
            EnsureRollCurves();

            playerMovement = new PlayerMovement(
                transform,
                moveCamera,
                characterController,
                playerInput,
                walkSpeed,
                sprintSpeed,
                turnSpeed,
                attackTurnSpeed,
                moveAcceleration,
                moveDeceleration,
                rollDistanceByStartSpeed,
                rollMoveProgressByTime,
                sprintRollStartSpeedRatio,
                normalRollTotalTime,
                sprintRollTotalTime,
                rollTurnTime,
                gravity,
                groundPull);

            playerStateMachine = new PlayerStateMachine(
                playerInput,
                playerMovement,
                characterController,
                playerAnimator,
                sprintSpeed,
                animationSmoothTime,
                attack01TotalTime,
                attack02TotalTime,
                attack03TotalTime,
                attack04TotalTime,
                attack05TotalTime,
                attack01NextTime,
                attack02NextTime,
                attack03NextTime,
                attack04NextTime,
                attack01MoveScale,
                attack02MoveScale,
                attack03MoveScale,
                attack04MoveScale,
                attack05MoveScale,
                runAttackTotalTime,
                runAttackMinimumSprintTime,
                runAttackStartSpeedRatio,
                runAttackMoveScale);
            playerStateMachine.SetLockOnTarget(lockOnTarget);

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

        // Animator 자식이 전달한 공격 이동을 플레이어 루트에 적용한다.
        public void ApplyAttackAnimationMove(Vector3 deltaPosition)
        {
            playerStateMachine?.ApplyAttackAnimationMove(deltaPosition);
        }

        // 방패를 들고 있을 때만 방어 피격 애니메이션을 재생한다.
        public void PlayBlockImpact()
        {
            playerWorldUnit?.PlayBlockImpact();
        }

        public void SetLockOnTarget(Transform target)
        {
            lockOnTarget = target;
            playerStateMachine?.SetLockOnTarget(target);
        }

        public void ClearLockOnTarget()
        {
            SetLockOnTarget(null);
        }

        // 비어 있는 롤 커브만 기본 설정으로 복구한다.
        private void EnsureRollCurves()
        {
            if (rollDistanceByStartSpeed == null || rollDistanceByStartSpeed.length == 0)
            {
                rollDistanceByStartSpeed = CreateDefaultRollDistanceByStartSpeed();
            }

            if (rollMoveProgressByTime == null || rollMoveProgressByTime.length == 0)
            {
                rollMoveProgressByTime = CreateDefaultRollMoveProgressByTime();
            }
        }

        // 정지 1.65m에서 최고속도 3.25m까지 거리를 늘린다.
        private static AnimationCurve CreateDefaultRollDistanceByStartSpeed()
        {
            AnimationCurve curve = AnimationCurve.Linear(
                0f,
                1.65f,
                1f,
                3.25f);
            curve.preWrapMode = WrapMode.Clamp;
            curve.postWrapMode = WrapMode.Clamp;
            return curve;
        }

        // 전체 시간의 75%까지 이동하고 남은 시간에는 일어난다.
        private static AnimationCurve CreateDefaultRollMoveProgressByTime()
        {
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0.15f),
                new Keyframe(0.12f, 0.03f, 0.4f, 0.8f),
                new Keyframe(0.36f, 0.45f, 1.9f, 1.9f),
                new Keyframe(0.60f, 0.88f, 1.1f, 1.1f),
                new Keyframe(0.75f, 1f, 0f, 0f),
                new Keyframe(1f, 1f, 0f, 0f));
            curve.preWrapMode = WrapMode.Clamp;
            curve.postWrapMode = WrapMode.Clamp;
            return curve;
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
    }
}
