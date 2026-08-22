// 이전 선택과 공격 후딜처럼 Tick 사이에 유지할 전투 기록이다.
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 한 번의 활성화 동안 이전 공격, Recovery와 공격 후 대기를 보관한다.
    internal sealed class NightShadeSwordCombatMemory
    {
        internal bool HasPreviousAttack { get; private set; }
        internal NightShadeSwordActionId PreviousAttack { get; private set; }
        internal bool HasPreviousRecovery { get; private set; }
        internal NightShadeSwordActionId PreviousRecovery { get; private set; }
        internal float RemainingPostAttackDelay { get; private set; }

        internal void Reset()
        {
            HasPreviousAttack = false;
            PreviousAttack = NightShadeSwordActionId.None;
            HasPreviousRecovery = false;
            PreviousRecovery = NightShadeSwordActionId.None;
            RemainingPostAttackDelay = 0f;
        }

        internal void UpdatePostAttackDelay(float deltaTime)
        {
            if (RemainingPostAttackDelay > 0f)
            {
                RemainingPostAttackDelay = Mathf.Max(0f, RemainingPostAttackDelay - deltaTime);
            }
        }

        internal void RecordAttack(NightShadeSwordActionId actionId)
        {
            PreviousAttack = actionId;
            HasPreviousAttack = true;
        }

        internal void RecordRecovery(NightShadeSwordActionId actionId)
        {
            PreviousRecovery = actionId;
            HasPreviousRecovery = true;
        }

        internal void StartPostAttackDelay(float postAttackDelay)
        {
            RemainingPostAttackDelay = Mathf.Max(0f, postAttackDelay);
        }

    }
}
