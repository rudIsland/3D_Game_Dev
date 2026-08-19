using Cinemachine;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Player.States.Attack;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField, Range(0f, 1f)]
        [FormerlySerializedAs("staggerHitSoundVolume")]
        private float smallHitSoundVolume = 0.9f;
        [SerializeField, Range(0f, 1f)]
        private float strongHitSoundVolume = 1f;
        [SerializeField, Min(0f)] private float confirmedHitImpulseForce = 0.12f;
        [FormerlySerializedAs("staggerHitImpulseForce")]
        [SerializeField, Min(0f)] private float smallHitImpulseForce = 0.16f;
        [SerializeField, Min(0f)] private float strongHitImpulseForce = 0.22f;

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

        public void PlayConfirmedHit(in EnemyHitResult hitResult)
        {
            if (!hitResult.HasDamageFeedback)
            {
                return;
            }

            float soundVolume = GetHitSoundVolume(in hitResult);
            if (attackAudioSource != null && confirmedHitSound != null)
            {
                attackAudioSource.PlayOneShot(
                    confirmedHitSound,
                    soundVolume);
            }

            hitImpulseSource?.GenerateImpulse(
                GetHitImpulseForce(in hitResult));
        }

        private float GetHitSoundVolume(in EnemyHitResult hitResult)
        {
            if (hitResult.DamageResult == HitDamageResult.Killed ||
                hitResult.Reaction == HitReaction.BigHit ||
                hitResult.Reaction == HitReaction.Knockback ||
                hitResult.Reaction == HitReaction.Knockdown)
            {
                return strongHitSoundVolume;
            }

            return hitResult.Reaction == HitReaction.SmallHit
                ? smallHitSoundVolume
                : confirmedHitSoundVolume;
        }

        private float GetHitImpulseForce(in EnemyHitResult hitResult)
        {
            if (hitResult.DamageResult == HitDamageResult.Killed ||
                hitResult.Reaction == HitReaction.BigHit ||
                hitResult.Reaction == HitReaction.Knockback ||
                hitResult.Reaction == HitReaction.Knockdown)
            {
                return strongHitImpulseForce;
            }

            return hitResult.Reaction == HitReaction.SmallHit
                ? smallHitImpulseForce
                : confirmedHitImpulseForce;
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
            smallHitSoundVolume = Mathf.Clamp01(smallHitSoundVolume);
            strongHitSoundVolume = Mathf.Clamp01(strongHitSoundVolume);
            confirmedHitImpulseForce = Mathf.Max(0f, confirmedHitImpulseForce);
            smallHitImpulseForce = Mathf.Max(0f, smallHitImpulseForce);
            strongHitImpulseForce = Mathf.Max(0f, strongHitImpulseForce);
        }
#endif
    }
}
