using System;
using UnityEngine;
using World.Management;

namespace World.Boot
{
    // Inspector의 카메라를 전달하고 게임 수명의 시작·갱신·종료를 요청한다.
    [DisallowMultipleComponent]
    public sealed class Boots : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        private GameManager gameManager;

        // Unity가 한 번 호출하며 생성 상태와 로딩 작업은 GameManager가 소유한다.
        private async void Start()
        {
            try
            {
                gameManager = new GameManager();
                gameManager.Init(mainCamera, gameObject.scene);
                await gameManager.Start();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        // 준비된 플레이어만 게임 관리자를 통해 갱신한다.
        private void Update() => gameManager?.Tick(Time.deltaTime);

        // Play 종료에서는 새로운 비동기 씬 해제를 시작하지 않는다.
        private void OnApplicationQuit() => gameManager?.Quit();

        // 실행 중 Boot를 제거하면 완료된 요청까지 기다려 반환한다.
        private async void OnDestroy()
        {
            if (gameManager == null) return;
            try { await gameManager.Release(); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }
}
