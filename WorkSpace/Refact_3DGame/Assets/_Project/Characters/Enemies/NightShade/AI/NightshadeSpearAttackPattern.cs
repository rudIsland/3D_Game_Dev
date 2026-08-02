using System;
using rudIsland.RPG3D.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    public enum NightshadeSpearAttackId
    {
        Attack01 = 1,
        Attack02,
        Attack03,
        Attack04,
        Attack05,
        Attack06,
        Attack07,
        Attack08,
        Attack09,
        Attack10,
        Attack11,
        Attack12,
        Attack13
    }
    public enum NightshadeSpearAttackGroup
    {
        Thrust,
        Sweep,
        Heavy,
        Approach,
        Retreat,
        Finisher
    }

    [Serializable]
    public sealed class NightshadeSpearAttackPattern
    {
        [SerializeField] private NightshadeSpearAttackId attackId =
            NightshadeSpearAttackId.Attack01;
        [SerializeField] private NightshadeSpearAttackGroup attackGroup =
            NightshadeSpearAttackGroup.Thrust;
        [SerializeField, Min(1)] private int phaseMask = 1;
        [SerializeField] private string displayName = "Lance Attack"; // 표시 이름
        [SerializeField] private string animatorStateName = "Attack 1"; // 애니메이터 상태 이름
        [SerializeField, Min(0f)] private float minimumDistance; // 거리 설정
        [SerializeField, Min(0.1f)] private float maximumDistance = 2.2f; // 거리 설정
        [SerializeField, Range(0f, 180f)] private float allowedAngle = 35f; // 각도 설정
        [SerializeField, Min(0f)] private float selectionWeight = 1f; // 내부에서 사용하는 값
        [SerializeField, Min(0f)] private float cooldown = 1.2f; // 시간 설정
        [SerializeField] private AttackDamage damage = new AttackDamage(15f); // 피격 또는 피해 관련 값
        [SerializeField] private HitStrength strength = HitStrength.Light;
        [SerializeField, Min(0f)] private float staggerDamage = 8f;
        [SerializeField, Min(0f)] private float pushDistance = 0.1f;
        [SerializeField, Range(0f, 1f)] private float hitStartTime = 0.25f; // 피격 또는 피해 관련 값
        [SerializeField, Range(0f, 1f)] private float hitEndTime = 0.55f; // 피격 또는 피해 관련 값
        [SerializeField, Min(0f)] private float transitionTime = 0.08f; // 시간 설정
        [SerializeField, Min(0.01f)] private float animationSpeed = 1f; // 이동 속도
        [SerializeField] private bool canChain = false;
        [SerializeField, Range(0f, 1f)] private float chainWeight = 0.35f;
        [SerializeField, Min(0f)] private float recoveryTime = 0.25f;
        [SerializeField] private bool canTurnDuringWindup = true;

        [NonSerialized] private int animatorStateId; // 애니메이터 참조
        [NonSerialized] private float nextReadyTime; // 시간 설정

        public NightshadeSpearAttackId AttackId => attackId;
        public NightshadeSpearAttackGroup AttackGroup => attackGroup;
        public int PhaseMask => phaseMask;
        public string DisplayName => displayName; // 표시 이름
        public int AnimatorStateId => animatorStateId; // 애니메이터 참조
        public float MinimumDistanceSquared => minimumDistance * minimumDistance; // 거리 설정
        public float MaximumDistanceSquared => maximumDistance * maximumDistance; // 거리 설정
        public float MinimumFacingDot => Mathf.Cos(allowedAngle * Mathf.Deg2Rad); // 외부에 제공하는 읽기 값
        public float SelectionWeight => selectionWeight; // 외부에 제공하는 읽기 값
        public float HitStartTime => hitStartTime; // 피격 또는 피해 관련 값
        public float HitEndTime => hitEndTime; // 피격 또는 피해 관련 값
        public float TransitionTime => transitionTime; // 시간 설정
        public float AnimationSpeed => animationSpeed; // 이동 속도
        public AttackDamage Damage => damage; // 피격 또는 피해 관련 값
        public HitStrength Strength => strength;
        public float StaggerDamage => staggerDamage;
        public float PushDistance => pushDistance;
        public bool CanChain => canChain;
        public float ChainWeight => chainWeight;
        public float RecoveryTime => recoveryTime;
        public bool CanTurnDuringWindup => canTurnDuringWindup;

        public void Prepare()
        {
            animatorStateId = string.IsNullOrWhiteSpace(animatorStateName)
                ? 0
                : Animator.StringToHash(animatorStateName);
            nextReadyTime = 0f;
        }

        public bool CanUse(float distanceSquared, float facingDot, float currentTime)
        {
            return CanUse(distanceSquared, facingDot, currentTime, 1);
        }

        public bool CanUse(
            float distanceSquared,
            float facingDot,
            float currentTime,
            int phase)
        {
            int phaseBit = 1 << Mathf.Clamp(phase - 1, 0, 30);
            return animatorStateId != 0 && damage.IsValid &&
                selectionWeight > 0f &&
                (phaseMask & phaseBit) != 0 &&
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
            phaseMask = Mathf.Max(1, phaseMask);
            minimumDistance = Mathf.Max(0f, minimumDistance);
            maximumDistance = Mathf.Max(0.1f, maximumDistance);
            minimumDistance = Mathf.Min(minimumDistance, maximumDistance);
            allowedAngle = Mathf.Clamp(allowedAngle, 0f, 180f);
            selectionWeight = Mathf.Max(0f, selectionWeight);
            cooldown = Mathf.Max(0f, cooldown);
            strength = strength >= HitStrength.Light &&
                strength <= HitStrength.Knockdown
                ? strength
                : HitStrength.Light;
            staggerDamage = Mathf.Max(0f, staggerDamage);
            pushDistance = Mathf.Max(0f, pushDistance);
            hitStartTime = Mathf.Clamp01(hitStartTime);
            hitEndTime = Mathf.Clamp(hitEndTime, hitStartTime, 1f);
            transitionTime = Mathf.Max(0f, transitionTime);
            animationSpeed = Mathf.Max(0.01f, animationSpeed);
            chainWeight = Mathf.Clamp01(chainWeight);
            recoveryTime = Mathf.Max(0f, recoveryTime);
        }
    }
}
