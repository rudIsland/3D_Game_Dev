using System;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 상태가 요청한 소리, 판정과 풀 반환을 Unity 경계로 전달한다.
    internal sealed class NightShadeSwordActions
    {
        private readonly Action<NightShadeSwordAttackType, int> playAttackSound;
        private readonly Action<NightShadeSwordAttackType, int> openAttackHit;
        private readonly Action closeAttackHit;
        private readonly Action requestRelease;

        internal NightShadeSwordActions(
            Action<NightShadeSwordAttackType, int> playAttackSound,
            Action<NightShadeSwordAttackType, int> openAttackHit,
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

        internal void OpenAttackHit(NightShadeSwordAttackType attackType, int hitIndex)
        {
            openAttackHit?.Invoke(attackType, hitIndex);
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
