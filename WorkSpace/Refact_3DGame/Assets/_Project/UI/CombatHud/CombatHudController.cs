using System.Collections.Generic;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Characters.Enemies.Zombie;
using rudIsland.RPG3D.Player;
using rudIsland.RPG3D.Player.Runtime;
using rudIsland.RPG3D.World;
using UnityEngine;
using UnityEngine.Serialization;

namespace rudIsland.RPG3D.UI
{
    // 플레이어 자원과 전투 중인 좀비 자원을 화면 HUD에 연결한다.
    public sealed class CombatHudController : MonoBehaviour
    {
        [Header("World")]
        [SerializeField] private WorldObjectManager worldObjectManager; // 씬 또는 시스템 참조

        [Header("Health Bars")]
        [SerializeField] private HealthBarView playerHealthBar; // 씬 또는 시스템 참조
        [FormerlySerializedAs("bossHealthBar")]
        [SerializeField] private HealthBarView enemyHealthBar;

        [Header("Stamina Bar")]
        [SerializeField] private StaminaBarView playerStaminaBar;

        [Header("Enemy Stagger Bar")]
        [SerializeField] private StaggerBarView enemyStaggerBar;

        private readonly Dictionary<UnitHealth, Unit> trackedUnits = // 씬 또는 시스템 참조
            new Dictionary<UnitHealth, Unit>(2);
        private ZombieWorldUnit displayedZombie;

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
                Debug.LogError(
                    "CombatHudController requires a WorldObjectManager.",
                    this);
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
            }

            trackedUnits.Clear();
            displayedZombie = null;
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
            if ((!isPlayer && !isZombie) ||
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
                    playerStaminaBar?.Show(
                        player.CurrentStamina,
                        player.MaxStamina);
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
        }

        private void StopTracking(Unit unit)
        {
            if (!trackedUnits.Remove(
                    unit.Health,
                    out _))
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
                if (ReferenceEquals(displayedZombie, zombie))
                {
                    HideZombie();
                }
            }
        }

        private void HandleHealthChanged(UnitHealth health)
        {
            if (!trackedUnits.TryGetValue(
                    health,
                    out Unit unit))
            {
                return;
            }

            if (unit is PlayerUnit)
            {
                playerHealthBar?.UpdateHealth(health);
                return;
            }

            if (unit is ZombieWorldUnit zombie &&
                ReferenceEquals(displayedZombie, zombie))
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

                if (unit is ZombieWorldUnit zombie &&
                    ReferenceEquals(displayedZombie, zombie))
                {
                    HideZombie();
                }
            }
        }

        private void HandleStaminaChanged(PlayerStamina stamina)
        {
            playerStaminaBar?.UpdateStamina(
                stamina.CurrentStamina,
                stamina.MaxStamina);
        }

        private void HandleZombieStaggerChanged(ZombieWorldUnit zombie)
        {
            if (!ReferenceEquals(displayedZombie, zombie))
            {
                return;
            }

            enemyStaggerBar?.UpdateStagger(
                zombie.CurrentStagger,
                zombie.MaxStagger);
        }

        private void HandleZombieCombatStateChanged(ZombieWorldUnit zombie)
        {
            if (zombie.IsInCombat)
            {
                ShowZombie(zombie);
                return;
            }

            if (ReferenceEquals(displayedZombie, zombie))
            {
                HideZombie();
            }
        }

        private void ShowZombie(ZombieWorldUnit zombie)
        {
            displayedZombie = zombie;
            enemyHealthBar?.Show("ZOMBIE", zombie.Health);
            enemyStaggerBar?.Show(
                zombie.CurrentStagger,
                zombie.MaxStagger);
        }

        private void HideZombie()
        {
            displayedZombie = null;
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
