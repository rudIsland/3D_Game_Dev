using System;
using rudIsland.RPG3D.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.MummyWarrior
{
    [Serializable]
    public sealed class MummyWarriorAttackPattern
    {
        [SerializeField] private string displayName = "Lance Attack";
        [SerializeField] private string animatorStateName = "Attack 1";
        [SerializeField, Min(0f)] private float minimumDistance;
        [SerializeField, Min(0.1f)] private float maximumDistance = 2.2f;
        [SerializeField, Range(0f, 180f)] private float allowedAngle = 35f;
        [SerializeField, Min(0f)] private float selectionWeight = 1f;
        [SerializeField, Min(0f)] private float cooldown = 1.2f;
        [SerializeField] private AttackDamage damage = new AttackDamage(15f);
        [SerializeField, Range(0f, 1f)] private float hitStartTime = 0.25f;
        [SerializeField, Range(0f, 1f)] private float hitEndTime = 0.55f;
        [SerializeField, Min(0f)] private float transitionTime = 0.08f;
        [SerializeField, Min(0.01f)] private float animationSpeed = 1f;

        [NonSerialized] private int animatorStateId;
        [NonSerialized] private float nextReadyTime;

        public string DisplayName => displayName;
        public int AnimatorStateId => animatorStateId;
        public float MinimumDistanceSquared => minimumDistance * minimumDistance;
        public float MaximumDistanceSquared => maximumDistance * maximumDistance;
        public float MinimumFacingDot => Mathf.Cos(allowedAngle * Mathf.Deg2Rad);
        public float SelectionWeight => selectionWeight;
        public float HitStartTime => hitStartTime;
        public float HitEndTime => hitEndTime;
        public float TransitionTime => transitionTime;
        public float AnimationSpeed => animationSpeed;
        public AttackDamage Damage => damage;

        public void Prepare()
        {
            animatorStateId = string.IsNullOrWhiteSpace(animatorStateName)
                ? 0
                : Animator.StringToHash(animatorStateName);
            nextReadyTime = 0f;
        }

        public bool CanUse(float distanceSquared, float facingDot, float currentTime)
        {
            return animatorStateId != 0 && damage.IsValid &&
                selectionWeight > 0f &&
                distanceSquared >= MinimumDistanceSquared &&
                distanceSquared <= MaximumDistanceSquared &&
                facingDot >= MinimumFacingDot && currentTime >= nextReadyTime;
        }

        public void StartCooldown(float currentTime)
        {
            nextReadyTime = currentTime + cooldown;
        }

        public void ClampValues()
        {
            minimumDistance = Mathf.Max(0f, minimumDistance);
            maximumDistance = Mathf.Max(0.1f, maximumDistance);
            minimumDistance = Mathf.Min(minimumDistance, maximumDistance);
            allowedAngle = Mathf.Clamp(allowedAngle, 0f, 180f);
            selectionWeight = Mathf.Max(0f, selectionWeight);
            cooldown = Mathf.Max(0f, cooldown);
            hitStartTime = Mathf.Clamp01(hitStartTime);
            hitEndTime = Mathf.Clamp(hitEndTime, hitStartTime, 1f);
            transitionTime = Mathf.Max(0f, transitionTime);
            animationSpeed = Mathf.Max(0.01f, animationSpeed);
        }
    }
}
