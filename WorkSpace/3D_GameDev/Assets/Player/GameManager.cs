using System;
using System.Collections;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    //public PlayerStateMachine player;
    //public PlayerResource Resource { get; set; }
    //public LevelStatSystem levelStatSystem { get; set; }

    public GameObject ESCPanel;

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

        ESCPanel.SetActive(false);
    }

    void Start()
    {
        // GetComponent
        //player = GameObject.Find("Player").GetComponent<PlayerStateMachine>();

        //SaveSystem.LoadFromAsset(player.playerStat);
        //if (!LoadFromScriptableObejct("PlayerData")) return;
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

    //public void getPlayerExpKillEnemy(float exp)
    //{
    //    player.playerStat.AddExp(exp);
    //    UpdateEnemyCount();
    //}

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

    //void OnApplicationQuit()
    //{
    //    SaveSystem.SaveToAsset(this);
    //}

    /***************************  StartScene Method   ***********************/

    public void NewBtn()
    {
        Debug.Log("새 게임 시작");

        // 기본 스탯 초기화
        PlayerStats stats = new PlayerStats();
        stats.ResetToDefault();

        SaveSystem.SaveData(); // 저장

        // UIManager가 포함된 HUD 씬 먼저 로드
        SceneManager.LoadSceneAsync("01_HUD", LoadSceneMode.Additive);
        SceneManager.LoadSceneAsync("001_Main");

    }

    public void ContinueBtn()
    {
        Debug.Log("이어하기 시작");
        SaveSystem.LoadAndContinue();
    }

    public void ExitBtn()
    {
        Debug.Log("게임 나가기");
    }

    


    /*************************   ESC  ***********************/
    bool isPause = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPause)
            {
                InGameContinue();
            }
            else
            {
                GamePause();
            }
        }
    }

    public void GamePause()
    {
        isPause = true;
        Time.timeScale = 0f;
        ESCPanel.SetActive(true);
    }

    public void InGameContinue()
    {
        ESCPanel.SetActive(false);
        isPause = false;
        Time.timeScale = 1.0f;
    }

    public void InGameEXIT()
    {
        ESCPanel.SetActive(false);
    }
}
