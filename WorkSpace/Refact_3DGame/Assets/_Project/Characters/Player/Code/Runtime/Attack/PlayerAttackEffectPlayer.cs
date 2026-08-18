using Cinemachine;
using rudIsland.RPG3D.Player.States.Attack;
using UnityEngine;

namespace rudIsland.RPG3D.Player.Runtime.Attack
{
    // 서로 다른 애니메이션 이벤트에서 검 소리와 검 궤적을 재생한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlayerAttackEffectPlayer : MonoBehaviour
    {
        private AudioSource attackAudioSource;
        private CinemachineImpulseSource hitImpulseSource;
        private PlayerBladeTrailRenderer bladeTrailRenderer;
        private bool isCreated;

        [Header("확정 타격")]
        [SerializeField] private AudioClip confirmedHitSound;
        [SerializeField, Range(0f, 1f)]
        private float confirmedHitSoundVolume = 0.8f;
        [SerializeField, Min(0f)] private float confirmedHitImpulseForce = 0.12f;

        private void Awake()
        {
            attackAudioSource = GetComponent<AudioSource>();
            attackAudioSource.playOnAwake = false;
            hitImpulseSource = GetComponent<CinemachineImpulseSource>();
            if (hitImpulseSource == null)
            {
                hitImpulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
            }
        }

        public void Create(Transform weaponHitStart, Transform weaponHitEnd)
        {
            if (isCreated)
            {
                return;
            }

            isCreated = true;
            bladeTrailRenderer = GetComponent<PlayerBladeTrailRenderer>();
            if (bladeTrailRenderer == null)
            {
                bladeTrailRenderer = gameObject.AddComponent<PlayerBladeTrailRenderer>();
            }

            bladeTrailRenderer.Create(weaponHitStart, weaponHitEnd);
        }

        public void PlaySound(PlayerAttackData attackData)
        {
            if (attackData == null)
            {
                return;
            }

            if (attackAudioSource != null && attackData.SwingSound != null)
            {
                attackAudioSource.PlayOneShot(attackData.SwingSound);
            }
        }

        public void BeginTrail()
        {
            bladeTrailRenderer?.BeginTrail();
        }

        public void PlayConfirmedHit()
        {
            if (attackAudioSource != null && confirmedHitSound != null)
            {
                attackAudioSource.PlayOneShot(
                    confirmedHitSound,
                    confirmedHitSoundVolume);
            }

            hitImpulseSource?.GenerateImpulse(confirmedHitImpulseForce);
        }

        public void Stop()
        {
            bladeTrailRenderer?.EndTrail();
        }

        private void OnDisable()
        {
            Stop();
            attackAudioSource?.Stop();
            bladeTrailRenderer?.ClearTrail();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            confirmedHitSoundVolume = Mathf.Clamp01(confirmedHitSoundVolume);
            confirmedHitImpulseForce = Mathf.Max(0f, confirmedHitImpulseForce);
        }
#endif
    }
}
