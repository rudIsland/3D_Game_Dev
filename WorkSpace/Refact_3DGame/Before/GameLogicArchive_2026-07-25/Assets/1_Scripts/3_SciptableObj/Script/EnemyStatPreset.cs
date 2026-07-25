using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStat", menuName = "Scriptable Object Asset/EnemyStat")]
public class EnemyStatPreset : CharacterStatPreset
{
    [Header("Enemy Default Stat")]
    public float _DetectRange = 15f;     //탐지범위
    public float _AttackRange = 1.0f;    //공격범위
    public float _MoveSpeed = 2.3f;      //이동속도
    public float _AngularSpeed = 180f;   //회전속도


    [Header("Level Scaling (성장 계수)")]
    public int baseDeathEXP = 1300;     // 1레벨 기본 경험치
    public int expGrowthPerLevel = 150; // 레벨당 추가 경험치

    public float hpGrowthPerLevel = 25f;  // 레벨당 추가 체력
    public float atkGrowthPerLevel = 3f;  // 레벨당 추가 공격력
    public float defGrowthPerLevel = 1.5f;// 레벨당 추가 방어력
}
