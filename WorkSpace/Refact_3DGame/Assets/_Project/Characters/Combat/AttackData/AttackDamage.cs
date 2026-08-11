using System;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Combat.AttackData
{
    [Serializable]
    public sealed class AttackDamage
    {
        [SerializeField, Min(0f)]
        private float healthDamage = 15f;
        [SerializeField, Min(0)]
        private int strength;
        [SerializeField, Min(0f)]
        private float staggerDamage;
        [SerializeField, Min(0f)]
        private float pushDistance;
        [SerializeField]
        private bool canBeBlocked = true;

        public AttackDamage()
        {
        }

        public AttackDamage(
            float healthDamage,
            int strength,
            float staggerDamage,
            float pushDistance,
            bool canBeBlocked)
        {
            this.healthDamage = Mathf.Max(0f, healthDamage);
            this.strength = Mathf.Max(0, strength);
            this.staggerDamage = Mathf.Max(0f, staggerDamage);
            this.pushDistance = Mathf.Max(0f, pushDistance);
            this.canBeBlocked = canBeBlocked;
        }

        public float HealthDamage => healthDamage;
        public int Strength => strength;
        public float StaggerDamage => staggerDamage;
        public float PushDistance => pushDistance;
        public bool CanBeBlocked => canBeBlocked;
    }
}
