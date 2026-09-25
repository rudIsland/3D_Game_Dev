using System.Collections.Generic;
using UnityEngine;
using Core;

namespace UI
{
    // 플레이어 자원과 현재 전투 중인 적 자원을 화면 HUD에 연결한다.
    public sealed class CombatHudController : MonoBehaviour
    {
        private IPlayerHudSource playerSource;
        private readonly List<IEnemyHudSource> enemySources = new List<IEnemyHudSource>();
        private bool connected;

        /// <summary>플레이어 이벤트 구독과 필수 HUD 요소 준비가 완료되었는지 반환한다.</summary>
        public bool IsReady => connected && toolkitView != null && toolkitView.IsReady;

        /// <summary>플레이어의 표시 계약을 받고 활성 상태이면 변경 알림을 구독한다.</summary>
        public void Connect(IPlayerHudSource player)
        {
            if (ReferenceEquals(playerSource, player)) return;
            OnDisable();
            playerSource = player;
            if (isActiveAndEnabled) OnEnable();
        }
        /// <summary>플레이어·적 이벤트 구독과 표시 대상 참조를 해제한다.</summary>
        public void Disconnect()
        {
            OnDisable();
            playerSource = null;
            enemySources.Clear();
        }
        public void WatchEnemies(IEnemyHudSource enemies)
        {
            if (enemySources.Contains(enemies)) return;
            enemySources.Add(enemies);
            if (connected) SubscribeEnemies(enemies);
        }
        public void StopWatchingEnemies(IEnemyHudSource enemies)
        {
            if (!enemySources.Remove(enemies)) return;
            enemies.EnemyEnabled -= HandleUnitEnabled;
            enemies.EnemyDisabled -= HandleUnitDisabled;
            foreach (Unit unit in enemies.ActiveObjects) StopTracking(unit);
        }
        private void SubscribeEnemies(IEnemyHudSource enemies)
        {
            enemies.EnemyEnabled += HandleUnitEnabled;
            enemies.EnemyDisabled += HandleUnitDisabled;
            foreach (Unit unit in enemies.ActiveObjects) Track(unit);
        }

        [Header("UI Toolkit")]
        [SerializeField] private CombatHudToolkitView toolkitView;

        private readonly Dictionary<UnitHealth, Unit> trackedUnits = // 씬 또는 시스템 참조
            new Dictionary<UnitHealth, Unit>(3);
        private bool playerTracked;
        private IEnemyCombatStatus displayedEnemy;

        private void Awake()
        {
            if (toolkitView == null)
            {
                toolkitView = GetComponent<CombatHudToolkitView>();
            }

            if (toolkitView == null)
            {
                Debug.LogError(
                    "CombatHudController requires a CombatHudToolkitView.",
                    this);
                enabled = false;
                return;
            }

            HideAllHud();
        }

        private void OnEnable()
        {
            if (connected || playerSource == null || playerSource.FollowTarget == null || toolkitView == null) return;
            connected = true;
            playerSource.InteractionGuideChanged += HandleInteractionGuideChanged;
            HandleInteractionGuideChanged(playerSource.CurrentInteractionGuide);
            playerSource.AvailabilityChanged += HandlePlayerAvailabilityChanged;
            if (playerSource.IsAvailable) TrackPlayer();
            foreach (IEnemyHudSource enemies in enemySources) SubscribeEnemies(enemies);
        }

        private void OnDisable()
        {
            if (playerSource != null)
            {
                playerSource.InteractionGuideChanged -= HandleInteractionGuideChanged;
                playerSource.AvailabilityChanged -= HandlePlayerAvailabilityChanged;
            }
            foreach (IEnemyHudSource enemies in enemySources)
            {
                enemies.EnemyEnabled -= HandleUnitEnabled;
                enemies.EnemyDisabled -= HandleUnitDisabled;
            }
            connected = false;
            StopTrackingPlayer();

            foreach (KeyValuePair<UnitHealth, Unit> entry in trackedUnits)
            {
                entry.Key.HealthChanged -= HandleHealthChanged;
                entry.Key.Died -= HandleUnitDied;

                if (entry.Value is IEnemyCombatStatus enemy)
                {
                    enemy.StaggerChanged -= HandleEnemyStaggerChanged;
                    enemy.CombatStateChanged -= HandleEnemyCombatStateChanged;
                }
            }

            trackedUnits.Clear();
            displayedEnemy = null;
            HideAllHud();
        }

        private void HandleInteractionGuideChanged(
            PlayerInteractionGuide guide)
        {
            if (guide.IsVisible)
            {
                ShowInteractionGuide(guide);
                return;
            }

            HideInteractionGuide();
        }

        private void HandleUnitEnabled(Unit worldObject)
        {
            Track(worldObject);
        }

        private void HandleUnitDisabled(Unit worldObject)
        {
            if (worldObject is Unit unit)
            {
                StopTracking(unit);
            }
        }

        // 플레이어의 표시 가능 여부에 맞춰 수치 구독을 시작하거나 해제한다.
        private void HandlePlayerAvailabilityChanged(bool available)
        {
            if (available) TrackPlayer();
            else StopTrackingPlayer();
        }

        // 기존 순서대로 체력·사망·스태미나·아이템을 구독하고 초기 값을 표시한다.
        private void TrackPlayer()
        {
            if (playerTracked) return;
            playerTracked = true;
            playerSource.HealthChanged += HandlePlayerHealthChanged;
            playerSource.Died += HandleUnitDied;
            ShowPlayerHealth();
            playerSource.StaminaChanged += HandleStaminaChanged;
            playerSource.InventoryChanged += HandleInventoryChanged;
            ShowPlayerInventory();
            ShowPlayerStamina(
                playerSource.CurrentStamina,
                playerSource.MaxStamina,
                playerSource.MaximumStaminaScale);
        }

        // 재활성화와 HUD 반환에서 같은 구독을 중복 없이 해제한다.
        private void StopTrackingPlayer()
        {
            if (!playerTracked) return;
            playerTracked = false;
            playerSource.HealthChanged -= HandlePlayerHealthChanged;
            playerSource.Died -= HandleUnitDied;
            HidePlayerHealth();
            playerSource.StaminaChanged -= HandleStaminaChanged;
            playerSource.InventoryChanged -= HandleInventoryChanged;
            HidePlayerInventory();
            HidePlayerStamina();
        }

        private void Track(Unit unit)
        {
            if (unit == null ||
                !(unit is IEnemyCombatStatus status) || !status.ShowScreenHealthBar ||
                trackedUnits.ContainsKey(unit.Health))
            {
                return;
            }

            trackedUnits.Add(unit.Health, unit);
            unit.Health.HealthChanged += HandleHealthChanged;
            unit.Health.Died += HandleUnitDied;
            status.StaggerChanged += HandleEnemyStaggerChanged;
            status.CombatStateChanged += HandleEnemyCombatStateChanged;
            if (status.IsInCombat) ShowEnemy(status);
        }

        private void StopTracking(Unit unit)
        {
            if (!trackedUnits.Remove(unit.Health, out _)) return;
            unit.Health.HealthChanged -= HandleHealthChanged;
            unit.Health.Died -= HandleUnitDied;
            if (unit is IEnemyCombatStatus enemy)
            {
                enemy.StaggerChanged -= HandleEnemyStaggerChanged;
                enemy.CombatStateChanged -= HandleEnemyCombatStateChanged;
                HideEnemy(enemy);
            }
        }

        // 플레이어 내부 타입을 판별하지 않고 계약에서 변경된 수치를 읽는다.
        private void HandlePlayerHealthChanged()
        {
            toolkitView.UpdatePlayerHealth(
                playerSource.CurrentHealth,
                playerSource.MaxHealth,
                playerSource.MaximumHealthScale);
        }

        private void HandleHealthChanged(UnitHealth health)
        {
            if (trackedUnits.TryGetValue(health, out Unit unit) &&
                ReferenceEquals(displayedEnemy, unit))
                UpdateEnemyHealth(health);
        }

        private void HandleUnitDied()
        {
            foreach (KeyValuePair<UnitHealth, Unit> entry in trackedUnits)
            {
                Unit unit = entry.Value;
                if (!entry.Key.IsDead)
                {
                    continue;
                }

                if (ReferenceEquals(displayedEnemy, unit))
                {
                    HideEnemy(displayedEnemy);
                }
            }
        }

        private void HandleStaminaChanged()
        {
            toolkitView.UpdatePlayerStamina(
                playerSource.CurrentStamina,
                playerSource.MaxStamina,
                playerSource.MaximumStaminaScale);
        }

        private void HandleEnemyStaggerChanged(IEnemyCombatStatus enemy)
        {
            if (!ReferenceEquals(displayedEnemy, enemy))
            {
                return;
            }

            UpdateEnemyStagger(enemy.CurrentStagger, enemy.MaxStagger);
        }

        private void HandleEnemyCombatStateChanged(IEnemyCombatStatus enemy)
        {
            if (enemy.IsInCombat)
            {
                ShowEnemy(enemy);
                return;
            }

            HideEnemy(enemy);
        }

        private void ShowEnemy(IEnemyCombatStatus enemy)
        {
            displayedEnemy = enemy;
            toolkitView.ShowEnemyHealth(enemy.DisplayName, enemy.Health);
            toolkitView.ShowEnemyStagger(enemy.CurrentStagger, enemy.MaxStagger);
        }

        private void HideEnemy(IEnemyCombatStatus enemy)
        {
            if (!ReferenceEquals(displayedEnemy, enemy))
            {
                return;
            }

            displayedEnemy = null;
            toolkitView.HideEnemyHealth();
            toolkitView.HideEnemyStagger();
        }

        private void HandleInventoryChanged()
        {
            toolkitView.UpdatePlayerInventory(playerSource.GetInventoryItem(0), playerSource.GetInventoryItem(1));
        }

        private void HideAllHud()
        {
            toolkitView?.HideAll();
        }

        private void ShowInteractionGuide(PlayerInteractionGuide guide)
        {
            toolkitView.ShowInteractionGuide(guide);
        }

        private void HideInteractionGuide()
        {
            toolkitView.HideInteractionGuide();
        }

        private void ShowPlayerHealth()
        {
            toolkitView.ShowPlayerHealth(
                "PLAYER",
                playerSource.CurrentHealth,
                playerSource.MaxHealth,
                playerSource.MaximumHealthScale);
        }

        private void HidePlayerHealth()
        {
            toolkitView.HidePlayerHealth();
        }

        private void ShowPlayerStamina(
            float currentStamina,
            float maxStamina,
            float maximumScale)
        {
            toolkitView.ShowPlayerStamina(
                currentStamina,
                maxStamina,
                maximumScale);
        }

        private void ShowPlayerInventory()
        {
            toolkitView.ShowPlayerInventory(playerSource.GetInventoryItem(0), playerSource.GetInventoryItem(1));
        }

        private void HidePlayerInventory()
        {
            toolkitView.HidePlayerInventory();
        }

        private void HidePlayerStamina()
        {
            toolkitView.HidePlayerStamina();
        }

        private void UpdateEnemyHealth(UnitHealth health)
        {
            toolkitView.UpdateEnemyHealth(health);
        }

        private void UpdateEnemyStagger(
            float currentStagger,
            float maxStagger)
        {
            toolkitView.UpdateEnemyStagger(
                currentStagger,
                maxStagger);
        }

#if UNITY_EDITOR
        public void ConnectToolkitForEditor(CombatHudToolkitView view)
        {
            toolkitView = view;
        }
#endif
    }
}
