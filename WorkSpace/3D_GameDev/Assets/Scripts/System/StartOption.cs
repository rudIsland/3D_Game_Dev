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

        // UIManager�� ���Ե� HUD �� ���� �ε�
        SceneManager.LoadSceneAsync("01_HUD", LoadSceneMode.Additive);
        StageManager.Instance.MoveToStage("001_Main");
    }

    public void StartScene_Continue()
    {
        ContinueBtn_Active();

        Debug.Log("�̾��ϱ� ����");

        if (!SaveSystem.HasSavedData())
        {
            Debug.LogWarning("����� �����Ͱ� �����ϴ�.");
            return;
        }
        SavedData data = SaveSystem.getSavedData();

        StartCoroutine(SaveSystem.LoadAndContinue(data));
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
