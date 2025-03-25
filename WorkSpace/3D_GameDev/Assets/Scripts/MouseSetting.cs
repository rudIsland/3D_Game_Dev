using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseSetting : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;   // 커서 안보이게

        Cursor.lockState = CursorLockMode.Confined; // 게임 화면 못 벗어나게 잠금
    }

}
