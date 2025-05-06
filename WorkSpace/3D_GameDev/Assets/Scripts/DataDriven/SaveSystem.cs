using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using System.Collections;



#if UNITY_EDITOR
using UnityEditor;
#endif

public static class SaveSystem
{
    private const string PATH = "Assets/Scripts/Resources/savedData.asset";
    private static SavedData pendingData;

    public static bool HasSavedData()
    {
        SavedData data = Resources.Load<SavedData>("savedData");
        return data != null && !string.IsNullOrEmpty(data.StageName);
    }

    public static void SaveData(PlayerStats stats)
    {
#if UNITY_EDITOR
        SavedData data;

        data = getSavedData();

        data.maxHP = stats.maxHP;
        data.currentHP = stats.currentHP;
        data.ATK = stats.ATK;
        data.DEF = stats.DEF;
        data.level = stats.level.currentLevel;
        data.currentExp = stats.level.currentExp;
        data.maxStamina = stats.maxStamina;
        data.currentStamina = stats.currentStamina;
        data.statPoint = stats.statPoint;
        data.StageName = StageManager.Instance.CurrentStageName;

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log("Saved current game state.");
#endif
    }

    public static void SaveData()
    {
#if UNITY_EDITOR
        SavedData data;

        data = getSavedData();

        PlayerStats stats = Player.Instance.playerStateMachine.playerStat;

        data.maxHP = stats.maxHP;
        data.currentHP = stats.currentHP;
        data.ATK = stats.ATK;
        data.DEF = stats.DEF;
        data.level = stats.level.currentLevel;
        data.currentExp = stats.level.currentExp;
        data.maxStamina = stats.maxStamina;
        data.currentStamina = stats.currentStamina;
        data.statPoint = stats.statPoint;
        data.StageName = StageManager.Instance.CurrentStageName;

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log("Saved current game state.");
#endif
    }

    public static SavedData getSavedData()
    {
        SavedData data = AssetDatabase.LoadAssetAtPath<SavedData>(PATH);
        if (data == null)
        {
            Debug.LogError("SavedData asset not found.");
            return data;
        }

        return data;
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

        Debug.Log("Game state loaded and continued.");

        // 이벤트 제거 & 데이터 초기화
        pendingData = null;
        SceneManager.sceneLoaded -= OnSceneLoadedAndApplyData;
    }
}
