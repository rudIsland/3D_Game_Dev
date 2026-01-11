


[System.Serializable]
public class Level
{
    public int currentLevel = 1;
    public float currentExp = 0f;
    public float[] expArray;

    public bool CanLevelUp()
    {
        if (expArray == null || expArray.Length == 0) return false;
        if (currentLevel > expArray.Length) return false;

        return currentExp >= expArray[currentLevel - 1];
    }

    public void LevelUp()
    {
        currentExp -= expArray[currentLevel - 1];
        currentLevel++;
    }
}