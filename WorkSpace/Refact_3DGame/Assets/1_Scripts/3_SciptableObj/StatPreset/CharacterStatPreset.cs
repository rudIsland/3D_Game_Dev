using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Object Asset/Character Base Stats")]
public class CharacterStatPreset : ScriptableObject
{
    public double maxHP = 100;
    public double attack = 10;
    public double defense = 5;
}