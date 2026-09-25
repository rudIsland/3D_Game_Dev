using System;
using UnityEngine;
using Core;
using Enemy;

namespace NightShade
{
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(CharacterController),
        typeof(NightShadeSwordAnimationController))]
    [RequireComponent(typeof(NightShadeSwordAttackAudio))]
    // Unity 프리팹과 일반 C# NightShade 양손검 전투를 연결한다.
    public sealed class NightShadeSwordController : EnemyView, IUnitDeathState, IEnemyDamageReceiver
    {
        [Header("필수 연결")]
        private Transform target;
        [SerializeField] private Animator enemyAnimator;
        [SerializeField] private NightShadeSwordHitShape swordHitShape;
        private NightShadeSwordConfig config;

        [Header("보스 전투 구역")]
        [Tooltip("지하 보스방을 감싸는 BoxCollider. 비워 두면 생성 위치 기준 범위를 사용합니다.")]
        [SerializeField] private BoxCollider battleArea;
        [SerializeField, Min(1f)] private float homeRadius = 30f;
        [SerializeField, Min(0.1f)] private float maximumHeightDifference = 2f;
        [SerializeField] private LayerMask obstacleLayers = 70657;

        private CharacterController characterController;
        private NightShadeSwordAnimationController swordAnimation;
        private NightShadeSwordAttackRangeDetector attackRangeDetector;
        private NightShadeSwordWorldUnit swordWorldUnit;
        private ICombatHitEffects hitEffectPlayer;
        private NightShadeSwordAttackAudio attackAudio;
        private NightShadeSwordBattleSpace battleSpace;
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private bool battleAnimationsValidated;

        public bool IsDead => swordWorldUnit != null && swordWorldUnit.IsDead;
        internal bool IsAttackStateActive =>
            swordWorldUnit != null && swordWorldUnit.IsAttackStateActive;

        internal NightShadeSwordCombatDebug CombatDebug =>
            swordWorldUnit?.CombatDebug;

        /// <summary>생성 담당에게 설정과 추적 대상을 받는다.</summary>
        public override void SetData(ScriptableObject data, Transform target)
        {
            config = (NightShadeSwordConfig)data;
            this.target = target;
        }

        protected override Unit CreateRuntimeObject()
        {
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
            enemyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            swordAnimation.ConnectAnimator(enemyAnimator);
            NightShadeSwordSettings settings = config.CreateRuntimeSettings();

            battleSpace = new NightShadeSwordBattleSpace(transform, characterController,
                battleArea, obstacleLayers, homeRadius, maximumHeightDifference);

            var movement = new NightShadeSwordMovement(
                transform,
                characterController,
                settings.Movement,
                settings.Recovery.MoveSpeed,
                battleSpace);
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
                RequestDespawn,
                CheckAttackHit);
            var stateMachine = new NightShadeSwordStateMachine(
                target,
                targetDeathState,
                movement,
                swordAnimation,
                settings,
                combatOutput,
                battleSpace: battleSpace);
            var stopPoint = new StopPoint(settings.Life);

            swordWorldUnit = new NightShadeSwordWorldUnit(
                settings.Life.MaxHealth,
                stateMachine,
                attackRangeDetector,
                stopPoint,
                hitStop,
                BeginBattle,
                RestoreBattlePosition);
            return swordWorldUnit;
        }

        public EnemyHitResult TakeHit(in EnemyHitRequest hitRequest)
        {
            return swordWorldUnit != null
                ? swordWorldUnit.TakeHit(in hitRequest)
                : EnemyHitResult.Ignored;
        }

        private void LateUpdate()
        {
            if (Time.timeScale <= 0f) return;
            // 풀 예열 중에는 Animator가 초기화되지 않아 HasState를 조회할 수 없다.
            if (!battleAnimationsValidated && swordWorldUnit != null &&
                swordWorldUnit.IsEnabled && enemyAnimator.isInitialized)
            {
                swordAnimation.ValidateBattleAnimations();
                battleAnimationsValidated = true;
            }
            swordWorldUnit?.LateUpdate();
        }

        public void ResetBattle()
        {
            swordWorldUnit?.ResetBattle();
        }

        private void BeginBattle()
        {
            // 풀에서 실제 생성 위치로 이동한 뒤 호출된다.
            homePosition = transform.position;
            homeRotation = transform.rotation;
            battleSpace.Begin();
        }

        private void RestoreBattlePosition()
        {
            attackAudio.Stop();
            characterController.enabled = false;
            transform.SetPositionAndRotation(homePosition, homeRotation);
            characterController.enabled = true;
            swordAnimation.ResetAnimation();
        }

        private void CheckAttackHit()
        {
            if (swordWorldUnit != null && swordWorldUnit.IsEnabled && !swordWorldUnit.IsDead)
                attackRangeDetector.Tick();
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

        private void FindUnityComponents()
        {
            characterController = GetComponent<CharacterController>();
            swordAnimation = GetComponent<NightShadeSwordAnimationController>();
            hitEffectPlayer = GetComponent<ICombatHitEffects>();
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
