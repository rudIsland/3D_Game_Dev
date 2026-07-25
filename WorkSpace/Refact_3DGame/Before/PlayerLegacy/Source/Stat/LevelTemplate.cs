using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object Asset/Level Template")]
public class LevelTemplate : ScriptableObject
{
    [Tooltip("각 레벨업에 필요한 경험치 테이블")]
    public int[] expArray;

    public int GetRequiredExp(int level)
    {
        // 1. 배열이 비어있는지 가장 먼저 체크
        if (expArray == null || expArray.Length == 0) return 999999;

        // 2. 레벨에 따른 인덱스 계산
        int index = level - 1;

        // [수정 핵심] 인덱스가 0보다 작은 경우 (레벨이 0 이하일 때)
        if (index < 0)
        {
            Debug.LogWarning($"[LevelTemplate] 레벨이 {level}입니다. 1레벨 데이터(Index 0)를 반환합니다.");
            return expArray[0];
        }

        // 3. 인덱스가 배열 크기를 넘어섰을 경우 (만렙 이후)
        if (index >= expArray.Length)
        {
            Debug.LogWarning($"[LevelTemplate] {level}레벨 데이터가 없습니다. 마지막 레벨 데이터를 반환합니다.");
            return expArray[expArray.Length - 1];
        }

        // 4. 정상 범위 내의 값 반환
        return expArray[index];
    }
}