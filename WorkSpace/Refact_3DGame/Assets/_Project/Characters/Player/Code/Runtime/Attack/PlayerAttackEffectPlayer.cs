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
        private PlayerBladeTrailRenderer bladeTrailRenderer;
        private bool isCreated;

        private void Awake()
        {
            attackAudioSource = GetComponent<AudioSource>();
            attackAudioSource.playOnAwake = false;
        }

        public void Create(
            Transform weaponHitStart,
            Transform weaponHitEnd)
        {
            if (isCreated)
            {
                return;
            }

            isCreated = true;
            bladeTrailRenderer =
                GetComponent<PlayerBladeTrailRenderer>();
            if (bladeTrailRenderer == null)
            {
                bladeTrailRenderer =
                    gameObject.AddComponent<PlayerBladeTrailRenderer>();
            }

            bladeTrailRenderer.Create(
                weaponHitStart,
                weaponHitEnd);
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
    }
}