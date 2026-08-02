using System.Collections.Generic;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.UI
{
    // Connects player and boss health events to their combat HUD bars.
    public sealed class CombatHudController : MonoBehaviour
    {
        [Header("World")]
        [SerializeField] private WorldObjectManager worldObjectManager; // 씬 또는 시스템 참조

        [Header("Health Bars")]
        [SerializeField] private HealthBarView playerHealthBar; // 씬 또는 시스템 참조
        [SerializeField] private HealthBarView bossHealthBar; // 씬 또는 시스템 참조

        private readonly Dictionary<UnitHealth, Unit> trackedUnits = // 씬 또는 시스템 참조
            new Dictionary<UnitHealth, Unit>(2);

        private void Awake()
        {
            if (worldObjectManager == null)
            {
                worldObjectManager =
                    FindFirstObjectByType<WorldObjectManager>();
            }

            playerHealthBar?.Hide();
            bossHealthBar?.Hide();
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
            }

            trackedUnits.Clear();
            playerHealthBar?.Hide();
            bossHealthBar?.Hide();
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

            bool isPlayer = unit.Team == UnitTeam.Player;
            bool isBoss = unit is EnemyUnit enemy && enemy.IsBoss;
            if ((!isPlayer && !isBoss) ||
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
            }
            else
            {
                bossHealthBar?.Show("DEMON SWORDSMAN", unit.Health);
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

            if (unit.Team == UnitTeam.Player)
            {
                playerHealthBar?.Hide();
            }
            else if (unit is EnemyUnit enemy && enemy.IsBoss)
            {
                bossHealthBar?.Hide();
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

            if (unit.Team == UnitTeam.Player)
            {
                playerHealthBar?.UpdateHealth(health);
                return;
            }

            if (unit is EnemyUnit enemy && enemy.IsBoss)
            {
                bossHealthBar?.UpdateHealth(health);
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

                if (unit is EnemyUnit enemy && enemy.IsBoss)
                {
                    bossHealthBar?.Hide();
                }
            }
        }

#if UNITY_EDITOR
        public void ConnectForEditor(
            WorldObjectManager manager,
            HealthBarView playerBar,
            HealthBarView bossBar)
        {
            worldObjectManager = manager;
            playerHealthBar = playerBar;
            bossHealthBar = bossBar;
        }
#endif
    }
}
