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
        stats.ResetToDefault();

        SaveSystem.SaveData(); // ����

        // UIManager�� ���Ե� HUD �� ���� �ε�
        SceneManager.LoadSceneAsync("01_HUD", LoadSceneMode.Additive);
        SceneManager.LoadSceneAsync("001_Main");

    }

    public void StartScene_Continue()
    {
        ContinueBtn_Active();

        Debug.Log("�̾��ϱ� ����");
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
        Debug.Log("���� ������");
    }
}
