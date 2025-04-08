using UnityEngine;

[System.Serializable]
public class CharacterStats
{
    public double MaxHP = 100; //HP
    public double CurrentHP = 100; //current HP
    public double Attack = 10; //Attack
    public double Dex = 5; //Dex

    public void TakeDamage(double amount)
    {
        CurrentHP -= amount;
        CurrentHP = Mathf.Max((float)CurrentHP, 0);
    }

    public bool IsDead => CurrentHP <= 0;
}
