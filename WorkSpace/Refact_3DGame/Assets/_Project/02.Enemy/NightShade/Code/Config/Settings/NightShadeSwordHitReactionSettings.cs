using System;
using UnityEngine;

namespace Characters.Enemies.NightShade
{
    [Serializable]
    internal sealed class NightShadeSwordHitReactionSettings
    {
        [SerializeField, Min(0.01f)] private float pushDuration = 0.18f;
        [SerializeField, Min(0.01f)] private float knockbackPushDuration = 0.28f;
        [SerializeField, Min(0.01f)] private float knockdownPushDuration = 0.38f;
        [SerializeField, Min(0f)] private float knockdownStayDuration = 0.75f;
        [SerializeField, Min(0f)] private float staggerBreakStayDuration = 1.25f;
        [SerializeField] private AnimationCurve pushCurve = CreateDefaultPushCurve();

        internal float PushDuration => pushDuration;
        internal float KnockbackPushDuration => knockbackPushDuration;
        internal float KnockdownPushDuration => knockdownPushDuration;
        internal float KnockdownStayDuration => knockdownStayDuration;
        internal float StaggerBreakStayDuration => staggerBreakStayDuration;
        internal AnimationCurve PushCurve => pushCurve;

        internal NightShadeSwordHitReactionSettings()
        {
        }

        internal NightShadeSwordHitReactionSettings(
            float pushDuration,
            float knockbackPushDuration,
            float knockdownPushDuration,
            float knockdownStayDuration,
            float staggerBreakStayDuration,
            AnimationCurve pushCurve)
        {
            this.pushDuration = pushDuration;
            this.knockbackPushDuration = knockbackPushDuration;
            this.knockdownPushDuration = knockdownPushDuration;
            this.knockdownStayDuration = knockdownStayDuration;
            this.staggerBreakStayDuration = staggerBreakStayDuration;
            this.pushCurve = pushCurve;
        }

        internal void Validate()
        {
            pushDuration = Mathf.Max(0.01f, pushDuration);
            knockbackPushDuration = Mathf.Max(
                pushDuration,
                knockbackPushDuration);
            knockdownPushDuration = Mathf.Max(
                knockbackPushDuration,
                knockdownPushDuration);
            knockdownStayDuration = Mathf.Max(0f, knockdownStayDuration);
            staggerBreakStayDuration = Mathf.Max(
                0f,
                staggerBreakStayDuration);
            if (pushCurve == null || pushCurve.length < 2)
            {
                pushCurve = CreateDefaultPushCurve();
            }
        }

        private static AnimationCurve CreateDefaultPushCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 2f, 2f),
                new Keyframe(1f, 1f, 0f, 0f));
        }
    }
}
