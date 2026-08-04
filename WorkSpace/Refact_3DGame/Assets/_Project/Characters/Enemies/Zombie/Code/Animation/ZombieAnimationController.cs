using rudIsland.RPG3D.Animation;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    [DisallowMultipleComponent]
    // 좀비 상태를 Animator 값으로 바꾸고 공격 클립의 루트 회전을 적용한다.
    public sealed class ZombieAnimationController : MonoBehaviour
    {
        private const float HitRestartBlendTime = 0.15f; // 피격 또는 피해 관련 값

        private static readonly int StateId = Animator.StringToHash("State"); // 현재 행동 상태
        private static readonly int AttackTypeId = Animator.StringToHash("AttackType"); // 공격 관련 설정 또는 상태
        private static readonly int IdleStateId = Animator.StringToHash("Idle"); // 현재 행동 상태
        private static readonly int AlertStateId = Animator.StringToHash("Alert"); // 현재 행동 상태
        private static readonly int SwingAttackStateId = Animator.StringToHash("Swing Attack"); // 공격 관련 설정 또는 상태
        private static readonly int KickAttackStateId = Animator.StringToHash("Kick Attack"); // 공격 관련 설정 또는 상태
        private static readonly int UpDownAttackStateId =Animator.StringToHash("Up Down Attack"); // 공격 관련 설정 또는 상태
        private static readonly int HitStateId = Animator.StringToHash("Hit"); // 피격 또는 피해 관련 값
        private static readonly int HitFullPathId = Animator.StringToHash("Base Layer.Hit"); // 피격 또는 피해 관련 값
        private static readonly int DeadStateId = Animator.StringToHash("Dead"); // 현재 행동 상태
        private enum AnimationState
        {
            Idle = 0,
            Alert = 1,
            Chase = 2,
            Attack = 3,
            Hit = 4,
            Dead = 5
        }

        [SerializeField] private Animator zombieAnimator; // 애니메이터 참조

        private AnimationState requestedAnimationState; // 현재 행동 상태
        private AnimatorPlaybackReader playbackReader; // 씬 또는 시스템 참조

        private void Awake()
        {
            FindZombieAnimator();
            ConnectAnimator(zombieAnimator);
        }

        public void PlayIdle()
        {
            RequestAnimation(AnimationState.Idle);
        }

        public void PlayChase()
        {
            RequestAnimation(AnimationState.Chase);
        }

        public void PlayAlert()
        {
            RequestAnimation(AnimationState.Alert);
        }

        internal void PlayAttack(ZombieAttackType attackType)
        {
            if (!CanControlAnimator())
            {
                return;
            }

            requestedAnimationState = AnimationState.Attack;
            zombieAnimator.SetInteger(AttackTypeId, (int)attackType);
            zombieAnimator.SetInteger(StateId, (int)AnimationState.Attack);
        }

        public void PlayHitFromStart()
        {
            RequestAnimation(AnimationState.Hit);

            if (!CanControlAnimator())
            {
                return;
            }

            bool isPlayingHit = playbackReader.IsCurrentState(0, HitStateId);
            bool isChangingToHit = playbackReader.IsChangingTo(0, HitStateId);

            if (isPlayingHit || isChangingToHit)
            {
                zombieAnimator.CrossFadeInFixedTime(
                    HitFullPathId,
                    HitRestartBlendTime,
                    0,
                    0f);
            }
        }

        public void PlayDead()
        {
            RequestAnimation(AnimationState.Dead);
        }

        internal bool TryGetCurrentAnimationTime(out float normalizedTime)
        {
            normalizedTime = 0f;
            if (!CanControlAnimator() ||
                !playbackReader.TryGetCurrentState(
                    0,
                    out AnimatorStateInfo stateInfo))
            {
                return false;
            }

            bool isExpectedState = requestedAnimationState == AnimationState.Attack
                ? IsAttackState(stateInfo.shortNameHash)
                : stateInfo.shortNameHash == GetRequestedStateId();
            if (!isExpectedState)
            {
                return false;
            }

            normalizedTime = stateInfo.normalizedTime;
            return true;
        }

        internal bool IsAnimationTransitioning()
        {
            return playbackReader != null && playbackReader.IsInTransition(0);
        }

        internal void ApplyAttackRootRotation(
            Quaternion deltaRotation,
            bool canTurn)
        {
            if (requestedAnimationState == AnimationState.Attack && canTurn)
            {
                transform.rotation *= deltaRotation;
            }
        }

        internal void ConnectAnimator(Animator animator)
        {
            if (animator == null)
            {
                return;
            }

            zombieAnimator = animator;
            playbackReader = new AnimatorPlaybackReader(animator);
            ZombieAnimationEventReceiver receiver =
                animator.GetComponent<ZombieAnimationEventReceiver>();
            if (receiver == null)
            {
                receiver =
                    animator.gameObject.AddComponent<ZombieAnimationEventReceiver>();
            }

            receiver.Initialize(this);
        }

        public void ResetAnimation()
        {
            FindZombieAnimator();
            if (playbackReader == null && zombieAnimator != null)
            {
                playbackReader = new AnimatorPlaybackReader(zombieAnimator);
            }
            requestedAnimationState = AnimationState.Idle;

            if (zombieAnimator == null)
            {
                return;
            }

            zombieAnimator.Rebind();
            zombieAnimator.SetInteger(StateId, (int)AnimationState.Idle);
            zombieAnimator.SetInteger(AttackTypeId, 0);
            if (zombieAnimator.isActiveAndEnabled)
            {
                zombieAnimator.Update(0f);
            }
        }

        private void RequestAnimation(AnimationState state)
        {
            requestedAnimationState = state;

            if (CanControlAnimator())
            {
                zombieAnimator.SetInteger(StateId, (int)state);
            }
        }

        private bool CanControlAnimator()
        {
            return playbackReader != null && playbackReader.CanRead(0);
        }

        private int GetRequestedStateId()
        {
            switch (requestedAnimationState)
            {
                case AnimationState.Alert:
                    return AlertStateId;
                case AnimationState.Hit:
                    return HitStateId;
                case AnimationState.Dead:
                    return DeadStateId;
                default:
                    return 0;
            }
        }

        private static bool IsAttackState(int stateHash)
        {
            return stateHash == SwingAttackStateId ||
                stateHash == KickAttackStateId ||
                stateHash == UpDownAttackStateId;
        }

        private void FindZombieAnimator()
        {
            if (zombieAnimator == null)
            {
                zombieAnimator = GetComponentInChildren<Animator>(true);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            FindZombieAnimator();
        }
#endif
    }
}
