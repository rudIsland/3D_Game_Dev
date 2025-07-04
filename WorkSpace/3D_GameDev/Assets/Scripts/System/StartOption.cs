using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

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

        SaveSystem.SaveData(stats); // 저장

        // HUD 씬 로드 (Additive)
        SceneManager.LoadScene("01_HUD", LoadSceneMode.Additive);

        // 바로 메인 스테이지로 이동
        StageManager.Instance.MoveToStage("001_Main");
    }

    public void StartScene_Continue()
    {
        Debug.Log("이어하기 시작");

        SavedData data = SaveSystem.getSavedData();

        // HUD 씬 로드
        SceneManager.LoadScene("01_HUD", LoadSceneMode.Additive);

        // HUD 씬 로드 완료 후 이어하기 실행
        SceneManager.sceneLoaded += OnHUDLoadedThenContinue;

        // 내부 클래스용 콜백 저장
        void OnHUDLoadedThenContinue(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "01_HUD") return;

            // 실행
            SaveSystem.LoadAndContinue(data);

            Debug.Log("저장된 정보로 게임을 이어합니다.");

            // 더 이상 호출되지 않게 제거
            SceneManager.sceneLoaded -= OnHUDLoadedThenContinue;
        }
    }

    private void ContinueBtn_Active()
    {

        SavedData data = SaveSystem.getSavedData();

        if (data.StageName == "0_Start" || data.StageName == "001_Main" || !File.Exists(SaveSystem.SavePath))
            ContinueBtn.interactable = false;
        else
            ContinueBtn.interactable = true;
    }

    public void StartSCene_Exit()
    {
        Debug.Log("게임 나가기");
    }

}
