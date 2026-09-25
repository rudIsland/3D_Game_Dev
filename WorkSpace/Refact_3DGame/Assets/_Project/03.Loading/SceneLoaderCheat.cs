#if UNITY_EDITOR
using System.Threading.Tasks;
using UnityScene = UnityEngine.SceneManagement.Scene;
namespace Loading
{
    // 치트는 로더의 요청만 조회하며 개별 객체 반환 전에 진행 중인 씬 처리를 기다린다.
    public sealed partial class SceneLoader
    {
        /// <summary>이 로더가 아직 반환하지 않은 씬 요청인지 확인한다.</summary>
        public bool OwnsSceneRequest(string address) => loadedAddress == address;
        /// <summary>개별 객체 정리 전에 현재 씬 작업의 완료를 기다린다.</summary>
        public Task WaitForLoad() => sceneTask;
        /// <summary>로딩을 마친 현재 실행 씬만 치트에 제공한다.</summary>
        public bool TryGetCheatMap(out UnityScene scene)
        {
            scene = default;
            if (!sceneTask.IsCompletedSuccessfully || released || currentScene == null) return false;
            scene = currentScene.gameObject.scene;
            return scene.IsValid() && scene.isLoaded;
        }
        /// <summary>개별 플레이어 반환 전에 맵의 실행·구독·적 연결을 끊는다.</summary>
        public void DisconnectPlayer()
        {
            currentScene?.Disable();
            currentScene?.DisconnectPlayer();
        }
    }
}
#endif
