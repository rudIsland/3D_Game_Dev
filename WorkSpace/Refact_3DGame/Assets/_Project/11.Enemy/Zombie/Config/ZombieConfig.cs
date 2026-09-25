using UnityEngine;
using Core;

namespace Zombie
{
    [CreateAssetMenu(fileName = "ZombieConfig", menuName = "Characters/Enemies/Zombie Config")]
    public sealed class ZombieConfig : ScriptableObject
    {
        [Header("생명")]
        [SerializeField, Min(1f)] private float maxHealth = 100f; // 최대 체력

        [Header("경직")]
        [SerializeField, Min(1f)] private float staggerLimit = 50f;
        [SerializeField, Min(0f)] private float staggerRecoverDelay = 3f;
        [SerializeField, Min(0f)] private float staggerRecoverSpeed = 5f;

        [Header("사망 후 정리")]
        [SerializeField, Min(0f)] private float deadBodyKeepTime = 2f; // 시간 설정

        [Header("Zone 복귀와 회복")]
        [SerializeField, Min(0f)] private float zoneTrackingMargin = 1f;
        [SerializeField, Min(0.01f)] private float homeArrivalDistance = 0.5f;
        [SerializeField, Min(0f)] private float homeRecoveryDelay = 3f;
        [SerializeField, Min(0f)] private float healthRecoverySpeed = 10f;


        [Header("찾기와 공격 거리")]
        [SerializeField, Min(0.1f)] private float findRange = 30f; // 거리 설정
        [SerializeField, Min(0.01f)]
        private float idleTargetCheckInterval = 0.1f; // 대상 참조
        [SerializeField, Min(0.1f)] private float attackRange = 1.8f; // 공격 관련 설정 또는 상태
        [SerializeField, Range(0f, 180f)]
        private float attackFacingAngle = 10f; // 공격 관련 설정 또는 상태

        [Header("공격 판정")]
        [SerializeField] private LayerMask targetLayers =
            1 << 17;
        [SerializeField] private AttackDamage swingAttackDamage =
            new AttackDamage(10f, AttackStrength.Light, 10f, 0.3f, 25f, true, 0.04f);
        [SerializeField] private AttackDamage kickAttackDamage =
            new AttackDamage(10f, AttackStrength.Heavy, 10f, 0.3f, 25f, true, 0.05f);
        [SerializeField] private AttackDamage upDownAttackDamage =
            new AttackDamage(10f, AttackStrength.Heavy, 10f, 0.3f, 25f, true, 0.06f);


        [Header("이동")]
        [SerializeField, Min(0.1f)] private float chaseSpeed = 3.5f; // 이동 속도
        [SerializeField, Min(1f)] private float turnSpeed = 360f; // 이동 속도
        [SerializeField] private float gravity = -22f; // Inspector 설정 값
        [SerializeField] private float groundPull = -2f; // Inspector 설정 값

        [Header("피격 이동")]
        [SerializeField, Min(0.01f)]
        private float hitPushDuration = 0.15f;
        [SerializeField, Min(0.01f)]
        private float knockbackPushDuration = 0.25f;
        [SerializeField]
        private AnimationCurve hitPushCurve = CreateDefaultHitPushCurve();

        internal float MaxHealth => maxHealth;
        internal float StaggerLimit => staggerLimit;
        internal float StaggerRecoverDelay => staggerRecoverDelay;
        internal float StaggerRecoverSpeed => staggerRecoverSpeed;
        internal float DeadBodyKeepTime => deadBodyKeepTime;
        internal float ZoneTrackingMargin => zoneTrackingMargin;
        internal float HomeArrivalDistance => homeArrivalDistance;
        internal float HomeRecoveryDelay => homeRecoveryDelay;
        internal float HealthRecoverySpeed => healthRecoverySpeed;
        internal float FindRange => findRange;
        internal float IdleTargetCheckInterval => idleTargetCheckInterval;
        internal float AttackRange => attackRange;
        internal float AttackFacingAngle => attackFacingAngle;
        internal LayerMask TargetLayers => targetLayers;
        internal AttackDamage SwingAttackDamage => swingAttackDamage;
        internal AttackDamage KickAttackDamage => kickAttackDamage;
        internal AttackDamage UpDownAttackDamage => upDownAttackDamage;
        internal float ChaseSpeed => chaseSpeed;
        internal float TurnSpeed => turnSpeed;
        internal float Gravity => gravity;
        internal float GroundPull => groundPull;
        internal float HitPushDuration => hitPushDuration;
        internal float KnockbackPushDuration => knockbackPushDuration;
        internal AnimationCurve HitPushCurve => hitPushCurve;

        private ZombieSettings runtimeSettings;
        private int settingsSession = -1;
        // 제한된 전역 값: Play 재시작 시 설정 캐시를 새로 만드는 번호이며 게임 진행 상태가 아니다.
        private static int playSession;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void BeginPlaySession()
        {
            unchecked { playSession++; }
        }

        internal ZombieSettings GetRuntimeSettings()
        {
            if (!Application.isPlaying)
            {
                ValidateSettings();
                return new ZombieSettings(this);
            }

            if (runtimeSettings == null || settingsSession != playSession)
            {
                ValidateSettings();
                runtimeSettings = new ZombieSettings(this);
                settingsSession = playSession;
            }
            return runtimeSettings;
        }

        private void OnValidate()
        {
            ValidateSettings();
            runtimeSettings = null;
        }

        private void ValidateSettings()
        {
            staggerLimit = Mathf.Max(1f, staggerLimit);
            staggerRecoverDelay = Mathf.Max(0f, staggerRecoverDelay);
            staggerRecoverSpeed = Mathf.Max(0f, staggerRecoverSpeed);
            findRange = Mathf.Max(0.1f, findRange);
            idleTargetCheckInterval =
                Mathf.Max(0.01f, idleTargetCheckInterval);
            attackRange = Mathf.Clamp(attackRange, 0.1f, findRange);
            attackFacingAngle = Mathf.Clamp(attackFacingAngle, 0f, 180f);
            hitPushDuration = Mathf.Max(0.01f, hitPushDuration);
            knockbackPushDuration =
                Mathf.Max(hitPushDuration, knockbackPushDuration);
            if (hitPushCurve == null || hitPushCurve.length < 2)
            {
                hitPushCurve = CreateDefaultHitPushCurve();
            }

            deadBodyKeepTime = Mathf.Max(0f, deadBodyKeepTime);
            zoneTrackingMargin = Mathf.Max(0f, zoneTrackingMargin);
            homeArrivalDistance = Mathf.Max(0.01f, homeArrivalDistance);
            homeRecoveryDelay = Mathf.Max(0f, homeRecoveryDelay);
            healthRecoverySpeed = Mathf.Max(0f, healthRecoverySpeed);
        }

        private static AnimationCurve CreateDefaultHitPushCurve()
        {
            return new AnimationCurve(new Keyframe(0f, 0f, 2f, 2f), new Keyframe(1f, 1f, 0f, 0f));
        }
    }
}
