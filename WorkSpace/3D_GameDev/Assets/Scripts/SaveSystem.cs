using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class SaveSystem
{
    private const string PATH = "Assets/Resources/savedData.asset";

    public static void SaveToAsset(PlayerStats stats)
    {
#if UNITY_EDITOR
        string assetPath = $"Assets/Resources/savedData.asset";
        SavedData data = AssetDatabase.LoadAssetAtPath<SavedData>(assetPath);
        if (data == null)
        {
            Debug.LogError("SavedData asset not found.");
            return;
        }

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

        EditorUtility.SetDirty(data); // 변경사항 저장
        AssetDatabase.SaveAssets();
        Debug.Log("Saved to ScriptableObject.");
#endif
    }

    public static SavedData LoadFromAsset(PlayerStats stats)
    {
        SavedData data = Resources.Load<SavedData>(PATH);
        if (data == null)
        {
            Debug.LogError("SavedData.asset not found in Resources.");
            return null;
        }

        stats.maxHP = data.maxHP;
        stats.currentHP = data.currentHP;
        stats.ATK = data.ATK;
        stats.DEF = data.DEF;
        stats.level.currentLevel = data.level;
        stats.level.currentExp = data.currentExp;
        stats.maxStamina = data.maxStamina;
        stats.currentStamina = data.currentStamina;
        stats.statPoint = data.statPoint;
        StageManager.Instance.CurrentStageName = data.StageName;

        Debug.Log("Loaded from ScriptableObject.");

        return data;
    }
}
