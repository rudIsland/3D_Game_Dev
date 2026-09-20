using UnityEngine;

namespace Characters.Player.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    // 걷기, 달리기와 구르기에서 나는 이동 소리를 재생한다.
    public sealed class PlayerFootstepAudio : MonoBehaviour
    {
        [Header("발소리")]
        [SerializeField] private AudioClip[] walkSounds;
        [SerializeField] private AudioClip[] runSounds;
        [SerializeField] private AudioClip rollSound;
        [SerializeField, Range(0f, 1f)] private float walkVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float runVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float rollVolume = 0.8f;
        [SerializeField, Range(0f, 0.2f)] private float pitchChange = 0.05f;

        private AudioSource footstepAudioSource;
        private int lastWalkSoundIndex = -1;
        private int lastRunSoundIndex = -1;

        private void Awake()
        {
            PrepareAudioSource();
        }

        private void Reset()
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 20f;
        }

        public void Create(
            AudioClip[] newWalkSounds,
            AudioClip[] newRunSounds,
            AudioClip newRollSound,
            float newWalkVolume,
            float newRunVolume,
            float newRollVolume,
            float newPitchChange)
        {
            walkSounds = newWalkSounds;
            runSounds = newRunSounds;
            rollSound = newRollSound;
            walkVolume = Mathf.Clamp01(newWalkVolume);
            runVolume = Mathf.Clamp01(newRunVolume);
            rollVolume = Mathf.Clamp01(newRollVolume);
            pitchChange = Mathf.Clamp(newPitchChange, 0f, 0.2f);
            PrepareAudioSource();
        }

#if UNITY_EDITOR
        internal void ConnectForEditor(
            AudioClip[] newWalkSounds,
            AudioClip[] newRunSounds,
            AudioClip newRollSound,
            float newWalkVolume,
            float newRunVolume,
            float newRollVolume,
            float newPitchChange)
        {
            walkSounds = newWalkSounds;
            runSounds = newRunSounds;
            rollSound = newRollSound;
            walkVolume = Mathf.Clamp01(newWalkVolume);
            runVolume = Mathf.Clamp01(newRunVolume);
            rollVolume = Mathf.Clamp01(newRollVolume);
            pitchChange = Mathf.Clamp(newPitchChange, 0f, 0.2f);
        }
#endif

        public void PlayWalkFootstep()
        {
            PlaySound(walkSounds, walkVolume, ref lastWalkSoundIndex);
        }

        public void PlayRunFootstep()
        {
            PlaySound(runSounds, runVolume, ref lastRunSoundIndex);
        }

        public void PlayRollSound()
        {
            if (footstepAudioSource == null || rollSound == null)
            {
                return;
            }

            footstepAudioSource.pitch = 1f + Random.Range(-pitchChange, pitchChange);
            footstepAudioSource.PlayOneShot(rollSound, rollVolume);
        }

        private void PrepareAudioSource()
        {
            if (footstepAudioSource == null)
            {
                footstepAudioSource = GetComponent<AudioSource>();
            }

            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.loop = false;
            footstepAudioSource.spatialBlend = 1f;
            footstepAudioSource.dopplerLevel = 0f;
            footstepAudioSource.minDistance = 1f;
            footstepAudioSource.maxDistance = 20f;
        }

        private void PlaySound(
            AudioClip[] sounds,
            float volume,
            ref int lastSoundIndex)
        {
            if (footstepAudioSource == null ||
                sounds == null ||
                sounds.Length == 0)
            {
                return;
            }

            int soundIndex = SelectSoundIndex(sounds.Length, lastSoundIndex);
            AudioClip sound = sounds[soundIndex];
            if (sound == null)
            {
                return;
            }

            lastSoundIndex = soundIndex;
            footstepAudioSource.pitch = 1f + Random.Range(-pitchChange, pitchChange);
            footstepAudioSource.PlayOneShot(sound, volume);
        }

        private static int SelectSoundIndex(int soundCount, int lastSoundIndex)
        {
            if (soundCount <= 1)
            {
                return 0;
            }

            int soundIndex = Random.Range(0, soundCount - 1);
            if (lastSoundIndex >= 0 && soundIndex >= lastSoundIndex)
            {
                soundIndex++;
            }

            return soundIndex;
        }
    }
}
