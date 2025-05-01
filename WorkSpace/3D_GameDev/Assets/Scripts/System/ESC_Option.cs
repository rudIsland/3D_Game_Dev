using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ESC_Option : MonoBehaviour
{
    bool isPause = false;

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
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
    }

    public void InGameContinue()
    {
        isPause = false;
        Time.timeScale = 1.0f;
    }

    public void InGameEXIT()
    {

    }

}
