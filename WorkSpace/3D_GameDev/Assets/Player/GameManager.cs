using System;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PlayerStateMachine player;
    public PlayerResource Resource { get; set; }
    public LevelStatSystem levelStatSystem { get; set; }

    public Action onLevelUp;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

    }

    void Start()
    {
        // GetComponent
        player = GameObject.Find("Player").GetComponent<PlayerStateMachine>();
    }


    public void getPlayerExpKillEnemy(float exp)
    {
        player.playerStat.AddExp(exp);
    }

    public void GameReset()
    {
        Debug.Log("���� �����");
        //SceneManager.LoadScene("Sample");
    }


    void OnEnable()
    {
        onLevelUp += PauseGame;
    }

    void OnDisable()
    {
        onLevelUp -= PauseGame;
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        Debug.Log("���� �Ͻ�����: ������ �߻�");
    }

    public void ContinueGame()
    {
        Time.timeScale = 1f;
        Debug.Log("���� ����ϱ�");
    }

}
