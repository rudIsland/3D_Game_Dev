using System;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Characters.Combat.AttackData;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Player;
using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(CharacterController),
        typeof(ZombieAnimationController),
        typeof(CombatHitEffectPlayer))]
    // Unity 씬과 일반 C# Zombie AI를 연결한다.
    public sealed class ZombieController :
        WorldObjectView,
        IUnitDeathState,
        IEnemyDamageReceiver
    {
        [Header("필수 연결")]
        [SerializeField] private Transform target; // 대상 참조
        [SerializeField] private Animator zombieAnimator; // 애니메이터 참조

        [Header("생명")]
        [SerializeField, Min(1f)] private float maxHealth = 100f; // 최대 체력

        [Header("경직")]
        [SerializeField, Min(1f)] private float staggerLimit = 50f;
        [SerializeField, Min(0f)] private float staggerRecoverDelay = 3f;
        [SerializeField, Min(0f)] private float staggerRecoverSpeed = 5f;

        [Header("사망 후 정리")]
        [SerializeField, Min(0f)] private float deadBodyKeepTime = 2f; // 시간 설정

#if UNITY_EDITOR
        [Header("체력 확인")]
        [SerializeField, Min(0f)] private float testDamage = 10f; // 피격 또는 피해 관련 값
        [SerializeField, Min(0f)] private float testStaggerDamage = 50f;
#endif

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
        [SerializeField] private ZombieAttackHitShape swingHitShape;
        [SerializeField] private ZombieAttackHitShape kickHitShape;
        [SerializeField] private ZombieAttackHitShape upDownHitShape;
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

        private CharacterController characterController; // 씬 또는 시스템 참조
        private ZombieAnimationController zombieAnimation; // 씬 또는 시스템 참조
        private ZombieAttackRangeDetector attackRangeDetector;
        private ZombieWorldUnit zombieWorldUnit; // 씬 또는 시스템 참조
        private CombatHitEffectPlayer hitEffectPlayer;


        public bool IsDead =>
            zombieWorldUnit != null && zombieWorldUnit.IsDead;
        protected override IWorldObject CreateRuntimeObject()
        {
            FindSceneReferences();
            FindUnityComponents();

            if (target == null ||
                zombieAnimator == null ||
                !HasValidAttackHitShapes())
            {
                throw new InvalidOperationException(
                    "ZombieController에 Target, Animator와 공격별 손·발 판정점이 필요합니다.");
            }


            zombieAnimation.ConnectAnimator(zombieAnimator);


            var movement = new ZombieMovement(
                transform,
                characterController,
                gravity,
                groundPull);
            var hitStop = new CombatHitStop(zombieAnimator);
            IUnitDeathState targetDeathState =
                target.GetComponentInParent<IUnitDeathState>();
            attackRangeDetector = new ZombieAttackRangeDetector(
                transform,
                targetLayers,
                swingHitShape,
                kickHitShape,
                upDownHitShape,
                hitStop,
                hitEffectPlayer);
            var stateMachine = new ZombieStateMachine(
                target,
                targetDeathState,
                movement,
                zombieAnimation,
                findRange,
                idleTargetCheckInterval,
                attackRange,
                attackFacingAngle,
                chaseSpeed,
                turnSpeed,
                hitPushDuration,
                knockbackPushDuration,
                hitPushCurve,
                deadBodyKeepTime,
                RequestDeadZombieRelease,
                EndAttackHit);
            var stopPoint = new StopPoint(
                staggerLimit,
                staggerRecoverDelay,
                staggerRecoverSpeed);

            zombieWorldUnit = new ZombieWorldUnit(
                maxHealth,
                stateMachine,
                attackRangeDetector,
                stopPoint,
                hitStop);
            return zombieWorldUnit;
        }

        private void RequestDeadZombieRelease()
        {
            RequestDespawn();
        }
        public void StartAttackHit(int attackNumber)
        {
            if (zombieWorldUnit?.BeginAttackHit() != true)
            {
                return;
            }

            attackRangeDetector?.Open(attackNumber, GetAttackDamage(attackNumber));

        }
        public void EndAttackHitAnimationEvent()
        {
            if (zombieWorldUnit?.BeginAttackRecovery() == true)
            {
                EndAttackHit();
            }
        }

        public void EndAttackHit()
        {
            attackRangeDetector?.Close();

        }

        internal void NotifyAttackAnimationEnded()
        {
            zombieWorldUnit?.BeginAttackRecovery();
            EndAttackHit();
            zombieWorldUnit?.NotifyAttackAnimationEnded();
        }

        internal void NotifyAlertAnimationEnded()
        {
            zombieWorldUnit?.NotifyAlertAnimationEnded();
        }

#if UNITY_EDITOR
        [ContextMenu("Test Damage")]
        private void TestDamage()
        {
            if (!Application.isPlaying || zombieWorldUnit == null)
            {
                Debug.LogWarning("Test Damage는 Play 중이고 좀비 준비가 끝난 뒤 사용할 수 있습니다.", this);
                return;
            }

            float healthBeforeDamage = zombieWorldUnit.CurrentHealth;

            var hitRequest = new EnemyHitRequest(
                testDamage,
                testStaggerDamage,
                transform.position,
                -transform.forward,
                0.25f);
            zombieWorldUnit.TakeHit(in hitRequest);

            Debug.Log($"좀비 체력: {healthBeforeDamage} → {zombieWorldUnit.CurrentHealth}", this);
        }
#endif

        private void FindSceneReferences()
        {
            if (target != null)
            {
                return;
            }

            PlayerController player =
                FindFirstObjectByType<PlayerController>();
            target = player != null ? player.transform : null;
        }

        private void FindUnityComponents()
        {
            characterController = GetComponent<CharacterController>();
            zombieAnimation = GetComponent<ZombieAnimationController>();
            hitEffectPlayer = GetComponent<CombatHitEffectPlayer>();

            if (zombieAnimator == null)
            {
                zombieAnimator = GetComponentInChildren<Animator>(true);
            }
        }


        private AttackDamage GetAttackDamage(int attackNumber)
        {
            switch (attackNumber)
            {
                case 1:
                    return swingAttackDamage;
                case 2:
                    return kickAttackDamage;
                case 3:
                    return upDownAttackDamage;
                default:
                    return null;
            }
        }


        private bool HasValidAttackHitShapes()
        {
            return swingHitShape != null &&
                swingHitShape.IsReady &&
                kickHitShape != null &&
                kickHitShape.IsReady &&
                upDownHitShape != null &&
                upDownHitShape.IsReady;
        }

        protected override void OnResetForPool()
        {
            EndAttackHit();
            zombieAnimation?.ResetAnimation();
        }

        public EnemyHitResult TakeHit(in EnemyHitRequest hitRequest)
        {
            return zombieWorldUnit != null
                ? zombieWorldUnit.TakeHit(in hitRequest)
                : EnemyHitResult.Ignored;
        }

        private static AnimationCurve CreateDefaultHitPushCurve()
        {
            return new AnimationCurve(new Keyframe(0f, 0f, 2f, 2f), new Keyframe(1f, 1f, 0f, 0f));
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            FindUnityComponents();
            staggerLimit = Mathf.Max(1f, staggerLimit);
            staggerRecoverDelay = Mathf.Max(0f, staggerRecoverDelay);
            staggerRecoverSpeed = Mathf.Max(0f, staggerRecoverSpeed);
            testStaggerDamage = Mathf.Max(0f, testStaggerDamage);
            findRange = Mathf.Max(0.1f, findRange);
            idleTargetCheckInterval =
                Mathf.Max(0.01f, idleTargetCheckInterval);
            attackRange = Mathf.Clamp(attackRange, 0.1f, findRange);
            attackFacingAngle = Mathf.Clamp(attackFacingAngle, 0f, 180f);
            swingHitShape?.Validate();
            kickHitShape?.Validate();
            upDownHitShape?.Validate();
            hitPushDuration = Mathf.Max(0.01f, hitPushDuration);
            knockbackPushDuration =
                Mathf.Max(hitPushDuration, knockbackPushDuration);
            if (hitPushCurve == null || hitPushCurve.length < 2)
            {
                hitPushCurve = CreateDefaultHitPushCurve();
            }

            deadBodyKeepTime = Mathf.Max(0f, deadBodyKeepTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, findRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            DrawAttackHitShape(swingHitShape, Color.magenta);
            DrawAttackHitShape(kickHitShape, Color.yellow);
            DrawAttackHitShape(upDownHitShape, Color.cyan);
        }

        private static void DrawAttackHitShape(ZombieAttackHitShape hitShape, Color color)
        {
            if (hitShape == null || !hitShape.IsReady)
            {
                return;
            }

            Gizmos.color = color;
            Gizmos.DrawWireSphere(hitShape.StartPoint.position, hitShape.Radius);
            Gizmos.DrawWireSphere(hitShape.EndPoint.position, hitShape.Radius);
            Gizmos.DrawLine(hitShape.StartPoint.position, hitShape.EndPoint.position);
        }
#endif
    }
}
