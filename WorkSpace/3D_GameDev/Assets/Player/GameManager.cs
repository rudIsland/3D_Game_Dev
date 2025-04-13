using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PlayerStateMachine playerStateMachine;

    void Start()
    {
        var ui = FindObjectOfType<PlayerStatUIComponent>();
        var player = GameObject.FindWithTag("Player").GetComponent<PlayerStatComponent>();
        ui.Init(player);
    }

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);


        playerStateMachine = GameObject.Find("Player").GetComponent<PlayerStateMachine>();
    }


    public void GameReset()
    {
        Debug.Log("게임 재시작");
        //SceneManager.LoadScene("Sample");
    }

}
