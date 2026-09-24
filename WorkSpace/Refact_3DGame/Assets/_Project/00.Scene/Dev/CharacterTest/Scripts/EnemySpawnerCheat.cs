#if UNITY_EDITOR
using UnityEngine;

namespace Development.CharacterTest
{
    // 개발 씬의 수동 소환 메뉴를 기존 소환 동작과 분리한다.
    public sealed partial class TestSceneEnemySpawner
    {
        // Play 중 대기 시간을 초기화한 뒤 기존 빈자리 소환을 요청한다.
        [ContextMenu("Spawn Missing Enemies")]
        private void SpawnMissingEnemiesFromInspector()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Spawn Missing Enemies는 Play 중에 사용해 주세요.", this);
                return;
            }

            for (int index = 0; index < remainingRespawnTimes.Length; index++)
            {
                remainingRespawnTimes[index] = 0f;
            }

            SpawnMissingEnemies();
        }
    }
}
#endif
