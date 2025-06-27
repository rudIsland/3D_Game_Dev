using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;


public class SaveSystem
{
    private const string FileName = "JsonSavedData.json";
    public static readonly string SavePath = Path.Combine(Application.persistentDataPath, FileName);

    private static SavedData pendingData;

    public static void SaveData(PlayerStats stats)
    {
        SavedData data = CreateSaveData(stats);
        string saveString = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, saveString);

        Debug.Log("���� ���� ����");
    }

    //���� �÷��̾��� ���¸� ����
    public static void SaveGameData()
    {
        SaveData(Player.Instance.playerStateMachine.playerStat);
    }


    public static SavedData getSavedData()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("���� ����! �ʱ� ������ ���� �� ����");
            SavedData data = SavedData.CreateDefault(); //�⺻������ ������Ŵ
            string saveString = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, saveString);
        }

        string json = File.ReadAllText(SavePath);
        Debug.Log("����� ���� �ε��");
        return JsonUtility.FromJson<SavedData>(json);
    }

    public static void LoadAndContinue(SavedData data)
    {
        pendingData = data;

        // �������� �̵�
        StageManager.Instance.MoveToStage(data.StageName);

        // �� �ε� �Ϸ� �� ����
        SceneManager.sceneLoaded += OnSceneLoadedAndApplyData;
    }

    private static void OnSceneLoadedAndApplyData(Scene scene, LoadSceneMode mode)
    {
        if (pendingData == null) return;

        var stats = Player.Instance.playerStateMachine.playerStat;
        stats.ApplySavedData(pendingData);

        UIManager.Instance.levelStatSystem.Update_StatUI();

        Debug.Log("������·� �������� �̵�");

        // �̺�Ʈ ���� & ������ �ʱ�ȭ
        pendingData = null;
        SceneManager.sceneLoaded -= OnSceneLoadedAndApplyData;
    }

    //�ʱ�ȭ
    public static void ResetData()
    {
        PlayerStats stats = new PlayerStats();
        SavedData data = SavedData.CreateDefault(); //�ʱ�ȭ�� ������ ����

        string SaveString = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, SaveString);

        Debug.Log("���� �ʱ�ȭ �� ����");
    }

    public bool HasSavedData()
    {
        return File.Exists(SavePath);
    }


    //���� ����� �������������� �÷��̾� ������ ����
    private static SavedData CreateSaveData(PlayerStats stats)
    {
        return new SavedData(
           stats.maxHP,
           stats.currentHP,
           stats.ATK,
           stats.DEF,
           stats.level.currentLevel,
           stats.level.currentExp,
           stats.maxStamina,
           stats.currentStamina,
           stats.statPoint,
           StageManager.Instance.CurrentStageName
       );
    }
}
