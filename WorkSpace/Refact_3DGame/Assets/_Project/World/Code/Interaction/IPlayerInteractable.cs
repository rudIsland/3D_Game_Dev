using Characters.Player.Lifecycle;

namespace World.Interaction
{
    // 플레이어 감지기가 찾고 실행을 요청할 수 있는 월드 물체다.
    public interface IPlayerInteractable
    {
        bool CanInteract(PlayerController player);
        bool TryInteract(PlayerController player);
    }
}
