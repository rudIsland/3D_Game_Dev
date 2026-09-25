
namespace Core
{
    // 플레이어 감지기가 찾고 실행을 요청할 수 있는 월드 물체다.
    public interface IPlayerInteractable
    {
        PlayerInteractionGuide GetInteractionGuide(IInteractionActor player);
        bool CanInteract(IInteractionActor player);
        bool TryInteract(IInteractionActor player);
    }
}
