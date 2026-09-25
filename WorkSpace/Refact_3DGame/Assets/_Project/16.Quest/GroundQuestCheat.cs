#if UNITY_EDITOR
namespace Quest
{
    // 치트에서 진행 기록만 처음으로 되돌린다. 가방·맵 객체는 이 클래스가 소유하지 않는다.
    public sealed partial class GroundQuestProgress
    {
        /// <summary>첫 단계로 돌아가고 기존 Changed 알림을 보낸다. 이미 첫 단계면 변경하지 않는다.</summary>
        public void CheatReset()
        {
            if (Step != GroundQuestStep.FindBook) SetStep(GroundQuestStep.FindBook);
        }
    }
}
#endif
