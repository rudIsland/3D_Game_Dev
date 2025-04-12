using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//플레이어
public class BaseStateMachine : CharacterBase
{
    //공통 변수
    public Animator animator;
    public int stateNum;

    public readonly int _animIDBlendIndex = Animator.StringToHash("BlendIndex"); //state Number

    //현재 상태
    public State currentState = null;

    //상태를 바꾸는 메소드
    public void SwitchState(State newState)
    {
        currentState?.Exit();
        currentState = newState;
        switch (currentState)
        {
            case PlayerFreeLookState:
                stateNum = 0;
                animator.SetInteger(_animIDBlendIndex, stateNum);
                break;

            case PlayerTargetLookState:
                stateNum = 1;
                animator.SetInteger(_animIDBlendIndex, stateNum);
                break;

            default:
                stateNum = 0;
                animator.SetInteger(_animIDBlendIndex, stateNum);
                break;
        }
        currentState.Enter();
    }

    private void Update()
    {
        currentState.Tick(Time.deltaTime);
    }

    public override void ApplyDamage(double damage)  { }
}
