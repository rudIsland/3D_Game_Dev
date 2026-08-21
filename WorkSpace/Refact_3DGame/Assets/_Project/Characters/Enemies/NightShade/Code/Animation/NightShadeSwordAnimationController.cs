using rudIsland.RPG3D.Characters.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 상태머신 요청을 NightShade 양손검 Animator 상태로 바꾼다.
    [DisallowMultipleComponent]
    public sealed class NightShadeSwordAnimationController : MonoBehaviour, INightShadeSwordAnimation
    {
        private const float DefaultBlendTime = 0.12f;
        private const float HitBlendTime = 0.06f;
        private const float StaggerEnterToStartBlendTime = 0.18f;
        private const float StaggerStartToIdleBlendTime = 0.06f;
        private const float StaggerIdleToEndBlendTime = 0.22f;

        private static readonly int IdleStateId =
            Animator.StringToHash("Base Layer.Idle");
        private static readonly int ChaseStateId =
            Animator.StringToHash("Base Layer.Chase");
        private static readonly int WalkStateId =
            Animator.StringToHash("Base Layer.Walk");
        private static readonly int CombatBackStateId =
            Animator.StringToHash("Base Layer.Combat Back");
        private static readonly int CombatLeftStateId =
            Animator.StringToHash("Base Layer.Combat Left");
        private static readonly int CombatRightStateId =
            Animator.StringToHash("Base Layer.Combat Right");
        private static readonly int LightAttackStateId =
            Animator.StringToHash("Base Layer.Light Attack");
        private static readonly int ComboFirstStateId =
            Animator.StringToHash("Base Layer.Combo First");
        private static readonly int ComboSecondStateId =
            Animator.StringToHash("Base Layer.Combo Second");
        private static readonly int HeavyAttackStateId =
            Animator.StringToHash("Base Layer.Heavy Attack");
        private static readonly int WideSwingStateId =
            Animator.StringToHash("Base Layer.Wide Swing");
        private static readonly int HitStateId =
            Animator.StringToHash("Base Layer.Hit");
        private static readonly int SmallHitFrontStateId =
            Animator.StringToHash("Base Layer.Small Hit Front");
        private static readonly int SmallHitBackStateId =
            Animator.StringToHash("Base Layer.Small Hit Back");
        private static readonly int SmallHitLeftStateId =
            Animator.StringToHash("Base Layer.Small Hit Left");
        private static readonly int SmallHitRightStateId =
            Animator.StringToHash("Base Layer.Small Hit Right");
        private static readonly int HitFrontStateId =
            Animator.StringToHash("Base Layer.Hit Front");
        private static readonly int HitBackStateId =
            Animator.StringToHash("Base Layer.Hit Back");
        private static readonly int HitLeftStateId =
            Animator.StringToHash("Base Layer.Hit Left");
        private static readonly int HitRightStateId =
            Animator.StringToHash("Base Layer.Hit Right");
        private static readonly int KnockbackStateId =
            Animator.StringToHash("Base Layer.Knockback");
        private static readonly int KnockdownStateId =
            Animator.StringToHash("Base Layer.Knockdown");
        private static readonly int GetUpStateId =
            Animator.StringToHash("Base Layer.Get Up");
        private static readonly int StaggerEnterStateId =
            Animator.StringToHash("Base Layer.Stagger Enter");
        private static readonly int StaggerStartStateId =
            Animator.StringToHash("Base Layer.Stagger Start");
        private static readonly int StaggerIdleStateId =
            Animator.StringToHash("Base Layer.Stagger Idle");
        private static readonly int StaggerEndStateId =
            Animator.StringToHash("Base Layer.Stagger End");
        private static readonly int DeadStateId =
            Animator.StringToHash("Base Layer.Dead");
        private static readonly int AttackSpeedId =
            Animator.StringToHash("AttackSpeed");

        [SerializeField] private Animator enemyAnimator;

        private int requestedStateId;

        private void Awake()
        {
            FindAnimator();
        }

        internal void ConnectAnimator(Animator animator)
        {
            enemyAnimator = animator;
        }

        internal void PlayIdle() => Play(IdleStateId, DefaultBlendTime);
        internal void PlayChase() => Play(ChaseStateId, DefaultBlendTime);
        internal void PlayWalk() => Play(WalkStateId, DefaultBlendTime);

        internal void PlayCombatMove(NightShadeCombatMoveType moveType)
        {
            switch (moveType)
            {
                case NightShadeCombatMoveType.Left:
                    Play(CombatLeftStateId, DefaultBlendTime);
                    break;
                case NightShadeCombatMoveType.Right:
                    Play(CombatRightStateId, DefaultBlendTime);
                    break;
                default:
                    Play(CombatBackStateId, DefaultBlendTime);
                    break;
            }
        }

        internal void PlayAttack(NightShadeSwordAttackType attackType)
        {
            switch (attackType)
            {
                case NightShadeSwordAttackType.ComboFirst:
                    Play(ComboFirstStateId, DefaultBlendTime);
                    break;
                case NightShadeSwordAttackType.ComboSecond:
                    Play(ComboSecondStateId, DefaultBlendTime);
                    break;
                case NightShadeSwordAttackType.Heavy:
                    Play(HeavyAttackStateId, DefaultBlendTime);
                    break;
                case NightShadeSwordAttackType.WideSwing:
                    Play(WideSwingStateId, DefaultBlendTime);
                    break;
                default:
                    Play(LightAttackStateId, DefaultBlendTime);
                    break;
            }
        }

        internal void PlaySmallHitFromStart(
            Vector3 incomingDirection)
        {
            int stateId = GetHitStateId(
                incomingDirection,
                HitReaction.SmallHit);
            if (CanReadAnimator() &&
                !enemyAnimator.HasState(0, stateId))
            {
                stateId = GetHitStateId(
                    incomingDirection,
                    HitReaction.BigHit);
            }

            Play(stateId, HitBlendTime, true);
        }

        internal void PlayBigHitFromStart(
            Vector3 incomingDirection)
        {
            int stateId = GetHitStateId(
                incomingDirection,
                HitReaction.BigHit);
            if (CanReadAnimator() &&
                !enemyAnimator.HasState(0, stateId))
            {
                stateId = HitStateId;
            }

            Play(stateId, HitBlendTime, true);
        }

        internal void PlayKnockbackFromStart()
        {
            Play(KnockbackStateId, HitBlendTime, true);
        }

        internal void PlayKnockdownFromStart()
        {
            Play(KnockdownStateId, HitBlendTime, true);
        }

        internal void PlayGetUpFromStart()
        {
            Play(GetUpStateId, DefaultBlendTime, true);
        }

        internal void PlayStaggerEnterFromStart()
        {
            Play(StaggerEnterStateId, HitBlendTime, true);
        }

        internal void PlayStaggerStartFromStart()
        {
            Play(StaggerStartStateId, StaggerEnterToStartBlendTime, true);
        }

        internal void PlayStaggerIdleFromStart()
        {
            Play(StaggerIdleStateId, StaggerStartToIdleBlendTime, true);
        }

        internal void PlayStaggerEndFromStart()
        {
            Play(StaggerEndStateId, StaggerIdleToEndBlendTime, true);
        }

        internal void PlayDead()
        {
            Play(DeadStateId, DefaultBlendTime);
        }

        internal void SetAttackPlaybackSpeed(float speed)
        {
            if (CanReadAnimator())
            {
                enemyAnimator.SetFloat(AttackSpeedId, Mathf.Clamp(speed, 0.1f, 1f));
            }
        }

        internal void ResetAttackPlaybackSpeed()
        {
            if (CanReadAnimator())
            {
                enemyAnimator.SetFloat(AttackSpeedId, 1f);
            }
        }

        internal bool TryGetRequestedAnimationTime(out float normalizedTime)
        {
            normalizedTime = 0f;
            if (!CanReadAnimator())
            {
                return false;
            }

            AnimatorStateInfo stateInfo =
                enemyAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.fullPathHash != requestedStateId)
            {
                return false;
            }

            normalizedTime = stateInfo.normalizedTime;
            return true;
        }

        internal bool IsTransitioning()
        {
            return CanReadAnimator() && enemyAnimator.IsInTransition(0);
        }

        internal void ResetAnimation()
        {
            FindAnimator();
            requestedStateId = IdleStateId;
            if (enemyAnimator == null)
            {
                return;
            }

            enemyAnimator.Rebind();
            if (enemyAnimator.isActiveAndEnabled &&
                enemyAnimator.runtimeAnimatorController != null)
            {
                enemyAnimator.SetFloat(AttackSpeedId, 1f);
                enemyAnimator.Play(IdleStateId, 0, 0f);
                enemyAnimator.Update(0f);
            }
        }

        private void Play(int stateId, float blendTime, bool restart = false)
        {
            requestedStateId = stateId;
            if (!CanReadAnimator())
            {
                return;
            }

            AnimatorStateInfo current = enemyAnimator.GetCurrentAnimatorStateInfo(0);
            if (!restart && current.fullPathHash == stateId)
            {
                return;
            }

            enemyAnimator.CrossFadeInFixedTime(
                stateId,
                blendTime,
                0,
                0f);
        }

        private bool CanReadAnimator()
        {
            return enemyAnimator != null &&
                enemyAnimator.isActiveAndEnabled &&
                enemyAnimator.runtimeAnimatorController != null &&
                enemyAnimator.layerCount > 0;
        }

        private void FindAnimator()
        {
            if (enemyAnimator == null)
            {
                enemyAnimator = GetComponentInChildren<Animator>(true);
            }
        }

        private int GetHitStateId(
            Vector3 incomingDirection,
            HitReaction reaction)
        {
            bool usesSmallHit =
                reaction == HitReaction.SmallHit;
            switch (NightShadeHitDirection.GetSide(
                        transform.forward,
                        transform.right,
                        incomingDirection))
            {
                case NightShadeHitSide.Back:
                    return usesSmallHit
                        ? SmallHitBackStateId
                        : HitBackStateId;
                case NightShadeHitSide.Left:
                    return usesSmallHit
                        ? SmallHitLeftStateId
                        : HitLeftStateId;
                case NightShadeHitSide.Right:
                    return usesSmallHit
                        ? SmallHitRightStateId
                        : HitRightStateId;
                default:
                    return usesSmallHit
                        ? SmallHitFrontStateId
                        : HitFrontStateId;
            }
        }

        void INightShadeSwordAnimation.PlayIdle() => PlayIdle();
        void INightShadeSwordAnimation.PlayChase() => PlayChase();
        void INightShadeSwordAnimation.PlayWalk() => PlayWalk();
        void INightShadeSwordAnimation.PlayCombatMove(NightShadeCombatMoveType moveType) => PlayCombatMove(moveType);
        void INightShadeSwordAnimation.PlayAttack(NightShadeSwordAttackType attackType) => PlayAttack(attackType);
        void INightShadeSwordAnimation.PlaySmallHitFromStart(
            Vector3 incomingDirection) =>
            PlaySmallHitFromStart(incomingDirection);
        void INightShadeSwordAnimation.PlayBigHitFromStart(
            Vector3 incomingDirection) =>
            PlayBigHitFromStart(incomingDirection);
        void INightShadeSwordAnimation.PlayKnockbackFromStart() =>
            PlayKnockbackFromStart();
        void INightShadeSwordAnimation.PlayKnockdownFromStart() =>
            PlayKnockdownFromStart();
        void INightShadeSwordAnimation.PlayGetUpFromStart() =>
            PlayGetUpFromStart();
        void INightShadeSwordAnimation.PlayStaggerEnterFromStart() =>
            PlayStaggerEnterFromStart();
        void INightShadeSwordAnimation.PlayStaggerStartFromStart() =>
            PlayStaggerStartFromStart();
        void INightShadeSwordAnimation.PlayStaggerIdleFromStart() =>
            PlayStaggerIdleFromStart();
        void INightShadeSwordAnimation.PlayStaggerEndFromStart() =>
            PlayStaggerEndFromStart();
        void INightShadeSwordAnimation.PlayDead() => PlayDead();
        void INightShadeSwordAnimation.ResetAttackPlaybackSpeed() => ResetAttackPlaybackSpeed();
        bool INightShadeSwordAnimation.TryGetRequestedAnimationTime(out float normalizedTime) => TryGetRequestedAnimationTime(out normalizedTime);
        bool INightShadeSwordAnimation.IsTransitioning() => IsTransitioning();

#if UNITY_EDITOR
        public void ConnectForEditor(Animator animator)
        {
            enemyAnimator = animator;
        }

        private void OnValidate()
        {
            FindAnimator();
        }
#endif
    }
}
