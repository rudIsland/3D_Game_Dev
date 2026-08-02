using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Combat;
using rudIsland.RPG3D.Player.Input;
using rudIsland.RPG3D.Player.Movement;
using rudIsland.RPG3D.Player.States;
using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Player
{
    [RequireComponent(typeof(CharacterController))]
    // Unity 생명주기에서 플레이어 입력, 이동, Animator를 연결한다.
    public sealed class PlayerController : MonoBehaviour, IAttackHitReceiver
    {
        [Header("필수 연결")]
        [SerializeField] private WorldObjectManager worldObjectManager;
        [SerializeField] private Transform moveCamera;
        [SerializeField] private Animator playerAnimator;

        [Header("생명")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private MeleeHitDetector attackHitDetector;
        [SerializeField] private PlayerAttackDamage[] attackDamages =
            CreateDefaultAttackDamages();

#if UNITY_EDITOR
        [Header("체력 확인")]
        [SerializeField, Min(0f)] private float testDamage = 10f;
#endif

        [Header("회전")]
        [SerializeField] private float turnSpeed = 720f;

        [Header("입력 이동")]
        [SerializeField, Min(0f)] private float walkSpeed = 2.5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 5f;

        [Header("이동 애니메이션")]
        [SerializeField] private float animationSmoothTime = 0.12f;

        [Header("구르기 이동 거리")]
        [Tooltip("1이면 원래 거리, 0.5면 절반, 1.5면 1.5배 이동합니다.")]
        [SerializeField, Min(0f)] private float rollDistanceScale = 0.75f;

        [Header("콤보 연결 시점 (0~1)")]
        [Tooltip("1타 재생이 이 비율을 넘은 뒤부터 2타 입력을 받습니다.")]
        [SerializeField, Range(0f, 1f)] private float attack01NextInputTime = 0.42f;
        [Tooltip("2타 재생이 이 비율을 넘은 뒤부터 3타 입력을 받습니다.")]
        [SerializeField, Range(0f, 1f)] private float attack02NextInputTime = 0.52f;
        [Tooltip("3타 재생이 이 비율을 넘은 뒤부터 4타 입력을 받습니다.")]
        [SerializeField, Range(0f, 1f)] private float attack03NextInputTime = 0.52f;
        [Tooltip("4타 재생이 이 비율을 넘은 뒤부터 5타 입력을 받습니다.")]
        [SerializeField, Range(0f, 1f)] private float attack04NextInputTime = 0.48f;

        [Header("콤보 입력 버퍼")]
        [Tooltip("콤보 연결 시점 전에 누른 공격 입력을 보관하는 시간입니다.")]
        [SerializeField, Min(0f)] private float comboInputBufferDuration = 0.15f;

        [Header("공격 전진 거리 비율 (0~1)")]
        [SerializeField, Range(0f, 1f)] private float attack01MoveScale = 0.65f;
        [SerializeField, Range(0f, 1f)] private float attack02MoveScale = 0.65f;
        [SerializeField, Range(0f, 1f)] private float attack03MoveScale = 0.55f;
        [SerializeField, Range(0f, 1f)] private float attack04MoveScale = 0.60f;
        [SerializeField, Range(0f, 1f)] private float attack05MoveScale = 0.50f;
        [SerializeField, Range(0f, 1f)] private float runAttackMoveScale = 0.80f;

        [Header("중력")]
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float groundPull = -2f;

        private CharacterController characterController;
        private PlayerInputReader playerInput;
        private PlayerStateMachine playerStateMachine;
        private PlayerMovement playerMovement;
        private PlayerWorldUnit playerWorldUnit;

        private void Awake()
        {
            if (worldObjectManager == null || moveCamera == null)
            {
                Debug.LogError(
                    "PlayerController에 WorldObjectManager와 이동 기준 카메라가 필요합니다.",
                    this);
                enabled = false;
                return;
            }

            characterController = GetComponent<CharacterController>();
            if (attackHitDetector == null)
            {
                attackHitDetector =
                    GetComponentInChildren<MeleeHitDetector>(true);
            }

            EnsureAttackDamages();
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
                groundPull);
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
                EndAttackHit);
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

        public void ReceiveHit(in AttackHitData hit)
        {
            playerWorldUnit?.ApplyHit(in hit);
        }

        public void StartAttackHit(int attackNumber)
        {
            EndAttackHit();

            if (attackHitDetector == null ||
                !PlayerAttackDamage.TryGetDamage(
                    attackDamages,
                    attackNumber,
                    out AttackDamage damage))
            {
                return;
            }

            var hit = new AttackHitData(
                damage,
                UnitTeam.Player,
                attackNumber);
            attackHitDetector.StartHit(in hit);
        }

        public void EndAttackHit()
        {
            attackHitDetector?.EndHit();
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

        private void EnsureAttackDamages()
        {
            if (attackDamages != null && attackDamages.Length > 0)
            {
                return;
            }

            attackDamages = CreateDefaultAttackDamages();
        }

        private static PlayerAttackDamage[] CreateDefaultAttackDamages()
        {
            var defaultDamages = new PlayerAttackDamage[6];
            for (int index = 0; index < defaultDamages.Length; index++)
            {
                defaultDamages[index] = new PlayerAttackDamage(
                    index + 1,
                    new AttackDamage(10f));
            }

            return defaultDamages;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (PlayerAttackDamage.HasDuplicateAttackNumber(attackDamages))
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
