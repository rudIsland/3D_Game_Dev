using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object Asset/Level Template")]
public class LevelTemplate : ScriptableObject
{
    [Tooltip("각 레벨업에 필요한 경험치 테이블")]
    public float[] expArray;

    public float GetRequiredExp(int level)
    {
        if (expArray == null || expArray.Length == 0)
            return float.MaxValue;

        if (level - 1 < 0 || level - 1 >= expArray.Length)
            return float.MaxValue;

        return expArray[level - 1];
    }
}