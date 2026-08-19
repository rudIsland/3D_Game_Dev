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
using rudIsland.RPG3D.Player.Runtime;
using rudIsland.RPG3D.Player.Runtime.Attack;
using rudIsland.RPG3D.Player.Runtime.Audio;
using UnityEngine;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Characters.Combat.AttackData;
using UnityEngine.Serialization;

namespace rudIsland.RPG3D.Player
{
    [RequireComponent(
        typeof(CharacterController),
        typeof(CombatHitEffectPlayer),
        typeof(PlayerAttackEffectPlayer))]
    // Unity 생명주기에서 플레이어 입력, 이동, Animator를 연결한다.
    public sealed class PlayerController :
        MonoBehaviour,
        IPlayerDamageReceiver,
        IUnitDeathState
    {
        [Header("필수 연결")]
        [SerializeField] private WorldObjectManager worldObjectManager; // 씬 또는 시스템 참조
        [SerializeField] private Transform moveCamera; // 이동 정보
        [SerializeField] private Animator playerAnimator; // 애니메이터 참조

        [Header("락온")]
        [SerializeField] private CinemachineFreeLook playerFreeLookCamera;
        [SerializeField] private CinemachineFreeLook playerTargetLookCamera;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private LayerMask targetObstructionLayers;
        [SerializeField, Min(0f)] private float targetRange = 12f;
        [SerializeField, Min(0f)] private float targetBreakDistance = 15f;
        [SerializeField, Range(0f, 180f)] private float targetMaximumAngle = 70f;
        [SerializeField, Min(0f)] private float targetHiddenGraceDuration = 0.35f;
        [SerializeField, Min(0f)] private float targetHeightOffset = 1.2f;

        [Header("생명")]
        [SerializeField, Min(1f)] private float maxHealth = 100f; // 최대 체력

        [Header("Stamina")]
        [SerializeField, Min(1f)] private float maxStamina = 100f;
        [SerializeField, Min(0f)] private float staminaRecoverDelay = 0.8f;
        [SerializeField, Min(0f)] private float staminaRecoverSpeed = 35f;
        [SerializeField, Range(0f, 1f)]
        private float guardStaminaRecoveryRate = 0f;
        [SerializeField, Min(0f)] private float rollStaminaCost = 25f;
        [SerializeField, Min(0f)]
        private float sprintStaminaCostPerSecond = 15f;
        [SerializeField, Min(0f)] private float sprintRestartStamina = 20f;

#if UNITY_EDITOR
        [Header("체력 확인")]
        [SerializeField, Min(0f)] private float testDamage = 10f; // 피격 또는 피해 관련 값
#endif

        [Header("회전")]
        [FormerlySerializedAs("turnSpeed")]
        [SerializeField, Min(0f)] private float freeMoveTurnSpeed = 720f;
        [SerializeField, Min(0f)] private float targetMoveTurnSpeed = 540f;
        [SerializeField, Min(0f)] private float attackTurnSpeed = 360f;

        [Header("입력 이동")]
        [SerializeField, Min(0f)] private float walkSpeed = 2.8f; // 걷기 속도
        [SerializeField, Min(0f)] private float guardMoveSpeed = 1.5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 5.5f; // 달리기 속도
        [SerializeField, Min(0f)] private float moveAcceleration = 30f;
        [SerializeField, Min(0f)] private float moveDeceleration = 40f;

        [Header("이동 애니메이션")]
        [SerializeField] private float animationSmoothTime = 0.06f; // 시간 설정

        [Header("구르기 동작 이동")]
        [FormerlySerializedAs("rollDistanceScale")]
        [SerializeField, Min(0f)] private float rollDistance = 2.2f;
        [SerializeField, Min(0f)] private float sprintRollDistance = 2.5f;
        [SerializeField, Range(0.01f, 1f)]
        private float rollCompleteNormalizedTime = 0.7f;
        [SerializeField]
        private AnimationCurve rollMovementCurve = CreateDefaultRollMovementCurve();

        [Header("이동 소리")]
        [SerializeField] private AudioClip[] walkFootstepSounds;
        [SerializeField] private AudioClip[] runFootstepSounds;
        [SerializeField] private AudioClip rollSound;
        [SerializeField, Range(0f, 1f)] private float walkFootstepVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float runFootstepVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float rollSoundVolume = 0.8f;
        [SerializeField, Range(0f, 0.2f)] private float footstepPitchChange = 0.05f;

        [Header("피격음")]
        [SerializeField] private PlayerDamageAudio playerDamageAudio;

        [Header("공격 데이터")]
        [SerializeField] private PlayerAttackData[] attackData = new PlayerAttackData[6];

        [Header("행동 입력 버퍼")]
        [Tooltip("공격 중 미리 누른 다음 공격과 구르기 입력을 보관하는 시간입니다.")]
        [FormerlySerializedAs("attackInputBufferDuration")]
        [SerializeField, Min(0f)]
        private float actionInputBufferDuration = 0.2f;

        [Header("중력")]
        [SerializeField] private float gravity = -22f; // Inspector 설정 값
        [SerializeField] private float groundPull = -2f; // Inspector 설정 값

        [Header("검 공격 판정")]
        [SerializeField] private Transform weaponHitStart;
        [SerializeField] private Transform weaponHitEnd;
        [SerializeField] private LayerMask attackLayers;
        [FormerlySerializedAs("attackRadius")]
        [SerializeField, Min(0f)] private float weaponHitRadius = 0.12f;

        [Header("방어 판정")]
        [SerializeField, Range(0f, 180f)] private float guardAngle = 120f;
        [SerializeField, Min(0f)] private float guardRaiseDuration = 0.1f;
        [SerializeField, Min(0f)]
        private float guardBreakControlLockDuration = 1f;
        [SerializeField] private PlayerGuardHitBox guardHitBox;

        [Header("피격 이동")]
        [SerializeField, Min(0.01f)] private float hitPushDuration = 0.15f;
        [SerializeField]
        private AnimationCurve hitPushCurve = CreateDefaultHitPushCurve();

        [Header("행동 중단")]
        [SerializeField, Min(1f)] private float stopPointLimit = 30f;
        [SerializeField, Min(0f)] private float stopPointRecoverDelay = 1.5f;
        [SerializeField, Min(0f)] private float stopPointRecoverSpeed = 15f;

        private CharacterController characterController; // 씬 또는 시스템 참조
        private PlayerInputReader playerInput; // 입력 또는 행동 여부
        private PlayerStateMachine playerStateMachine; // 현재 행동 상태
        private PlayerMovement playerMovement; // 이동 정보
        private PlayerWorldUnit playerWorldUnit; // 씬 또는 시스템 참조
        private CombatHitEffectPlayer hitEffectPlayer;
        private PlayerAttackEffectPlayer attackEffectPlayer;
        private PlayerFootstepAudio footstepAudio;

        // 적이 플레이어를 계속 추적할 수 있는지 확인할 때 사용한다.
        public bool IsDead =>
            playerWorldUnit != null && playerWorldUnit.IsDead;

        private void Awake()
        {
            if (worldObjectManager == null ||
                moveCamera == null ||
                playerFreeLookCamera == null ||
                playerTargetLookCamera == null)
            {
                Debug.LogError("PlayerController에 WorldObjectManager와 이동 기준 카메라가 필요합니다.", this);
                enabled = false;
                return;
            }

            if (!HasValidAttackData())
            {
                Debug.LogError("PlayerController에 공격 1~6 PlayerAttackData가 필요합니다.", this);
                enabled = false;
                return;
            }

            characterController = GetComponent<CharacterController>();
            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>();
            }

            hitEffectPlayer = GetComponent<CombatHitEffectPlayer>();
            attackEffectPlayer = GetComponent<PlayerAttackEffectPlayer>();
            if (attackEffectPlayer == null)
            {
                attackEffectPlayer = gameObject.AddComponent<PlayerAttackEffectPlayer>();
            }

            if (playerDamageAudio == null)
            {
                playerDamageAudio = GetComponentInChildren<PlayerDamageAudio>(true);
            }

            if (playerDamageAudio == null)
            {
                Debug.LogError("PlayerController에 PlayerDamageAudio 연결이 필요합니다.", this);
            }

            if (playerAnimator != null)
            {
                footstepAudio = playerAnimator.GetComponent<PlayerFootstepAudio>();
                if (footstepAudio == null)
                {
                    footstepAudio =
                        playerAnimator.gameObject
                            .AddComponent<PlayerFootstepAudio>();
                }

                footstepAudio.Create(
                    walkFootstepSounds,
                    runFootstepSounds,
                    rollSound,
                    walkFootstepVolume,
                    runFootstepVolume,
                    rollSoundVolume,
                    footstepPitchChange);
            }

            if (guardHitBox == null)
            {
                Debug.LogError("PlayerController에 방패의 PlayerGuardHitBox 연결이 필요합니다.", this);
            }

            if (rollMovementCurve == null || rollMovementCurve.length < 2)
            {
                rollMovementCurve = CreateDefaultRollMovementCurve();
            }

            if (hitPushCurve == null || hitPushCurve.length < 2)
            {
                hitPushCurve = CreateDefaultHitPushCurve();
            }

            if (weaponHitStart == null || weaponHitEnd == null)
            {
                Debug.LogError("PlayerController에 검의 Weapon Hit Start와 End가 필요합니다.", this);
            }

            LayerMask attackMask =
                attackLayers.value != 0
                    ? attackLayers
                    : targetLayers;

            attackEffectPlayer.Create(weaponHitStart, weaponHitEnd);

            playerInput = new PlayerInputReader();
            playerMovement = new PlayerMovement(
                transform,
                moveCamera,
                characterController,
                playerInput,
                freeMoveTurnSpeed,
                targetMoveTurnSpeed,
                attackTurnSpeed,
                walkSpeed,
                guardMoveSpeed,
                sprintSpeed,
                moveAcceleration,
                moveDeceleration,
                gravity,
                groundPull);
            var targetFinder = new PlayerTargetFinder(
                transform,
                moveCamera,
                targetLayers,
                targetRange,
                targetMaximumAngle,
                targetObstructionLayers,
                targetHeightOffset);
            var targetCamera = new PlayerTargetCamera(
                playerFreeLookCamera,
                playerTargetLookCamera);
            var playerStamina = new PlayerStamina(
                maxStamina,
                staminaRecoverDelay,
                staminaRecoverSpeed);
            var hitStop = new CombatHitStop(playerAnimator);
            var stopPoint = new StopPoint(
                stopPointLimit,
                stopPointRecoverDelay,
                stopPointRecoverSpeed);
            playerStateMachine = new PlayerStateMachine(
                playerInput,
                playerMovement,
                playerStamina,
                guardStaminaRecoveryRate,
                rollStaminaCost,
                sprintStaminaCostPerSecond,
                sprintRestartStamina,
                playerAnimator,
                animationSmoothTime,
                rollDistance,
                sprintRollDistance,
                rollMovementCurve,
                rollCompleteNormalizedTime,
                attackData,
                actionInputBufferDuration,
                targetFinder,
                targetCamera,
                Mathf.Max(targetRange, targetBreakDistance),
                targetHiddenGraceDuration,
                guardAngle,
                guardRaiseDuration,
                guardHitBox,
                guardBreakControlLockDuration,
                hitPushDuration,
                hitPushCurve,
                transform,
                weaponHitStart,
                weaponHitEnd,
                attackMask,
                weaponHitRadius,
                hitStop,
                hitEffectPlayer,
                attackEffectPlayer
                );
            playerWorldUnit = new PlayerWorldUnit(
                maxHealth,
                playerStamina,
                stopPoint,
                playerInput,
                playerStateMachine,
                hitStop);
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


        public PlayerHitResult TryTakeHit(in PlayerHitRequest hitRequest)
        {
            PlayerHitResult hitResult = playerWorldUnit != null
                ? playerWorldUnit.TryTakeHit(in hitRequest)
                : PlayerHitResult.Ignored;

            if (hitResult == PlayerHitResult.Damaged)
            {
                playerDamageAudio?.Play(hitRequest.Damage.DamageSoundType);
            }

            return hitResult;
        }


        public void PlayDeathKneeImpact()
        {
            playerDamageAudio?.PlayDeathKneeImpact();
        }

        public void PlayDeathBodyImpact()
        {
            playerDamageAudio?.PlayDeathBodyImpact();
        }

        public void PlayAttackSound(int attackNumber)
        {
            playerStateMachine?.PlayAttackSound(attackNumber);
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

        internal void NotifyAttackAnimationEnded(int attackNumber)
        {
            playerStateMachine?.NotifyAttackAnimationEnded(attackNumber);
        }

        internal void BeginRollInvulnerability()
        {
            playerStateMachine?.BeginRollInvulnerability();
        }

        internal void EndRollInvulnerability()
        {
            playerStateMachine?.EndRollInvulnerability();
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
                if (attackData[index] == null || attackData[index].AttackNumber != index + 1)
                {
                    return false;
                }
            }

            return true;
        }

        private static AnimationCurve CreateDefaultRollMovementCurve()
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 3f),
                new Keyframe(0.15f, 0.45f, 1.2f, 1.2f),
                new Keyframe(0.35f, 0.75f, 0.6f, 0.6f),
                new Keyframe(0.7f, 0.95f, 0.1f, 0.1f),
                new Keyframe(1f, 1f, 0f, 0f));
            curve.preWrapMode = WrapMode.Clamp;
            curve.postWrapMode = WrapMode.Clamp;
            return curve;
        }

        private static AnimationCurve CreateDefaultHitPushCurve()
        {
            return new AnimationCurve(new Keyframe(0f, 0f, 2f, 2f), new Keyframe(1f, 1f, 0f, 0f));
        }


#if UNITY_EDITOR
        [ContextMenu("Test Damage")]
        private void TestDamage()
        {
            if (!Application.isPlaying || playerWorldUnit == null)
            {
                Debug.LogWarning("Test Damage는 Play 중이고 플레이어 준비가 끝난 뒤 사용할 수 있습니다.", this);
                return;
            }

            float healthBeforeDamage = playerWorldUnit.CurrentHealth;
            var damage = new AttackDamage(
                testDamage,
                AttackStrength.Light,
                0f,
                0f,
                0f,
                false);
            var hitRequest = new PlayerHitRequest(
                damage,
                transform.position,
                Vector3.zero);
            TryTakeHit(in hitRequest);

            Debug.Log($"플레이어 체력: {healthBeforeDamage} → {playerWorldUnit.CurrentHealth}", this);
        }

        private void OnValidate()
        {
            maxStamina = Mathf.Max(1f, maxStamina);
            staminaRecoverDelay = Mathf.Max(0f, staminaRecoverDelay);
            staminaRecoverSpeed = Mathf.Max(0f, staminaRecoverSpeed);
            guardStaminaRecoveryRate = Mathf.Clamp01(guardStaminaRecoveryRate);
            rollStaminaCost = Mathf.Max(0f, rollStaminaCost);
            sprintStaminaCostPerSecond = Mathf.Max(0f, sprintStaminaCostPerSecond);
            sprintRestartStamina = Mathf.Clamp(sprintRestartStamina, 0f, maxStamina);
            targetHiddenGraceDuration = Mathf.Max(0f, targetHiddenGraceDuration);
            targetHeightOffset = Mathf.Max(0f, targetHeightOffset);
            freeMoveTurnSpeed = Mathf.Max(0f, freeMoveTurnSpeed);
            targetMoveTurnSpeed = Mathf.Max(0f, targetMoveTurnSpeed);
            attackTurnSpeed = Mathf.Max(0f, attackTurnSpeed);
            guardMoveSpeed = Mathf.Clamp(guardMoveSpeed, 0f, walkSpeed);
            moveAcceleration = Mathf.Max(0f, moveAcceleration);
            moveDeceleration = Mathf.Max(0f, moveDeceleration);
            rollCompleteNormalizedTime = Mathf.Clamp(
                rollCompleteNormalizedTime,
                0.01f,
                1f);
            actionInputBufferDuration = Mathf.Max(0f, actionInputBufferDuration);
            weaponHitRadius = Mathf.Max(0f, weaponHitRadius);
            guardAngle = Mathf.Clamp(guardAngle, 0f, 180f);
            guardRaiseDuration = Mathf.Max(0f, guardRaiseDuration);
            guardBreakControlLockDuration = Mathf.Max(0f, guardBreakControlLockDuration);
            hitPushDuration = Mathf.Max(0.01f, hitPushDuration);
            stopPointLimit = Mathf.Max(1f, stopPointLimit);
            stopPointRecoverDelay =
                Mathf.Max(0f, stopPointRecoverDelay);
            stopPointRecoverSpeed =
                Mathf.Max(0f, stopPointRecoverSpeed);
            if (hitPushCurve == null || hitPushCurve.length < 2)
            {
                hitPushCurve = CreateDefaultHitPushCurve();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (weaponHitStart != null && weaponHitEnd != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(weaponHitStart.position, weaponHitRadius);
                Gizmos.DrawWireSphere(weaponHitEnd.position, weaponHitRadius);
                Gizmos.DrawLine(weaponHitStart.position, weaponHitEnd.position);
            }

        }

#endif
    }
}
