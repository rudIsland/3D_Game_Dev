using UnityEngine;

namespace Items
{
    [CreateAssetMenu(fileName = "Item", menuName = "Items/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Item";

        [SerializeField]
        private Sprite icon;

        [SerializeField, Min(1)]
        private int maxStackCount = 1;

        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int MaxStackCount => Mathf.Max(1,maxStackCount);
    }
}