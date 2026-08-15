using UnityEngine;

namespace rudIsland.RPG3D.Player.Runtime.Hit
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    // 방어 중에만 활성화되는 방패의 물리 접촉 표면이다.
    public sealed class PlayerGuardHitBox : MonoBehaviour
    {
        private BoxCollider guardCollider;

        internal void SetGuardActive(bool isActive)
        {
            if (guardCollider != null &&
                guardCollider.enabled != isActive)
            {
                guardCollider.enabled = isActive;
            }
        }

        private void Awake()
        {
            guardCollider = GetComponent<BoxCollider>();
            guardCollider.isTrigger = true;
            guardCollider.enabled = false;
        }

        private void OnDisable()
        {
            SetGuardActive(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }
#endif
    }
}
