using System;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PlayerStateMachine player;
    public PlayerResource Resource { get; set; }
    public LevelStatSystem levelStatSystem { get; set; }

    public Action onLevelUp;

    public TextMeshProUGUI enemyCountTxt;
    public int EnemyCount = 0;
    public int currentEnemyCount = 0;

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

        ResetEnemyCount();
    }

    public void ResetEnemyCount()
    {
        currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Count();
        EnemyCount = currentEnemyCount;
        enemyCountTxt.text = $"{currentEnemyCount} / {EnemyCount} ";
    }

    public void UpdateEnemyCount()
    {
        enemyCountTxt.text = $"{currentEnemyCount -= 1} / {EnemyCount} ";
    }

    public void getPlayerExpKillEnemy(float exp)
    {
        player.playerStat.AddExp(exp);
        UpdateEnemyCount();
    }

    public void GameReset()
    {
        Debug.Log("게임 재시작");
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
        Debug.Log("게임 일시정지: 레벨업 발생");
    }

    public void ContinueGame()
    {
        Time.timeScale = 1f;
        Debug.Log("게임 계속하기");
    }

}
