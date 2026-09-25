using System;
using System.Threading.Tasks;
using Core;
using UnityEngine;
using Loading;
using Scene;
using Data;

namespace Boot
{
    // Inspector의 시작 데이터를 먼저 등록하고 카메라·첫 씬 로드·종료를 전달한다.
    [DisallowMultipleComponent]
    public sealed partial class Boots : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private GameData gameData;
        private StartScene startScene;
        private PlayerDataContainer playerData;
        private EnemyDataContainer enemyData;
        private ItemDataContainer itemData;
        private QuestDataContainer questData;
        private SceneLoader sceneLoader;
        private Task cleanupTask = Task.CompletedTask;

        // Unity가 한 번 호출하며 데이터를 준비한 뒤 씬 로더에 첫 주소를 전달한다.
        private async void Start()
        {
            try
            {
                playerData = PlayerDataContainer.Create(gameData.Player);
                enemyData = EnemyDataContainer.Create(gameData.Enemies);
                itemData = ItemDataContainer.Create(gameData.Items);
                questData = QuestDataContainer.Create(gameData.GroundQuest);
                startScene = GetComponent<StartScene>();
                startScene.Init(mainCamera, gameData);
                sceneLoader = SceneLoader.Create(ConstantValid.StartSceneName);
                await sceneLoader.LoadAsync(gameData.StartMap);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                await Release();
            }
        }

        // Play 종료에서는 새로운 비동기 씬 해제를 시작하지 않는다.
        private void OnApplicationQuit()
        {
            sceneLoader?.Quit();
            startScene?.Quit();
            ReleaseData();
        }

        // 게임 객체의 반환이 끝나면 각 종류의 데이터 참조도 놓는다.
        private void ReleaseData()
        {
            questData?.Dispose();
            itemData?.Dispose();
            enemyData?.Dispose();
            playerData?.Dispose();
        }

        // 개별 치트 반환과 전체 종료가 겹치면 앞선 객체 정리가 끝난 뒤 진행한다.
        private Task Release() => cleanupTask = ReleaseObjects(cleanupTask);

        private async Task ReleaseObjects(Task previous)
        {
            try { await previous; }
            catch { /* 개별 반환 호출자가 오류를 받는다. 전체 정리는 계속한다. */ }
            if (sceneLoader != null) await sceneLoader.Release();
            if (!ReferenceEquals(startScene, null)) await startScene.Release();
        }

        // 실행 중 Boot를 제거하면 완료된 요청까지 기다려 반환한다.
        private async void OnDestroy()
        {
            try { await Release(); }
            catch (Exception exception) { Debug.LogException(exception); }
            finally { ReleaseData(); }
        }
    }
}
