using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 상태머신 요청을 NightShade 양손검 Animator 상태로 바꾼다.
    [DisallowMultipleComponent]
    public sealed class NightShadeSwordAnimationController : MonoBehaviour, INightShadeSwordAnimation
    {
        private const float DefaultBlendTime = 0.12f;
        private const float HitBlendTime = 0.06f;

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

        internal void PlayHitFromStart()
        {
            Play(HitStateId, HitBlendTime, true);
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

        void INightShadeSwordAnimation.PlayIdle() => PlayIdle();
        void INightShadeSwordAnimation.PlayChase() => PlayChase();
        void INightShadeSwordAnimation.PlayWalk() => PlayWalk();
        void INightShadeSwordAnimation.PlayCombatMove(NightShadeCombatMoveType moveType) => PlayCombatMove(moveType);
        void INightShadeSwordAnimation.PlayAttack(NightShadeSwordAttackType attackType) => PlayAttack(attackType);
        void INightShadeSwordAnimation.PlayHitFromStart() => PlayHitFromStart();
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
