using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour, PlayerControls.IPlayerActions
{

    private PlayerControls controls;

    public Vector2 lookInput; //�ٶ󺸱� ���� ����
    public Vector2 moveInput; //������ ���� ���� -1 or 1
    public event Action jumpPressed;
    public bool onSprint = false;

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
        }
        else if (context.canceled)
        {
            moveInput = Vector2.zero; // �Է� ���� �� �ʱ�ȭ
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onSprint = true;
        }
        else
        {
            onSprint = false;
        }
    }
}
