using UnityEngine;

namespace rudIsland.RPG3D.World
{
    // 하나의 프리팹을 몇 개 준비하고 보관할지 설정한다.
    [CreateAssetMenu(
        fileName = "SpawnSettings",
        menuName = "rudIsland/RPG3D/Spawn Settings")]
    public sealed class SpawnSettings : ScriptableObject
    {
        // 풀에서 복제할 프리팹이다.
        [SerializeField] private WorldObjectView prefab;
        // 시작할 때 미리 만들 뷰의 수다.
        [SerializeField, Min(0)] private int initialSize = 4;
        // 풀이 보관할 수 있는 최대 뷰 수다.
        [SerializeField, Min(1)] private int maxSize = 20;

        // 풀에서 복제할 프리팹을 반환한다.
        public WorldObjectView Prefab => prefab;
        // 시작 시 미리 만들 개수를 반환한다.
        public int InitialSize => initialSize;
        // 풀이 보관할 최대 개수를 반환한다.
        public int MaxSize => maxSize;

        // Inspector 값이 올바른 개수 범위가 되도록 조정한다.
        private void OnValidate()
        {
            maxSize = Mathf.Max(1, maxSize);
            initialSize = Mathf.Clamp(initialSize, 0, maxSize);
        }

        // 테스트에서 프리팹과 풀 크기를 지정한다.
        internal void SetValuesForTests(
            WorldObjectView testPrefab,
            int testInitialSize,
            int testMaxSize)
        {
            prefab = testPrefab;
            maxSize = Mathf.Max(1, testMaxSize);
            initialSize = Mathf.Clamp(testInitialSize, 0, maxSize);
        }
    }
}
