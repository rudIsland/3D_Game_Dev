
using UnityEngine;

[CreateAssetMenu(fileName = "ZombieStat", menuName = "Scriptable Object Asset/ZombitStat")]
public class ZombieStatPreset : EnemyStatPreset
{
    [Header("Zombie Stat")]
    public AttackData SwingAttack;
}
