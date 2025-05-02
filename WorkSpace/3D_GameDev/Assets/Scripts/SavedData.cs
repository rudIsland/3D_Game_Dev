using UnityEngine;

[CreateAssetMenu(fileName = "savedData", menuName = "Game Data/SavedData")]
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
    public string StageName;
}
