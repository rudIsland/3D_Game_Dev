using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSaveData", menuName = "Stats/Player Save Data")]
public class PlayerSavedData : ScriptableObject
{
    [Header("진행 상태 (빌드 인덱스)")]
    public int currentStage; // 현재 스테이지의 Build Index 저장
    public int killCount;

    [Header("플레이어 성장 데이터")]
    public int level;
    public int exp;
    public int nextLevelMaxExp;
    public int statPoints;

    [Header("실시간 스탯")]
    public float currentHP;
    public float maxHP;
    public float currentStamina;
    public float maxStamina;
    public float attack;
    public float defense;

    // 초기화 함수 (새 게임 시 호출용)
    public void ResetFromPreset(PlayerStatPreset preset)
    {
        if (preset == null) return;

        // 1. 기본 레벨 및 스테이지 정보
        level = 1;
        currentStage = 3; // 첫 인게임 스테이지 빌드 번호
        killCount = 0; // 초기화 시 0으로 설정
        exp = 0;
        statPoints = 0;

        // 2. 능력치 초기화 (중요: max를 먼저 세팅해야 current가 0이 안 됨)
        maxHP = preset.maxHP;
        currentHP = maxHP;

        maxStamina = preset.maxStamina;
        currentStamina = maxStamina;

        attack = preset.attack;
        defense = preset.defense;

        // 3. 경험치 통 세팅
        if (preset.levelTemplate != null)
            nextLevelMaxExp = preset.levelTemplate.GetRequiredExp(level);

        Debug.Log("<color=yellow>[DataManager]</color> 모든 스탯이 프리셋 데이터로 초기화되었습니다.");
    }
}
