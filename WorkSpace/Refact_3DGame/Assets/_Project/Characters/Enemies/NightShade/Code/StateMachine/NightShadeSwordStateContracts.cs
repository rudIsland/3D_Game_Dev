using rudIsland.RPG3D.Characters.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
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

        void Reset();
        void MoveTo(Vector3 targetPosition,float moveSpeed,float turnSpeed, float deltaTime);
        void TurnTo(Vector3 targetPosition,float turnSpeed,float deltaTime);
        void MoveForCombat(
            Vector3 targetPosition,
            NightShadeCombatMoveType moveType,
            float moveSpeed,
            float turnSpeed,
            float deltaTime);
        void StayOnGround(float deltaTime);
        void ApplyHitMovement(Vector3 horizontalMovement, float deltaTime);
        bool IsFacing(Vector3 targetPosition, float minimumFacingDot);
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
        void PlayDead();
        void ResetAttackPlaybackSpeed();
        bool TryGetRequestedAnimationTime(out float normalizedTime);
        bool IsTransitioning();
    }
}
