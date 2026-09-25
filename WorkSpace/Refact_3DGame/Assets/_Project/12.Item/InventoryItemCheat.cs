#if UNITY_EDITOR
using Core;

namespace Item
{
    public sealed partial class InventoryItem
    {
        /// <summary>치트 목록에 표시할 씬 배치 아이템 정의다.</summary>
        public ItemDefinition CheatItem => itemDefinition;
    }
}
#endif
