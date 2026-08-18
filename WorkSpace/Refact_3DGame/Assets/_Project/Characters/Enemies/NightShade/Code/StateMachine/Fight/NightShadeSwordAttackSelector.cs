using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 거리, 확률과 직전 공격만 보고 다음 공격을 고른다.
    internal sealed class NightShadeSwordAttackSelector
    {
        private readonly float attackRangeSquared;

        internal NightShadeSwordAttackSelector(float attackRangeSquared)
        {
            this.attackRangeSquared = attackRangeSquared;
        }

        internal NightShadeSwordAttackType Choose(float distanceSquared, NightShadeSwordFightMemory fightMemory)
        {
            return ChooseByRoll(
                distanceSquared,
                fightMemory.HasPreviousAttack,
                fightMemory.PreviousAttackType,
                Random.Range(0, 100));
        }

        internal NightShadeSwordAttackType ChooseByRoll(
            float distanceSquared,
            bool hasPreviousAttack,
            NightShadeSwordAttackType previousAttackType,
            int roll)
        {
            roll = Mathf.Clamp(roll, 0, 99);
            NightShadeSwordAttackType selectedAttack;

            if (distanceSquared <= attackRangeSquared * 0.36f)
            {
                selectedAttack = roll < 60
                    ? NightShadeSwordAttackType.ComboFirst
                    : NightShadeSwordAttackType.Light;
            }
            else if (distanceSquared <= attackRangeSquared * 0.75f)
            {
                selectedAttack = roll < 30
                    ? NightShadeSwordAttackType.Light
                    : roll < 45
                        ? NightShadeSwordAttackType.ComboFirst
                        : NightShadeSwordAttackType.WideSwing;
            }
            else
            {
                selectedAttack = roll < 60
                    ? NightShadeSwordAttackType.Heavy
                    : NightShadeSwordAttackType.WideSwing;
            }

            if (!hasPreviousAttack ||
                selectedAttack != previousAttackType)
            {
                return selectedAttack;
            }

            if (distanceSquared <= attackRangeSquared * 0.36f)
            {
                return selectedAttack == NightShadeSwordAttackType.ComboFirst
                    ? NightShadeSwordAttackType.Light
                    : NightShadeSwordAttackType.ComboFirst;
            }

            if (distanceSquared <= attackRangeSquared * 0.75f)
            {
                return selectedAttack == NightShadeSwordAttackType.WideSwing
                    ? NightShadeSwordAttackType.Light
                    : NightShadeSwordAttackType.WideSwing;
            }

            return selectedAttack == NightShadeSwordAttackType.Heavy
                ? NightShadeSwordAttackType.WideSwing
                : NightShadeSwordAttackType.Heavy;
        }
    }
}
