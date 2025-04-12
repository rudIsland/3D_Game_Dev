using System.Collections;
using UnityEngine;

public abstract class CharacterBase : MonoBehaviour, IDamagable
{
    public virtual CharacterStatsComponent statComp => GetComponent<CharacterStatsComponent>();
    public CharacterStats stats => statComp.stats;

    public System.Action OnDeath;

    public virtual void CheckDie()
    {
        if (stats.currentHP <= 0)
        {
            OnDeath?.Invoke();
        }
    }

    public abstract void ApplyDamage(double damage);
}
