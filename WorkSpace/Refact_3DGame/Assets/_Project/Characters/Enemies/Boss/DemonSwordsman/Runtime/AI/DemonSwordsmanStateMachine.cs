using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    // 생명주기, 피해 입력과 행동 상태 전환만 조정한다.
    public sealed class DemonSwordsmanStateMachine
    {
        private const uint DefaultRandomSeed = 0x9E3779B9u;

        private readonly DemonSwordsmanBossSettings settings;
        private readonly IDemonSwordsmanTarget target;
        private readonly IDemonSwordsmanMovement movement;
        private readonly IDemonSwordsmanAnimation animation;
        private readonly IDemonSwordsmanCombatOutput combatOutput;
        private readonly DemonSwordsmanAttackChooser attackChooser;
        private readonly DemonSwordsmanMoveState moveState;
        private readonly DemonSwordsmanAttackState attackState;
        private readonly DemonSwordsmanStyleChangeState styleChangeState;
        private readonly DemonSwordsmanPhaseChangeState phaseChangeState;
        private static readonly string NoAttackName =
            string.Concat((char)0xC5C6, (char)0xC74C);

        private UnitHealth health;
        private IDemonSwordsmanState currentState;
        private DemonSwordsmanPhase currentPhase;
        private DemonSwordsmanStyle currentStyle;
        private bool isEnabled;
        private bool phaseChangeCompleted;
        private float currentTime;
        private float stateTime;
        private int styleActionsRemaining;
        private uint randomState;

        public DemonSwordsmanPhase CurrentPhase => currentPhase;
        public DemonSwordsmanStyle CurrentStyle => currentStyle;
        public string CurrentStateName =>
            currentState != null
                ? currentState.Name
                : nameof(DemonSwordsmanActionState.Disabled);
        public string CurrentAttackName =>
            currentState == attackState
                ? attackState.CurrentAttackName
                : NoAttackName;
        public float HealthRatio =>
            health == null ? 0f : health.CurrentHealth / health.MaxHealth;
        public bool IsPhaseChanging => currentState == phaseChangeState;

        internal DemonSwordsmanBossSettings Settings => settings;
        internal IDemonSwordsmanTarget Target => target;
        internal IDemonSwordsmanMovement Movement => movement;
        internal IDemonSwordsmanAnimation Animation => animation;
        internal DemonSwordsmanPhase Phase => currentPhase;
        internal DemonSwordsmanStyle Style => currentStyle;
        internal float StateTime => stateTime;

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
