using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }

}
