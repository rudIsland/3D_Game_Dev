using rudIsland.RPG3D.Characters.Combat.AttackData;
using UnityEngine;

namespace rudIsland.RPG3D.Player.Runtime.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    // 플레이어가 실제 피해를 받았을 때 공격 종류에 맞는 피격음을 재생한다.
    public sealed class PlayerDamageAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource damageAudioSource;
        [SerializeField] private AudioClip bodyImpactSound;
        [SerializeField] private AudioClip swordCutSound;
        [SerializeField] private AudioClip deathKneeImpactSound;
        [SerializeField] private AudioClip deathBodyImpactSound;
        [SerializeField, Range(0f, 1f)] private float volume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float deathKneeVolume = 0.3f;
        [SerializeField, Range(0f, 1f)] private float deathBodyVolume = 0.7f;

        private void Awake()
        {
            PrepareAudioSource();
        }

        public void Play(DamageSoundType soundType)
        {
            AudioClip sound = soundType == DamageSoundType.SwordCut
                ? swordCutSound
                : bodyImpactSound;
            PlaySound(sound, volume);
        }

        public void PlayDeathKneeImpact()
        {
            PlaySound(deathKneeImpactSound, deathKneeVolume);
        }

        public void PlayDeathBodyImpact()
        {
            PlaySound(deathBodyImpactSound, deathBodyVolume);
        }

        private void PlaySound(AudioClip sound, float soundVolume)
        {
            PrepareAudioSource();
            if (damageAudioSource == null || sound == null)
            {
                return;
            }

            damageAudioSource.PlayOneShot(sound, soundVolume);
        }

        public void Stop()
        {
            damageAudioSource?.Stop();
        }

        private void PrepareAudioSource()
        {
            if (damageAudioSource == null)
            {
                damageAudioSource = GetComponent<AudioSource>();
            }

            if (damageAudioSource == null)
            {
                return;
            }

            damageAudioSource.playOnAwake = false;
            damageAudioSource.loop = false;
            damageAudioSource.spatialBlend = 1f;
            damageAudioSource.dopplerLevel = 0f;
            damageAudioSource.minDistance = 1.5f;
            damageAudioSource.maxDistance = 24f;
        }

        private void OnDisable()
        {
            Stop();
        }

#if UNITY_EDITOR
        public void ConnectForEditor(
            AudioSource audioSource,
            AudioClip newBodyImpactSound,
            AudioClip newSwordCutSound)
        {
            damageAudioSource = audioSource;
            bodyImpactSound = newBodyImpactSound;
            swordCutSound = newSwordCutSound;
            PrepareAudioSource();
        }

        private void OnValidate()
        {
            PrepareAudioSource();
            volume = Mathf.Clamp01(volume);
            deathKneeVolume = Mathf.Clamp01(deathKneeVolume);
            deathBodyVolume = Mathf.Clamp01(deathBodyVolume);
        }
#endif
    }
}
