using System.Collections.Generic;
using Characters.Player.Interaction;
using Characters.Player.Inventory;
using Characters;
using Characters.Enemies.NightShade;
using Characters.Enemies.Zombie;
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
        [Header("World")]
        [SerializeField] private WorldObjectManager worldObjectManager; // 씬 또는 시스템 참조

        [Header("UI Toolkit")]
        [SerializeField] private CombatHudToolkitView toolkitView;

        [Header("Interaction UI")]
        [SerializeField]
        private PlayerInteractionController playerInteractionController;

        private readonly Dictionary<UnitHealth, Unit> trackedUnits = // 씬 또는 시스템 참조
            new Dictionary<UnitHealth, Unit>(3);
        private PlayerWorldUnit displayedPlayer;
        private Unit displayedEnemy;

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

            if (worldObjectManager == null)
            {
                worldObjectManager =
                    FindFirstObjectByType<WorldObjectManager>();
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
            if (playerInteractionController != null)
            {
                playerInteractionController.InteractionGuideChanged +=
                    HandleInteractionGuideChanged;
                HandleInteractionGuideChanged(
                    playerInteractionController.CurrentInteractionGuide);
            }

            if (worldObjectManager == null)
            {
                Debug.LogError("CombatHudController requires a WorldObjectManager.", this);
                enabled = false;
                return;
            }

            worldObjectManager.WorldObjectEnabled += HandleWorldObjectEnabled;
            worldObjectManager.WorldObjectDisabled += HandleWorldObjectDisabled;

            IReadOnlyList<IWorldObject> activeObjects =
                worldObjectManager.ActiveObjects;
            for (int index = 0; index < activeObjects.Count; index++)
            {
                Track(activeObjects[index]);
            }
        }

        private void OnDisable()
        {
            if (playerInteractionController != null)
            {
                playerInteractionController.InteractionGuideChanged -=
                    HandleInteractionGuideChanged;
            }

            if (worldObjectManager != null)
            {
                worldObjectManager.WorldObjectEnabled -= HandleWorldObjectEnabled;
                worldObjectManager.WorldObjectDisabled -= HandleWorldObjectDisabled;
            }

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
                else if (entry.Value is ZombieWorldUnit zombie)
                {
                    zombie.StaggerChanged -= HandleZombieStaggerChanged;
                    zombie.CombatStateChanged -= HandleZombieCombatStateChanged;
                }
                else if (entry.Value is NightShadeSwordWorldUnit nightShade)
                {
                    nightShade.StaggerChanged -= HandleNightShadeStaggerChanged;
                    nightShade.CombatStateChanged -=
                        HandleNightShadeCombatStateChanged;
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

        private void HandleWorldObjectEnabled(IWorldObject worldObject)
        {
            Track(worldObject);
        }

        private void HandleWorldObjectDisabled(IWorldObject worldObject)
        {
            if (worldObject is Unit unit)
            {
                StopTracking(unit);
            }
        }

        private void Track(IWorldObject worldObject)
        {
            if (!(worldObject is Unit unit))
            {
                return;
            }

            bool isPlayer = unit is PlayerUnit;
            bool isZombie = unit is ZombieWorldUnit;
            bool isNightShade = unit is NightShadeSwordWorldUnit;
            if ((!isPlayer && !isZombie && !isNightShade) ||
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
            else if (unit is ZombieWorldUnit zombie)
            {
                zombie.StaggerChanged += HandleZombieStaggerChanged;
                zombie.CombatStateChanged += HandleZombieCombatStateChanged;
                if (zombie.IsInCombat)
                {
                    ShowZombie(zombie);
                }
            }
            else if (unit is NightShadeSwordWorldUnit nightShade)
            {
                nightShade.StaggerChanged += HandleNightShadeStaggerChanged;
                nightShade.CombatStateChanged +=
                    HandleNightShadeCombatStateChanged;
                if (nightShade.IsInCombat)
                {
                    ShowNightShade(nightShade);
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
            else if (unit is ZombieWorldUnit zombie)
            {
                zombie.StaggerChanged -= HandleZombieStaggerChanged;
                zombie.CombatStateChanged -= HandleZombieCombatStateChanged;
                HideEnemy(zombie);
            }
            else if (unit is NightShadeSwordWorldUnit nightShade)
            {
                nightShade.StaggerChanged -= HandleNightShadeStaggerChanged;
                nightShade.CombatStateChanged -=
                    HandleNightShadeCombatStateChanged;
                HideEnemy(nightShade);
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
                    HideEnemy(unit);
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

        private void HandleZombieStaggerChanged(ZombieWorldUnit zombie)
        {
            if (!ReferenceEquals(displayedEnemy, zombie))
            {
                return;
            }

            UpdateEnemyStagger(zombie.CurrentStagger, zombie.MaxStagger);
        }

        private void HandleZombieCombatStateChanged(ZombieWorldUnit zombie)
        {
            if (zombie.IsInCombat)
            {
                ShowZombie(zombie);
                return;
            }

            HideEnemy(zombie);
        }

        private void ShowZombie(ZombieWorldUnit zombie)
        {
            ShowEnemy(
                "ZOMBIE",
                zombie,
                zombie.CurrentStagger,
                zombie.MaxStagger);
        }

        private void HandleNightShadeStaggerChanged(NightShadeSwordWorldUnit nightShade)
        {
            if (!ReferenceEquals(displayedEnemy, nightShade))
            {
                return;
            }

            UpdateEnemyStagger(
                nightShade.CurrentStagger,
                nightShade.MaxStagger);
        }

        private void HandleNightShadeCombatStateChanged(NightShadeSwordWorldUnit nightShade)
        {
            if (nightShade.IsInCombat)
            {
                ShowNightShade(nightShade);
                return;
            }

            HideEnemy(nightShade);
        }

        private void ShowNightShade(NightShadeSwordWorldUnit nightShade)
        {
            ShowEnemy(
                "NIGHTSHADE",
                nightShade,
                nightShade.CurrentStagger,
                nightShade.MaxStagger);
        }

        private void ShowEnemy(
            string enemyName,
            Unit enemy,
            float currentStagger,
            float maxStagger)
        {
            displayedEnemy = enemy;
            toolkitView.ShowEnemyHealth(enemyName, enemy.Health);
            toolkitView.ShowEnemyStagger(currentStagger, maxStagger);
        }

        private void HideEnemy(Unit enemy)
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
