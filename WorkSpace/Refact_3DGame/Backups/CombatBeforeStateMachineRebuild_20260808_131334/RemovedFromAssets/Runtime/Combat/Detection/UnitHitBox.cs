using UnityEngine;
using rudIsland.RPG3D.Combat.Resolution;

namespace rudIsland.RPG3D.Combat.Detection
{
    [DisallowMultipleComponent]
    // 이동 충돌과 분리된 신체 부위가 공격 접촉을 받는다.
    public sealed class UnitHitBox : MonoBehaviour
    {
        [SerializeField] private HitBodyPart bodyPart = HitBodyPart.Body; // Inspector 설정 값

        private Collider hitCollider; // 피격 또는 피해 관련 값
        private IAttackHitReceiver hitReceiver; // 피격 또는 피해 관련 값

        internal HitBodyPart BodyPart => bodyPart; // 외부에 제공하는 읽기 값

        private void Awake()
        {
            CacheReferences();
            CheckRequiredReferences();
        }

        private void OnEnable()
        {
            if (hitReceiver == null)
            {
                CacheReferences();
            }
        }

        private void OnTransformParentChanged()
        {
            CacheReferences();
        }

        internal bool TryGetHitReceiver(
            out IAttackHitReceiver receiver)
        {
            if (hitReceiver == null)
            {
                CacheReferences();
            }

            receiver = hitReceiver;
            return receiver != null;
        }

        private void CacheReferences()
        {
            hitCollider = GetComponent<Collider>();
            hitReceiver =
                GetComponentInParent<IAttackHitReceiver>();
        }

        private void CheckRequiredReferences()
        {
            if (hitCollider == null)
            {
                Debug.LogError(
                    "UnitHitBox에 Collider가 필요합니다.",
                    this);
                return;
            }

            if (!hitCollider.isTrigger)
            {
                Debug.LogError(
                    "UnitHitBox의 Collider는 Trigger여야 합니다.",
                    this);
            }

            if (hitReceiver == null)
            {
                Debug.LogError(
                    "UnitHitBox가 부모에서 타격 받을 Unit을 찾지 못했습니다.",
                    this);
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            hitCollider = GetComponent<Collider>();
            if (hitCollider != null)
            {
                hitCollider.isTrigger = true;
            }
        }

        private void OnValidate()
        {
            hitCollider = GetComponent<Collider>();
        }
#endif
    }
}
