// 전투 Action이 사용하는 Unity 경계 기능을 한곳에 모은다.
using System;
using rudIsland.RPG3D.Characters.Combat.AttackData;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 상태가 요청한 소리, 판정과 풀 반환을 Unity 경계로 전달한다.
    internal sealed class NightShadeSwordCombatOutput
    {
        private readonly Action<NightShadeSwordAttackType, int> playAttackSound;
        private readonly Action<AttackDamage> openAttackHit;
        private readonly Action closeAttackHit;
        private readonly Action requestRelease;

        internal NightShadeSwordCombatOutput(
            Action<NightShadeSwordAttackType, int> playAttackSound,
            Action<AttackDamage> openAttackHit,
            Action closeAttackHit,
            Action requestRelease)
        {
            this.playAttackSound = playAttackSound;
            this.openAttackHit = openAttackHit;
            this.closeAttackHit = closeAttackHit;
            this.requestRelease = requestRelease;
        }

        internal void PlayAttackSound(NightShadeSwordAttackType attackType, int hitIndex)
        {
            playAttackSound?.Invoke(attackType, hitIndex);
        }

        internal void OpenAttackHit(AttackDamage attackDamage)
        {
            openAttackHit?.Invoke(attackDamage);
        }

        internal void CloseAttackHit()
        {
            closeAttackHit?.Invoke();
        }

        internal void RequestRelease()
        {
            requestRelease?.Invoke();
        }
    }
}
