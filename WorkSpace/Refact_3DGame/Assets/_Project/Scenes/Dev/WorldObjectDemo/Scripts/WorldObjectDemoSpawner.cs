using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Dev.WorldDemo
{
    // 씬이 시작되면 테스트 객체 하나를 풀에서 꺼낸다.
    public sealed class WorldObjectDemoSpawner : MonoBehaviour
    {
        [SerializeField] private WorldObjectManager worldObjectManager; // 씬 또는 시스템 참조
        [SerializeField] private SpawnSettings spawnSettings; // 행동 설정 참조

        private WorldObjectView spawnedView; // 씬 또는 시스템 참조

        private void Start()
        {
            if (worldObjectManager == null || spawnSettings == null)
            {
                Debug.LogError("WorldObjectDemoSpawner에 Manager와 SpawnSettings가 필요합니다.", this);
                return;
            }

            if (!worldObjectManager.TrySpawn(
                    spawnSettings,
                    transform.position,
                    transform.rotation,
                    out spawnedView))
            {
                Debug.LogError("테스트 WorldObject를 풀에서 꺼내지 못했습니다.", this);
            }
        }

        // 씬 종료 전이라면 사용 중인 뷰를 원래 풀로 돌려준다.
        private void OnDestroy()
        {
            if (worldObjectManager != null && spawnedView != null)
            {
                worldObjectManager.Despawn(spawnedView);
            }
        }
    }
}
