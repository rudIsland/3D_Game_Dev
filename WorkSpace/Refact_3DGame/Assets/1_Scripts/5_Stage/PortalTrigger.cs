using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalTrigger : MonoBehaviour
{
    [Header("이동할 목적지 씬 이름")]
    public string targetSceneName;

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어와 충돌했는지 확인
        if (other.CompareTag("Player"))
        {
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                Debug.Log($"{targetSceneName}으로 이동을 요청합니다.");
                // StageManager에게 목적지를 전달하며 이동 요청
                StageManager.Instance.MoveToNextStage(targetSceneName);
            }
            else
            {
                Debug.LogWarning("포탈에 목적지 씬 이름이 설정되지 않았습니다!");
            }
        }
    }
}
