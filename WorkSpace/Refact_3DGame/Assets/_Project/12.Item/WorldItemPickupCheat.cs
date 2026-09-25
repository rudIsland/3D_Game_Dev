#if UNITY_EDITOR
using Core;

namespace Item
{
    public sealed partial class WorldItemPickup
    {
        /// <summary>치트 목록에 표시할 실제 연결된 아이템 정의다.</summary>
        public ItemDefinition CheatItem => itemDefinition;
    }
}
#endif
