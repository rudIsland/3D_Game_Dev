using UnityEngine;
using Core;
using Enemy;
using Item;
using Player;
using Quest;

namespace Data
{
    // Boots Inspector에서 각 데이터 컨테이너에 나눠 전달할 설정을 지정하는 입력 목록이다.
    [CreateAssetMenu(fileName = "GameData", menuName = "Game/Game Data")]
    public sealed class GameData : ScriptableObject
    {
        [SerializeField] private string startMap = ConstantValid.GroundMapAddress;
        [SerializeField] private string playerAddress = ConstantValid.PlayerAddress;
        [SerializeField] private string hudAddress = ConstantValid.HudAddress;
        [SerializeField] private PlayerCharacterConfig player;
        [SerializeField] private EnemySpawnSettings[] enemies;
        [SerializeField] private ItemCatalog items;
        [SerializeField] private GroundQuestConfig groundQuest;

        public string StartMap => startMap;
        public string PlayerAddress => playerAddress;
        public string HudAddress => hudAddress;
        public PlayerCharacterConfig Player => player;
        public EnemySpawnSettings[] Enemies => enemies;
        public ItemCatalog Items => items;
        public GroundQuestConfig GroundQuest => groundQuest;
    }
}
