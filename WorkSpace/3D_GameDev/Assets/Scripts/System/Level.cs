using UnityEngine;

[System.Serializable]
public class Level
{
    public const int MaxLevel = 50;
    public float[] expArray = new float[MaxLevel];
    public int currentLevel  = 1;
    public float currentExp  = 0f;

    public Level()
    {
        for (int i = 0; i < MaxLevel; i++)
            expArray[i] = Mathf.Round(100f * Mathf.Pow(i + 1, 1.5f));
    }

    public float MaxExp => expArray[currentLevel - 1];
    public bool IsLevelUp => currentExp >= MaxExp;

    public void AddExp(float exp)
    {
        currentExp += exp;
        Debug.Log($"현재 경험치량: {currentExp} / {MaxExp}");

        while (currentLevel < MaxLevel && currentExp >= expArray[currentLevel - 1])
        {
            currentExp -= expArray[currentLevel - 1]; // 현재 레벨의 필요 경험치만큼 빼고
            currentLevel++; // 레벨업
        }

        currentExp = Mathf.Max(0, currentExp); // 혹시 음수 방지
    }

    public void LevelUp_EXP()
    {
        if (currentLevel < MaxLevel)
        {
            currentExp -= MaxExp;
            currentLevel++;
        }
    }

    public void SetLevel(int level)
    {
        currentLevel = level;
    }
}
