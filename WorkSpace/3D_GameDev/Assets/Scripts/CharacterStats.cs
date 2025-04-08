using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CharacterStats
{
    public double maxHP = 100; //HP
    public double currentHP = 100; //current HP
    public double attack = 10; //Attack
    public double dex = 5; //Dex

    public void TakeDamage(double amount)
    {
        currentHP -= amount;
        currentHP = Mathf.Max((float)currentHP, 0);
    }

    public bool IsDead => currentHP <= 0;
}
