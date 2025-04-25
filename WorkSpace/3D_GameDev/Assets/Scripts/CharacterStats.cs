using TMPro;

using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public abstract class CharacterStats
{
    public virtual double maxHP { get; set; } = 100;
    public virtual double currentHP { get; set; } = 100;
    public virtual double ATK { get; set; } = 10;
    public virtual double DEF { get; set; } = 5;

    public bool IsDead => currentHP <= 0;
}
