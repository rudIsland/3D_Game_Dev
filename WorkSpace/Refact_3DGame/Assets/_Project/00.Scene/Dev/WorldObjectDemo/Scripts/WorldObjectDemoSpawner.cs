using Characters.Enemies;
using Characters;
using World;
using UnityEngine;

namespace Development.WorldObjectDemo
{
    // 씬이 시작되면 테스트 객체 하나를 풀에서 꺼낸다.
    public sealed class WorldObjectDemoSpawner : MonoBehaviour
    {
        private EnemyContainer enemyContainer;
        [SerializeField] private EnemySpawnSettings spawnSettings; // 행동 설정 참조

        private EnemyView spawnedView; // 씬 또는 시스템 참조

        public void Connect(EnemyContainer container)
        {
            if (enemyContainer != null) return;
            enemyContainer = container;
            if (enemyContainer == null || spawnSettings == null)
            {
                Debug.LogError("WorldObjectDemoSpawner에 Manager와 SpawnSettings가 필요합니다.", this);
                return;
            }

            enemyContainer.RegisterPool(spawnSettings);
            if (!enemyContainer.TrySpawn(
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
            if (enemyContainer != null && spawnedView != null)
            {
                enemyContainer.Despawn(spawnedView);
            }
        }
    }
}
