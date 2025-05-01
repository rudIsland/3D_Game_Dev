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
    public PlayerResource Resource { get; set; }
    public LevelStatSystem levelStatSystem { get; set; }

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


    public void SaveGame(PlayerStats stats)
    {
        SaveSystem.SaveToAsset(stats);
    }

    //void OnApplicationQuit()
    //{
    //    SaveSystem.SaveToAsset(this);
    //}

    /***************************  StartScene Method   ***********************/

    public void NewBtn()
    {
        Debug.Log("새 게임 시작");

        //초기화된 스탯을 갖고
        //PlayerStats DefaultStats = new PlayerStats();
        //DefaultStats.maxHP = 100;
        //DefaultStats.currentHP = 100;
        //DefaultStats.ATK = 100;
        //DefaultStats.DEF = 5;
        //DefaultStats.level.currentLevel = 1;
        //DefaultStats.level.currentExp = 0;
        //DefaultStats.statPoint = 0;
        //DefaultStats.currentStamina = 100;

        //해당 스탯으로 Save시킨 후
        //SaveSystem.SaveToAsset(DefaultStats);

        //HUD씬을 불러온 후 씬을 이동한다.

        // 먼저 StageManager가 있는 씬을 로드
        SceneManager.LoadScene("01_HUD", LoadSceneMode.Additive); // 예시

        // 그 다음 HUD와 스테이지 씬을 순서대로
        StageManager.Instance.MoveToStage("1_Stage");
        //SceneManager.LoadScene("1_Stage", LoadSceneMode.Additive);
    }

    public void ContinueBtn()
    {
        Debug.Log("이어하기 시작");

        StartCoroutine(LoadContinueRoutine());
    }

    private IEnumerator LoadContinueRoutine()
    {
        // 1. PlayerScene 로드
        AsyncOperation playerSceneLoad = SceneManager.LoadSceneAsync("Sample", LoadSceneMode.Additive);
        yield return playerSceneLoad;

        // 2. PlayerStateMachine 찾기
        var player = GameObject.FindWithTag("Player")?.GetComponent<PlayerStateMachine>();
        if (player == null)
        {
            Debug.LogError("플레이어를 찾을 수 없습니다.");
            yield break;
        }

        // 3. 스탯 로드 및 저장된 스테이지 이름 확보
        SavedData data = SaveSystem.LoadFromAsset(player.playerStat);
        if (data == null)
        {
            Debug.LogError("세이브 데이터를 불러올 수 없습니다.");
            yield break;
        }

        // 4. HUD 씬 로드
        AsyncOperation hudLoad = SceneManager.LoadSceneAsync("01_HUD", LoadSceneMode.Additive);
        yield return hudLoad;

        // 5. 저장된 스테이지 씬 로드
        StageManager.Instance.MoveToStage(data.StageName);
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
