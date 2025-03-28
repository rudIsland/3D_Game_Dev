using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour, PlayerControls.IPlayerActions
{

    private PlayerControls controls;

    public Vector2 lookInput; //�ٶ󺸱� ���� ����
    public Vector2 moveInput; //������ ���� ���� -1 or 1
    public bool isMove = false;
    public bool isJump = false;
    public event Action jumpPressed;
    public bool onSprint = false;
    public bool isAttack = false;
    public event Action TargetPressed;

    void Start()
    {
        // Input System �ʱ�ȭ
        controls = new PlayerControls();
        controls.Player.SetCallbacks(this);

        controls.Player.Enable();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            //Debug.Log("����");
            jumpPressed?.Invoke();
            isJump = true;
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            lookInput = context.ReadValue<Vector2>();
           // Debug.Log($"�ٶ󺸱� �Է�: {lookInput}");
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            moveInput = context.ReadValue<Vector2>();
            isMove = true;
        }
        else if (context.canceled)
        {
            moveInput = Vector2.zero; // �Է� ���� �� �ʱ�ȭ
            isMove = false;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if(context.performed && !isJump)
        {
            isAttack = true;
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // ������Ʈ Ȱ��ȭ
            onSprint = true;
            Debug.Log("������Ʈ ����");
        }
        else if (context.canceled)
        {
            // ������Ʈ ��Ȱ��ȭ
            onSprint = false;
            Debug.Log("������Ʈ ����");
        }
    }


    public void OnTarget(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TargetPressed?.Invoke();
        }
    }
}
