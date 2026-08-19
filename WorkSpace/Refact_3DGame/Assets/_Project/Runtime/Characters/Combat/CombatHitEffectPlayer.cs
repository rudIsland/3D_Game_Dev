using UnityEngine;
using UnityEngine.SceneManagement;

namespace rudIsland.RPG3D.Characters.Combat
{
    // 확정된 전투 접촉 위치에 신체·방패 파티클을 재생한다.
    [DisallowMultipleComponent]
    public sealed class CombatHitEffectPlayer : MonoBehaviour
    {
        private const float DirectionThreshold = 0.000001f;
        private const int DefaultFloorLayerMask = 1 << 0;

        [Header("타격 효과")]
        [SerializeField] private GameObject bodyHitEffectPrefab;
        [SerializeField] private GameObject guardHitEffectPrefab;

        [Header("바닥 혈흔")]
        [SerializeField] private LayerMask floorLayerMask = DefaultFloorLayerMask;
        [SerializeField, Min(0f)] private float floorRayStartHeight = 0.25f;
        [SerializeField, Min(0.1f)] private float floorRayDistance = 4f;
        [SerializeField, Min(0f)] private float floorSurfaceOffset = 0.015f;

        [Header("효과 풀")]
        [SerializeField, Min(1)] private int bodyHitPoolSize = 4;
        [SerializeField, Min(1)] private int guardHitPoolSize = 2;

        private GameObject effectRoot;
        private HitEffectPool bodyHitPool;
        private HitEffectPool guardHitPool;
        private bool isCreated;

        private void Awake()
        {
            Create();
        }

        public void PlayBodyHit(Vector3 hitPosition, Vector3 incomingDirection)
        {
            Create();
            if (bodyHitPool == null)
            {
                return;
            }

            if (TryFindBloodFloor(hitPosition, out RaycastHit floorHit))
            {
                Vector3 floorPosition =
                    floorHit.point + floorHit.normal * floorSurfaceOffset;
                Quaternion floorRotation =
                    GetFloorEffectRotation(
                        floorHit.normal,
                        incomingDirection);
                bodyHitPool.Play(floorPosition, floorRotation);
                return;
            }

            bodyHitPool.Play(
                hitPosition,
                GetEffectRotation(incomingDirection));
        }

        public void PlayGuardHit(Vector3 hitPosition, Vector3 incomingDirection)
        {
            Create();
            guardHitPool?.Play(hitPosition, GetEffectRotation(incomingDirection));
        }

        private void Create()
        {
            if (isCreated ||
                (bodyHitEffectPrefab == null && guardHitEffectPrefab == null))
            {
                return;
            }

            isCreated = true;
            effectRoot = new GameObject($"{name} Hit Effects");
            Scene ownerScene = gameObject.scene;
            if (ownerScene.IsValid())
            {
                SceneManager.MoveGameObjectToScene(effectRoot, ownerScene);
            }

            bodyHitPool = new HitEffectPool(
                bodyHitEffectPrefab,
                bodyHitPoolSize,
                effectRoot.transform);
            guardHitPool = new HitEffectPool(
                guardHitEffectPrefab,
                guardHitPoolSize,
                effectRoot.transform);
        }

        private void OnDisable()
        {
            bodyHitPool?.Stop();
            guardHitPool?.Stop();
        }

        private void OnDestroy()
        {
            if (effectRoot != null)
            {
                Destroy(effectRoot);
            }

            bodyHitPool = null;
            guardHitPool = null;
            effectRoot = null;
            isCreated = false;
        }

        private static Quaternion GetEffectRotation(Vector3 incomingDirection)
        {
            if (incomingDirection.sqrMagnitude <= DirectionThreshold)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(-incomingDirection.normalized, Vector3.up);
        }

        private bool TryFindBloodFloor(
            Vector3 hitPosition,
            out RaycastHit floorHit)
        {
            Vector3 rayStart =
                hitPosition + Vector3.up * floorRayStartHeight;
            float rayDistance =
                floorRayStartHeight + floorRayDistance;

            return Physics.Raycast(
                rayStart,
                Vector3.down,
                out floorHit,
                rayDistance,
                floorLayerMask,
                QueryTriggerInteraction.Ignore);
        }

        private static Quaternion GetFloorEffectRotation(
            Vector3 floorNormal,
            Vector3 incomingDirection)
        {
            if (floorNormal.sqrMagnitude <= DirectionThreshold)
            {
                return Quaternion.identity;
            }

            Vector3 normalizedFloorNormal = floorNormal.normalized;
            Vector3 bloodDirection = Vector3.ProjectOnPlane(
                -incomingDirection,
                normalizedFloorNormal);
            if (bloodDirection.sqrMagnitude <= DirectionThreshold)
            {
                return Quaternion.FromToRotation(
                    Vector3.forward,
                    normalizedFloorNormal);
            }

            return Quaternion.LookRotation(
                normalizedFloorNormal,
                bloodDirection.normalized);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            bodyHitPoolSize = Mathf.Max(1, bodyHitPoolSize);
            guardHitPoolSize = Mathf.Max(1, guardHitPoolSize);
            floorRayStartHeight = Mathf.Max(0f, floorRayStartHeight);
            floorRayDistance = Mathf.Max(0.1f, floorRayDistance);
            floorSurfaceOffset = Mathf.Max(0f, floorSurfaceOffset);
        }
#endif

        private sealed class HitEffectPool
        {
            private readonly HitEffect[] effects;
            private int nextEffectIndex;

            public HitEffectPool(
                GameObject effectPrefab,
                int poolSize,
                Transform effectParent)
            {
                if (effectPrefab == null || effectParent == null)
                {
                    effects = null;
                    return;
                }

                int validPoolSize = Mathf.Max(1, poolSize);
                effects = new HitEffect[validPoolSize];
                for (int index = 0; index < validPoolSize; index++)
                {
                    GameObject effectObject = Object.Instantiate(effectPrefab, effectParent);
                    effectObject.name = effectPrefab.name;
                    effects[index] = new HitEffect(effectObject);
                }
            }

            public void Play(Vector3 hitPosition, Quaternion hitRotation)
            {
                if (effects == null || effects.Length == 0)
                {
                    return;
                }

                int selectedIndex = FindAvailableEffectIndex();
                effects[selectedIndex].Play(hitPosition, hitRotation);
                nextEffectIndex =
                    (selectedIndex + 1) % effects.Length;
            }

            public void Stop()
            {
                if (effects == null)
                {
                    return;
                }

                for (int index = 0; index < effects.Length; index++)
                {
                    effects[index].Stop();
                }
            }

            private int FindAvailableEffectIndex()
            {
                for (int offset = 0; offset < effects.Length; offset++)
                {
                    int index =
                        (nextEffectIndex + offset) % effects.Length;
                    if (!effects[index].IsAlive)
                    {
                        return index;
                    }
                }

                return nextEffectIndex;
            }
        }

        private sealed class HitEffect
        {
            private readonly GameObject effectObject;
            private readonly Transform effectTransform;
            private readonly ParticleSystem[] particleSystems;

            public HitEffect(GameObject effectObject)
            {
                this.effectObject = effectObject;
                effectTransform = effectObject.transform;
                particleSystems =
                    effectObject.GetComponentsInChildren<ParticleSystem>(true);
                DisablePreviewComponents();
                Stop();
                effectObject.SetActive(false);
            }

            public bool IsAlive
            {
                get
                {
                    if (!effectObject.activeSelf)
                    {
                        return false;
                    }

                    for (int index = 0;
                         index < particleSystems.Length;
                         index++)
                    {
                        ParticleSystem particleSystem =
                            particleSystems[index];
                        if (particleSystem != null &&
                            particleSystem.IsAlive(false))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            public void Play(Vector3 hitPosition, Quaternion hitRotation)
            {
                effectObject.SetActive(true);
                effectTransform.SetPositionAndRotation(hitPosition, hitRotation);

                for (int index = 0;
                     index < particleSystems.Length;
                     index++)
                {
                    ParticleSystem particleSystem = particleSystems[index];
                    if (particleSystem == null)
                    {
                        continue;
                    }

                    particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particleSystem.Play(false);
                }
            }

            public void Stop()
            {
                for (int index = 0;
                     index < particleSystems.Length;
                     index++)
                {
                    ParticleSystem particleSystem =
                        particleSystems[index];
                    if (particleSystem == null)
                    {
                        continue;
                    }

                    particleSystem.Stop(
                        false,
                        ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            private void DisablePreviewComponents()
            {
                Renderer previewRenderer =
                    effectObject.GetComponent<Renderer>();
                if (previewRenderer != null)
                {
                    previewRenderer.enabled = false;
                }

                Collider previewCollider =
                    effectObject.GetComponent<Collider>();
                if (previewCollider != null)
                {
                    previewCollider.enabled = false;
                }
            }
        }
    }
}
