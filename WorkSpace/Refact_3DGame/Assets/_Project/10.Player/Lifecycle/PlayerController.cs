using System;
using Characters.Combat;
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
    // Addressables로 생성한 플레이어 한 명의 등록·활성·해제 경계를 제공한다. 원본 로드·반환은 GameManager가 맡는다.
    public sealed partial class PlayerController :
        MonoBehaviour,
        IPlayerDamageReceiver,
        IUnitDeathState
    {
        private bool released;

        // 초기화한 플레이어를 제공하며, 생성 전과 해제 후에는 null이다.
        public static PlayerController Instance { get; private set; }

        [Header("필수 연결")]
        private readonly PlayerInventory inventory = new PlayerInventory();
        private readonly PlayerStatUpgradeSession upgrades = new PlayerStatUpgradeSession();
        public PlayerWorldUnit RuntimeUnit => playerWorldUnit;
        /// <summary>플레이어의 갱신만 중지하며 비활성화나 데이터 초기화를 수행하지 않는다.</summary>
        public bool IsPaused { get; set; }
        public event Action<Unit> PlayerEnabled;
        public event Action<Unit> PlayerDisabled;
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
        public bool IsReady => playerWorldUnit != null;

        // 적이 플레이어를 계속 추적할 수 있는지 확인할 때 사용한다.
        public bool IsDead =>
            playerWorldUnit != null && playerWorldUnit.IsDead;
        internal Transform ViewTransform => moveCamera;
        internal LayerMask ObstructionLayers => targetConfig != null
            ? targetConfig.ObstructionLayers
            : default;

        /// <summary>카메라를 전달받아 내부 Unit을 초기화한다. GameManager는 비활성 부모 아래에서 호출한다.</summary>
        public void Init(Transform moveCamera)
        {
            CheckPlayerObject();
            if (moveCamera == null) throw new ArgumentNullException(nameof(moveCamera));
            if (IsReady && this.moveCamera != moveCamera)
                throw new InvalidOperationException("준비된 플레이어의 이동 카메라는 바꿀 수 없습니다.");
            this.moveCamera = moveCamera;
            Create();
            if (!IsReady) throw new InvalidOperationException("PlayerRoot 초기화에 실패했습니다. 필수 연결을 확인하세요.");
        }

        /// <summary>씬에 생성된 플레이어만 기존 초기화를 실행하고 성공 후 등록한다. 원본 프리팹·중복 객체는 거절한다.</summary>
        public void Create()
        {
            CheckPlayerObject();
            CreatePlayerObjects();
            if (IsReady) Instance = this;
        }

        // 원본 프리팹을 변경하거나 다른 플레이어를 초기화하기 전에 객체 수명과 단일 등록을 검사한다.
        private void CheckPlayerObject()
        {
            if (released) throw new ObjectDisposedException(nameof(PlayerController));
            if (!Application.isPlaying || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
                throw new InvalidOperationException("Play 중 씬에 생성한 PlayerRoot만 초기화할 수 있습니다. 원본 프리팹은 등록하지 않습니다.");
            if (Instance != null && Instance != this)
                throw new InvalidOperationException("이미 준비된 플레이어가 있습니다. 기존 플레이어를 반환한 뒤 생성하세요.");
        }

        // 기존 내부 생성 순서를 유지한다. 단일 객체 등록과 Addressables 처리는 이 안에 넣지 않는다.
        private void CreatePlayerObjects()
        {
            if (playerWorldUnit != null) return;
            if (moveCamera == null ||
                playerFreeLookCamera == null ||
                playerTargetLookCamera == null ||
                config == null)
            {
                Debug.LogError("PlayerController에 카메라와 PlayerCharacterConfig가 필요합니다.", this);
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

            if (characterController == null || playerAnimator == null ||
                playerDamageAudio == null || guardHitBox == null || interactionController == null)
            {
                Debug.LogError("PlayerController의 이동·애니메이션·소리·방패·상호작용 연결을 확인하세요.", this);
                enabled = false;
                return;
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
                movementConfig);
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
                    upgrades.CurrentMaxStaminaMultiplier,
                combatConfig.StaminaRecoverDelay,
                combatConfig.StaminaRecoverSpeed);
            var playerInventory = inventory;
            var hitStop = new CombatHitStop(playerAnimator);
            var stopPoint = new StopPoint(combatConfig.Life);
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
                upgrades.CurrentStrengthMultiplier);
            playerWorldUnit = new PlayerWorldUnit(
                combatConfig.MaxHealth *
                    upgrades.CurrentMaxHealthMultiplier,
                upgrades.CurrentMaxHealthMultiplier,
                upgrades.CurrentMaxStaminaMultiplier,
                playerStamina,
                stopPoint,
                playerInput,
                playerStateMachine,
                hitStop,
                interactionController,
                playerInventory);
            try
            {
                playerWorldUnit.Create();
                if (isActiveAndEnabled) Enable();
            }
            catch
            {
                Disable();
                playerWorldUnit.Dispose();
                playerWorldUnit = null;
                throw;
            }
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

        internal bool HasInventoryItem(ItemDefinition item)
        {
            return playerWorldUnit != null &&
                !IsDead &&
                playerWorldUnit.Inventory.HasItem(item);
        }

        internal bool CanExchangeInventoryItem(
            ItemDefinition costItem,
            ItemDefinition rewardItem)
        {
            return playerWorldUnit != null &&
                !IsDead &&
                playerWorldUnit.Inventory.CanExchangeItem(
                    costItem,
                    rewardItem);
        }

        internal bool TryExchangeInventoryItem(
            ItemDefinition costItem,
            ItemDefinition rewardItem)
        {
            return playerWorldUnit != null &&
                !IsDead &&
                playerWorldUnit.Inventory.TryExchangeItem(
                    costItem,
                    rewardItem);
        }

        internal bool HasStatueUpgrade(StatueUpgradeType upgradeType)
        {
            return IsReady && PlayerStatUpgrade.HasUpgrade(upgrades, upgradeType);
        }

        internal bool TryApplyStatueUpgrade(StatueUpgradeType upgradeType)
        {
            return IsReady && PlayerStatUpgrade.TryApply(
                upgrades,
                upgradeType,
                playerWorldUnit,
                playerStateMachine);
        }

        private void OnEnable()
        {
            if (IsReady) Enable();
        }

        /// <summary>준비된 활성 플레이어의 입력·행동을 시작한다. 중복 호출은 무시한다.</summary>
        public void Enable()
        {
            if (!isActiveAndEnabled || playerWorldUnit == null || playerWorldUnit.IsEnabled) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            playerWorldUnit.Enable();
            PlayerEnabled?.Invoke(playerWorldUnit);
        }


        /// <summary>GameManager가 전달한 프레임 시간으로 준비된 플레이어를 갱신한다. 일시정지 중에는 건너뛴다.</summary>
        public void Tick(float deltaTime)
        {
            if (IsPaused || !isActiveAndEnabled) return;
            playerWorldUnit?.Tick(deltaTime);
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
            Disable();
        }

        public void Disable()
        {
            if (playerWorldUnit == null || !playerWorldUnit.IsEnabled) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            playerWorldUnit.Disable();
            PlayerDisabled?.Invoke(playerWorldUnit);
        }

        private void OnDestroy()
        {
            Release();
        }

        /// <summary>입력·구독·단일 객체 등록을 해제한다. 비활성 생성 실패에도 호출하며 해제한 객체는 재초기화하지 않는다.</summary>
        public void Release()
        {
            if (released) return;
            released = true;
            try
            {
                ReleasePlayerObjects();
            }
            finally
            {
                if (Instance == this) Instance = null;
            }
        }

        // 기존 내부 해제 순서를 유지한다. 객체 파괴와 원본 요청 반환은 생성한 GameManager가 처리한다.
        private void ReleasePlayerObjects()
        {
            Disable();
            playerWorldUnit?.Dispose();
            playerWorldUnit = null;
            playerInput?.Destroy();
            playerInput = null;
            PlayerEnabled = null;
            PlayerDisabled = null;
        }

        // 에디터 이동 도구가 상태와 이동 속도를 정리한 뒤 위치를 바꾼다.
        public bool TryMoveToPosition(Vector3 position, Quaternion rotation)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || IsDead ||
                playerMovement == null || playerStateMachine == null ||
                characterController == null || !characterController.enabled)
            {
                return false;
            }

            playerStateMachine.Disable();
            playerMovement.MoveToPosition(position, rotation);
            playerStateMachine.Enable();

            if (playerFreeLookCamera != null)
            {
                playerFreeLookCamera.PreviousStateIsValid = false;
            }

            if (playerTargetLookCamera != null)
            {
                playerTargetLookCamera.PreviousStateIsValid = false;
            }

            Physics.SyncTransforms();
            interactionController?.RefreshCurrentTarget();
            return true;
        }

    }
}
