using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Action onLevelUp;

    public int EnemyCount = 0;
    public int currentEnemyCount = 0;

    public bool isPause=false;

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

    public void GameReset()
    {
        Debug.Log("게임 재시작");
        SceneManager.LoadScene("0_Start");

        // 기본 스탯 초기화
        SaveSystem.ResetData();
    }


    void OnEnable()
    {
        onLevelUp += LevelUpPauseGame;
    }

    void OnDisable()
    {
        onLevelUp -= LevelUpPauseGame;
    }

    public void LevelUpPauseGame()
    {
        isPause = true;
        Time.timeScale = 0f;
        UIManager.Instance.SetStageClearText();

        Debug.Log("게임 일시정지: 레벨업 발생");
    }

    public void ContinueGame()
    {
        isPause = false;
        Time.timeScale = 1f;
        Debug.Log("게임 계속하기");
    }

    void OnApplicationQuit()
    {
        SaveSystem.SaveGameData();
    }


    public void PlayderDead(float time)
    {

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UIManager.Instance.deadPanel.ActiveDeadPanel(true);

        UIManager.Instance.deadPanel.ShowDeadPanel(time);

    }


}
