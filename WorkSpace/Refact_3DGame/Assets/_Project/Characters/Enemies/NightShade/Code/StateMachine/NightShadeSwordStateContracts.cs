using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
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
        void PlayHitFromStart();
        void PlayDead();
        void ResetAttackPlaybackSpeed();
        bool TryGetRequestedAnimationTime(out float normalizedTime);
        bool IsTransitioning();
    }
}
