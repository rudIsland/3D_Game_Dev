using UnityEngine;

namespace rudIsland.RPG3D.World
{
    // 프리팹 하나의 예열 개수와 최대 보관 개수를 Inspector에서 설정한다.
    [CreateAssetMenu(
        fileName = "SpawnSettings",
        menuName = "rudIsland/RPG3D/Spawn Settings")]
    public sealed class SpawnSettings : ScriptableObject
    {
        [SerializeField] private WorldObjectView prefab;
        [SerializeField, Min(0)] private int initialSize = 4;
        [SerializeField, Min(1)] private int maxSize = 20;

        public WorldObjectView Prefab => prefab;
        public int InitialSize => initialSize;
        public int MaxSize => maxSize;

        // 잘못된 Inspector 값이 들어오지 않도록 개수 범위를 바로잡는다.
        private void OnValidate()
        {
            maxSize = Mathf.Max(1, maxSize);
            initialSize = Mathf.Clamp(initialSize, 0, maxSize);
        }

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
