#if UNITY_EDITOR
using System;
using System.Threading.Tasks;

namespace Boot
{
    // Editor 창에 소유 리소스·현재 맵·보관 데이터를 제공하고 변경 요청은 기존 게임 API로 전달한다.
    public sealed partial class Boots
    {
        /// <summary>Editor 데이터 창에 시작 설정 원본을 제공한다. 데이터를 생성하거나 등록하지 않는다.</summary>
        public Data.GameData CheatGameData => gameData;

        /// <summary>선택한 종류의 컨테이너가 보관 중인 원본만 추가한다. 해제된 설정은 제외한다.</summary>
        public void CopyCheatData(Type containerType, System.Collections.Generic.List<UnityEngine.ScriptableObject> result)
        {
            if (!UnityEngine.Application.isPlaying) return;
            if (containerType == typeof(Data.PlayerDataContainer) && playerData?.Config != null) result.Add(playerData.Config);
            if (containerType == typeof(Data.EnemyDataContainer)) enemyData?.CopyCheatData(result);
            if (containerType == typeof(Data.ItemDataContainer) && itemData?.Catalog != null) result.Add(itemData.Catalog);
            if (containerType == typeof(Data.QuestDataContainer) && questData?.Ground != null) result.Add(questData.Ground);
        }

        /// <summary>맵 준비가 끝난 게임의 유지 중인 퀘스트 기록을 조회한다. 새 기록을 만들지 않는다.</summary>
        public bool TryGetCheatQuest(out Quest.GroundQuestProgress progress)
        {
            progress = null;
            if (!TryGetCheatMap(out _) || startScene.Quests == null) return false;
            progress = startScene.Quests.Ground;
            return progress != null;
        }

        /// <summary>치트가 조회·실행할 수 있는 현재 게임 맵을 반환한다.</summary>
        public bool TryGetCheatMap(out UnityEngine.SceneManagement.Scene scene)
        {
            scene = default;
            return startScene != null && startScene.Player != null &&
                sceneLoader != null && sceneLoader.TryGetCheatMap(out scene);
        }

        /// <summary>게임이 소유한 주소면 함께 정리할 대상을 설명하고, 소유하지 않으면 null을 반환한다.</summary>
        public string GetResourceDeleteDescription(string address, bool isScene)
        {
            if (isScene) return sceneLoader != null && sceneLoader.OwnsSceneRequest(address)
                ? "현재 맵과 플레이어·HUD·몬스터·풀을 함께 정리합니다." : null;
            if (startScene == null) return null;
            if (startScene.OwnsHudRequest(address)) return "게임 HUD와 구독을 정리합니다. 플레이어·맵은 유지합니다.";
            if (startScene.OwnsPlayerRequest(address)) return "게임 플레이어와 연결된 HUD·몬스터·풀을 정리합니다. 맵은 유지합니다.";
            return null;
        }

        /// <summary>치트에서 현재 게임의 씬 정리·전환 경로를 호출한다.</summary>
        public async Task MoveScene(string address)
        {
            await cleanupTask;
            await sceneLoader.LoadAsync(address);
        }

        /// <summary>게임이 소유한 요청을 사용 객체와 함께 반환한다. 다른 소유자의 요청은 건드리지 않는다.</summary>
        public Task DeleteResource(string address, bool isScene)
        {
            if (GetResourceDeleteDescription(address, isScene) == null)
                throw new InvalidOperationException("현재 실행이 소유한 리소스 요청이 아닙니다: " + address);
            if (isScene) return Release();
            return cleanupTask = DeleteObject(cleanupTask, startScene.OwnsPlayerRequest(address));
        }

        // 대기·반환은 한 작업으로 이어서 같은 원본 요청을 동시에 반환하지 않는다.
        private async Task DeleteObject(Task previous, bool player)
        {
            await previous;
            try { await sceneLoader.WaitForLoad(); }
            catch { /* 로딩 실패 뒤에도 확보한 유지 객체는 반환한다. */ }
            if (player)
            {
                sceneLoader.DisconnectPlayer();
                await startScene.ReleasePlayer();
            }
            else await startScene.ReleaseHud();
        }
    }
}
#endif
