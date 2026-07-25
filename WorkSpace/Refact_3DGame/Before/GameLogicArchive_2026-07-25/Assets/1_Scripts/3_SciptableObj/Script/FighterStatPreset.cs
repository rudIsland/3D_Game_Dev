using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FighterStat", menuName = "Scriptable Object Asset/FighterStat")]
public class FighterStatPreset : EnemyStatPreset
{
    [Header("Fighter Stat")]
    //공격 범위
    public AttackData Jab;
    public AttackData LowKick;
    public AttackData Hook;
    public AttackData Kick;
    public AttackData JabCross;
    public AttackData FlyKick;

    // 탐지/전투 범위+속도
    public float DetectedRange;
    public float DetectedWalkSpeed;
    public float FightReadyRange;
    public float FightWalkSpeed;
}
