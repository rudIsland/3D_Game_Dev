using System.Collections.Generic;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using rudIsland.RPG3D.Characters.Enemies.Zombie;
using rudIsland.RPG3D.Player;
using rudIsland.RPG3D.Player.Runtime;
using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.UI
{
    // 플레이어 자원과 현재 전투 중인 적 자원을 화면 HUD에 연결한다.
    public sealed class CombatHudController : MonoBehaviour
    {
        [Header("World")]
        [SerializeField] private WorldObjectManager worldObjectManager; // 씬 또는 시스템 참조

        [Header("Health Bars")]
        [SerializeField] private HealthBarView playerHealthBar; // 씬 또는 시스템 참조
        [SerializeField] private HealthBarView enemyHealthBar;

        [Header("Stamina Bar")]
        [SerializeField] private StaminaBarView playerStaminaBar;

        [Header("Enemy Stagger Bar")]
        [SerializeField] private StaggerBarView enemyStaggerBar;

        private readonly Dictionary<UnitHealth, Unit> trackedUnits = // 씬 또는 시스템 참조
            new Dictionary<UnitHealth, Unit>(3);
        private Unit displayedEnemy;

        private void Awake()
        {
            if (worldObjectManager == null)
            {
                worldObjectManager =
                    FindFirstObjectByType<WorldObjectManager>();
            }

            playerHealthBar?.Hide();
            playerStaminaBar?.Hide();
            enemyHealthBar?.Hide();
            enemyStaggerBar?.Hide();
        }

        private void OnEnable()
        {
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
            displayedEnemy = null;
            playerHealthBar?.Hide();
            playerStaminaBar?.Hide();
            enemyHealthBar?.Hide();
            enemyStaggerBar?.Hide();
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
                playerHealthBar?.Show("PLAYER", unit.Health);

                if (unit is PlayerWorldUnit player)
                {
                    player.Stamina.StaminaChanged +=
                        HandleStaminaChanged;
                    playerStaminaBar?.Show(player.CurrentStamina, player.MaxStamina);
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
                playerHealthBar?.Hide();

                if (unit is PlayerWorldUnit player)
                {
                    player.Stamina.StaminaChanged -=
                        HandleStaminaChanged;
                }

                playerStaminaBar?.Hide();
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

            if (unit is PlayerUnit)
            {
                playerHealthBar?.UpdateHealth(health);
                return;
            }

            if (ReferenceEquals(displayedEnemy, unit))
            {
                enemyHealthBar?.UpdateHealth(health);
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
            playerStaminaBar?.UpdateStamina(stamina.CurrentStamina, stamina.MaxStamina);
        }

        private void HandleZombieStaggerChanged(ZombieWorldUnit zombie)
        {
            if (!ReferenceEquals(displayedEnemy, zombie))
            {
                return;
            }

            enemyStaggerBar?.UpdateStagger(zombie.CurrentStagger, zombie.MaxStagger);
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

            enemyStaggerBar?.UpdateStagger(nightShade.CurrentStagger, nightShade.MaxStagger);
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
            enemyHealthBar?.Show(enemyName, enemy.Health);
            enemyStaggerBar?.Show(currentStagger, maxStagger);
        }

        private void HideEnemy(Unit enemy)
        {
            if (!ReferenceEquals(displayedEnemy, enemy))
            {
                return;
            }

            displayedEnemy = null;
            enemyHealthBar?.Hide();
            enemyStaggerBar?.Hide();
        }

#if UNITY_EDITOR
        public void ConnectForEditor(
            WorldObjectManager manager,
            HealthBarView playerBar,
            StaminaBarView staminaBar,
            HealthBarView enemyBar,
            StaggerBarView staggerBar)
        {
            worldObjectManager = manager;
            playerHealthBar = playerBar;
            playerStaminaBar = staminaBar;
            enemyHealthBar = enemyBar;
            enemyStaggerBar = staggerBar;
        }
#endif
    }
}
