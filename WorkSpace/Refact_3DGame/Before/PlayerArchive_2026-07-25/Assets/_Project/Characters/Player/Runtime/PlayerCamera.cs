using Cinemachine;
using UnityEngine;

public sealed class PlayerCamera : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler playerInput;
    [SerializeField] private CinemachineFreeLook freeLookCamera;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float horizontalSpeed = 2f;
    [SerializeField] private float verticalSpeed = 0.012f;
    [SerializeField] private bool lockCursor = true;

    private void Awake()
    {
        if (mainCamera != null)
        {
            mainCamera.transform.SetParent(null, true);
        }

        if (freeLookCamera != null)
        {
            freeLookCamera.transform.SetParent(null, true);
        }

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (mainCamera != null)
        {
            Destroy(mainCamera.gameObject);
        }

        if (freeLookCamera != null)
        {
            Destroy(freeLookCamera.gameObject);
        }
    }

    private void LateUpdate()
    {
        if (playerInput == null || freeLookCamera == null)
        {
            return;
        }

        Vector2 lookInput = playerInput.LookInput;
        freeLookCamera.m_XAxis.Value += lookInput.x * horizontalSpeed;
        freeLookCamera.m_YAxis.Value = Mathf.Clamp01(
            freeLookCamera.m_YAxis.Value - lookInput.y * verticalSpeed);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus || !lockCursor)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
