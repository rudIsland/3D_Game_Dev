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
        [SerializeField] private NightShadeSwordHitShape swordHitShape;
        [SerializeField] private NightShadeSwordConfig config;

        private CharacterController characterController;
        private NightShadeSwordAnimationController swordAnimation;
        private NightShadeSwordAttackRangeDetector attackRangeDetector;
        private NightShadeSwordWorldUnit swordWorldUnit;
        private CombatHitEffectPlayer hitEffectPlayer;
        private NightShadeSwordAttackAudio attackAudio;
        private NightShadeSwordSettings runtimeSettings;

        public bool IsDead => swordWorldUnit != null && swordWorldUnit.IsDead;
        internal bool IsAttackStateActive =>
            swordWorldUnit != null && swordWorldUnit.IsAttackStateActive;

        public string DebugTopStateName => GetCombatDebug()?.TopState.ToString() ?? "없음";
        public string DebugCombatPhaseName => GetCombatDebug()?.CombatPhase.ToString() ?? "없음";
        public string DebugCurrentActionName => GetCombatDebug()?.CurrentAction.ToString() ?? "없음";
        public string DebugCurrentStopReasonName => GetCombatDebug()?.CurrentActionStopReason.ToString() ?? "없음";
        public string DebugLastEvaluatedPhaseName => GetCombatDebug()?.LastEvaluatedPhase.ToString() ?? "없음";
        public string DebugSelectedActionName => GetCombatDebug()?.SelectedAction.ToString() ?? "없음";
        public string DebugPreviousStopReasonName => GetCombatDebug()?.PreviousActionStopReason.ToString() ?? "없음";
        public int DebugCandidateCount => GetCombatDebug()?.CandidateCount ?? 0;

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
            runtimeSettings = config.CreateRuntimeSettings();

            var movement = new NightShadeSwordMovement(
                transform,
                characterController,
                runtimeSettings.Gravity,
                runtimeSettings.GroundPull);
            var hitStop = new CombatHitStop(enemyAnimator);
            attackRangeDetector = new NightShadeSwordAttackRangeDetector(
                transform,
                runtimeSettings.TargetLayers,
                swordHitShape,
                hitStop,
                hitEffectPlayer);
            IUnitDeathState targetDeathState =
                target.GetComponentInParent<IUnitDeathState>();
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
                runtimeSettings,
                actions);
            var stopPoint = new StopPoint(
                runtimeSettings.StaggerLimit,
                runtimeSettings.StaggerRecoverDelay,
                runtimeSettings.StaggerRecoverSpeed);

            swordWorldUnit = new NightShadeSwordWorldUnit(
                runtimeSettings.MaxHealth,
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
            return runtimeSettings.GetAttackDamage(attackType);
        }

        public string GetDebugCandidateActionName(int index)
        {
            return TryGetDebugCandidate(index, out NightShadeSwordActionDebugEntry entry)
                ? entry.ActionId.ToString()
                : "없음";
        }

        public bool GetDebugCandidateCanStart(int index)
        {
            return TryGetDebugCandidate(index, out NightShadeSwordActionDebugEntry entry) &&
                entry.CanStart;
        }

        public string GetDebugCandidateRejectReasonName(int index)
        {
            return TryGetDebugCandidate(index, out NightShadeSwordActionDebugEntry entry)
                ? entry.RejectReason.ToString()
                : "없음";
        }

        public float GetDebugCandidateBaseScore(int index) =>
            TryGetDebugCandidate(index, out NightShadeSwordActionDebugEntry entry)
                ? entry.Score.BaseScore
                : 0f;

        public float GetDebugCandidateDistanceScore(int index) =>
            TryGetDebugCandidate(index, out NightShadeSwordActionDebugEntry entry)
                ? entry.Score.DistanceScore
                : 0f;

        public float GetDebugCandidateRepeatPenalty(int index) =>
            TryGetDebugCandidate(index, out NightShadeSwordActionDebugEntry entry)
                ? entry.Score.RepeatPenalty
                : 0f;

        public float GetDebugCandidateRandomBonus(int index) =>
            TryGetDebugCandidate(index, out NightShadeSwordActionDebugEntry entry)
                ? entry.Score.RandomBonus
                : 0f;

        public float GetDebugCandidateFinalScore(int index) =>
            TryGetDebugCandidate(index, out NightShadeSwordActionDebugEntry entry)
                ? entry.Score.FinalScore
                : 0f;

        public bool GetDebugCandidateIsSelected(int index)
        {
            return TryGetDebugCandidate(index, out NightShadeSwordActionDebugEntry entry) &&
                entry.IsSelected;
        }

        private NightShadeSwordCombatDebug GetCombatDebug()
        {
            return swordWorldUnit?.CombatDebug;
        }

        private bool TryGetDebugCandidate(
            int index,
            out NightShadeSwordActionDebugEntry entry)
        {
            NightShadeSwordCombatDebug combatDebug = GetCombatDebug();
            if (combatDebug == null ||
                (uint)index >= (uint)combatDebug.CandidateCount)
            {
                entry = default;
                return false;
            }

            entry = combatDebug.Candidates[index];
            return true;
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
                Mathf.Sqrt(previewSettings.FindRangeSquared));
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(
                transform.position,
                previewSettings.AttackRange);

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
