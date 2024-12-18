using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

/*
 * �÷��̾��� ���� ����
 */
public class PlayerStateMachine : BaseStateMachine
{
    public CharacterController characterController;
    public Rigidbody rigid;

    [Header("�÷��̾� ������")]
    public VariableJoystick joystick;
    public float moveSpeed = 5f;
    public float rotateSpeed = 0.2f;

    

    
   

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
      
    }

   
    private void Start()
    {
        SwitchState(new PlayerFreeLookState(this));
    }


}
