using UnityEngine;

namespace Characters.Enemies.NightShade
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    // NightShade 공격 종류에 맞는 무거운 검풍 소리를 재생한다.
    public sealed class NightShadeSwordAttackAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource bodyAudioSource;
        [SerializeField] private AudioSource accentAudioSource;
        [SerializeField] private AudioClip swordBodySound;
        [SerializeField] private AudioClip swordAccentSound;

        [Header("공격별 무게")]
        [SerializeField, Range(0.1f, 1f)] private float lightPitch = 0.82f;
        [SerializeField, Range(0.1f, 1f)] private float comboPitch = 0.8f;
        [SerializeField, Range(0.1f, 1f)] private float wideSwingPitch = 0.74f;
        [SerializeField, Range(0.1f, 1f)] private float heavyPitch = 0.68f;
        [SerializeField, Range(0f, 0.1f)]
        private float pitchVariation = 0.025f;
        [SerializeField, Range(0f, 1f)] private float bodyVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float accentVolume = 0.4f;
        [SerializeField, Range(0f, 0.2f)] private float accentPitchOffset = 0.08f;

        private void Awake()
        {
            PrepareAudioSources();
        }

        internal void Play(NightShadeSwordAttackType attackType, int hitIndex)
        {
            PrepareAudioSources();
            if (bodyAudioSource == null || accentAudioSource == null ||
                swordBodySound == null || swordAccentSound == null)
            {
                return;
            }

            float pitch = GetPitch(attackType);
            if (attackType == NightShadeSwordAttackType.ComboSecond)
            {
                pitch -= 0.03f;
            }

            float variedPitch = pitch +
                Random.Range(-pitchVariation, pitchVariation);
            float attackWeight = GetAttackWeight(attackType);

            bodyAudioSource.pitch = Mathf.Clamp(
                variedPitch,
                0.1f,
                1f);
            accentAudioSource.pitch = Mathf.Clamp(
                variedPitch + accentPitchOffset,
                0.1f,
                1f);

            bodyAudioSource.PlayOneShot(swordBodySound, bodyVolume * attackWeight);
            accentAudioSource.PlayOneShot(swordAccentSound, accentVolume * attackWeight);
        }

        internal void Stop()
        {
            bodyAudioSource?.Stop();
            accentAudioSource?.Stop();
        }

        private float GetAttackWeight(NightShadeSwordAttackType attackType)
        {
            return attackType == NightShadeSwordAttackType.Heavy ||
                attackType == NightShadeSwordAttackType.WideSwing
                    ? 1f
                    : 0.82f;
        }

        private float GetPitch(NightShadeSwordAttackType attackType)
        {
            switch (attackType)
            {
                case NightShadeSwordAttackType.ComboFirst:
                case NightShadeSwordAttackType.ComboSecond:
                    return comboPitch;
                case NightShadeSwordAttackType.WideSwing:
                    return wideSwingPitch;
                case NightShadeSwordAttackType.Heavy:
                    return heavyPitch;
                default:
                    return lightPitch;
            }
        }

        private void PrepareAudioSources()
        {
            if (bodyAudioSource == null)
            {
                bodyAudioSource = GetComponent<AudioSource>();
            }

            PrepareAudioSource(bodyAudioSource);
            PrepareAudioSource(accentAudioSource);
        }

        private static void PrepareAudioSource(AudioSource audioSource)
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
            audioSource.dopplerLevel = 0f;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 28f;
        }

        private void OnDisable()
        {
            Stop();
        }

#if UNITY_EDITOR
        public void ConnectForEditor(
            AudioSource bodySource,
            AudioSource accentSource,
            AudioClip bodySound,
            AudioClip accentSound)
        {
            bodyAudioSource = bodySource;
            accentAudioSource = accentSource;
            swordBodySound = bodySound;
            swordAccentSound = accentSound;
            PrepareAudioSources();
        }

        private void OnValidate()
        {
            PrepareAudioSources();
            lightPitch = Mathf.Clamp(lightPitch, 0.1f, 1f);
            comboPitch = Mathf.Clamp(comboPitch, 0.1f, 1f);
            wideSwingPitch = Mathf.Clamp(wideSwingPitch, 0.1f, 1f);
            heavyPitch = Mathf.Clamp(heavyPitch, 0.1f, 1f);
            pitchVariation = Mathf.Clamp(pitchVariation, 0f, 0.1f);
            bodyVolume = Mathf.Clamp01(bodyVolume);
            accentVolume = Mathf.Clamp01(accentVolume);
            accentPitchOffset = Mathf.Clamp(accentPitchOffset, 0f, 0.2f);
        }
#endif
    }
}
