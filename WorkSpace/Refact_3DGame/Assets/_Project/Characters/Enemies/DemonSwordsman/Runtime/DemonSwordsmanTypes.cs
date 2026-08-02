using System;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    public enum DemonSwordsmanPhase // 보스 전투가 현재 몇 번째 단계인지 나타낸다.
    {
        PhaseOne = 1, // 전투 시작 단계다.
        PhaseTwo = 2 // 체력이 줄어든 뒤 사용하는 강화 단계다.
    }

    public enum DemonSwordsmanStyle // 보스가 검과 맨손 중 어떤 전투 자세를 사용하는지 나타낸다.
    {
        Sword, // 검을 들고 싸우는 자세다.
        Beast // 검을 넣고 맨손으로 싸우는 자세다.
    }

    [Flags]
    public enum DemonSwordsmanPhaseMask // 공격을 사용할 수 있는 페이즈를 여러 개 지정한다.
    {
        None = 0, // 사용할 수 있는 페이즈가 없다.
        PhaseOne = 1, // 1페이즈에서만 사용한다.
        PhaseTwo = 2, // 2페이즈에서만 사용한다.
        Both = PhaseOne | PhaseTwo // 모든 페이즈에서 사용한다.
    }

    public enum DemonSwordsmanAttackKind // 상태머신이 선택할 수 있는 보스 공격 종류다.
    {
        QuickSlash, // 빠르게 한 번 벤다.
        SwordCombo, // 검으로 연속 공격한다.
        HeavySlash, // 느리지만 강하게 벤다.
        ChaseSlash, // 앞으로 따라가며 벤다.
        JumpSlash, // 뛰어들며 공격한다.
        BeastCombo, // 맨손으로 연속 공격한다.
        BeastSlam, // 맨손으로 내려찍는다.
        BeastRush, // 맨손 자세로 돌진한다.
        BeastWideAttack // 넓은 범위를 맨손으로 공격한다.
    }

    internal enum DemonSwordsmanActionState // 상태머신이 현재 수행 중인 행동을 나타낸다.
    {
        Disabled, // 상태머신이 꺼진 상태다.
        Idle, // 제자리에서 대기한다.
        Notice, // 플레이어를 발견하고 방향을 맞춘다.
        Approach, // 플레이어에게 접근한다.
        Circle, // 플레이어 주변을 옆으로 돈다.
        BackAway, // 플레이어에게서 뒤로 물러난다.
        Attack, // 선택한 공격을 실행한다.
        StyleChange, // 검과 맨손 자세를 바꾼다.
        PhaseChange, // 1페이즈에서 2페이즈로 변경한다.
    }

    internal interface IDemonSwordsmanTarget // 보스가 추적할 대상에게서 필요한 값만 제공한다.
    {
        bool HasTarget { get; } // 현재 추적할 대상이 있는지 알려준다.
        Vector3 Position { get; } // 추적 대상의 현재 월드 위치다.
    }

    internal interface IDemonSwordsmanCombatOutput // 전투 상태가 화면의 무기 표시 변경을 요청하는 통로다.
    {
        void SwapWeapon(); // 손의 검과 허리의 검 표시를 서로 바꾼다.
    }

    internal interface IDemonSwordsmanMovement // 상태머신의 이동 명령을 실제 보스 이동으로 바꾸는 통로다.
    {
        Vector3 Position { get; } // 보스의 현재 월드 위치다.
        float MoveForward { get; } // Animator에 전달할 앞뒤 이동 값이다.
        float MoveSide { get; } // Animator에 전달할 좌우 이동 값이다.
        float MoveAmount { get; } // Animator에 전달할 전체 이동 세기다.

        void ResetMovement(); // 남아 있는 이동 값과 공격 이동을 초기화한다.
        void MoveTo(
            Vector3 targetPosition,
            float moveSpeed,
            float turnSpeed,
            float deltaTime); // 대상을 바라보며 지정한 속도로 접근한다.
        void CircleAround(
            Vector3 targetPosition,
            float moveSpeed,
            float preferredDistance,
            float sideDirection,
            float turnSpeed,
            float deltaTime); // 지정한 거리를 유지하며 대상 주변을 옆으로 돈다.
        void BackAwayFrom(
            Vector3 targetPosition,
            float moveSpeed,
            float turnSpeed,
            float deltaTime); // 대상을 바라보며 뒤로 물러난다.
        void TurnTo(
            Vector3 targetPosition,
            float turnSpeed,
            float deltaTime); // 이동하지 않고 대상 방향으로 회전한다.
        void StayOnGround(float deltaTime); // 중력을 적용해 보스가 바닥에서 뜨지 않게 한다.
        void Stop(float deltaTime); // 수평 이동을 멈추고 바닥 상태만 유지한다.
        void SetAttackRootMove(bool isEnabled, float moveMultiplier); // 공격 애니메이션의 이동 사용 여부와 세기를 정한다.
        void ApplyAttackAnimationMove(Vector3 animationMove); // 애니메이션에서 계산된 공격 이동을 실제 위치에 적용한다.
        float GetSignedTargetAngle(Vector3 targetPosition); // 보스 정면에서 대상까지의 좌우 각도를 구한다.
    }

    internal interface IDemonSwordsmanAnimation // 상태머신의 재생 요청을 실제 Animator 동작으로 바꾸는 통로다.
    {
        void ResetAnimation(DemonSwordsmanStyle style); // 지정한 전투 자세의 기본 애니메이션으로 초기화한다.
        void SetMovement(
            float moveForward,
            float moveSide,
            float moveAmount,
            float deltaTime); // 이동 Blend Tree에 앞뒤, 좌우, 이동 세기를 전달한다.
        void PlayLocomotion(DemonSwordsmanStyle style, float crossFadeTime); // 현재 자세의 대기와 이동 애니메이션을 재생한다.
        void PlayTurn(bool turnLeft); // 제자리 왼쪽 또는 오른쪽 회전 애니메이션을 재생한다.
        void PlayAttack(DemonSwordsmanAttackPattern attack); // 선택된 공격 설정에 맞는 애니메이션을 재생한다.
        void PlayPhaseFear(); // 페이즈 변경의 두려움 동작을 재생한다.
        void PlayPhaseRage(); // 페이즈 변경의 분노 동작을 재생한다.
        void PlayStyleChange(DemonSwordsmanStyle nextStyle); // 다음 전투 자세로 바꾸는 애니메이션을 재생한다.
        void ShowStyle(DemonSwordsmanStyle style); // 현재 자세에 맞게 손과 허리의 검 표시를 바꾼다.
        void SetAnimationSpeed(float speed); // 현재 Animator의 전체 재생 속도를 바꾼다.
    }
}
