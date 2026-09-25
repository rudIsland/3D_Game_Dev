// 전투 Action이 사용하는 Unity 경계 기능을 한곳에 모은다.
using System;
using Core;

namespace NightShade
{
    // 상태가 요청한 소리, 판정과 풀 반환을 Unity 경계로 전달한다.
    internal sealed class NightShadeSwordCombatOutput
    {
        private readonly Action<NightShadeSwordAttackType, int> playAttackSound;
        private readonly Action<AttackDamage> openAttackHit;
        private readonly Action closeAttackHit;
        private readonly Action requestRelease;
        private readonly Action checkAttackHit;

        internal NightShadeSwordCombatOutput(
            Action<NightShadeSwordAttackType, int> playAttackSound,
            Action<AttackDamage> openAttackHit,
            Action closeAttackHit,
            Action requestRelease,
            Action checkAttackHit = null)
        {
            this.playAttackSound = playAttackSound;
            this.openAttackHit = openAttackHit;
            this.closeAttackHit = closeAttackHit;
            this.requestRelease = requestRelease;
            this.checkAttackHit = checkAttackHit;
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

        internal void CheckAttackHit()
        {
            checkAttackHit?.Invoke();
        }

        internal void RequestRelease()
        {
            requestRelease?.Invoke();
        }
    }
}
