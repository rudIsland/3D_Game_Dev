using System;
using Characters.Combat;
using Characters.Combat.AttackData;
using Characters.Player.Lifecycle;
using World;
using UnityEngine;

namespace Characters.Enemies.NightShade
{
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(CharacterController),
        typeof(NightShadeSwordAnimationController),
        typeof(CombatHitEffectPlayer))]
    [RequireComponent(typeof(NightShadeSwordAttackAudio))]
    // Unity 프리팹과 일반 C# NightShade 양손검 전투를 연결한다.
    public sealed class NightShadeSwordController : WorldObjectView, IUnitDeathState, IEnemyDamageReceiver
    {
        [Header("필수 연결")]
        [SerializeField] private Transform target;
        [SerializeField] private Animator enemyAnimator;
        [SerializeField] private NightShadeSwordHitShape swordHitShape;
        [SerializeField] private NightShadeSwordConfig config;

        private CharacterController characterController;
        private NightShadeSwordAnimationController swordAnimation;
        private NightShadeSwordAttackRangeDetector attackRangeDetector;
        private NightShadeSwordWorldUnit swordWorldUnit;
        private CombatHitEffectPlayer hitEffectPlayer;
        private NightShadeSwordAttackAudio attackAudio;

        public bool IsDead => swordWorldUnit != null && swordWorldUnit.IsDead;
        internal bool IsAttackStateActive =>
            swordWorldUnit != null && swordWorldUnit.IsAttackStateActive;

        internal NightShadeSwordCombatDebug CombatDebug =>
            swordWorldUnit?.CombatDebug;

        protected override IWorldObject CreateRuntimeObject()
        {
            FindSceneReferences();
            FindUnityComponents();

            if (target == null ||
                enemyAnimator == null ||
                swordHitShape == null ||
                !swordHitShape.IsReady ||
                config == null)
            {
                throw new InvalidOperationException(
                    "NightShadeSwordController에 Target, Animator, RustySword 검날 판정점과 Config가 필요합니다.");
            }

            enemyAnimator.applyRootMotion = false;
            swordAnimation.ConnectAnimator(enemyAnimator);
            NightShadeSwordSettings settings = config.CreateRuntimeSettings();

            var movement = new NightShadeSwordMovement(
                transform,
                characterController,
                settings.Movement,
                settings.Recovery.MoveSpeed);
            var hitStop = new CombatHitStop(enemyAnimator);
            attackRangeDetector = new NightShadeSwordAttackRangeDetector(
                transform,
                settings.CombatRange.TargetLayers,
                swordHitShape,
                hitStop,
                hitEffectPlayer);
            IUnitDeathState targetDeathState =
                target.GetComponentInParent<IUnitDeathState>();
            var combatOutput = new NightShadeSwordCombatOutput(
                PlayAttackSound,
                OpenAttackHit,
                CloseAttackHit,
                RequestDespawn);
            var stateMachine = new NightShadeSwordStateMachine(
                target,
                targetDeathState,
                movement,
                swordAnimation,
                settings,
                combatOutput);
            var stopPoint = new StopPoint(
                settings.Life.StaggerLimit,
                settings.Life.StaggerRecoverDelay,
                settings.Life.StaggerRecoverSpeed);

            swordWorldUnit = new NightShadeSwordWorldUnit(
                settings.Life.MaxHealth,
                stateMachine,
                attackRangeDetector,
                stopPoint,
                hitStop);
            return swordWorldUnit;
        }

        public EnemyHitResult TakeHit(in EnemyHitRequest hitRequest)
        {
            return swordWorldUnit != null
                ? swordWorldUnit.TakeHit(in hitRequest)
                : EnemyHitResult.Ignored;
        }

        protected override void OnResetForPool()
        {
            CloseAttackHit();
            attackAudio?.Stop();
            swordAnimation?.ResetAnimation();
        }

        internal void StopAttackTurnAnimationEvent()
        {
            swordWorldUnit?.StopAttackTurnAnimationEvent();
        }

        internal void PlayAttackSoundAnimationEvent(int hitIndex)
        {
            swordWorldUnit?.PlayAttackSoundAnimationEvent(hitIndex);
        }

        internal void OpenAttackHitAnimationEvent(int hitIndex)
        {
            swordWorldUnit?.OpenAttackHitAnimationEvent(hitIndex);
        }

        internal void CloseAttackHitAnimationEvent()
        {
            swordWorldUnit?.CloseAttackHitAnimationEvent();
        }

        private void PlayAttackSound(NightShadeSwordAttackType attackType, int hitIndex)
        {
            attackAudio?.Play(attackType, hitIndex);
        }

        private void OpenAttackHit(AttackDamage attackDamage)
        {
            attackRangeDetector?.Open(attackDamage);
        }

        private void CloseAttackHit()
        {
            attackRangeDetector?.Close();
        }

        private void FindSceneReferences()
        {
            if (target != null)
            {
                return;
            }

            PlayerController player = FindFirstObjectByType<PlayerController>();
            target = player != null ? player.transform : null;
        }

        private void FindUnityComponents()
        {
            characterController = GetComponent<CharacterController>();
            swordAnimation = GetComponent<NightShadeSwordAnimationController>();
            hitEffectPlayer = GetComponent<CombatHitEffectPlayer>();
            attackAudio = GetComponent<NightShadeSwordAttackAudio>();
            if (enemyAnimator == null)
            {
                enemyAnimator = GetComponentInChildren<Animator>(true);
            }
        }

#if UNITY_EDITOR
        public void ConnectForEditor(
            Animator animator,
            Transform swordStartPoint,
            Transform swordEndPoint,
            float swordRadius,
            NightShadeSwordConfig runtimeConfig)
        {
            enemyAnimator = animator;
            config = runtimeConfig;
            swordHitShape ??= new NightShadeSwordHitShape();
            swordHitShape.ConnectForEditor(
                swordStartPoint,
                swordEndPoint,
                swordRadius);
        }

        private void OnValidate()
        {
            FindUnityComponents();
            swordHitShape?.Validate();
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null)
            {
                return;
            }

            NightShadeSwordSettings previewSettings =
                config.CreateRuntimeSettings();
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(
                transform.position,
                Mathf.Sqrt(previewSettings.CombatRange.FindRangeSquared));
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(
                transform.position,
                previewSettings.CombatRange.AttackRange);

            if (swordHitShape == null || !swordHitShape.IsReady)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(swordHitShape.StartPoint.position, swordHitShape.Radius);
            Gizmos.DrawWireSphere(swordHitShape.EndPoint.position, swordHitShape.Radius);
            Gizmos.DrawLine(swordHitShape.StartPoint.position, swordHitShape.EndPoint.position);
        }
#endif
    }
}
