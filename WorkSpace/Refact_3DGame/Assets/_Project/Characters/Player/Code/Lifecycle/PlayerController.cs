using Characters;
using Characters.Combat;
using Characters.Combat.AttackData;
using Characters.Player.Audio;
using Characters.Player.Camera;
using Characters.Player.Combat.Attack;
using Characters.Player.Combat.Hit;
using Characters.Player.Config;
using Characters.Player.Input;
using Characters.Player.Inventory;
using Characters.Player.Interaction;
using Characters.Player.Movement;
using Characters.Player.StateMachine;
using Characters.Player.StateMachine.States.Target;
using Characters.Player.Stats;
using Cinemachine;
using UnityEngine;
using World;
using World.Interaction;
using Items;

namespace Characters.Player.Lifecycle
{
    [RequireComponent(
        typeof(CharacterController),
        typeof(CombatHitEffectPlayer),
        typeof(PlayerAttackEffectPlayer))]
    [RequireComponent(typeof(PlayerWeaponHitShape))]
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
        [SerializeField] private PlayerGuardHitBox guardHitBox;
        [SerializeField] private PlayerDamageAudio playerDamageAudio;
        [SerializeField] private PlayerWeaponHitShape weaponHitShape;
        [SerializeField] private PlayerCharacterConfig config;

        [Header("락온")]
        [SerializeField] private CinemachineFreeLook playerFreeLookCamera;
        [SerializeField] private CinemachineFreeLook playerTargetLookCamera;

        private CharacterController characterController; // 씬 또는 시스템 참조
        private PlayerInputReader playerInput; // 입력 또는 행동 여부
        private PlayerStateMachine playerStateMachine; // 현재 행동 상태
        private PlayerMovement playerMovement; // 이동 정보
        private PlayerWorldUnit playerWorldUnit; // 씬 또는 시스템 참조
        private PlayerTargetRuntimeConfig targetConfig;
        private CombatHitEffectPlayer hitEffectPlayer;
        private PlayerAttackEffectPlayer attackEffectPlayer;
        private PlayerInteractionController interactionController;

        // 적이 플레이어를 계속 추적할 수 있는지 확인할 때 사용한다.
        public bool IsDead =>
            playerWorldUnit != null && playerWorldUnit.IsDead;
        internal Transform ViewTransform => moveCamera;
        internal LayerMask ObstructionLayers => targetConfig != null
            ? targetConfig.ObstructionLayers
            : default;

        private void Awake()
        {
            if (worldObjectManager == null ||
                moveCamera == null ||
                playerFreeLookCamera == null ||
                playerTargetLookCamera == null ||
                config == null)
            {
                Debug.LogError("PlayerController에 WorldObjectManager, 카메라와 PlayerCharacterConfig가 필요합니다.", this);
                enabled = false;
                return;
            }

            PlayerCharacterRuntimeConfig runtimeConfig;
            try
            {
                runtimeConfig = config.CreateRuntimeConfig();
            }
            catch (System.ArgumentException exception)
            {
                Debug.LogError(exception.Message, this);
                enabled = false;
                return;
            }

            PlayerMovementRuntimeConfig movementConfig =
                runtimeConfig.Movement;
            PlayerCombatRuntimeConfig combatConfig =
                runtimeConfig.Combat;
            targetConfig = runtimeConfig.Target;

            characterController = GetComponent<CharacterController>();
            interactionController = GetComponent<PlayerInteractionController>();
            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>();
            }

            hitEffectPlayer = GetComponent<CombatHitEffectPlayer>();
            attackEffectPlayer = GetComponent<PlayerAttackEffectPlayer>();
            weaponHitShape ??= GetComponent<PlayerWeaponHitShape>();
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

            if (guardHitBox == null)
            {
                Debug.LogError("PlayerController에 방패의 PlayerGuardHitBox 연결이 필요합니다.", this);
            }

            if (weaponHitShape == null || !weaponHitShape.IsReady)
            {
                Debug.LogError("PlayerController에 준비된 PlayerWeaponHitShape가 필요합니다.", this);
                enabled = false;
                return;
            }

            attackEffectPlayer.Create(
                weaponHitShape.StartPoint,
                weaponHitShape.EndPoint);

            playerInput = new PlayerInputReader();
            playerMovement = new PlayerMovement(
                transform,
                moveCamera,
                characterController,
                playerInput,
                movementConfig.FreeMoveTurnSpeed,
                movementConfig.TargetMoveTurnSpeed,
                movementConfig.AttackTurnSpeed,
                movementConfig.WalkSpeed,
                movementConfig.GuardMoveSpeed,
                movementConfig.SprintSpeed,
                movementConfig.MoveAcceleration,
                movementConfig.MoveDeceleration,
                movementConfig.Gravity,
                movementConfig.GroundPull);
            var targetFinder = new PlayerTargetFinder(
                transform,
                moveCamera,
                targetConfig.TargetLayers,
                targetConfig.FindRange,
                targetConfig.MaximumAngle,
                targetConfig.ObstructionLayers,
                targetConfig.HeightOffset);
            var targetCamera = new PlayerTargetCamera(
                playerFreeLookCamera,
                playerTargetLookCamera);
            var playerStamina = new PlayerStamina(
                combatConfig.MaxStamina *
                    PlayerStatUpgradeSession.CurrentMaxStaminaMultiplier,
                combatConfig.StaminaRecoverDelay,
                combatConfig.StaminaRecoverSpeed);
            var playerInventory = new PlayerInventory();
            var hitStop = new CombatHitStop(playerAnimator);
            var stopPoint = new StopPoint(
                combatConfig.StopPointLimit,
                combatConfig.StopPointRecoverDelay,
                combatConfig.StopPointRecoverSpeed);
            playerStateMachine = new PlayerStateMachine(
                playerInput,
                playerMovement,
                playerStamina,
                playerAnimator,
                runtimeConfig,
                targetFinder,
                targetCamera,
                guardHitBox,
                transform,
                weaponHitShape,
                hitStop,
                hitEffectPlayer,
                attackEffectPlayer
                );
            playerStateMachine.SetAttackDamageMultiplier(
                PlayerStatUpgradeSession.CurrentStrengthMultiplier);
            playerWorldUnit = new PlayerWorldUnit(
                combatConfig.MaxHealth *
                    PlayerStatUpgradeSession.CurrentMaxHealthMultiplier,
                PlayerStatUpgradeSession.CurrentMaxHealthMultiplier,
                PlayerStatUpgradeSession.CurrentMaxStaminaMultiplier,
                playerStamina,
                stopPoint,
                playerInput,
                playerStateMachine,
                hitStop,
                interactionController,
                playerInventory);
            worldObjectManager.Register(playerWorldUnit);
        }

        internal bool CanStoreInventoryItem(ItemDefinition item)
        {
            return playerWorldUnit != null &&
                !IsDead &&
                playerWorldUnit.Inventory.CanAdd(item);
        }

        internal bool TryStoreInventoryItem(ItemDefinition item)
        {
            return playerWorldUnit != null &&
                !IsDead &&
                playerWorldUnit.Inventory.TryAdd(item);
        }

        internal bool HasStatueUpgrade(StatueUpgradeType upgradeType)
        {
            return PlayerStatUpgradeSession.HasUpgrade(upgradeType);
        }

        internal bool TryApplyStatueUpgrade(StatueUpgradeType upgradeType)
        {
            if (playerWorldUnit == null ||
                playerStateMachine == null ||
                IsDead ||
                !PlayerStatUpgradeSession.TryActivate(upgradeType))
            {
                return false;
            }

            switch (upgradeType)
            {
                case StatueUpgradeType.MaxHealth:
                    playerWorldUnit.MultiplyMaximumHealth(
                        PlayerStatUpgradeSession.MaxHealthMultiplier);
                    return true;

                case StatueUpgradeType.MaxStamina:
                    playerWorldUnit.MultiplyMaximumStamina(
                        PlayerStatUpgradeSession.MaxStaminaMultiplier);
                    return true;

                case StatueUpgradeType.Strength:
                    playerStateMachine.SetAttackDamageMultiplier(
                        PlayerStatUpgradeSession.StrengthMultiplier);
                    return true;

                default:
                    return false;
            }
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
                10f,
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
#endif
    }
}
