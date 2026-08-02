using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    // 상태 머신의 명령을 Animator 상태와 검 표시로 바꾼다.
    public sealed class DemonSwordsmanAnimationController :MonoBehaviour, IDemonSwordsmanAnimation
    {
        private const float MoveDampTime = 0.1f; // 시간 설정
        private const float LocomotionFadeTime = 0.15f; // 시간 설정
        private const float TurnFadeTime = 0.1f; // 시간 설정
        private const float HitFadeTime = 0.06f; // 피격 또는 피해 관련 값
        private const float PhaseFadeTime = 0.11f; // 시간 설정
        private const float DeathFadeTime = 0.11f; // 시간 설정

        private static readonly int MoveForwardHash = // 이동 정보
            Animator.StringToHash("MoveForward");
        private static readonly int MoveSideHash = // 이동 정보
            Animator.StringToHash("MoveSide");
        private static readonly int MoveAmountHash = // 이동 정보
            Animator.StringToHash("MoveAmount");
        private static readonly int ActionHash = // 현재 행동 상태
            Animator.StringToHash("Action");
        private static readonly int StyleHash = // 현재 행동 상태
            Animator.StringToHash("Style");
        private static readonly int AttackKindHash = // 공격 관련 설정 또는 상태
            Animator.StringToHash("AttackKind");

        private static readonly int SwordLocomotionHash = // 내부에서 사용하는 값
            Animator.StringToHash("Base Layer.Sword.SwordLocomotion");
        private static readonly int BeastLocomotionHash = // 내부에서 사용하는 값
            Animator.StringToHash("Base Layer.Beast.BeastLocomotion");
        private static readonly int SwordTurnLeftHash = // 내부에서 사용하는 값
            Animator.StringToHash("Base Layer.Sword.SwordTurnLeft");
        private static readonly int SwordTurnRightHash = // 내부에서 사용하는 값
            Animator.StringToHash("Base Layer.Sword.SwordTurnRight");
        private static readonly int SwordHitHash = // 피격 또는 피해 관련 값
            Animator.StringToHash("Base Layer.Sword.SwordHit");
        private static readonly int BeastHitHash = // 피격 또는 피해 관련 값
            Animator.StringToHash("Base Layer.Beast.BeastHit");
        private static readonly int PhaseFearHash = // 현재 행동 상태
            Animator.StringToHash("Base Layer.PhaseChangeFear");
        private static readonly int PhaseRageHash = // 현재 행동 상태
            Animator.StringToHash("Base Layer.PhaseChangeRage");
        private static readonly int StyleToSwordHash = // 현재 행동 상태
            Animator.StringToHash("Base Layer.StyleChangeToSword");
        private static readonly int StyleToBeastHash = // 현재 행동 상태
            Animator.StringToHash("Base Layer.StyleChangeToBeast");
        private static readonly int SwordDeathHash = // 내부에서 사용하는 값
            Animator.StringToHash("Base Layer.SwordDeath");
        private static readonly int BeastDeathHash = // 내부에서 사용하는 값
            Animator.StringToHash("Base Layer.BeastDeath");

        [SerializeField] private Animator bossAnimator; // 애니메이터 참조
        [SerializeField] private GameObject handSword; // 씬 또는 시스템 참조
        [SerializeField] private GameObject beltSword; // 씬 또는 시스템 참조

        private int lastPlayedStateHash; // 현재 행동 상태

        private void Awake()
        {
            if (bossAnimator == null)
            {
                bossAnimator = GetComponent<Animator>();
            }

            bossAnimator.applyRootMotion = true;
        }

        public void ResetAnimation(DemonSwordsmanStyle style)
        {
            lastPlayedStateHash = 0;
            ShowStyle(style);

            if (!CanAnimate())
            {
                return;
            }

            bossAnimator.speed = 1f;
            bossAnimator.Rebind();
            bossAnimator.Update(0f);
            SetMovement(0f, 0f, 0f, 0f);
            PlayLocomotion(style, 0f);
        }

        public void SetMovement(
            float moveForward,
            float moveSide,
            float moveAmount,
            float deltaTime)
        {
            if (!CanAnimate())
            {
                return;
            }

            bossAnimator.SetFloat(
                MoveForwardHash,
                moveForward,
                MoveDampTime,
                deltaTime);
            bossAnimator.SetFloat(
                MoveSideHash,
                moveSide,
                MoveDampTime,
                deltaTime);
            bossAnimator.SetFloat(
                MoveAmountHash,
                moveAmount,
                MoveDampTime,
                deltaTime);
        }

        public void PlayLocomotion(
            DemonSwordsmanStyle style,
            float crossFadeTime)
        {
            SetAnimationRequest(BossAnimationAction.Locomotion, style);

            int stateHash = style == DemonSwordsmanStyle.Sword
                ? SwordLocomotionHash
                : BeastLocomotionHash;
            PlayState(stateHash, Mathf.Max(0f, crossFadeTime));
        }

        public void PlayTurn(bool turnLeft)
        {
            SetAnimationRequest(
                turnLeft
                    ? BossAnimationAction.TurnLeft
                    : BossAnimationAction.TurnRight,
                DemonSwordsmanStyle.Sword);
            PlayState(
                turnLeft ? SwordTurnLeftHash : SwordTurnRightHash,
                TurnFadeTime);
        }

        public void PlayAttack(DemonSwordsmanAttackPattern attack)
        {
            if (attack == null || !CanAnimate())
            {
                return;
            }

            SetAnimationRequest(
                BossAnimationAction.Attack,
                attack.Style,
                attack.Kind);
            bossAnimator.speed = Mathf.Min(1.12f, attack.AnimationSpeed);
            PlayState(attack.AnimatorStateHash, attack.CrossFadeTime);
        }

        public void PlayPhaseFear()
        {
            if (!CanAnimate())
            {
                return;
            }

            SetAnimationAction(BossAnimationAction.PhaseFear);
            bossAnimator.speed = 1f;
            PlayState(PhaseFearHash, PhaseFadeTime);
        }

        public void PlayPhaseRage()
        {
            if (!CanAnimate())
            {
                return;
            }

            SetAnimationAction(BossAnimationAction.PhaseRage);
            bossAnimator.speed = 1f;
            PlayState(PhaseRageHash, PhaseFadeTime);
        }

        public void PlayStyleChange(DemonSwordsmanStyle nextStyle)
        {
            if (!CanAnimate())
            {
                return;
            }

            SetAnimationRequest(BossAnimationAction.StyleChange, nextStyle);
            bossAnimator.speed = 1f;
            PlayState(
                nextStyle == DemonSwordsmanStyle.Sword
                    ? StyleToSwordHash
                    : StyleToBeastHash,
                LocomotionFadeTime);
        }

        public void ShowStyle(DemonSwordsmanStyle style)
        {
            bool showHandSword = style == DemonSwordsmanStyle.Sword;

            if (handSword != null)
            {
                handSword.SetActive(showHandSword);
            }

            if (beltSword != null)
            {
                beltSword.SetActive(!showHandSword);
            }
        }

        public void SetAnimationSpeed(float speed)
        {
            if (bossAnimator != null)
            {
                bossAnimator.speed = Mathf.Clamp(speed, 0.5f, 1.12f);
            }
        }

        internal void Configure(
            Animator animator,
            GameObject handSwordObject,
            GameObject beltSwordObject)
        {
            bossAnimator = animator;
            handSword = handSwordObject;
            beltSword = beltSwordObject;
        }

        private void PlayState(int stateHash, float crossFadeTime)
        {
            if (!CanAnimate() ||
                stateHash == 0 ||
                lastPlayedStateHash == stateHash ||
                !bossAnimator.HasState(0, stateHash))
            {
                return;
            }

            lastPlayedStateHash = stateHash;

            if (crossFadeTime <= 0f)
            {
                bossAnimator.Play(stateHash, 0, 0f);
                return;
            }

            bossAnimator.CrossFadeInFixedTime(
                stateHash,
                crossFadeTime,
                0,
                0f);
        }

        private void SetAnimationRequest(
            BossAnimationAction action,
            DemonSwordsmanStyle style,
            DemonSwordsmanAttackKind attackKind = 0)
        {
            if (!CanAnimate())
            {
                return;
            }

            bossAnimator.SetInteger(StyleHash, (int)style);

            if (action == BossAnimationAction.Attack)
            {
                bossAnimator.SetInteger(AttackKindHash, (int)attackKind);
            }

            bossAnimator.SetInteger(ActionHash, (int)action);
        }

        private void SetAnimationAction(BossAnimationAction action)
        {
            if (CanAnimate())
            {
                bossAnimator.SetInteger(ActionHash, (int)action);
            }
        }

        private bool CanAnimate()
        {
            return bossAnimator != null &&
                bossAnimator.isActiveAndEnabled &&
                bossAnimator.runtimeAnimatorController != null;
        }

        private enum BossAnimationAction
        {
            Locomotion,
            TurnLeft,
            TurnRight,
            Attack,
            Hit,
            PhaseFear,
            PhaseRage,
            StyleChange,
            Death
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (bossAnimator == null)
            {
                bossAnimator = GetComponent<Animator>();
            }
        }
#endif
    }
}
