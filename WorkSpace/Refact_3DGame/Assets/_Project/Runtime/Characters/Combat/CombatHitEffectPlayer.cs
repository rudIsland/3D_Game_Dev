using UnityEngine;
using UnityEngine.SceneManagement;

namespace rudIsland.RPG3D.Characters.Combat
{
    // 확정된 전투 접촉 위치에 신체·방패 파티클을 재생한다.
    [DisallowMultipleComponent]
    public sealed class CombatHitEffectPlayer : MonoBehaviour
    {
        private const float DirectionThreshold = 0.000001f;

        [Header("타격 효과")]
        [SerializeField] private GameObject bodyHitEffectPrefab;
        [SerializeField] private GameObject guardHitEffectPrefab;

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

        public void PlayBodyHit(
            Vector3 hitPosition,
            Vector3 incomingDirection)
        {
            Create();
            bodyHitPool?.Play(
                hitPosition,
                GetEffectRotation(incomingDirection));
        }

        public void PlayGuardHit(
            Vector3 hitPosition,
            Vector3 incomingDirection)
        {
            Create();
            guardHitPool?.Play(
                hitPosition,
                GetEffectRotation(incomingDirection));
        }

        private void Create()
        {
            if (isCreated ||
                (bodyHitEffectPrefab == null &&
                 guardHitEffectPrefab == null))
            {
                return;
            }

            isCreated = true;
            effectRoot = new GameObject($"{name} Hit Effects");
            Scene ownerScene = gameObject.scene;
            if (ownerScene.IsValid())
            {
                SceneManager.MoveGameObjectToScene(
                    effectRoot,
                    ownerScene);
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

            return Quaternion.LookRotation(
                -incomingDirection.normalized,
                Vector3.up);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            bodyHitPoolSize = Mathf.Max(1, bodyHitPoolSize);
            guardHitPoolSize = Mathf.Max(1, guardHitPoolSize);
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
                    GameObject effectObject = Object.Instantiate(
                        effectPrefab,
                        effectParent);
                    effectObject.name = effectPrefab.name;
                    effects[index] = new HitEffect(effectObject);
                }
            }

            public void Play(
                Vector3 hitPosition,
                Quaternion hitRotation)
            {
                if (effects == null || effects.Length == 0)
                {
                    return;
                }

                int selectedIndex = FindAvailableEffectIndex();
                effects[selectedIndex].Play(
                    hitPosition,
                    hitRotation);
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
                        if (particleSystems[index].IsAlive(false))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            public void Play(
                Vector3 hitPosition,
                Quaternion hitRotation)
            {
                effectObject.SetActive(true);
                effectTransform.SetPositionAndRotation(
                    hitPosition,
                    hitRotation);

                for (int index = 0;
                     index < particleSystems.Length;
                     index++)
                {
                    ParticleSystem particleSystem = particleSystems[index];
                    particleSystem.Stop(
                        false,
                        ParticleSystemStopBehavior.StopEmittingAndClear);
                    particleSystem.Play(false);
                }
            }

            public void Stop()
            {
                for (int index = 0;
                     index < particleSystems.Length;
                     index++)
                {
                    particleSystems[index].Stop(
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
