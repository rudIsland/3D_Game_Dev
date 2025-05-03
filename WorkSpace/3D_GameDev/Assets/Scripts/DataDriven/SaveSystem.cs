using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class SaveSystem
{
    private const string PATH = "Assets/Scripts/Resources/savedData.asset";

    public static bool HasSavedData()
    {
        SavedData data = Resources.Load<SavedData>("savedData");
        return data != null && !string.IsNullOrEmpty(data.StageName);
    }
    public static void SaveData()
    {
#if UNITY_EDITOR
        SavedData data = AssetDatabase.LoadAssetAtPath<SavedData>(PATH);
        if (data == null)
        {
            Debug.LogError("SavedData asset not found.");
            return;
        }

        PlayerStats stats = new PlayerStats();

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

    public static void LoadAndContinue()
    {
        if (!HasSavedData())
        {
            Debug.LogWarning("저장된 데이터가 없습니다.");
            return;
        }

        SavedData data = Resources.Load<SavedData>("savedData");
        if (data == null)
        {
            Debug.LogError("SavedData.asset not found in Resources.");
            return;
        }

        PlayerStats stats = new PlayerStats();
        stats.maxHP = data.maxHP;
        stats.currentHP = data.currentHP;
        stats.ATK = data.ATK;
        stats.DEF = data.DEF;
        stats.level.currentLevel = data.level;
        stats.level.currentExp = data.currentExp;
        stats.maxStamina = data.maxStamina;
        stats.currentStamina = data.currentStamina;
        stats.statPoint = data.statPoint;

        // 스테이지 이동
        SceneManager.LoadScene("01_HUD", LoadSceneMode.Additive);
        StageManager.Instance.MoveToStage(data.StageName);

        Debug.Log("Game state loaded and continued.");
    }
}
