using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    // 외부에서 참조할 상태 머신
    public PlayerStateMachine playerStateMachine;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 상태 머신 컴포넌트 자동 할당
        if (playerStateMachine == null)
        {
            playerStateMachine = GetComponentInChildren<PlayerStateMachine>();
        }
    }
}
