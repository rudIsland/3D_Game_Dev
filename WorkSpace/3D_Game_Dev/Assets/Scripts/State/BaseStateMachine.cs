using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//�÷��̾� �Ǵ� ���� ���� ����
public class BaseStateMachine : MonoBehaviour
{
    //���� ����
    public Animator animator;

    //���� ����
    State currentState = null;


    //���¸� �ٲٴ� �޼ҵ�
    public void SwitchState(State newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    private void Update()
    {
        currentState.Tick(Time.deltaTime);
    }
}
