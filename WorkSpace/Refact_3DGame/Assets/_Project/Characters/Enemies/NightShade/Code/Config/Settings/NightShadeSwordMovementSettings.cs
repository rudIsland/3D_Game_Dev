using System;
using UnityEngine;

namespace Characters.Enemies.NightShade
{
    [Serializable]
    internal sealed class NightShadeSwordMovementSettings
    {
        [SerializeField, Min(0.1f)] private float walkSpeed = 1.8f;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 3.8f;
        [SerializeField, Min(1f)] private float turnSpeed = 420f;
        [SerializeField, Min(1f)] private float attackTurnSpeed = 180f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float groundPull = -2f;

        internal float WalkSpeed => walkSpeed;
        internal float ChaseSpeed => chaseSpeed;
        internal float TurnSpeed => turnSpeed;
        internal float AttackTurnSpeed => attackTurnSpeed;
        internal float Gravity => gravity;
        internal float GroundPull => groundPull;

        internal NightShadeSwordMovementSettings()
        {
        }

        internal NightShadeSwordMovementSettings(
            float walkSpeed,
            float chaseSpeed,
            float turnSpeed,
            float attackTurnSpeed,
            float gravity,
            float groundPull)
        {
            this.walkSpeed = walkSpeed;
            this.chaseSpeed = chaseSpeed;
            this.turnSpeed = turnSpeed;
            this.attackTurnSpeed = attackTurnSpeed;
            this.gravity = gravity;
            this.groundPull = groundPull;
        }

        internal void Validate()
        {
            walkSpeed = Mathf.Max(0.1f, walkSpeed);
            chaseSpeed = Mathf.Max(0.1f, chaseSpeed);
            turnSpeed = Mathf.Max(1f, turnSpeed);
            attackTurnSpeed = Mathf.Max(1f, attackTurnSpeed);
        }
    }
}
