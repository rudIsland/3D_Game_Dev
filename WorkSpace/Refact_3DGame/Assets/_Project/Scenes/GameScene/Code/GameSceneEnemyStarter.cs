using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Scenes.Game
{
    // GameScene 시작 시 지정한 적 한 명을 기존 WorldObject 풀에서 꺼낸다.
    [DisallowMultipleComponent]
    internal sealed class GameSceneEnemyStarter : MonoBehaviour
    {
        [SerializeField] private WorldObjectManager worldObjectManager;
        [SerializeField] private SpawnSettings zombieSpawnSettings;
        [SerializeField] private Transform zombieStartPoint;

        private WorldObjectView spawnedZombie;

        private void Start()
        {
            if (worldObjectManager == null ||
                zombieSpawnSettings == null ||
                zombieStartPoint == null)
            {
                Debug.LogError(
                    "GameSceneEnemyStarter에 WorldObjectManager, Zombie 설정과 시작 위치가 필요합니다.",
                    this);
                return;
            }

            if (!worldObjectManager.TrySpawn(
                    zombieSpawnSettings,
                    zombieStartPoint.position,
                    zombieStartPoint.rotation,
                    out spawnedZombie))
            {
                Debug.LogError(
                    "GameScene 시작 Zombie를 Spawn하지 못했습니다.",
                    this);
            }
        }

        private void OnDestroy()
        {
            if (worldObjectManager != null && spawnedZombie != null)
            {
                worldObjectManager.Despawn(spawnedZombie);
            }
        }
    }
}
