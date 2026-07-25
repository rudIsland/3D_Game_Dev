using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct HitInfo // 개별 타격 정보
{
    public string hitName;      // 구분용 이름 (예: Jab, Cross)
    public int weaponIndex;     // 사용할 무기 번호 (0: 왼손, 1: 오른손 등)
    public float damage;        // 이 타격의 데미지
}


[CreateAssetMenu(fileName = "AttackData", menuName = "Scriptable Object Asset/AttackData")]
public class AttackData : ScriptableObject
{
    public string animName;
    public float attackRange;
    public List<HitInfo> hitList = new List<HitInfo>();
}
