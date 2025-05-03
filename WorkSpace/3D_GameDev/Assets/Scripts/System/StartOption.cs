using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartOption : MonoBehaviour
{
    [SerializeField] Button ContinueBtn;

    private void Start()
    {
        ContinueBtn_Active();
    }

    public void StartScene_New()
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

    public void StartScene_Continue()
    {
        ContinueBtn_Active();

        Debug.Log("이어하기 시작");
        SaveSystem.LoadAndContinue();
    }

    private void ContinueBtn_Active()
    {
        if (!SaveSystem.HasSavedData()) return;

        SavedData data = Resources.Load<SavedData>("savedData");

        if (data.StageName == "0_Start")
            ContinueBtn.interactable = false;
        else
            ContinueBtn.interactable = true;
    }

    public void StartSCene_Exit()
    {
        Debug.Log("게임 나가기");
    }
}
