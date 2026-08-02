using rudIsland.RPG3D.Animation;
using UnityEngine;

namespace rudIsland.RPG3D.Player.Animations
{
    // 플레이어 Animator의 파라미터와 재생 시간을 한곳에서 관리한다.
    public sealed class PlayerAnimationController
    {
        private static readonly int MoveAmountId = Animator.StringToHash("MoveAmount");
        private static readonly int BlockMoveXId = Animator.StringToHash("BlockMoveX");
        private static readonly int BlockMoveYId = Animator.StringToHash("BlockMoveY");
        private static readonly int RollDirectionXId = Animator.StringToHash("RollDirectionX");
        private static readonly int RollDirectionYId = Animator.StringToHash("RollDirectionY");
        private static readonly int RollId = Animator.StringToHash("Roll");
        private static readonly int SprintRollId = Animator.StringToHash("SprintRoll");
        private static readonly int IsBlockingId = Animator.StringToHash("IsBlocking");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int AttackIndexId = Animator.StringToHash("AttackIndex");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DeathId = Animator.StringToHash("Death");
        private static readonly int PlayerHitStateId =
            Animator.StringToHash("PlayerHit");
        private static readonly int PlayerHitFullPathId =
            Animator.StringToHash("Base Layer.PlayerHit");
        private static readonly int PlayerRollStateId =
            Animator.StringToHash("Base Layer.Movement.PlayerRoll");
        private static readonly int PlayerSprintRollStateId =
            Animator.StringToHash("Base Layer.Movement.PlayerSprintRoll");
        private static readonly int PlayerAttack01StateId = Animator.StringToHash("PlayerAttack01");
        private static readonly int PlayerAttack02StateId = Animator.StringToHash("PlayerAttack02");
        private static readonly int PlayerAttack03StateId = Animator.StringToHash("PlayerAttack03");
        private static readonly int PlayerAttack04StateId = Animator.StringToHash("PlayerAttack04");
        private static readonly int PlayerAttack05StateId = Animator.StringToHash("PlayerAttack05");
        private static readonly int PlayerRunAttackStateId = Animator.StringToHash("PlayerRunAttack");

        private readonly Animator playerAnimator;
        private readonly AnimatorPlaybackReader playbackReader;
        private readonly float smoothTime;

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

            playerAnimator.SetFloat(RollDirectionXId, rollInput.x);
            playerAnimator.SetFloat(RollDirectionYId, rollInput.y);
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
