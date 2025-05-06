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

    public static IEnumerator LoadAndContinue(SavedData data)
    {
        SceneManager.LoadScene("01_HUD", LoadSceneMode.Additive);
        StageManager.Instance.MoveToStage(data.StageName);

        // 1프레임 대기 (씬 내 오브젝트들이 Awake 실행되도록)
        while (Player.Instance == null || Player.Instance.playerStateMachine == null)
            yield return null;

        // 플레이어 정보는 HUD씬에 있으므로 스테이지 이동까지 확실히 하고 실행하도록 yield 사용
        PlayerStats stats = Player.Instance.playerStateMachine.playerStat;


        stats.maxHP = data.maxHP;
        stats.currentHP = data.currentHP;
        stats.ATK = data.ATK;
        stats.DEF = data.DEF;
        stats.level.currentLevel = data.level;
        stats.level.currentExp = data.currentExp;
        stats.maxStamina = data.maxStamina;
        stats.currentStamina = data.currentStamina;
        stats.statPoint = data.statPoint;

        // LoadAndContinue() 끝부분에 추가
        UIManager.Instance.playerResource.UpdateHPUI();
        UIManager.Instance.playerResource.UpdateStaminaUI();
        UIManager.Instance.levelStatSystem.UpdateExpSlider();
        UIManager.Instance.levelStatSystem.Update_StatUI();

        Debug.Log("Game state loaded and continued.");
    }
}
