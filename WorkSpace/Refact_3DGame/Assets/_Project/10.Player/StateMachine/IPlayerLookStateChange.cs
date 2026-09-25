namespace Player
{
    // 시점 상태와 피격 상태가 상위 상태 전환만 즉시 요청할 수 있는 경계다.
    internal interface IPlayerLookStateChange
    {
        // 대상을 찾았을 때 락온 시점으로 전환한다.
        void TryChangeToTargetLookState();
        // 대상을 해제하고 자유 시점으로 전환한다.
        void ChangeToFreeLookState();
        // 피격 전 시점으로 복귀하되 대상이 사라졌으면 자유 시점을 사용한다.
        void ChangeToLookState();
    }
}
