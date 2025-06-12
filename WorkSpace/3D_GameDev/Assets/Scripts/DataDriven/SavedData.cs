using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

[System.Serializable]
public class SavedData
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
    public string StageName;

    public SavedData( double maxHP,double currentHP, double ATK, double DEF,int level,
       float currentExp, float maxStamina,float currentStamina, int statPoint, string stageName)
    {
        this.maxHP = maxHP;
        this.currentHP = currentHP;
        this.ATK = ATK;
        this.DEF = DEF;
        this.level = level;
        this.currentExp = currentExp;
        this.maxStamina = maxStamina;
        this.currentStamina = currentStamina;
        this.statPoint = statPoint;
        this.StageName = stageName;
    }



    public static SavedData CreateDefault()
    {
        PlayerStats stats = new PlayerStats();
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
            "0_Start"
        );
    }

}
