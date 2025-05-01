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
        Debug.Log("�� ���� ����");

        //�ʱ�ȭ�� ������ ����
        //PlayerStats DefaultStats = new PlayerStats();
        //DefaultStats.maxHP = 100;
        //DefaultStats.currentHP = 100;
        //DefaultStats.ATK = 100;
        //DefaultStats.DEF = 5;
        //DefaultStats.level.currentLevel = 1;
        //DefaultStats.level.currentExp = 0;
        //DefaultStats.statPoint = 0;
        //DefaultStats.currentStamina = 100;

        //�ش� �������� Save��Ų ��
        //SaveSystem.SaveToAsset(DefaultStats);

        //HUD���� �ҷ��� �� ���� �̵��Ѵ�.

        // ���� StageManager�� �ִ� ���� �ε�
        SceneManager.LoadScene("01_HUD", LoadSceneMode.Additive); // ����

        // �� ���� HUD�� �������� ���� �������
        StageManager.Instance.MoveToStage("1_Stage");
        //SceneManager.LoadScene("1_Stage", LoadSceneMode.Additive);
    }

    public void ContinueBtn()
    {
        Debug.Log("�̾��ϱ� ����");

        StartCoroutine(LoadContinueRoutine());
    }

    private IEnumerator LoadContinueRoutine()
    {
        // 1. PlayerScene �ε�
        AsyncOperation playerSceneLoad = SceneManager.LoadSceneAsync("Sample", LoadSceneMode.Additive);
        yield return playerSceneLoad;

        // 2. PlayerStateMachine ã��
        var player = GameObject.FindWithTag("Player")?.GetComponent<PlayerStateMachine>();
        if (player == null)
        {
            Debug.LogError("�÷��̾ ã�� �� �����ϴ�.");
            yield break;
        }

        // 3. ���� �ε� �� ����� �������� �̸� Ȯ��
        SavedData data = SaveSystem.LoadFromAsset(player.playerStat);
        if (data == null)
        {
            Debug.LogError("���̺� �����͸� �ҷ��� �� �����ϴ�.");
            yield break;
        }

        // 4. HUD �� �ε�
        AsyncOperation hudLoad = SceneManager.LoadSceneAsync("01_HUD", LoadSceneMode.Additive);
        yield return hudLoad;

        // 5. ����� �������� �� �ε�
        StageManager.Instance.MoveToStage(data.StageName);
    }

    public void ExitBtn()
    {
        Debug.Log("���� ������");
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
