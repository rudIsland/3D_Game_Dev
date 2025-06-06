using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;


public static class SaveSystem
{
    private const string FileName = "JsonSavedData.json";
    public static readonly string SavePath = Path.Combine(Application.persistentDataPath, FileName);

    private static SavedData pendingData;

    public static void SaveData(PlayerStats stats)
    {

        SavedData data = new SavedData
        {
            maxHP = stats.maxHP,
            currentHP = stats.currentHP,
            ATK = stats.ATK,
            DEF = stats.DEF,
            level = stats.level.currentLevel,
            currentExp = stats.level.currentExp,
            maxStamina = stats.maxStamina,
            currentStamina = stats.currentStamina,
            statPoint = stats.statPoint,
            StageName = StageManager.Instance.CurrentStageName
        };

        string SaveString = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, SaveString);

        Debug.Log("���� ���� ����");
    }

    public static void SaveGameData()
    {
        PlayerStats stats = Player.Instance.playerStateMachine.playerStat;
        SavedData data = new SavedData
        {
            maxHP = stats.maxHP,
            currentHP = stats.currentHP,
            ATK = stats.ATK,
            DEF = stats.DEF,
            level = stats.level.currentLevel,
            currentExp = stats.level.currentExp,
            maxStamina = stats.maxStamina,
            currentStamina = stats.currentStamina,
            statPoint = stats.statPoint,
            StageName = StageManager.Instance.CurrentStageName
        };

        string SaveString = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, SaveString);

        Debug.Log("���� ���� ����");
    }

    public static SavedData getSavedData()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("���� ����!");

            SaveGameData();
        }

        // ����� ���̺� ���� �ҷ�����
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
        SavedData data = new SavedData
        {
            maxHP = stats.maxHP,
            currentHP = stats.currentHP,
            ATK = stats.ATK,
            DEF = stats.DEF,
            level = stats.level.currentLevel,
            currentExp = stats.level.currentExp,
            maxStamina = stats.maxStamina,
            currentStamina = stats.currentStamina,
            statPoint = stats.statPoint,
            StageName = "0_Start"
        };

        string SaveString = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, SaveString);

        Debug.Log("���� ���� ����");
    }
}
