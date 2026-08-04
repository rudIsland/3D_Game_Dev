using rudIsland.RPG3D.Animation;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.MummyWarrior
{
    public enum MummyWarriorHitDirection
    {
        Forward,
        Backward,
        Left,
        Right
    }

    // 네 방향 피격 상태를 빠르게 찾기 위한 Animator 해시
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    // 상태머신의 이동값과 전신 행동 요청을 Animator에 전달한다.
    public sealed class MummyWarriorAnimationController : MonoBehaviour
    {
        private static readonly int HitForwardStateId =
            Animator.StringToHash("Hit_Fw");
        private static readonly int HitBackwardStateId =
            Animator.StringToHash("Hit_Bw");
        private static readonly int HitLeftStateId =
            Animator.StringToHash("Hit_L");
        private static readonly int HitRightStateId =
            Animator.StringToHash("Hit_R");

        private const int AnimationLayerIndex = 0; // 이동과 전신 행동 상태

        private static readonly int MoveSideId = Animator.StringToHash("MoveSide"); // 이동 정보
        private static readonly int MoveSpeedId = Animator.StringToHash("MoveSpeed"); // 이동 속도
        private static readonly int EnterId = Animator.StringToHash("Enter"); // 내부에서 사용하는 값
        private static readonly int IsDeadId = Animator.StringToHash("IsDead"); // 기능 사용 여부
        private static readonly int EnterStateId = Animator.StringToHash("Enter"); // 현재 행동 상태
        private static readonly int HitStateId = Animator.StringToHash("Hit"); // 피격 또는 피해 관련 값
        private static readonly int MovementStateId = Animator.StringToHash("Movement"); // 이동 상태

        [SerializeField] private Animator mummyAnimator; // 애니메이터 참조

        private int requestedActionStateId; // 현재 행동 상태
        private AnimatorPlaybackReader playbackReader; // 씬 또는 시스템 참조

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

        public void PlayAttack(
            int animatorStateId,
            float transitionTime,
            float animationSpeed)
        {
            if (!CanControlAnimator()) return;

            StartAction(animatorStateId);
            mummyAnimator.speed = Mathf.Max(0.01f, animationSpeed);

            if (animatorStateId != 0 &&
                mummyAnimator.HasState(AnimationLayerIndex, animatorStateId))
            {
                mummyAnimator.CrossFadeInFixedTime(
                    animatorStateId,
                    Mathf.Max(0f, transitionTime),
                    AnimationLayerIndex,
                    0f);
            }
        }

        public void PlayHit(
            MummyWarriorHitDirection direction =
                MummyWarriorHitDirection.Forward)
        {
            if (!CanControlAnimator()) return;

            int hitStateId = GetHitStateId(direction);
            mummyAnimator.speed = 1f;
            StartAction(hitStateId);

            if (mummyAnimator.HasState(AnimationLayerIndex, hitStateId))
            {
                mummyAnimator.CrossFadeInFixedTime(
                    hitStateId,
                    0.04f,
                    AnimationLayerIndex,
                    0f);
            }
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
            requestedActionStateId = 0;
        }

        public void ResetActionSpeed()
        {
            if (mummyAnimator == null) return;

            mummyAnimator.speed = 1f;
            requestedActionStateId = 0;
            if (CanControlAnimator() &&
                mummyAnimator.HasState(AnimationLayerIndex, MovementStateId))
            {
                mummyAnimator.CrossFadeInFixedTime(
                    MovementStateId,
                    0.08f,
                    AnimationLayerIndex);
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
                playbackReader.IsInTransition(AnimationLayerIndex);
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
            mummyAnimator.SetBool(IsDeadId, false);
            ResetActionTriggers();

            if (mummyAnimator.isActiveAndEnabled &&
                mummyAnimator.gameObject.activeInHierarchy)
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

        private static int GetHitStateId(MummyWarriorHitDirection direction)
        {
            switch (direction)
            {
                case MummyWarriorHitDirection.Backward:
                    return HitBackwardStateId;
                case MummyWarriorHitDirection.Left:
                    return HitLeftStateId;
                case MummyWarriorHitDirection.Right:
                    return HitRightStateId;
                default:
                    return HitForwardStateId;
            }
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
                AnimationLayerIndex,
                animatorStateId,
                out normalizedTime);
        }

        private void ResetActionTriggers()
        {
            if (mummyAnimator == null) return;

            mummyAnimator.ResetTrigger(EnterId);
        }

        private bool CanControlAnimator()
        {
            return playbackReader != null &&
                playbackReader.CanRead(AnimationLayerIndex);
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
