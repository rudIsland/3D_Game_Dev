using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseSetting : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;   // Ŀ�� �Ⱥ��̰�

        Cursor.lockState = CursorLockMode.Confined; // ���� ȭ�� �� ����� ���
    }

}
