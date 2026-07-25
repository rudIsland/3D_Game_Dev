using System.IO;
using UnityEngine;

public static class SaveSystem
{
    // 저장 경로 설정
    private static string SavePath => Path.Combine(Application.persistentDataPath, "player_save.json");

    // [추가] 저장 파일이 존재하는지 확인하는 함수 (LobbyUI에서 사용)
    public static bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }

    // [추가] 저장 파일을 물리적으로 삭제하는 함수
    public static void DeleteSaveFile()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("<color=red>[SaveSystem]</color> 기존 저장 파일이 삭제되었습니다.");
        }
    }

    // [추가] 저장 파일 삭제 및 데이터 장부 초기화
    public static void Reset(PlayerSavedData data, PlayerStatPreset preset)
    {
        // 1. 물리적인 세이브 파일 삭제
        DeleteSaveFile();

        // 2. 전달받은 데이터 장부(SO)를 프리셋 값으로 리셋
        if (data != null && preset != null)
        {
            data.ResetFromPreset(preset);
            Debug.Log("<color=green>[SaveSystem]</color> 데이터 장부가 프리셋 값으로 리셋되었습니다.");
        }

        // 3. 리셋된 깨끗한 상태를 즉시 저장 (필요 시)
        Save(data); 
    }

    public static void Save(PlayerSavedData data)
    {
        SaveData wrapper = new SaveData
        {
            currentStage = data.currentStage,
            killCount = data.killCount,
            level = data.level,
            exp = data.exp,
            nextLevelMaxExp = data.nextLevelMaxExp,
            statPoints = data.statPoints,
            currentHP = data.currentHP,
            maxHP = data.maxHP,
            attack = data.attack,
            defense = data.defense
        };

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"<color=blue>[저장 완료]</color> 경로: {SavePath}");
    }

    public static bool Load(PlayerSavedData data)
    {
        if (!File.Exists(SavePath)) return false;

        string json = File.ReadAllText(SavePath);
        SaveData wrapper = JsonUtility.FromJson<SaveData>(json);

        data.currentStage = wrapper.currentStage;
        data.killCount = wrapper.killCount;
        data.level = wrapper.level;
        data.exp = wrapper.exp;
        data.nextLevelMaxExp = wrapper.nextLevelMaxExp;
        data.statPoints = wrapper.statPoints;
        data.currentHP = wrapper.currentHP;
        data.maxHP = wrapper.maxHP;
        data.attack = wrapper.attack;
        data.defense = wrapper.defense;

        return true;
    }
}