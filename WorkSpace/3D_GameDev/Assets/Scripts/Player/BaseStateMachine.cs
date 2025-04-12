using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//�÷��̾�
public class BaseStateMachine : CharacterBase
{
    //���� ����
    public Animator animator;
    public int stateNum;

    public readonly int _animIDBlendIndex = Animator.StringToHash("BlendIndex"); //state Number

    //���� ����
    public State currentState = null;

    //���¸� �ٲٴ� �޼ҵ�
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
