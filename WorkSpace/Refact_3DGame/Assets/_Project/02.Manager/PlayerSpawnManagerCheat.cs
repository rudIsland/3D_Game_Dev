#if UNITY_EDITOR
namespace Manager
{
    // 치트가 생성 담당의 보유 요청만 조회하도록 연결한다.
    public sealed partial class PlayerSpawnManager
    {
        // 치트는 전체 요청 수 대신 이 생성 담당의 보유 요청만 조회한다.
        internal bool OwnsRequest(string requestedAddress) => loaded && address == requestedAddress;
    }
}
#endif
