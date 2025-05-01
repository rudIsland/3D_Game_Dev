using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavedData : ScriptableObject
{
    public double maxHP;
    public double currentHP;
    public double ATK;
    public double DEF;
    public int level;
    public float currentExp;

    public float maxStamina;
    public float currentStamina;

    public int statPoint;
    public string StageName; // StageNumµµ ¿˙¿Â

    public SavedData(PlayerStats stats, string StageName)
    {
        maxHP = stats.maxHP;
        currentHP = stats.currentHP;
        ATK = stats.ATK;
        DEF = stats.DEF;
        level = stats.level.currentLevel;
        currentExp = stats.level.currentExp;
        maxStamina = stats.maxStamina;
        currentStamina = stats.currentStamina;
        this.StageName = StageName;
    }

}
