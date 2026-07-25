using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MutantStat", menuName = "Scriptable Object Asset/MutantStat")]
public class MutantStatPreset : EnemyStatPreset
{

    [Header("Mutant Stat")]
    public AttackData SwingAttack;
    public AttackData PunchAttack;
    public AttackData JumpAttack;

    public float _JumpAttackRange;
    public float _SwingAttackRange;
    public float _PunchAttackRange;
    public float _WalkRange;
    public float _WalkSpeed;
    public float _RunRange ;
    public float _RunSpeed;

}
