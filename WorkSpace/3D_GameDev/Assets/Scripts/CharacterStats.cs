using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CharacterStats
{
    public double maxHP = 100; //HP
    public double currentHP; //current HP
    public double attack = 10; //Attack
    public double dex = 5; //Dex

    public void TakeDamage(double damage)
    {
        currentHP -= damage;
        currentHP = Mathf.Max((float)currentHP, 0);
    }

    public bool IsDead => currentHP <= 0;
}
