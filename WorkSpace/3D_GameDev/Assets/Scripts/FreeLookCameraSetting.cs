using Cinemachine;
using UnityEngine;

public class FreeLookCameraSetting : MonoBehaviour
{
    public CinemachineFreeLook freeLookCamera;
    public Transform player;
    public PlayerInputReader inputReader;

    public float holdDuration = 0.5f; // �ڷ� �Է� ���� �ð� (��)
    public float rotateSpeed = 10f; // ȸ�� �ӵ�

    private float backMoveTimer = 0f;
    private bool isRotatingBack = false;

    void Update()
    {
        float moveY = inputReader.moveInput.y;

        // �ڷ� �̵� ���� �� �ð� ����
        if (moveY < -0.1f)
        {
            backMoveTimer += Time.deltaTime;

            // ���� �ð� ���ӵǸ� ī�޶� ȸ�� ����
            if (backMoveTimer >= holdDuration)
            {
                isRotatingBack = true;
            }
        }
        else
        {
            // �ڷ� �Է� �ƴϸ� �ʱ�ȭ
            backMoveTimer = 0f;
            isRotatingBack = false;
        }

        // ȸ�� ����
        if (isRotatingBack)
        {
            float playerY = player.eulerAngles.y + 180f;
            playerY %= 360f;

            // �ε巯�� ȸ��
            freeLookCamera.m_XAxis.Value = Mathf.LerpAngle(
                freeLookCamera.m_XAxis.Value,
                playerY,
                Time.deltaTime * rotateSpeed
            );
        }
    }
}
