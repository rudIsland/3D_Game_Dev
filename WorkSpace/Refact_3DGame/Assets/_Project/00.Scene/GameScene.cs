using System.Threading.Tasks;
using UnityEngine;

namespace Scene
{
    // 씬에 배치된 실행 코드의 공통 호출 순서다. 전역 데이터와 씬별 규칙은 구현 씬이 선택한다.
    public abstract class GameScene : MonoBehaviour
    {
        /// <summary>씬 내부에서 필요한 설정과 연결을 준비한다.</summary>
        public abstract void Init();
        /// <summary>씬의 데이터와 실행 객체를 준비한다.</summary>
        public abstract Task Create();
        /// <summary>준비된 씬의 플레이를 시작한다.</summary>
        public abstract void Enable();
        /// <summary>이탈 전에 씬의 플레이와 새 소환을 중지한다.</summary>
        public abstract void Disable();
        /// <summary>플레이어 개별 반환 전에 이 씬의 플레이어 연결을 끊는다.</summary>
        public abstract void DisconnectPlayer();
        /// <summary>씬이 소유한 객체·구독·리소스 요청을 반환한다.</summary>
        public abstract void Release();
    }
}
