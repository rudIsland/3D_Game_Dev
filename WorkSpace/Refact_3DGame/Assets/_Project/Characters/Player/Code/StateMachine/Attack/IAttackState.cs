using UnityEngine;

namespace rudIsland.RPG3D.Player.States.Attack
{
    // 공격 부모 상태가 공격별 상태를 다루기 위한 공통 읽기 계약이다.
    internal interface IAttackState
    {
        int AttackNumber { get; }
        float Damage { get; }
        float StaggerDamage { get; }
        float PushDistance { get; }
        float HitStopDuration { get; }
        float StaminaCost { get; }
        float NextInputTime { get; }
        float MoveDistance { get; }
        AnimationCurve MovementCurve { get; }
    }
}
