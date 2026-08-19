using System;
using rudIsland.RPG3D.Characters.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Combat.AttackData
{
    public enum DamageSoundType
    {
        BodyImpact = 0,
        SwordCut = 1
    }

    [Serializable]
    public sealed class AttackDamage
    {
        [SerializeField, Min(0f)]
        private float healthDamage = 15f;
        [SerializeField]
        private AttackStrength strength;
        [SerializeField, Min(0f)]
        private float staggerDamage;
        [SerializeField, Min(0f)]
        private float pushDistance;
        [SerializeField, Min(0f)]
        private float hitStopDuration =
            CombatHitStop.DefaultDamageDuration;
        [SerializeField, Min(0f)]
        private float guardStaminaDamage = 25f;
        [SerializeField]
        private bool canBlock = true;
        [SerializeField]
        private DamageSoundType damageSoundType =
            DamageSoundType.BodyImpact;

        public AttackDamage()
        {
        }

        public AttackDamage(
            float healthDamage,
            AttackStrength strength,
            float staggerDamage,
            float pushDistance,
            float guardStaminaDamage,
            bool canBlock,
            float hitStopDuration =
                CombatHitStop.DefaultDamageDuration,
            DamageSoundType damageSoundType =
                DamageSoundType.BodyImpact)
        {
            this.healthDamage = Mathf.Max(0f, healthDamage);
            this.strength = strength;
            this.staggerDamage = Mathf.Max(0f, staggerDamage);
            this.pushDistance = Mathf.Max(0f, pushDistance);
            this.hitStopDuration = Mathf.Max(0f, hitStopDuration);
            this.guardStaminaDamage = Mathf.Max(0f, guardStaminaDamage);
            this.canBlock = canBlock;
            this.damageSoundType = damageSoundType;
        }

        public float HealthDamage => healthDamage;
        public AttackStrength Strength => strength;
        public float StaggerDamage => staggerDamage;
        public float PushDistance => pushDistance;
        public float HitStopDuration => hitStopDuration;
        public float GuardStaminaDamage => guardStaminaDamage;
        public bool CanBlock => canBlock;
        public DamageSoundType DamageSoundType => damageSoundType;
    }
}
