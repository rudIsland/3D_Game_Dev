using rudIsland.RPG3D.Characters.Combat.AttackData;
using rudIsland.RPG3D.Player.Runtime.Hit;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // NightShade의 공격 범위 감지기
    internal sealed class NightShadeSpearAttackRangeDetector
    {
        private const int MaximumDetecedColliderCount = 16; // 최대 감지 콜라이더 수
        private readonly Transform attackOrigin; // 공격 시작 위치
        private readonly LayerMask targetLayers; // 공격 레이어 마스크
        private readonly float attackRange; // 공격 범위
        private readonly float attackForwardOffset; // 공격 전방 오프셋
        private readonly Collider[] detectedColliders
            = new Collider[MaximumDetecedColliderCount]; // 감지된 콜라이더 배열

        private bool isWindowOpen;
        private bool hasHitTarget;
        private bool wasDamageApplied;
        private AttackDamage attackDamage;

        public bool WasDamageApplied => wasDamageApplied;

        public NightShadeSpearAttackRangeDetector(
            Transform attackOrigin,
            LayerMask targetLayers,
            float attackRange,
            float attackForwardOffset)
        {
            this.attackOrigin = attackOrigin;
            this.targetLayers = targetLayers;
            this.attackRange = Mathf.Max(0f, attackRange);
            this.attackForwardOffset = attackForwardOffset;
        }

        public void Open(AttackDamage damage)
        {
            isWindowOpen = true;
            hasHitTarget = false;
            wasDamageApplied = false;
            attackDamage = damage;
        }
        public void Tick()
          {
              if (!isWindowOpen ||
                  hasHitTarget ||
                  attackDamage == null ||
                  attackOrigin == null ||
                  targetLayers.value == 0 ||
                  attackRange <= 0f)
              {
                  return;
              }

              Vector3 center =
                  attackOrigin.position +
                  attackOrigin.forward * attackForwardOffset;

              int detectedCount = Physics.OverlapSphereNonAlloc(
                  center,
                  attackRange,
                  detectedColliders,
                  targetLayers,
                  QueryTriggerInteraction.Collide);

              for (int index = 0; index < detectedCount; index++)
              {
                  IPlayerDamageReceiver target =
                      detectedColliders[index].GetComponentInParent<IPlayerDamageReceiver>();

                  if (target == null)
                  {
                      continue;
                  }

                  hasHitTarget = true;
                  wasDamageApplied = target.TryTakeDamage(attackDamage);
                  return;
              }
          }

          public void Close()
          {
              isWindowOpen = false;
          }
    }
}
