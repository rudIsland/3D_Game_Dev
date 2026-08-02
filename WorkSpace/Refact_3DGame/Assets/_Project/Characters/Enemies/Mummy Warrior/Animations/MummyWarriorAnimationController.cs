using rudIsland.RPG3D.Animation;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.MummyWarrior
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    // 상태머신의 이동값과 전신 행동 요청을 Animator에 전달한다.
    public sealed class MummyWarriorAnimationController : MonoBehaviour
    {
        private const int ActionLayerIndex = 1;

        private static readonly int MoveSideId = Animator.StringToHash("MoveSide");
        private static readonly int MoveSpeedId = Animator.StringToHash("MoveSpeed");
        private static readonly int AlternateIdleId = Animator.StringToHash("AlternateIdle");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int AttackNumberId = Animator.StringToHash("AttackNumber");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int BlockId = Animator.StringToHash("Block");
        private static readonly int TurnId = Animator.StringToHash("Turn");
        private static readonly int StepBackId = Animator.StringToHash("StepBack");
        private static readonly int EnterId = Animator.StringToHash("Enter");
        private static readonly int ExitId = Animator.StringToHash("Exit");
        private static readonly int IsDeadId = Animator.StringToHash("IsDead");
        private static readonly int EnterStateId = Animator.StringToHash("Enter");
        private static readonly int HitStateId = Animator.StringToHash("Hit");
        private static readonly int ActionIdleStateId = Animator.StringToHash("Action Idle");

        [SerializeField] private Animator mummyAnimator;

        private int requestedActionStateId;
        private AnimatorPlaybackReader playbackReader;

        private void Awake()
        {
            FindMummyAnimator();
            ConnectAnimator(mummyAnimator);
        }

        public void SetMovement(float moveSide, float moveSpeed)
        {
            if (!CanControlAnimator()) return;
            mummyAnimator.SetFloat(MoveSideId, moveSide);
            mummyAnimator.SetFloat(MoveSpeedId, moveSpeed);
        }

        public void PlayAlternateIdle() => SetTrigger(AlternateIdleId);
        public void PlayBlock() => SetTrigger(BlockId);
        public void PlayTurn() => SetTrigger(TurnId);
        public void PlayStepBack() => SetTrigger(StepBackId);
        public void PlayExit() => SetTrigger(ExitId);

        public void PlayAttack(
            int attackNumber,
            int animatorStateId,
            float transitionTime,
            float animationSpeed)
        {
            if (!CanControlAnimator()) return;

            StartAction(animatorStateId);
            mummyAnimator.speed = Mathf.Max(0.01f, animationSpeed);
            mummyAnimator.SetInteger(AttackNumberId, attackNumber);
            mummyAnimator.SetTrigger(AttackId);

            if (animatorStateId != 0 &&
                mummyAnimator.HasState(ActionLayerIndex, animatorStateId))
            {
                mummyAnimator.CrossFadeInFixedTime(
                    animatorStateId,
                    Mathf.Max(0f, transitionTime),
                    ActionLayerIndex,
                    0f);
                // 직접 전환한 공격 Trigger가 다음 행동에서 다시 소비되지 않게 한다.
                mummyAnimator.ResetTrigger(AttackId);
            }
        }

        public void PlayHit()
        {
            if (!CanControlAnimator()) return;
            mummyAnimator.speed = 1f;
            StartAction(HitStateId);
            mummyAnimator.SetTrigger(HitId);
        }

        public void PlayEnter()
        {
            if (!CanControlAnimator()) return;
            StartAction(EnterStateId);
            mummyAnimator.SetTrigger(EnterId);
        }

        public void PlayDeath()
        {
            if (!CanControlAnimator()) return;

            ResetActionTriggers();
            SetMovement(0f, 0f);
            mummyAnimator.SetBool(IsDeadId, true);
            mummyAnimator.Update(0f);

            // 전용 Death 클립이 추가되기 전까지 현재 자세를 그대로 유지한다.
            FreezeDeathPose();
        }

        public void FreezeDeathPose()
        {
            if (mummyAnimator == null)
            {
                return;
            }

            mummyAnimator.speed = 0f;
            requestedActionStateId = 0;
        }

        public void ResetActionSpeed()
        {
            if (mummyAnimator == null) return;

            mummyAnimator.speed = 1f;
            requestedActionStateId = 0;
            if (CanControlAnimator() &&
                mummyAnimator.HasState(ActionLayerIndex, ActionIdleStateId))
            {
                mummyAnimator.CrossFadeInFixedTime(
                    ActionIdleStateId,
                    0.08f,
                    ActionLayerIndex);
            }
        }

        public bool TryGetAttackNormalizedTime(
            int animatorStateId,
            out float normalizedTime)
        {
            return TryGetCurrentActionTime(
                animatorStateId,
                out normalizedTime);
        }

        internal bool TryGetCurrentActionTime(out float normalizedTime)
        {
            return TryGetCurrentActionTime(
                requestedActionStateId,
                out normalizedTime);
        }

        internal bool IsActionTransitioning()
        {
            return playbackReader != null &&
                playbackReader.IsInTransition(ActionLayerIndex);
        }

        public void ResetAnimation()
        {
            FindMummyAnimator();
            if (playbackReader == null && mummyAnimator != null)
            {
                playbackReader = new AnimatorPlaybackReader(mummyAnimator);
            }
            requestedActionStateId = 0;

            if (mummyAnimator == null) return;

            mummyAnimator.speed = 1f;
            mummyAnimator.Rebind();
            mummyAnimator.SetFloat(MoveSideId, 0f);
            mummyAnimator.SetFloat(MoveSpeedId, 0f);
            mummyAnimator.SetInteger(AttackNumberId, 0);
            mummyAnimator.SetBool(IsDeadId, false);
            ResetActionTriggers();

            if (mummyAnimator.isActiveAndEnabled)
            {
                mummyAnimator.Update(0f);
            }
        }

        internal void ConnectAnimator(Animator animator)
        {
            if (animator == null)
            {
                return;
            }

            mummyAnimator = animator;
            playbackReader = new AnimatorPlaybackReader(animator);
        }

        private void StartAction(int animatorStateId)
        {
            requestedActionStateId = animatorStateId;
        }

        private bool TryGetCurrentActionTime(
            int animatorStateId,
            out float normalizedTime)
        {
            normalizedTime = 0f;

            if (animatorStateId == 0 ||
                requestedActionStateId != animatorStateId ||
                !CanControlAnimator())
            {
                return false;
            }

            return playbackReader.TryGetCurrentOrNextStateTime(
                ActionLayerIndex,
                animatorStateId,
                out normalizedTime);
        }

        private void SetTrigger(int triggerId)
        {
            if (CanControlAnimator()) mummyAnimator.SetTrigger(triggerId);
        }

        private void ResetActionTriggers()
        {
            if (mummyAnimator == null) return;

            mummyAnimator.ResetTrigger(AlternateIdleId);
            mummyAnimator.ResetTrigger(AttackId);
            mummyAnimator.ResetTrigger(HitId);
            mummyAnimator.ResetTrigger(BlockId);
            mummyAnimator.ResetTrigger(TurnId);
            mummyAnimator.ResetTrigger(StepBackId);
            mummyAnimator.ResetTrigger(EnterId);
            mummyAnimator.ResetTrigger(ExitId);
        }

        private bool CanControlAnimator()
        {
            return playbackReader != null &&
                playbackReader.CanRead(ActionLayerIndex);
        }

        private void FindMummyAnimator()
        {
            if (mummyAnimator == null)
            {
                mummyAnimator = GetComponentInChildren<Animator>(true);
            }
        }

#if UNITY_EDITOR
        private void OnValidate() => FindMummyAnimator();
#endif
    }
}
