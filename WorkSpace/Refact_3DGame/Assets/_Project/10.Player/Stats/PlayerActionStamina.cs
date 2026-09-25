namespace Player
{
    // 행동별 스태미나 규칙과 달리기 기록을 소유한다. 현재 수치와 변경 알림은 PlayerStamina가 맡는다.
    internal sealed class PlayerActionStamina
    {
        private readonly PlayerInputReader playerInput;
        private readonly PlayerStamina playerStamina;
        private readonly float guardStaminaRecoveryRate;
        private readonly float rollStaminaCost;
        private readonly float sprintStaminaCostPerSecond;
        private readonly float sprintRestartStamina;
        private bool isSprintingThisFrame;
        private bool wasSprintingLastFrame;
        private bool isSprintRecoveryRequired;

        internal bool ShouldStartRunAttack => wasSprintingLastFrame;

        // 기존 입력과 수치 저장소를 공유하고 행동에 필요한 설정만 보관한다.
        internal PlayerActionStamina(
            PlayerInputReader playerInput,
            PlayerStamina playerStamina,
            PlayerCombatRuntimeConfig config)
        {
            this.playerInput = playerInput;
            this.playerStamina = playerStamina;
            guardStaminaRecoveryRate = config.GuardStaminaRecoveryRate;
            rollStaminaCost = config.RollStaminaCost;
            sprintStaminaCostPerSecond = config.SprintStaminaCostPerSecond;
            sprintRestartStamina = config.SprintRestartStamina;
        }

        // 상태 갱신 직전에 지난 프레임의 실제 달리기를 보관한다.
        internal void BeginFrame()
        {
            wasSprintingLastFrame = isSprintingThisFrame;
            isSprintingThisFrame = false;
        }

        // 활성화와 비활성화에서만 달리기 재시작 대기까지 초기화한다.
        internal void ResetSprint()
        {
            isSprintingThisFrame = false;
            wasSprintingLastFrame = false;
            isSprintRecoveryRequired = false;
        }

        // 피격과 사망은 달리기 기록만 지우고 소진 후 재시작 대기는 유지한다.
        internal void StopSprint()
        {
            isSprintingThisFrame = false;
            wasSprintingLastFrame = false;
        }

        // 입력과 재시작 조건을 확인한 뒤 이번 프레임의 비용을 소비한다.
        internal bool TryConsumeSprintStamina(float deltaTime)
        {
            if (!playerInput.IsSprinting || playerInput.MoveValue.sqrMagnitude < 0.01f)
            {
                return false;
            }

            if (isSprintRecoveryRequired)
            {
                if (playerStamina.CurrentStamina < sprintRestartStamina)
                {
                    return false;
                }

                isSprintRecoveryRequired = false;
            }

            if (!playerStamina.TryConsume(sprintStaminaCostPerSecond * deltaTime))
            {
                isSprintRecoveryRequired = true;
                return false;
            }

            isSprintingThisFrame = true;
            return true;
        }

        // 공격 상태가 선택한 공격의 비용을 기존 수치 저장소에 전달한다.
        internal bool TryConsumeAttackStamina(float staminaCost)
        {
            return playerStamina.TryConsume(staminaCost);
        }

        // 일반 구르기는 방향을 정하기 전에 비용을 낼 수 있는지 먼저 확인한다.
        internal bool CanConsumeRollStamina()
        {
            return playerStamina.CanConsume(rollStaminaCost);
        }

        // 구르기 종류별 호출 순서는 구르기 상태가 정하고 실제 소비만 수행한다.
        internal bool TryConsumeRollStamina()
        {
            return playerStamina.TryConsume(rollStaminaCost);
        }

        // 갱신을 마친 행동 상태와 이번 프레임의 달리기로 회복 배율을 결정한다.
        internal float GetRecoveryRate(
            bool isDead, bool isHit, bool isAttacking, bool isRolling, bool isBlocking)
        {
            if (isDead || isHit || isAttacking || isRolling || isSprintingThisFrame)
            {
                return 0f;
            }

            return isBlocking ? guardStaminaRecoveryRate : 1f;
        }
    }
}
