using UnityEngine;

namespace Items
{
    [DisallowMultipleComponent]
    public sealed class ItemSpawnPoint : MonoBehaviour
    {
        [SerializeField]
        private ItemType itemType;

        public ItemType ItemType => itemType;
    }
}
