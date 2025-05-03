using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

[System.Serializable]
public class EnemyStats : CharacterStats
{
    [SerializeField] private double serializedMaxHP = 100;
    [SerializeField] private double serializedCurrentHP = 100;
    [SerializeField] private double serializedATK = 10;
    [SerializeField] private double serializedDEF = 5;

    public override double maxHP { get => serializedMaxHP; set => serializedMaxHP = value; }
    public override double currentHP { get => serializedCurrentHP; set => serializedCurrentHP = value; }
    public override double ATK { get => serializedATK; set => serializedATK = value; }
    public override double DEF { get => serializedDEF; set => serializedDEF = value; }
}
