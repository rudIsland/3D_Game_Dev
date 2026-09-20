using System.Collections.Generic;
using Characters.Player.Interaction;
using Characters.Player.Inventory;
using Characters;
using Characters.Enemies;
using Characters.Player.Lifecycle;
using Characters.Player.Stats;
using World;
using World.Interaction;
using UnityEngine;

namespace GameUI.CombatHud
{
    // 플레이어 자원과 현재 전투 중인 적 자원을 화면 HUD에 연결한다.
    public sealed class CombatHudController : MonoBehaviour
    {
        private PlayerController playerController;
        private readonly List<EnemyContainer> enemyContainers = new List<EnemyContainer>();
        private bool connected;

        public void Connect(PlayerController player)
        {
            if (ReferenceEquals(playerController, player)) return;
            OnDisable();
            playerController = player;
            if (isActiveAndEnabled) OnEnable();
        }
        public void Disconnect()
        {
            OnDisable();
            playerController = null;
            enemyContainers.Clear();
        }
        public void WatchEnemies(EnemyContainer enemies)
        {
            if (enemyContainers.Contains(enemies)) return;
            enemyContainers.Add(enemies);
            if (connected) SubscribeEnemies(enemies);
        }
        public void StopWatchingEnemies(EnemyContainer enemies)
        {
            if (!enemyContainers.Remove(enemies)) return;
            enemies.EnemyEnabled -= HandleUnitEnabled;
            enemies.EnemyDisabled -= HandleUnitDisabled;
            foreach (Unit unit in enemies.ActiveObjects) StopTracking(unit);
        }
        private void SubscribeEnemies(EnemyContainer enemies)
        {
            enemies.EnemyEnabled += HandleUnitEnabled;
            enemies.EnemyDisabled += HandleUnitDisabled;
            foreach (Unit unit in enemies.ActiveObjects) Track(unit);
        }

        [Header("UI Toolkit")]
        [SerializeField] private CombatHudToolkitView toolkitView;

        [Header("Interaction UI")]
        [SerializeField]
        private PlayerInteractionController playerInteractionController;

        private readonly Dictionary<UnitHealth, Unit> trackedUnits = // 씬 또는 시스템 참조
            new Dictionary<UnitHealth, Unit>(3);
        private PlayerWorldUnit displayedPlayer;
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

            if (playerInteractionController == null)
            {
                playerInteractionController =
                    FindFirstObjectByType<PlayerInteractionController>();
            }

            HideAllHud();
        }

        private void OnEnable()
        {
            if (connected || playerController == null || toolkitView == null) return;
            connected = true;
            if (playerInteractionController != null)
            {
                playerInteractionController.InteractionGuideChanged += HandleInteractionGuideChanged;
                HandleInteractionGuideChanged(playerInteractionController.CurrentInteractionGuide);
            }
            playerController.PlayerEnabled += HandleUnitEnabled;
            playerController.PlayerDisabled += HandleUnitDisabled;
            if (playerController.RuntimeUnit != null && playerController.RuntimeUnit.IsEnabled) Track(playerController.RuntimeUnit);
            foreach (EnemyContainer enemies in enemyContainers) SubscribeEnemies(enemies);
        }

        private void OnDisable()
        {
            if (playerInteractionController != null)
            {
                playerInteractionController.InteractionGuideChanged -=
                    HandleInteractionGuideChanged;
            }

            if (playerController != null)
            {
                playerController.PlayerEnabled -= HandleUnitEnabled;
                playerController.PlayerDisabled -= HandleUnitDisabled;
            }
            foreach (EnemyContainer enemies in enemyContainers)
            {
                enemies.EnemyEnabled -= HandleUnitEnabled;
                enemies.EnemyDisabled -= HandleUnitDisabled;
            }
            connected = false;

            foreach (KeyValuePair<UnitHealth, Unit> entry in trackedUnits)
            {
                entry.Key.HealthChanged -= HandleHealthChanged;
                entry.Key.Died -= HandleUnitDied;

                if (entry.Value is PlayerWorldUnit player)
                {
                    player.Stamina.StaminaChanged -=
                        HandleStaminaChanged;
                    player.Inventory.Changed -=
                        HandleInventoryChanged;
                }
                else if (entry.Value is IEnemyCombatStatus enemy)
                {
                    enemy.StaggerChanged -= HandleEnemyStaggerChanged;
                    enemy.CombatStateChanged -= HandleEnemyCombatStateChanged;
                }
            }

            trackedUnits.Clear();
            displayedPlayer = null;
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

        private void Track(Unit worldObject)
        {
            if (!(worldObject is Unit unit))
            {
                return;
            }

            bool isPlayer = unit is PlayerUnit;
            bool isEnemy = unit is IEnemyCombatStatus status && status.ShowScreenHealthBar;
            if ((!isPlayer && !isEnemy) ||
                trackedUnits.ContainsKey(unit.Health))
            {
                return;
            }

            trackedUnits.Add(unit.Health, unit);
            unit.Health.HealthChanged += HandleHealthChanged;
            unit.Health.Died += HandleUnitDied;

            if (isPlayer)
            {
                if (unit is PlayerWorldUnit player)
                {
                    displayedPlayer = player;
                    ShowPlayerHealth(
                        unit.Health,
                        player.MaximumHealthScale);
                    player.Stamina.StaminaChanged +=
                        HandleStaminaChanged;
                    player.Inventory.Changed +=
                        HandleInventoryChanged;
                    ShowPlayerInventory(player.Inventory);
                    ShowPlayerStamina(
                        player.CurrentStamina,
                        player.MaxStamina,
                        player.MaximumStaminaScale);
                }
                else
                {
                    ShowPlayerHealth(unit.Health, 1f);
                }
            }
            else if (unit is IEnemyCombatStatus enemy)
            {
                enemy.StaggerChanged += HandleEnemyStaggerChanged;
                enemy.CombatStateChanged += HandleEnemyCombatStateChanged;
                if (enemy.IsInCombat)
                {
                    ShowEnemy(enemy);
                }
            }
        }

        private void StopTracking(Unit unit)
        {
            if (!trackedUnits.Remove(unit.Health, out _))
            {
                return;
            }

            unit.Health.HealthChanged -= HandleHealthChanged;
            unit.Health.Died -= HandleUnitDied;

            if (unit is PlayerUnit)
            {
                HidePlayerHealth();

                if (unit is PlayerWorldUnit player)
                {
                    player.Stamina.StaminaChanged -=
                        HandleStaminaChanged;
                    player.Inventory.Changed -=
                        HandleInventoryChanged;

                    if (ReferenceEquals(displayedPlayer, player))
                    {
                        displayedPlayer = null;
                    }
                }

                HidePlayerInventory();
                HidePlayerStamina();
            }
            else if (unit is IEnemyCombatStatus enemy)
            {
                enemy.StaggerChanged -= HandleEnemyStaggerChanged;
                enemy.CombatStateChanged -= HandleEnemyCombatStateChanged;
                HideEnemy(enemy);
            }
        }

        private void HandleHealthChanged(UnitHealth health)
        {
            if (!trackedUnits.TryGetValue(health, out Unit unit))
            {
                return;
            }

            if (unit is PlayerWorldUnit player)
            {
                UpdatePlayerHealth(
                    health,
                    player.MaximumHealthScale);
                return;
            }

            if (unit is PlayerUnit)
            {
                UpdatePlayerHealth(health, 1f);
                return;
            }

            if (ReferenceEquals(displayedEnemy, unit))
            {
                UpdateEnemyHealth(health);
            }
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

        private void HandleStaminaChanged(PlayerStamina stamina)
        {
            toolkitView.UpdatePlayerStamina(
                stamina.CurrentStamina,
                stamina.MaxStamina,
                displayedPlayer != null
                    ? displayedPlayer.MaximumStaminaScale
                    : 1f);
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

        private void HandleInventoryChanged(PlayerInventory inventory)
        {
            toolkitView.UpdatePlayerInventory(inventory);
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

        private void ShowPlayerHealth(
            UnitHealth health,
            float maximumScale)
        {
            toolkitView.ShowPlayerHealth(
                "PLAYER",
                health,
                maximumScale);
        }

        private void UpdatePlayerHealth(
            UnitHealth health,
            float maximumScale)
        {
            toolkitView.UpdatePlayerHealth(health, maximumScale);
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

        private void ShowPlayerInventory(PlayerInventory inventory)
        {
            toolkitView.ShowPlayerInventory(inventory);
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
