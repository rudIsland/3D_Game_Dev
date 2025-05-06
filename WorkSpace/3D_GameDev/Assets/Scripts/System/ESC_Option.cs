using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ESC_Option : MonoBehaviour
{
    public GameObject ESCPanel;

    private void Awake()
    {
        ESCPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance.isPause && ESCPanel.activeSelf)
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
        GameManager.Instance.isPause = true;
        Time.timeScale = 0f;
        ESCPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void InGameContinue()
    {
        ESCPanel.SetActive(false);
        if (UIManager.Instance.levelStatSystem.levelUpPanel.activeSelf) return;
        GameManager.Instance.isPause = false;
        Time.timeScale = 1.0f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void InGameEXIT()
    {
        ESCPanel.SetActive(false);
        Time.timeScale = 1.0f;
        PlayerStats savedStats = Player.Instance.playerStateMachine.playerStat;
        SaveSystem.SaveData(savedStats);
        SceneManager.LoadScene("0_Start");

        // Player ÆÄ±«
        if (Player.Instance != null)
        {
            Destroy(Player.Instance.gameObject);
        }

        // UIManager ÆÄ±«
        if (UIManager.Instance != null)
        {
            Destroy(UIManager.Instance.gameObject);
        }
    }

}
