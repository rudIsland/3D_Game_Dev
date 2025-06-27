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

        Debug.Log("현재 상태 저장");
    }

    //현재 플레이어의 상태를 저장
    public static void SaveGameData()
    {
        SaveData(Player.Instance.playerStateMachine.playerStat);
    }


    public static SavedData getSavedData()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("파일 없음! 초기 데이터 생성 후 저장");
            SavedData data = SavedData.CreateDefault(); //기본값으로 생성시킴
            string saveString = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, saveString);
        }

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
        SavedData data = SavedData.CreateDefault(); //초기화된 데이터 생성

        string SaveString = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, SaveString);

        Debug.Log("게임 초기화 후 저장");
    }

    public bool HasSavedData()
    {
        return File.Exists(SavePath);
    }


    //현재 저장된 스테이지정보와 플레이어 정보로 저장
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
