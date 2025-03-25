using Cinemachine;
using UnityEngine;

public class FreeLookCameraSetting : MonoBehaviour
{
    public CinemachineFreeLook freeLookCamera;
    public Transform player;
    public PlayerInputReader inputReader;

    public float holdDuration = 0.5f; // 뒤로 입력 지속 시간 (초)
    public float rotateSpeed = 10f; // 회전 속도

    private float backMoveTimer = 0f;
    private bool isRotatingBack = false;

    void Update()
    {
        float moveY = inputReader.moveInput.y;

        // 뒤로 이동 중일 때 시간 누적
        if (moveY < -0.1f)
        {
            backMoveTimer += Time.deltaTime;

            // 일정 시간 지속되면 카메라 회전 시작
            if (backMoveTimer >= holdDuration)
            {
                isRotatingBack = true;
            }
        }
        else
        {
            // 뒤로 입력 아니면 초기화
            backMoveTimer = 0f;
            isRotatingBack = false;
        }

        // 회전 로직
        if (isRotatingBack)
        {
            float playerY = player.eulerAngles.y + 180f;
            playerY %= 360f;

            // 부드러운 회전
            freeLookCamera.m_XAxis.Value = Mathf.LerpAngle(
                freeLookCamera.m_XAxis.Value,
                playerY,
                Time.deltaTime * rotateSpeed
            );
        }
    }
}
