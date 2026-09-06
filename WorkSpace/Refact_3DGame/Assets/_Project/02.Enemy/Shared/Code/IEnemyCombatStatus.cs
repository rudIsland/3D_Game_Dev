using System;

namespace Characters.Enemies
{
    // 적 종류와 관계없이 화면에 표시할 전투 정보와 변경 알림을 제공한다.
    public interface IEnemyCombatStatus
    {
        string DisplayName { get; }
        bool ShowScreenHealthBar { get; }
        UnitHealth Health { get; }
        float CurrentStagger { get; }
        float MaxStagger { get; }
        bool IsInCombat { get; }

        event Action<IEnemyCombatStatus> StaggerChanged;
        event Action<IEnemyCombatStatus> CombatStateChanged;
    }
}
