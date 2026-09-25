using Core;
using UnityEngine;

namespace Item
{
    [DisallowMultipleComponent]
    public sealed class ItemSpawnPoint : MonoBehaviour
    {
        [SerializeField]
        private ItemType itemType;

        public ItemType ItemType => itemType;
    }
}
