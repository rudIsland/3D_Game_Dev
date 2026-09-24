using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Core;

namespace World.Loading
{
    // 맵 요청을 AddressableManager에 전달한다. 씬 핸들과 사용 횟수는 직접 보관하지 않는다.
    public sealed class MapManager : Singleton<MapManager>
    {
        /// <summary>맵 로딩·반환 요청을 받는 단일 진입점이다. Unity 메인 스레드에서 사용한다.</summary>
        public new static MapManager Instance => CurrentInstance ?? StoreInstance(new MapManager());

        // 맵 요청 진입점을 하나로 유지한다.
        private MapManager() { }

        // Domain Reload를 꺼도 이전 Play의 매니저를 다시 사용하지 않는다.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlay() { ResetInstance(); }

        /// <summary>AddressableManager가 보관 중인 씬 수를 반환한다.</summary>
        public int ResourceCount => AddressableManager.Instance.SceneCount;

        /// <summary>에셋 또는 씬의 로딩·정리가 진행 중인지 반환한다.</summary>
        public bool IsBusy => AddressableManager.Instance.IsBusy;

        /// <summary>주소에 해당하는 씬의 사용 요청 수를 반환한다. 미등록 주소는 0이다.</summary>
        public int GetRefCount(string address) => AddressableManager.Instance.GetSceneRefCount(address);

        /// <summary>맵 주소를 전달해 추가 로딩하고 완료된 씬을 반환한다. 실패는 예외로 알린다.</summary>
        public Task<Scene> LoadAsync(string address) => AddressableManager.Instance.LoadSceneAsync(address);

        /// <summary>맵 사용 요청을 한 번 반환한다. 감소하면 true이며 실제 씬 해제는 별도 정리 시 수행한다.</summary>
        public bool Release(string address) => AddressableManager.Instance.ReleaseScene(address);

        /// <summary>사용 요청 수가 0인 씬을 해제하고 정리 완료까지 기다린다.</summary>
        public Task CleanUpAsync() => AddressableManager.Instance.CleanUpUnusedScenesAsync();
    }
}
