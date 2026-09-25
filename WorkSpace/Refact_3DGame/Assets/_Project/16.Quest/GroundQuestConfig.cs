using UnityEngine;
using Core;

namespace Quest
{
    // 기존 지상 퀘스트가 요구하는 물품과 교환 결과를 정의한다.
    [CreateAssetMenu(fileName = "GroundQuestConfig", menuName = "Game/Ground Quest")]
    public sealed class GroundQuestConfig : ScriptableObject
    {
        [SerializeField] private ItemDefinition book;
        [SerializeField] private ItemDefinition scroll;

        public ItemDefinition Book => book;
        public ItemDefinition Scroll => scroll;
    }
}
