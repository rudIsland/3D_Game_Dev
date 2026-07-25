using System.Collections;
using UnityEngine;

public class ZomBDeadState : EnemyDeadState<Zombie>
{
    public ZomBDeadState(EnemyStateMachine stateMachine, Zombie zombie) : base(stateMachine, zombie)
    {
    }

    public override void Enter()
    {
        // 부모의 Enter실행
        base.Enter();

        //좀비만의 죽음연출 가능
    }
    public override void Tick(float deltaTime)
    {

    }


    public override void Exit()
    {
       // Debug.Log("Zombie Dead State나가기");
    }
}
