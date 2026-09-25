#if UNITY_EDITOR
using Core;

namespace Interaction
{
    public sealed partial class ItemExchangeInteraction
    {
        /// <summary>현재 교환에 실제 연결된 비용 아이템이다.</summary>
        public ItemDefinition CheatCostItem => costItem;
        /// <summary>현재 교환에 실제 연결된 보상 아이템이다.</summary>
        public ItemDefinition CheatRewardItem => rewardItem;
    }
}
#endif
