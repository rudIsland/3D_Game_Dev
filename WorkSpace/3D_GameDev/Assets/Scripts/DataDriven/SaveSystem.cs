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

        Debug.Log("현재 상태 저장");
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

        Debug.Log("현재 상태 저장");
    }

    public static SavedData getSavedData()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("파일 없음!");

            SaveGameData();
        }

        // 저장된 세이브 파일 불러오기
        string json = File.ReadAllText(SavePath);
        Debug.Log("저장된 파일 로드됨");
        return JsonUtility.FromJson<SavedData>(json);
    }

    public static void LoadAndContinue(SavedData data)
    {
        pendingData = data;

        // 스테이지 이동
        StageManager.Instance.MoveToStage(data.StageName);

        // 씬 로드 완료 후 실행
        SceneManager.sceneLoaded += OnSceneLoadedAndApplyData;
    }

    private static void OnSceneLoadedAndApplyData(Scene scene, LoadSceneMode mode)
    {
        if (pendingData == null) return;

        var stats = Player.Instance.playerStateMachine.playerStat;
        stats.ApplySavedData(pendingData);

        UIManager.Instance.levelStatSystem.Update_StatUI();

        Debug.Log("현재상태로 스테이지 이동");

        // 이벤트 제거 & 데이터 초기화
        pendingData = null;
        SceneManager.sceneLoaded -= OnSceneLoadedAndApplyData;
    }

    //초기화
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

        Debug.Log("현재 상태 저장");
    }
}
