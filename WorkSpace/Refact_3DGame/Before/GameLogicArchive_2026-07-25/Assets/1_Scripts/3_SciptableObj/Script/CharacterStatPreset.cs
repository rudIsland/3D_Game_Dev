using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Object Asset/Character Base Stats")]
public class CharacterStatPreset : ScriptableObject
{
    [Header("Character Default Stat")]
    public float maxHP = 100;
    public float attack = 10;
    public float defense = 5;
}