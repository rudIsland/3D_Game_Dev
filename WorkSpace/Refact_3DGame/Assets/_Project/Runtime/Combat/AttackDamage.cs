using System;
using UnityEngine;

namespace rudIsland.RPG3D.Combat
{
    [Serializable]
    // 한 번의 공격이 줄 체력 피해를 보관한다.
    public struct AttackDamage
    {
        [SerializeField] private float healthDamage;

        public float HealthDamage => IsValid ? healthDamage : 0f;
        public bool IsValid => IsAllowedDamage(healthDamage);

        public AttackDamage(float healthDamage)
        {
            this.healthDamage = IsAllowedDamage(healthDamage)
                ? healthDamage
                : 0f;
        }

        private static bool IsAllowedDamage(float damage)
        {
            return damage > 0f &&
                !float.IsNaN(damage) &&
                !float.IsInfinity(damage);
        }
    }
}
