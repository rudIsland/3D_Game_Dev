using UnityEngine;
using Core;

namespace Player
{
    // 공격 판정기와 공격력 배율을 소유하고 판정·소리·궤적의 실행 순서를 묶는다.
    internal sealed class PlayerAttackHit
    {
        private readonly PlayerAttackRangeDetector attackRangeDetector;
        private readonly PlayerAttackEffectPlayer attackEffectPlayer;
        private float attackDamageMultiplier = 1f;

        // 플레이어와 무기 참조로 판정기를 한 번 만든다. Unity 컴포넌트 수명은 소유자가 관리한다.
        internal PlayerAttackHit(
            Transform attackerRoot,
            PlayerWeaponHitShape weaponHitShape,
            CombatHitStop hitStop,
            ICombatHitEffects hitEffectPlayer,
            PlayerAttackEffectPlayer attackEffectPlayer)
        {
            this.attackEffectPlayer = attackEffectPlayer;
            attackRangeDetector = new PlayerAttackRangeDetector(
                attackerRoot,
                weaponHitShape.StartPoint,
                weaponHitShape.EndPoint,
                weaponHitShape.TargetLayers,
                weaponHitShape.Radius,
                hitStop,
                hitEffectPlayer,
                attackEffectPlayer);
        }

        // 상태머신이 현재 공격임을 확인한 데이터로 판정을 열고 궤적을 시작한다.
        internal void Open(PlayerAttackData attack)
        {
            attackRangeDetector.Open(
                attack.Damage.HealthDamage * attackDamageMultiplier,
                attack.Damage.StaggerDamage,
                attack.Damage.Strength,
                attack.Damage.PushDistance,
                attack.Damage.HitStopDuration);
            attackEffectPlayer?.BeginTrail();
        }

        // 상태 갱신 뒤 현재 무기 위치와 지난 프레임의 궤적으로 명중을 확인한다.
        internal void Tick()
        {
            attackRangeDetector.Tick();
        }

        // 판정을 먼저 닫고 효과를 멈춘다. 상태 종료와 비활성화의 중복 호출을 허용한다.
        internal void Close()
        {
            attackRangeDetector.Close();
            attackEffectPlayer?.Stop();
        }

        // 상태머신이 공격 번호를 확인한 뒤 해당 공격의 소리를 재생한다.
        internal void PlaySound(PlayerAttackData attack)
        {
            attackEffectPlayer?.PlaySound(attack);
        }

        // 강화 배율을 보관하며 다음 판정을 열 때 적용한다.
        internal void SetDamageMultiplier(float multiplier)
        {
            attackDamageMultiplier = Mathf.Max(1f, multiplier);
        }
    }
}
