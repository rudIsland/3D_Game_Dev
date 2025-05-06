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
        Debug.Log("�� ���� ����");

        // �⺻ ���� �ʱ�ȭ
        PlayerStats stats = new PlayerStats();
        SaveSystem.SaveData(stats); // ����

        // HUD �� �ε� (Additive)
        SceneManager.LoadScene("01_HUD", LoadSceneMode.Additive);

        // �ٷ� ���� ���������� �̵�
        StageManager.Instance.MoveToStage("001_Main");
    }

    public void StartScene_Continue()
    {
        Debug.Log("�̾��ϱ� ����");

        if (!SaveSystem.HasSavedData())
        {
            Debug.LogWarning("����� �����Ͱ� �����ϴ�.");
            return;
        }

        SavedData data = SaveSystem.getSavedData();

        // HUD �� �ε�
        SceneManager.LoadScene("01_HUD", LoadSceneMode.Additive);

        // HUD �� �ε� �Ϸ� �� �̾��ϱ� ����
        SceneManager.sceneLoaded += OnHUDLoadedThenContinue;

        // ���� Ŭ������ �ݹ� ����
        void OnHUDLoadedThenContinue(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "01_HUD") return;

            // ����
            SaveSystem.LoadAndContinue(data);

            Debug.Log("Game state loaded and continued.");

            // �� �̻� ȣ����� �ʰ� ����
            SceneManager.sceneLoaded -= OnHUDLoadedThenContinue;
        }
    }

    private void ContinueBtn_Active()
    {
        if (!SaveSystem.HasSavedData()) return;

        SavedData data = SaveSystem.getSavedData();

        if (data.StageName == "0_Start")
            ContinueBtn.interactable = false;
        else
            ContinueBtn.interactable = true;
    }

    public void StartSCene_Exit()
    {
        Debug.Log("���� ������");
    }

}
