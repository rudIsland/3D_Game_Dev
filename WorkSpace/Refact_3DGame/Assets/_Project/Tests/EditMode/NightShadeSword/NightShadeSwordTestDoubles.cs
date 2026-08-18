using System;
using System.Collections.Generic;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    internal sealed class NightShadeSwordTestScope : IDisposable
    {
        internal GameObject TargetObject { get; }
        internal FakeNightShadeSwordMovement Movement { get; }
        internal FakeNightShadeSwordAnimation Animation { get; }
        internal FakeUnitDeathState TargetDeathState { get; }
        internal NightShadeSwordTestActions Actions { get; }

        internal NightShadeSwordTestScope(Vector3 targetPosition)
        {
            TargetObject = new GameObject("NightShade Test Target");
            TargetObject.transform.position = targetPosition;
            Movement = new FakeNightShadeSwordMovement();
            Animation = new FakeNightShadeSwordAnimation();
            TargetDeathState = new FakeUnitDeathState();
            Actions = new NightShadeSwordTestActions();
        }

        internal NightShadeSwordSettings CreateSettings(
            float findRange = 10f,
            float attackRange = 4f,
            float walkStartRange = 5f,
            float runStartRange = 6f,
            int attacksBeforeCombatMove = 2,
            float deadBodyKeepTime = 1f)
        {
            return new NightShadeSwordSettings(
                findRange,
                attackRange,
                walkStartRange,
                runStartRange,
                15f,
                1.8f,
                3f,
                360f,
                180f,
                2f,
                2.5f,
                0.7f,
                2.5f,
                3f,
                2f,
                0.6f,
                attacksBeforeCombatMove,
                0.2f,
                AnimationCurve.Linear(0f, 0f, 1f, 1f),
                deadBodyKeepTime);
        }

        internal NightShadeSwordStateMachine CreateStateMachine(NightShadeSwordSettings settings)
        {
            return new NightShadeSwordStateMachine(
                TargetObject.transform,
                TargetDeathState,
                Movement,
                Animation,
                settings,
                Actions.Value);
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(TargetObject);
        }
    }

    internal sealed class FakeNightShadeSwordMovement : INightShadeSwordMovement
    {
        internal int ResetCount { get; private set; }
        internal int MoveToCount { get; private set; }
        internal int TurnToCount { get; private set; }
        internal int StayCount { get; private set; }
        internal int HitMoveCount { get; private set; }
        internal float LastMoveSpeed { get; private set; }
        internal Vector3 LastHitMovement { get; private set; }
        internal NightShadeCombatMoveType LastCombatMoveType { get; private set; }
        internal bool IsFacingTarget { get; set; } = true;

        public Vector3 Position { get; set; }

        public void Reset()
        {
            ResetCount++;
        }

        public void MoveTo(
            Vector3 targetPosition,
            float moveSpeed,
            float turnSpeed,
            float deltaTime)
        {
            MoveToCount++;
            LastMoveSpeed = moveSpeed;
        }

        public void TurnTo(
            Vector3 targetPosition,
            float turnSpeed,
            float deltaTime)
        {
            TurnToCount++;
        }

        public void MoveForCombat(
            Vector3 targetPosition,
            NightShadeCombatMoveType moveType,
            float moveSpeed,
            float turnSpeed,
            float deltaTime)
        {
            LastCombatMoveType = moveType;
        }

        public void StayOnGround(float deltaTime)
        {
            StayCount++;
        }

        public void ApplyHitMovement(Vector3 horizontalMovement, float deltaTime)
        {
            HitMoveCount++;
            LastHitMovement = horizontalMovement;
        }

        public bool IsFacing(Vector3 targetPosition, float minimumFacingDot)
        {
            return IsFacingTarget;
        }
    }

    internal sealed class FakeNightShadeSwordAnimation : INightShadeSwordAnimation
    {
        internal int IdleCount { get; private set; }
        internal int ChaseCount { get; private set; }
        internal int WalkCount { get; private set; }
        internal int CombatMoveCount { get; private set; }
        internal int AttackCount { get; private set; }
        internal int HitCount { get; private set; }
        internal int DeadCount { get; private set; }
        internal int ResetSpeedCount { get; private set; }
        internal NightShadeSwordAttackType LastAttackType { get; private set; }
        internal bool CanReadTime { get; set; } = true;
        internal bool IsInTransition { get; set; }
        internal float NormalizedTime { get; set; }

        public void PlayIdle() => IdleCount++;
        public void PlayChase() => ChaseCount++;
        public void PlayWalk() => WalkCount++;
        public void PlayCombatMove(NightShadeCombatMoveType moveType) => CombatMoveCount++;

        public void PlayAttack(NightShadeSwordAttackType attackType)
        {
            AttackCount++;
            LastAttackType = attackType;
            NormalizedTime = 0f;
        }

        public void PlayHitFromStart()
        {
            HitCount++;
            NormalizedTime = 0f;
        }

        public void PlayDead()
        {
            DeadCount++;
            NormalizedTime = 0f;
        }

        public void ResetAttackPlaybackSpeed() => ResetSpeedCount++;

        public bool TryGetRequestedAnimationTime(out float normalizedTime)
        {
            normalizedTime = NormalizedTime;
            return CanReadTime;
        }

        public bool IsTransitioning() => IsInTransition;
    }

    internal sealed class FakeUnitDeathState : IUnitDeathState
    {
        public bool IsDead { get; set; }
    }

    internal sealed class NightShadeSwordTestActions
    {
        internal NightShadeSwordActions Value { get; }
        internal List<string> Calls { get; } = new List<string>();
        internal int CloseCount { get; private set; }
        internal int ReleaseCount { get; private set; }

        internal NightShadeSwordTestActions()
        {
            Value = new NightShadeSwordActions(
                PlaySound,
                OpenHit,
                CloseHit,
                RequestRelease);
        }

        private void PlaySound(NightShadeSwordAttackType attackType, int hitIndex)
        {
            Calls.Add($"Sound:{hitIndex}");
        }

        private void OpenHit(NightShadeSwordAttackType attackType, int hitIndex)
        {
            Calls.Add($"Open:{hitIndex}");
        }

        private void CloseHit()
        {
            CloseCount++;
            Calls.Add("Close");
        }

        private void RequestRelease()
        {
            ReleaseCount++;
        }
    }
}
