using System;
using UnityEngine;
using UnityEngine.AI;
using Core;
using Enemy;

namespace Zombie
{
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(CharacterController),
        typeof(ZombieAnimationController))]
    [RequireComponent(typeof(NavMeshAgent))]
    // Unity 씬과 일반 C# Zombie AI를 연결한다.
    public sealed partial class ZombieController :
        EnemyView,
        IUnitDeathState,
        IEnemyDamageReceiver,
        IZoneEnemy
    {
        [Header("필수 연결")]
        private Transform target; // 대상 참조
        [SerializeField] private Animator zombieAnimator; // 애니메이터 참조

        private ZombieConfig config;
        [Header("공격 판정점")]
        [SerializeField] private ZombieAttackHitShape swingHitShape;
        [SerializeField] private ZombieAttackHitShape kickHitShape;
        [SerializeField] private ZombieAttackHitShape upDownHitShape;
        private ZombieSettings settings;

        private CharacterController characterController; // 씬 또는 시스템 참조
        private ZombieAnimationController zombieAnimation; // 씬 또는 시스템 참조
        private ZombieAttackRangeDetector attackRangeDetector;
        private ZombieWorldUnit zombieWorldUnit; // 씬 또는 시스템 참조
        private ICombatHitEffects hitEffectPlayer;
        private NavMeshAgent navMeshAgent;
        private ZombieStateMachine stateMachine;


        public bool IsDead =>
            zombieWorldUnit != null && zombieWorldUnit.IsDead;
        public EnemyZoneArea HomeZone => stateMachine?.HomeZone;
        /// <summary>생성 담당에게 설정과 추적 대상을 받는다.</summary>
        public override void SetData(ScriptableObject data, Transform target)
        {
            config = (ZombieConfig)data;
            this.target = target;
        }

        protected override Unit CreateRuntimeObject()
        {
            if (config == null)
                throw new InvalidOperationException("ZombieController에 ZombieConfig가 필요합니다.");
            settings = config.GetRuntimeSettings();
            FindUnityComponents();

            if (target == null ||
                zombieAnimator == null ||
                !HasValidAttackHitShapes())
            {
                throw new InvalidOperationException(
                    "ZombieController에 Target, Animator와 공격별 손·발 판정점이 필요합니다.");
            }


            zombieAnimation.ConnectAnimator(zombieAnimator);


            var pathGuide = new EnemyNavMeshPathGuide(
                transform,
                navMeshAgent);
            var movement = new ZombieMovement(
                transform,
                characterController,
                pathGuide,
                settings.Gravity,
                settings.GroundPull);
            var hitStop = new CombatHitStop(zombieAnimator);
            IUnitDeathState targetDeathState =
                target.GetComponentInParent<IUnitDeathState>();
            attackRangeDetector = new ZombieAttackRangeDetector(
                transform,
                settings.TargetLayers,
                swingHitShape,
                kickHitShape,
                upDownHitShape,
                hitStop,
                hitEffectPlayer);
            stateMachine = new ZombieStateMachine(
                target,
                targetDeathState,
                movement,
                zombieAnimation,
                settings,
                RequestDeadZombieRelease,
                EndAttackHit);
            var stopPoint = new StopPoint(settings.Life);

            zombieWorldUnit = new ZombieWorldUnit(
                settings.Life.MaxHealth,
                stateMachine,
                attackRangeDetector,
                stopPoint,
                hitStop,
                settings.HomeRecoveryDelay,
                settings.HealthRecoverySpeed,
                settings.Life.StaggerRecoverSpeed);
            return zombieWorldUnit;
        }

        public void SetHomeZone(
            EnemyZoneArea homeZone,
            Vector3 homePosition)
        {
            stateMachine?.SetHomeZone(homeZone, homePosition);
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


        private void FindUnityComponents()
        {
            characterController = GetComponent<CharacterController>();
            zombieAnimation = GetComponent<ZombieAnimationController>();
            hitEffectPlayer = GetComponent<ICombatHitEffects>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            ConfigureNavMeshAgent();

            if (zombieAnimator == null)
            {
                zombieAnimator = GetComponentInChildren<Animator>(true);
            }
        }

        private void ConfigureNavMeshAgent()
        {
            if (navMeshAgent == null)
            {
                return;
            }

            navMeshAgent.speed = settings.ChaseSpeed;
            navMeshAgent.angularSpeed = settings.TurnSpeed;
            navMeshAgent.autoTraverseOffMeshLink = false;
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
            navMeshAgent.updateUpAxis = false;
        }


        private AttackDamage GetAttackDamage(int attackNumber)
        {
            switch (attackNumber)
            {
                case 1:
                    return settings.SwingAttackDamage;
                case 2:
                    return settings.KickAttackDamage;
                case 3:
                    return settings.UpDownAttackDamage;
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
            SetHomeZone(null, default);
            zombieAnimation?.ResetAnimation();
        }

        public EnemyHitResult TakeHit(in EnemyHitRequest hitRequest)
        {
            return zombieWorldUnit != null
                ? zombieWorldUnit.TakeHit(in hitRequest)
                : EnemyHitResult.Ignored;
        }



#if UNITY_EDITOR
        private void OnValidate()
        {
            swingHitShape?.Validate();
            kickHitShape?.Validate();
            upDownHitShape?.Validate();
            ValidateCheatValues();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, config != null ? config.FindRange : 0f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, config != null ? config.AttackRange : 0f);
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
