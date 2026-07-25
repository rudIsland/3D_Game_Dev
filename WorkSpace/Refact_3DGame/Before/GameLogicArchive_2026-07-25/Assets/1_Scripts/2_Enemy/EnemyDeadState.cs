
using UnityEngine;

// T는 Enemy를 상속받는 타입이어야 함
public abstract class EnemyDeadState<T> : EnemyState where T : Enemy
{
    protected T enemy; // 제네릭 타입으로 선언

    public EnemyDeadState(EnemyStateMachine stateMachine, T enemy) : base(stateMachine)
    {
        this.enemy = enemy; // 여기서 할당됨
    }

    public override void Enter()
    {
        // 1. 물리 충돌 즉시 제외 (시체에 걸리는 현상 방지)
        if (enemy.TryGetComponent(out Collider col)) col.enabled = false;

        // 2. 경험치 전달 (즉시 호출)
        // 이 시점에 플레이어의 AddExperience가 실행됩니다.
        enemy.CallDeathEvent();

        enemy.isTarget = false; // 타겟 해제

        // 3. 애니메이션 및 시각 연출 시작
        stateMachine.enemy.animator.SetTrigger(enemy._animIDDead);
        enemy.StartDissolve();
    }

}
