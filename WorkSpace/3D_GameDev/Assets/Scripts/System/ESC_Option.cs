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
            if (GameManager.Instance.isPause)
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
    }

    public void InGameContinue()
    {
        ESCPanel.SetActive(false);
        GameManager.Instance.isPause = false;
        Time.timeScale = 1.0f;
    }

    public void InGameEXIT()
    {
        ESCPanel.SetActive(false);
    }

}
