using System;
using UnityEngine;

namespace Characters.Enemies.NightShade
{
    [Serializable]
    internal sealed class NightShadeSwordLifeSettings
    {
        [SerializeField, Min(1f)] private float maxHealth = 250f;
        [SerializeField, Min(1f)] private float staggerLimit = 100f;
        [SerializeField, Min(0f)] private float staggerRecoverDelay = 2.5f;
        [SerializeField, Min(0f)] private float staggerRecoverSpeed = 8f;
        [SerializeField, Min(0f)] private float deadBodyKeepTime = 3f;

        internal float MaxHealth => maxHealth;
        internal float StaggerLimit => staggerLimit;
        internal float StaggerRecoverDelay => staggerRecoverDelay;
        internal float StaggerRecoverSpeed => staggerRecoverSpeed;
        internal float DeadBodyKeepTime => deadBodyKeepTime;

        internal NightShadeSwordLifeSettings()
        {
        }

        internal NightShadeSwordLifeSettings(
            float maxHealth,
            float staggerLimit,
            float staggerRecoverDelay,
            float staggerRecoverSpeed,
            float deadBodyKeepTime)
        {
            this.maxHealth = maxHealth;
            this.staggerLimit = staggerLimit;
            this.staggerRecoverDelay = staggerRecoverDelay;
            this.staggerRecoverSpeed = staggerRecoverSpeed;
            this.deadBodyKeepTime = deadBodyKeepTime;
        }

        internal void Validate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            staggerLimit = Mathf.Max(1f, staggerLimit);
            staggerRecoverDelay = Mathf.Max(0f, staggerRecoverDelay);
            staggerRecoverSpeed = Mathf.Max(0f, staggerRecoverSpeed);
            deadBodyKeepTime = Mathf.Max(0f, deadBodyKeepTime);
        }
    }
}
