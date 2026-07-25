using System.Collections;
using UnityEngine;

public class MutantDeadState : EnemyDeadState<Mutant>
{

    public MutantDeadState(EnemyStateMachine stateMachine, Mutant mutant) : base(stateMachine, mutant)
    {
      
    }

    public override void Enter()
    {
        // 부모의 Enter실행
        base.Enter();

        
    }
    public override void Tick(float deltaTime)
    {

    }


    public override void Exit()
    {
        
    }
}
