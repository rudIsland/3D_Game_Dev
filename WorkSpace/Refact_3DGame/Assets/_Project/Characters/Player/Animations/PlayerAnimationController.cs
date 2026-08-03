using rudIsland.RPG3D.Animation;
using UnityEngine;

namespace rudIsland.RPG3D.Player.Animations
{
    // 플레이어 Animator의 파라미터와 재생 시간을 한곳에서 관리한다.
    public sealed class PlayerAnimationController
    {
        private static readonly int MoveAmountId = Animator.StringToHash("MoveAmount"); // 이동 정보
        private static readonly int InputDirXId = Animator.StringToHash("InputDirX"); // 이동 정보
        private static readonly int InputDirYId = Animator.StringToHash("InputDirY"); // 이동 정보
        private static readonly int IsSprintingId = Animator.StringToHash("IsSprinting"); // 기능 사용 여부
        private static readonly int BlockMoveXId = Animator.StringToHash("BlockMoveX"); // 이동 정보
        private static readonly int BlockMoveYId = Animator.StringToHash("BlockMoveY"); // 이동 정보
        private static readonly int RollId = Animator.StringToHash("Roll"); // 내부에서 사용하는 값
        private static readonly int SprintRollId = Animator.StringToHash("SprintRoll"); // 내부에서 사용하는 값
        private static readonly int IsBlockingId = Animator.StringToHash("IsBlocking"); // 기능 사용 여부
        private static readonly int AttackId = Animator.StringToHash("Attack"); // 공격 관련 설정 또는 상태
        private static readonly int AttackIndexId = Animator.StringToHash("AttackIndex"); // 공격 관련 설정 또는 상태
        private static readonly int HitId = Animator.StringToHash("Hit"); // 피격 또는 피해 관련 값
        private static readonly int DeathId = Animator.StringToHash("Death"); // 내부에서 사용하는 값
        private static readonly int PlayerHitStateId = // 피격 또는 피해 관련 값
            Animator.StringToHash("PlayerHit");
        private static readonly int PlayerHitFullPathId = // 피격 또는 피해 관련 값
            Animator.StringToHash("Base Layer.PlayerHit");
        private static readonly int PlayerRollStateId = // 현재 행동 상태
            Animator.StringToHash("Base Layer.Movement.PlayerRoll");
        private static readonly int PlayerSprintRollStateId = // 현재 행동 상태
            Animator.StringToHash("Base Layer.Movement.PlayerSprintRoll");
        private static readonly int PlayerAttack01StateId = Animator.StringToHash("PlayerAttack01"); // 공격 관련 설정 또는 상태
        private static readonly int PlayerAttack02StateId = Animator.StringToHash("PlayerAttack02"); // 공격 관련 설정 또는 상태
        private static readonly int PlayerAttack03StateId = Animator.StringToHash("PlayerAttack03"); // 공격 관련 설정 또는 상태
        private static readonly int PlayerAttack04StateId = Animator.StringToHash("PlayerAttack04"); // 공격 관련 설정 또는 상태
        private static readonly int PlayerAttack05StateId = Animator.StringToHash("PlayerAttack05"); // 공격 관련 설정 또는 상태
        private static readonly int PlayerRunAttackStateId = Animator.StringToHash("PlayerRunAttack"); // 공격 관련 설정 또는 상태

        private readonly Animator playerAnimator; // 애니메이터 참조
        private readonly AnimatorPlaybackReader playbackReader; // 씬 또는 시스템 참조
        private readonly float smoothTime; // 시간 설정

        public PlayerAnimationController(Animator playerAnimator, float smoothTime)
        {
            this.playerAnimator = playerAnimator;
            playbackReader = new AnimatorPlaybackReader(playerAnimator);
            this.smoothTime = smoothTime;
        }

        public void Reset()
        {
            if (playerAnimator == null)
            {
                return;
            }

            playerAnimator.SetBool(IsBlockingId, false);
            playerAnimator.SetFloat(MoveAmountId, 0f);
            playerAnimator.SetFloat(InputDirXId, 0f);
            playerAnimator.SetFloat(InputDirYId, 0f);
            playerAnimator.SetBool(IsSprintingId, false);
            playerAnimator.SetFloat(BlockMoveXId, 0f);
            playerAnimator.SetFloat(BlockMoveYId, 0f);
            playerAnimator.ResetTrigger(RollId);
            playerAnimator.ResetTrigger(SprintRollId);
            playerAnimator.ResetTrigger(AttackId);
            playerAnimator.ResetTrigger(HitId);
            playerAnimator.ResetTrigger(DeathId);
            playerAnimator.SetInteger(AttackIndexId, 0);
        }

        public void UpdateMove(Vector2 moveInput, bool isSprinting, float deltaTime)
        {
            if (playerAnimator == null)
            {
                return;
            }

            float inputAmount = Mathf.Clamp01(moveInput.magnitude);
            float moveAmount = inputAmount < 0.01f
                ? 0f
                : isSprinting ? inputAmount : inputAmount * 0.5090909f;
            playerAnimator.SetFloat(MoveAmountId, moveAmount, smoothTime, deltaTime);
            playerAnimator.SetFloat(InputDirXId, moveInput.x, smoothTime, deltaTime);
            playerAnimator.SetFloat(InputDirYId, moveInput.y, smoothTime, deltaTime);
            playerAnimator.SetBool(IsSprintingId, isSprinting);
        }

        public void UpdateLocomotion(
            Vector2 localMoveInput,
            bool isSprinting,
            float deltaTime)
        {
            if (playerAnimator == null)
            {
                return;
            }

            Vector2 moveInput = Vector2.ClampMagnitude(localMoveInput, 1f);
            float inputAmount = Mathf.Clamp01(moveInput.magnitude);
            float moveAmount = inputAmount < 0.01f
                ? 0f
                : isSprinting ? inputAmount : inputAmount * 0.5090909f;
            playerAnimator.SetFloat(MoveAmountId, moveAmount, smoothTime, deltaTime);
            playerAnimator.SetFloat(InputDirXId, moveInput.x, smoothTime, deltaTime);
            playerAnimator.SetFloat(InputDirYId, moveInput.y, smoothTime, deltaTime);
            playerAnimator.SetBool(IsSprintingId, isSprinting);
        }

        public void UpdateBlockMove(Vector2 moveInput, float deltaTime)
        {
            if (playerAnimator == null)
            {
                return;
            }

            playerAnimator.SetFloat(BlockMoveXId, moveInput.x, smoothTime, deltaTime);
            playerAnimator.SetFloat(BlockMoveYId, Mathf.Max(0f, moveInput.y), smoothTime, deltaTime);
        }

        public void StopMove()
        {
            playerAnimator?.SetFloat(MoveAmountId, 0f);
        }

        public void SetBlocking(bool isBlocking)
        {
            playerAnimator?.SetBool(IsBlockingId, isBlocking);
        }

        public void PlayRoll(
            Vector2 rollInput,
            bool usesSprintRoll,
            bool startsAfterAttackCancel)
        {
            if (playerAnimator == null)
            {
                return;
            }

            playerAnimator.SetFloat(InputDirXId, rollInput.x);
            playerAnimator.SetFloat(InputDirYId, rollInput.y);
            playerAnimator.SetBool(IsBlockingId, false);
            playerAnimator.SetFloat(MoveAmountId, 0f);
            playerAnimator.ResetTrigger(RollId);
            playerAnimator.ResetTrigger(SprintRollId);

            if (startsAfterAttackCancel)
            {
                playerAnimator.ResetTrigger(AttackId);
                playerAnimator.SetInteger(AttackIndexId, 0);
                playerAnimator.SetTrigger(RollId);
                return;
            }

            playerAnimator.SetTrigger(usesSprintRoll ? SprintRollId : RollId);
        }

        public void PlayAttack(int attackNumber)
        {
            if (playerAnimator == null)
            {
                return;
            }

            playerAnimator.SetBool(IsBlockingId, false);
            playerAnimator.SetFloat(MoveAmountId, 0f);
            playerAnimator.ResetTrigger(AttackId);
            playerAnimator.SetInteger(AttackIndexId, attackNumber);
            playerAnimator.SetTrigger(AttackId);
        }

        public void PlayDeath()
        {
            if (playerAnimator == null)
            {
                return;
            }

            playerAnimator.SetBool(IsBlockingId, false);
            playerAnimator.SetFloat(MoveAmountId, 0f);
            playerAnimator.SetFloat(BlockMoveXId, 0f);
            playerAnimator.SetFloat(BlockMoveYId, 0f);
            playerAnimator.ResetTrigger(RollId);
            playerAnimator.ResetTrigger(SprintRollId);
            playerAnimator.ResetTrigger(AttackId);
            playerAnimator.ResetTrigger(HitId);
            playerAnimator.SetInteger(AttackIndexId, 0);
            playerAnimator.ResetTrigger(DeathId);
            playerAnimator.SetTrigger(DeathId);
        }

        public bool TryGetRollTime(out float normalizedTime)
        {
            if (playbackReader.TryGetCurrentFullPathStateTime(
                    0,
                    PlayerRollStateId,
                    out normalizedTime) ||
                playbackReader.TryGetCurrentFullPathStateTime(
                    0,
                    PlayerSprintRollStateId,
                    out normalizedTime))
            {
                return true;
            }

            normalizedTime = 0f;
            return false;
        }

        public void PlayHitFromStart()
        {
            if (playerAnimator == null)
            {
                return;
            }

            playerAnimator.SetBool(IsBlockingId, false);
            playerAnimator.SetFloat(MoveAmountId, 0f);
            playerAnimator.ResetTrigger(RollId);
            playerAnimator.ResetTrigger(SprintRollId);
            playerAnimator.ResetTrigger(AttackId);
            playerAnimator.SetInteger(AttackIndexId, 0);

            bool isPlayingHit = playbackReader.IsCurrentState(0, PlayerHitStateId);
            bool isChangingToHit = playbackReader.IsChangingTo(0, PlayerHitStateId);

            if (isPlayingHit || isChangingToHit)
            {
                playerAnimator.Play(PlayerHitFullPathId, 0, 0f);
                return;
            }

            playerAnimator.ResetTrigger(HitId);
            playerAnimator.SetTrigger(HitId);
        }

        public bool TryGetAttackTime(out float normalizedTime)
        {
            normalizedTime = 0f;
            if (!playbackReader.TryGetCurrentState(
                    0,
                    out AnimatorStateInfo stateInfo) ||
                !IsAttackState(stateInfo.shortNameHash))
            {
                return false;
            }

            normalizedTime = stateInfo.normalizedTime;
            return true;
        }

        public bool IsPlayingAttack(int attackNumber)
        {
            return playbackReader.IsCurrentState(
                0,
                GetAttackStateId(attackNumber));
        }

        public bool TryGetHitTime(out float normalizedTime)
        {
            return playbackReader.TryGetCurrentStateTime(
                0,
                PlayerHitStateId,
                out normalizedTime);
        }

        public bool IsChangingAttackState()
        {
            if (!playbackReader.IsInTransition(0))
            {
                return false;
            }

            return playbackReader.IsCurrentOrNextState(0, PlayerAttack01StateId) ||
                playbackReader.IsCurrentOrNextState(0, PlayerAttack02StateId) ||
                playbackReader.IsCurrentOrNextState(0, PlayerAttack03StateId) ||
                playbackReader.IsCurrentOrNextState(0, PlayerAttack04StateId) ||
                playbackReader.IsCurrentOrNextState(0, PlayerAttack05StateId) ||
                playbackReader.IsCurrentOrNextState(0, PlayerRunAttackStateId);
        }

        private static bool IsAttackState(int stateId)
        {
            return stateId == PlayerAttack01StateId || stateId == PlayerAttack02StateId ||
                stateId == PlayerAttack03StateId || stateId == PlayerAttack04StateId ||
                stateId == PlayerAttack05StateId || stateId == PlayerRunAttackStateId;
        }

        private static int GetAttackStateId(int attackNumber)
        {
            switch (attackNumber)
            {
                case 1:
                    return PlayerAttack01StateId;
                case 2:
                    return PlayerAttack02StateId;
                case 3:
                    return PlayerAttack03StateId;
                case 4:
                    return PlayerAttack04StateId;
                case 5:
                    return PlayerAttack05StateId;
                case 6:
                    return PlayerRunAttackStateId;
                default:
                    return 0;
            }
        }
    }
}
