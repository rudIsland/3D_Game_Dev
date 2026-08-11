using UnityEngine;
using System.Collections.Generic;
using rudIsland.RPG3D.Characters;

namespace rudIsland.RPG3D.Player.Runtime.Attack
{
    internal sealed class PlayerAttackRangeDetector
    {
        private const int MaximumDetectedColliderCount = 32; //최대 충돌 collider 갯수

        private readonly Transform attackOrigin; // 공격 시작 위치
        private readonly LayerMask enemyLayers; // 적 레이어 마스크
        private readonly float attackRange; //공격 범위
        private readonly float attackForwardOffset; //공격 전방 오프셋
        

        private readonly Collider[] detectedColliders = new Collider[MaximumDetectedColliderCount]; //감지된 충돌체 배열;

        private bool isWindowOpen;

        private float attackDamage; //공격 피해량

        //같은 적을 참조하지 않고 Hash로 빠르게 타깃을 가리키기 위해 HashSet을 사용한다. HashSet은 중복을 허용하지 않으며, O(1) 시간 복잡도로 요소를 추가, 제거 및 검색할 수 있다.
        private readonly HashSet<IEnemyDamageReceiver> hitTargets = new HashSet<IEnemyDamageReceiver>(); //감지된 적 객체 집합



        public PlayerAttackRangeDetector(
            Transform attackOrigin, LayerMask enemyLayers, 
            float attackRange, float attackForwardOffset)
        {
            this.attackOrigin = attackOrigin;
            this.enemyLayers = enemyLayers;
            this.attackRange = Mathf.Max(0f, attackRange);
            this.attackForwardOffset = attackForwardOffset;
        }

        public void Open(float damage)
        {
            isWindowOpen = true;
            attackDamage = Mathf.Max(0f, damage);
            hitTargets.Clear();
        }

        public void Tick()
        {
            if(!isWindowOpen || attackOrigin == null || enemyLayers.value == 0 || attackRange <= 0f)
                return;
            Vector3 center = attackOrigin.position + attackOrigin.forward * attackForwardOffset;

            //탐지 갯수 확인
            int detectedCount = Physics.OverlapSphereNonAlloc(
                center,
                attackRange,
                detectedColliders,
                enemyLayers,
                QueryTriggerInteraction.Collide);

            if (detectedCount <= 0)
            {
                return;
            }

            for(int i=0; i<detectedCount; i++)
            {
                Collider detectedCollider = detectedColliders[i];
                if(detectedCollider == null)
                {
                    continue;
                }

                IEnemyDamageReceiver target = detectedCollider.GetComponentInParent<IEnemyDamageReceiver>();
                if(target == null || !hitTargets.Add(target))
                    continue;

                Vector3 hitPosition = detectedCollider.ClosestPoint(center);
                target.TakeDamage(attackDamage, hitPosition);
            }
        }

        public void Close()
        {
            isWindowOpen = false;
        }
    }
}
