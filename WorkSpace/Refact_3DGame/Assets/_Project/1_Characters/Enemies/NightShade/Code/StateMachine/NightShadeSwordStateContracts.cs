using Characters.Combat;
using UnityEngine;

namespace Characters.Enemies.NightShade
{
    internal enum NightShadeHitSide
    {
        Front = 0,
        Back = 1,
        Left = 2,
        Right = 3
    }

    internal static class NightShadeHitDirection
    {
        internal static NightShadeHitSide GetSide(
            Vector3 forward,
            Vector3 right,
            Vector3 incomingDirection)
        {
            Vector3 attackerDirection = -incomingDirection;
            attackerDirection.y = 0f;
            if (attackerDirection.sqrMagnitude <= 0.000001f)
            {
                return NightShadeHitSide.Front;
            }

            float frontAmount = Vector3.Dot(forward, attackerDirection);
            float rightAmount = Vector3.Dot(right, attackerDirection);
            if (Mathf.Abs(frontAmount) >= Mathf.Abs(rightAmount))
            {
                return frontAmount >= 0f
                    ? NightShadeHitSide.Front
                    : NightShadeHitSide.Back;
            }

            return rightAmount >= 0f
                ? NightShadeHitSide.Right
                : NightShadeHitSide.Left;
        }
    }

    internal interface INightShadeSwordState
    {
        void Enter();
        NightShadeSwordStateId? Update(float deltaTime);
        void Exit();
    }

    internal interface INightShadeSwordMovement
    {
        Vector3 Position { get; }
        Vector3 Forward { get; }

        void Reset();
        void ChaseTarget(Vector3 targetPosition, float deltaTime);
        void WalkToTarget(Vector3 targetPosition, float deltaTime);
        void TurnToTarget(Vector3 targetPosition, float deltaTime);
        void MoveForRecovery(
            Vector3 targetPosition,
            NightShadeCombatMoveType moveType,
            float deltaTime);
        void StayOnGround(float deltaTime);
        void ApplyAttackMovement(
            Vector3 wantedTurnDirection,
            bool canTurn,
            float deltaDistance,
            float deltaTime);
        void ApplyHitMovement(Vector3 horizontalMovement, float deltaTime);
    }

    internal interface INightShadeSwordRandomProvider
    {
        float Next01();
    }

    internal interface INightShadeSwordCombatAction
    {
        // 공격과 Recovery Action의 공통 생명주기다.
        NightShadeSwordActionId ActionId { get; }
        bool IsFinished { get; }

        bool CanStart(out NightShadeSwordActionRejectReason rejectReason);
        bool CanContinue(out NightShadeSwordActionStopReason stopReason);
        NightShadeSwordActionScore GetScore(float randomBonus);
        void Enter();
        void Update(float deltaTime);
        void Exit(NightShadeSwordActionStopReason stopReason);
    }

    internal interface INightShadeSwordAttackAction : INightShadeSwordCombatAction
    {
        // Animation Event는 현재 실행 중인 공격 Action에만 대기열로 전달된다.
        bool ProtectsSmallHit { get; }

        void QueueStopTurn();
        void QueuePlaySound(int hitIndex);
        void QueueOpenHit(int hitIndex);
        void QueueCloseHit();
    }

    internal interface INightShadeSwordAnimation
    {
        void PlayIdle();
        void PlayChase();
        void PlayWalk();
        void PlayCombatMove(NightShadeCombatMoveType moveType);
        void PlayAttack(NightShadeSwordAttackType attackType);
        void PlaySmallHitFromStart(Vector3 incomingDirection);
        void PlayBigHitFromStart(Vector3 incomingDirection);
        void PlayKnockbackFromStart();
        void PlayKnockdownFromStart();
        void PlayGetUpFromStart();
        void PlayStaggerEnterFromStart();
        void PlayStaggerStartFromStart();
        void PlayStaggerIdleFromStart();
        void PlayStaggerEndFromStart();
        void PlayDead();
        void ResetAttackPlaybackSpeed();
        bool TryGetRequestedAnimationTime(out float normalizedTime);
        bool IsTransitioning();
    }
}
