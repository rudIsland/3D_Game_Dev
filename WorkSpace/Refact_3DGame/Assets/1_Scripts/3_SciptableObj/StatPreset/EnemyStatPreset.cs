using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStat", menuName = "Scriptable Object Asset/EnemyStat")]
public class EnemyStatPreset : CharacterStatPreset
{
    public float DetectRange = 15f;     //탐지범위
    public float AttackRange = 1.0f;    //공격범위
    public float MoveSpeed = 2.3f;      //이동속도
    public float AngularSpeed = 180f;   //회전속도

    public int DeathEXP = 1300;         //경험치
}
