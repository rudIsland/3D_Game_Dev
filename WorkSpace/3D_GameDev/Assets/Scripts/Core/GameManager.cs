using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour, IGamePauseHandler
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
        Debug.Log("���� �����");
        SceneManager.LoadScene("0_Start");

        // �⺻ ���� �ʱ�ȭ
        SaveSystem.ResetData();
    }


    void OnEnable()
    {
        onLevelUp += LevelUpTrigger;
    }

    void OnDisable()
    {
        onLevelUp -= LevelUpTrigger;
    }

    public void LevelUpTrigger()
    {
        isPause = true;
        Time.timeScale = 0f;
        UIManager.Instance.SetStageClearText();
        SaveSystem.SaveGameData();
        Debug.Log("���� �Ͻ�����: ������ �߻�");
    }

    public void ResumeGame() //���� ����
    {
        isPause = false;
        Time.timeScale = 1f;
        Debug.Log("���� ����ϱ�");
    }

    void OnApplicationQuit()
    {
       //�׳� ���� ����
    }

    public void PauseGame()
    {
        //���� ����
    }
}
