using System;
using System.Collections.Generic;
using UnityEngine;
using rudIsland.RPG3D.Combat.Attack;
using rudIsland.RPG3D.Combat.Resolution;
using rudIsland.RPG3D.Combat.Result;

namespace rudIsland.RPG3D.Combat.Detection
{
    [DisallowMultipleComponent]
    // Attack Window 동안 주변에 있는 적에게 한 번씩 데미지를 준다.
    public sealed class MeleeHitDetector : MonoBehaviour
    {
        private const int MaxDetectedColliderCount = 32;

        [Header("Attack Window 공격 범위")]
        [SerializeField, Min(0.1f)] private float hitRadius = 1f;

        [Header("공격할 대상 레이어")]
        [SerializeField] private LayerMask targetLayers;

        private readonly Collider[] detectedColliders =
            new Collider[MaxDetectedColliderCount];
        private readonly HashSet<IAttackHitReceiver> hitTargets =
            new HashSet<IAttackHitReceiver>(16);

        private AttackHitInput currentHit;
        private bool isHitActive;
        private int currentAttackSequence;

        internal bool IsHitActive => isHitActive;
        public event Action<AttackHitResult, AttackHitInput> HitResultReady;

        // 애니메이션 클립의 Attack Window 시작 이벤트에서 호출한다.
        public void StartHit(in AttackHitInput hit)
        {
            EndHit();

            if (!hit.Damage.IsValid)
            {
                return;
            }

            currentHit = hit;
            IncreaseAttackSequence();
            isHitActive = true;
            DetectActiveHit();
        }

        // 애니메이션 클립의 Attack Window 종료 이벤트에서 호출한다.
        public void EndHit()
        {
            isHitActive = false;
            currentHit = default;
            hitTargets.Clear();
        }

        // Window가 열려 있는 동안 매 프레임 적을 확인한다.
        private void LateUpdate()
        {
            DetectActiveHit();
        }

        internal void DetectActiveHit()
        {
            if (!isHitActive)
            {
                return;
            }

            int detectedCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                hitRadius,
                detectedColliders,
                targetLayers.value,
                QueryTriggerInteraction.Collide);

            for (int index = 0; index < detectedCount; index++)
            {
                TryApplyHit(detectedColliders[index]);
            }
        }

        private void TryApplyHit(Collider detectedCollider)
        {
            if (detectedCollider == null)
            {
                return;
            }

            IAttackHitReceiver receiver =
                detectedCollider.GetComponentInParent<IAttackHitReceiver>();
            if (receiver == null || !hitTargets.Add(receiver))
            {
                return;
            }

            AttackHitResult result = receiver.ReceiveAttackHit(
                in currentHit);
            HitResultReady?.Invoke(result, currentHit);
        }

        private void IncreaseAttackSequence()
        {
            currentAttackSequence = currentAttackSequence == int.MaxValue
                ? 1
                : currentAttackSequence + 1;
        }

        // 기존 Resolver 코드와의 컴파일 호환을 위한 메서드다.
        internal bool MatchesAttackSequence(int attackSequence)
        {
            return currentAttackSequence == 0 ||
                currentAttackSequence == attackSequence;
        }

        // 현재 공격은 즉시 전달하므로 Resolver가 사용하지 않는다.
        internal void NotifyHitResolved(
            AttackHitResult hitResult,
            in AttackHitInput hit)
        {
            HitResultReady?.Invoke(hitResult, hit);
        }

        private void OnDisable()
        {
            EndHit();
            IncreaseAttackSequence();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
#endif
    }
}