using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    // 생명주기, 피해 입력과 행동 상태 전환만 조정한다.
    public sealed class DemonSwordsmanStateMachine
    {
        private const uint DefaultRandomSeed = 0x9E3779B9u; // 내부에서 사용하는 값

        private readonly DemonSwordsmanBossSettings settings; // 행동 설정 참조
        private readonly IDemonSwordsmanTarget target; // 대상 참조
        private readonly IDemonSwordsmanMovement movement; // 이동 정보
        private readonly IDemonSwordsmanAnimation animation; // 내부에서 사용하는 값
        private readonly IDemonSwordsmanCombatOutput combatOutput; // 내부에서 사용하는 값
        private readonly DemonSwordsmanAttackChooser attackChooser; // 공격 관련 설정 또는 상태
        private readonly DemonSwordsmanMoveState moveState; // 이동 정보
        private readonly DemonSwordsmanAttackState attackState; // 공격 관련 설정 또는 상태
        private readonly DemonSwordsmanStyleChangeState styleChangeState; // 현재 행동 상태
        private readonly DemonSwordsmanPhaseChangeState phaseChangeState; // 현재 행동 상태
        private static readonly string NoAttackName = // 공격 관련 설정 또는 상태
            string.Concat((char)0xC5C6, (char)0xC74C);

        private UnitHealth health; // 씬 또는 시스템 참조
        private IDemonSwordsmanState currentState; // 현재 행동 상태
        private DemonSwordsmanPhase currentPhase; // 현재 페이즈
        private DemonSwordsmanStyle currentStyle; // 현재 자세
        private bool isEnabled; // 기능 사용 여부
        private bool phaseChangeCompleted; // 기능 사용 여부
        private float currentTime; // 시간 설정
        private float stateTime; // 상태 진행 시간
        private int styleActionsRemaining; // 현재 행동 상태
        private uint randomState; // 현재 행동 상태

        public DemonSwordsmanPhase CurrentPhase => currentPhase; // 현재 페이즈
        public DemonSwordsmanStyle CurrentStyle => currentStyle; // 현재 자세
        public string CurrentStateName => // 현재 상태 이름
            currentState != null
                ? currentState.Name
                : nameof(DemonSwordsmanActionState.Disabled);
        public string CurrentAttackName => // 현재 공격 이름
            currentState == attackState
                ? attackState.CurrentAttackName
                : NoAttackName;
        public float HealthRatio => // 체력 비율
            health == null ? 0f : health.CurrentHealth / health.MaxHealth;
        public bool IsPhaseChanging => currentState == phaseChangeState; // 기능 사용 여부

        internal DemonSwordsmanBossSettings Settings => settings; // 행동 설정 참조
        internal IDemonSwordsmanTarget Target => target; // 대상 참조
        internal IDemonSwordsmanMovement Movement => movement; // 이동 정보
        internal IDemonSwordsmanAnimation Animation => animation; // 외부에 제공하는 읽기 값
        internal DemonSwordsmanPhase Phase => currentPhase; // 현재 페이즈
        internal DemonSwordsmanStyle Style => currentStyle; // 현재 자세
        internal float StateTime => stateTime; // 상태 진행 시간

        internal DemonSwordsmanStateMachine(
            DemonSwordsmanBossSettings settings,
            IDemonSwordsmanTarget target,
            IDemonSwordsmanMovement movement,
            IDemonSwordsmanAnimation animation,
            IDemonSwordsmanCombatOutput combatOutput = null)
        {
            this.settings = settings;
            this.target = target;
            this.movement = movement;
            this.animation = animation;
            this.combatOutput = combatOutput;
            attackChooser = new DemonSwordsmanAttackChooser(settings.Attacks);
            moveState = new DemonSwordsmanMoveState(this);
            attackState = new DemonSwordsmanAttackState(this);
            styleChangeState = new DemonSwordsmanStyleChangeState(this);
            phaseChangeState = new DemonSwordsmanPhaseChangeState(this);
        }

        internal void SetHealth(UnitHealth unitHealth)
        {
            health = unitHealth;
        }

        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            isEnabled = true;
            currentState = null;
            currentPhase = DemonSwordsmanPhase.PhaseOne;
            currentStyle = DemonSwordsmanStyle.Sword;
            phaseChangeCompleted = false;
            currentTime = 0f;
            stateTime = 0f;
            styleActionsRemaining = 2;
            randomState = DefaultRandomSeed;

            attackChooser.Reset();
            moveState.Reset();
            attackState.Reset();
            movement.ResetMovement();
            animation.ResetAnimation(currentStyle);
            ChangeToMove(DemonSwordsmanMoveMode.Idle);
        }

        public void Update(float deltaTime)
        {
            if (!isEnabled ||
                health == null ||
                currentState == null ||
                deltaTime <= 0f)
            {
                return;
            }

            currentTime += deltaTime;
            stateTime += deltaTime;

            currentState.Update(deltaTime);
        }

        public void Disable()
        {
            if (!isEnabled)
            {
                return;
            }

            currentState?.Exit();
            movement.SetAttackRootMove(false, 1f);
            movement.ResetMovement();
            animation.SetAnimationSpeed(1f);
            animation.ResetAnimation(DemonSwordsmanStyle.Sword);
            isEnabled = false;
            currentState = null;
            attackState.Reset();
        }

        internal void ChangeToMove(DemonSwordsmanMoveMode moveMode)
        {
            moveState.Prepare(moveMode);
            ChangeState(moveState);
        }

        internal bool TryChangeToAttack(
            float distance,
            float absoluteAngle)
        {
            DemonSwordsmanAttackPattern attack = attackChooser.Choose(
                currentPhase,
                currentStyle,
                distance,
                absoluteAngle,
                currentTime,
                NextRandomValue());

            if (attack == null)
            {
                return false;
            }

            attackChooser.MarkUsed(attack, currentTime);
            attackState.Prepare(attack);
            ChangeState(attackState);
            return true;
        }

        internal void ChangeToFollowUp(
            DemonSwordsmanAttackPattern attack)
        {
            if (attack == null)
            {
                ChangeToMove(DemonSwordsmanMoveMode.Circle);
                return;
            }

            currentState?.Exit();
            attackState.PrepareFollowUp(attack);
            currentState = attackState;
            stateTime = 0f;
            currentState.Enter();
        }

        internal DemonSwordsmanAttackPattern FindAttack(
            DemonSwordsmanAttackKind kind)
        {
            DemonSwordsmanAttackPattern[] attacks = settings.Attacks;

            for (int index = 0; index < attacks.Length; index++)
            {
                DemonSwordsmanAttackPattern attack = attacks[index];
                if (attack.Kind == kind &&
                    attack.Style == currentStyle)
                {
                    return attack;
                }
            }

            return null;
        }

        public void OpenBranchWindow()
        {
            if (currentState == attackState)
            {
                attackState.OpenBranchWindow();
            }
        }

        public void SwapWeapon()
        {
            if (currentState == phaseChangeState ||
                currentState == styleChangeState)
            {
                combatOutput?.SwapWeapon();
            }
        }

        public void FinishAction()
        {
            if (currentState == attackState)
            {
                attackState.FinishFromAnimation();
            }
        }

        internal void ChangeToStyleChange()
        {
            ChangeState(styleChangeState);
        }

        internal void ChangeToPhaseChange()
        {
            if (phaseChangeCompleted ||
                currentState == phaseChangeState)
            {
                return;
            }

            ChangeState(phaseChangeState);
        }

        internal void BeginPhaseChange()
        {
        }

        internal void CompletePhaseChange()
        {
            phaseChangeCompleted = true;
            currentPhase = DemonSwordsmanPhase.PhaseTwo;
            currentStyle = DemonSwordsmanStyle.Beast;
            styleActionsRemaining = 2;
        }

        internal void ChangeStyle(DemonSwordsmanStyle nextStyle)
        {
            currentStyle = nextStyle;
        }

        internal void ResetStyleActionCount()
        {
            styleActionsRemaining =
                2 + (NextRandomValue() >= 0.5f ? 1 : 0);
        }

        internal int UseStyleAction()
        {
            styleActionsRemaining--;
            return styleActionsRemaining;
        }

        internal float GetCurrentMoveSpeed()
        {
            return settings.PhaseOneMoveSpeed *
                GetPhaseMoveMultiplier();
        }

        internal float GetPhaseMoveMultiplier()
        {
            return currentPhase == DemonSwordsmanPhase.PhaseTwo
                ? settings.PhaseTwoMoveMultiplier
                : 1f;
        }

        internal float GetCurrentTurnSpeed()
        {
            return settings.PhaseOneTurnSpeed *
                (currentPhase == DemonSwordsmanPhase.PhaseTwo
                    ? settings.PhaseTwoTurnMultiplier
                    : 1f);
        }

        internal bool TryGetTargetInfo(
            out Vector3 targetPosition,
            out float distance,
            out float signedAngle)
        {
            targetPosition = default;
            distance = float.MaxValue;
            signedAngle = 0f;

            if (!target.HasTarget)
            {
                return false;
            }

            targetPosition = target.Position;
            Vector3 toTarget = targetPosition - movement.Position;
            toTarget.y = 0f;
            distance = toTarget.magnitude;
            signedAngle =
                movement.GetSignedTargetAngle(targetPosition);
            return true;
        }

        internal void UpdateMoveAnimation(float deltaTime)
        {
            animation.SetMovement(
                movement.MoveForward,
                movement.MoveSide,
                movement.MoveAmount,
                deltaTime);
        }

        private void ChangeState(IDemonSwordsmanState nextState)
        {
            currentState?.Exit();
            currentState = nextState;
            stateTime = 0f;
            currentState.Enter();
        }

        private float NextRandomValue()
        {
            randomState ^= randomState << 13;
            randomState ^= randomState >> 17;
            randomState ^= randomState << 5;
            return (randomState & 0x00FFFFFFu) / 16777216f;
        }
    }
}
