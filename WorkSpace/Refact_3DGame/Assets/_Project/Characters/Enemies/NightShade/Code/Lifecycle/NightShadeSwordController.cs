using System;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Characters.Combat.AttackData;
using rudIsland.RPG3D.Player;
using rudIsland.RPG3D.World;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
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

        [Header("생명")]
        [SerializeField, Min(1f)] private float maxHealth = 250f;

        [Header("경직")]
        [SerializeField, Min(1f)] private float staggerLimit = 100f;
        [SerializeField, Min(0f)] private float staggerRecoverDelay = 2.5f;
        [SerializeField, Min(0f)] private float staggerRecoverSpeed = 8f;

        [Header("사망 후 정리")]
        [SerializeField, Min(0f)] private float deadBodyKeepTime = 3f;

        [Header("찾기와 공격 거리")]
        [SerializeField, Min(0.1f)] private float findRange = 24f;
        [SerializeField, Min(0.1f)] private float attackRange = 2.4f;
        [SerializeField, Min(0.1f)] private float walkStartRange = 5f;
        [SerializeField, Min(0.1f)] private float runStartRange = 6f;
        [SerializeField, Range(0f, 180f)]
        private float attackFacingAngle = 14f;

        [Header("RustySword 공격 판정")]
        [SerializeField] private LayerMask targetLayers = 1 << 17;
        [SerializeField] private NightShadeSwordHitShape swordHitShape;
        [SerializeField] private AttackDamage lightAttackDamage =
            new AttackDamage(18f, 1, 18f, 0.4f, 30f, true, 0.06f, DamageSoundType.SwordCut);
        [SerializeField] private AttackDamage comboFirstAttackDamage =
            new AttackDamage(12f, 1, 12f, 0.25f, 20f, true, 0.045f, DamageSoundType.SwordCut);
        [SerializeField] private AttackDamage comboSecondAttackDamage =
            new AttackDamage(16f, 1, 18f, 0.4f, 25f, true, 0.06f, DamageSoundType.SwordCut);
        [SerializeField] private AttackDamage heavyAttackDamage =
            new AttackDamage(28f, 2, 35f, 0.75f, 45f, true, 0.08f, DamageSoundType.SwordCut);
        [SerializeField] private AttackDamage wideSwingAttackDamage =
            new AttackDamage(22f, 1, 24f, 0.55f, 35f, true, 0.07f, DamageSoundType.SwordCut);

        [Header("이동")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 1.8f;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 3.8f;
        [SerializeField, Min(1f)] private float turnSpeed = 420f;
        [SerializeField, Min(1f)] private float attackTurnSpeed = 180f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float groundPull = -2f;

        [Header("공격 후 쉬는 시간")]
        [SerializeField, Min(0f)] private float lightAttackRecovery = 2f;
        [SerializeField, Min(0f)] private float comboAttackRecovery = 2.5f;
        [SerializeField, Min(0f)] private float wideSwingAttackRecovery = 2.5f;
        [SerializeField, Min(0f)] private float heavyAttackRecovery = 3f;

        [Header("콤보 연결")]
        [SerializeField, Range(0.35f, 1f)]
        private float comboFirstExitNormalizedTime = 0.4f;
        [SerializeField, Min(0f)] private float comboSecondDelay = 0.15f;

        [Header("전투 거리 조절")]
        [SerializeField, Min(0.1f)] private float combatMoveSpeed = 2f;
        [SerializeField, Min(0.1f)] private float combatMoveDuration = 0.6f;
        [SerializeField, Min(1)] private int attacksBeforeCombatMove = 2;

        [Header("피격 이동")]
        [SerializeField, Min(0.01f)] private float hitPushDuration = 0.18f;
        [SerializeField] private AnimationCurve hitPushCurve = CreateDefaultHitPushCurve();

        private CharacterController characterController;
        private NightShadeSwordAnimationController swordAnimation;
        private NightShadeSwordAttackRangeDetector attackRangeDetector;
        private NightShadeSwordWorldUnit swordWorldUnit;
        private CombatHitEffectPlayer hitEffectPlayer;
        private NightShadeSwordAttackAudio attackAudio;

        public bool IsDead => swordWorldUnit != null && swordWorldUnit.IsDead;
        internal bool IsAttackStateActive =>
            swordWorldUnit != null && swordWorldUnit.IsAttackStateActive;

        protected override IWorldObject CreateRuntimeObject()
        {
            FindSceneReferences();
            FindUnityComponents();

            if (target == null ||
                enemyAnimator == null ||
                swordHitShape == null ||
                !swordHitShape.IsReady)
            {
                throw new InvalidOperationException(
                    "NightShadeSwordController에 Target, Animator와 RustySword 검날 판정점이 필요합니다.");
            }

            enemyAnimator.applyRootMotion = false;
            swordAnimation.ConnectAnimator(enemyAnimator);

            var movement = new NightShadeSwordMovement(
                transform,
                characterController,
                gravity,
                groundPull);
            var hitStop = new CombatHitStop(enemyAnimator);
            attackRangeDetector = new NightShadeSwordAttackRangeDetector(
                transform,
                targetLayers,
                swordHitShape,
                hitStop,
                hitEffectPlayer);
            IUnitDeathState targetDeathState =
                target.GetComponentInParent<IUnitDeathState>();
            var settings = new NightShadeSwordSettings(
                findRange,
                attackRange,
                walkStartRange,
                runStartRange,
                attackFacingAngle,
                walkSpeed,
                chaseSpeed,
                turnSpeed,
                attackTurnSpeed,
                lightAttackRecovery,
                comboAttackRecovery,
                comboFirstExitNormalizedTime,
                comboSecondDelay,
                wideSwingAttackRecovery,
                heavyAttackRecovery,
                combatMoveSpeed,
                combatMoveDuration,
                attacksBeforeCombatMove,
                hitPushDuration,
                hitPushCurve,
                deadBodyKeepTime);
            var actions = new NightShadeSwordActions(
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
                actions);
            var stagger = new NightShadeSwordStagger(
                staggerLimit,
                staggerRecoverDelay,
                staggerRecoverSpeed);

            swordWorldUnit = new NightShadeSwordWorldUnit(
                maxHealth,
                stateMachine,
                attackRangeDetector,
                stagger,
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

        private void OpenAttackHit(NightShadeSwordAttackType attackType, int hitIndex)
        {
            attackRangeDetector?.Open(GetAttackDamage(attackType, hitIndex));
        }

        private void CloseAttackHit()
        {
            attackRangeDetector?.Close();
        }

        private AttackDamage GetAttackDamage(NightShadeSwordAttackType attackType, int hitIndex)
        {
            switch (attackType)
            {
                case NightShadeSwordAttackType.ComboFirst:
                    return comboFirstAttackDamage;
                case NightShadeSwordAttackType.ComboSecond:
                    return comboSecondAttackDamage;
                case NightShadeSwordAttackType.Heavy:
                    return heavyAttackDamage;
                case NightShadeSwordAttackType.WideSwing:
                    return wideSwingAttackDamage;
                default:
                    return lightAttackDamage;
            }
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

        private static AnimationCurve CreateDefaultHitPushCurve()
        {
            return new AnimationCurve(new Keyframe(0f, 0f, 2f, 2f), new Keyframe(1f, 1f, 0f, 0f));
        }

#if UNITY_EDITOR
        public void ConnectForEditor(
            Animator animator,
            Transform swordStartPoint,
            Transform swordEndPoint,
            float swordRadius)
        {
            enemyAnimator = animator;
            swordHitShape ??= new NightShadeSwordHitShape();
            swordHitShape.ConnectForEditor(
                swordStartPoint,
                swordEndPoint,
                swordRadius);
        }

        private void OnValidate()
        {
            FindUnityComponents();
            maxHealth = Mathf.Max(1f, maxHealth);
            staggerLimit = Mathf.Max(1f, staggerLimit);
            staggerRecoverDelay = Mathf.Max(0f, staggerRecoverDelay);
            staggerRecoverSpeed = Mathf.Max(0f, staggerRecoverSpeed);
            deadBodyKeepTime = Mathf.Max(0f, deadBodyKeepTime);
            findRange = Mathf.Max(0.1f, findRange);
            attackRange = Mathf.Clamp(attackRange, 0.1f, findRange);
            walkStartRange = Mathf.Clamp(
                walkStartRange,
                attackRange,
                findRange);
            runStartRange = Mathf.Clamp(
                runStartRange,
                walkStartRange,
                findRange);
            attackFacingAngle = Mathf.Clamp(attackFacingAngle, 0f, 180f);
            walkSpeed = Mathf.Max(0.1f, walkSpeed);
            chaseSpeed = Mathf.Max(0.1f, chaseSpeed);
            turnSpeed = Mathf.Max(1f, turnSpeed);
            attackTurnSpeed = Mathf.Max(1f, attackTurnSpeed);
            lightAttackRecovery = Mathf.Max(0f, lightAttackRecovery);
            comboAttackRecovery = Mathf.Max(0f, comboAttackRecovery);
            comboFirstExitNormalizedTime = Mathf.Clamp(
                comboFirstExitNormalizedTime,
                0.35f,
                1f);
            comboSecondDelay = Mathf.Max(0f, comboSecondDelay);
            wideSwingAttackRecovery =
                Mathf.Max(0f, wideSwingAttackRecovery);
            heavyAttackRecovery = Mathf.Max(0f, heavyAttackRecovery);
            combatMoveSpeed = Mathf.Max(0.1f, combatMoveSpeed);
            combatMoveDuration = Mathf.Max(0.1f, combatMoveDuration);
            attacksBeforeCombatMove =
                Mathf.Max(1, attacksBeforeCombatMove);
            hitPushDuration = Mathf.Max(0.01f, hitPushDuration);
            if (hitPushCurve == null || hitPushCurve.length < 2)
            {
                hitPushCurve = CreateDefaultHitPushCurve();
            }

            swordHitShape?.Validate();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, findRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

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
