using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HP_Bar : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            // 카메라 쪽을 바라보게 회전
            transform.rotation = Quaternion.LookRotation(Camera.main.transform.position - transform.position);
        }
    }

}
