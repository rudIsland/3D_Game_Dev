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
    }

    public void LevelUp()
    {
        if (currentLevel < MaxLevel)
        {
            currentExp -= MaxExp;
            currentLevel++;
        }
    }

    public void GainExpFromMonster(int baseExp, int monsterLevel, int playerLevel)
    {
        float multiplier = Mathf.Clamp(1f + (monsterLevel - playerLevel) * 0.1f, 0.5f, 2f);
        AddExp(baseExp * multiplier);
    }

    public bool TryLevelUp()
    {
        if (IsLevelUp)
        {
            LevelUp();
            return true;
        }
        return false;
    }

    public void SetLevel(int level)
    {
        currentLevel = level;
    }
}
