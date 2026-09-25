#if UNITY_EDITOR
namespace Scene
{
    // 치트가 Start 씬의 유지 객체를 소유 요청 기준으로 조회한다.
    public sealed partial class StartScene
    {
        internal bool OwnsPlayerRequest(string address) => playerSpawn != null && playerSpawn.OwnsRequest(address);
        internal bool OwnsHudRequest(string address) => hudSpawn != null && hudSpawn.OwnsRequest(address);
    }
}
#endif
