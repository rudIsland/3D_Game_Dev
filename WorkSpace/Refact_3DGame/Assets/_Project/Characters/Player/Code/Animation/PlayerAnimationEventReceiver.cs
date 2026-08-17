using rudIsland.RPG3D.Player.Runtime.Audio;
using UnityEngine;

namespace rudIsland.RPG3D.Player.Animations
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerFootstepAudio))]
    // 플레이어 애니메이션 이벤트를 이동·공격·오디오 기능에 전달한다.
    public sealed class PlayerAnimationEventReceiver : MonoBehaviour
    {
        private PlayerController playerController; // 씬 또는 시스템 참조
        private PlayerFootstepAudio footstepAudio; // 걷기·달리기·구르기 이동음 재생

        private void Awake()
        {
            playerController = GetComponentInParent<PlayerController>();
            footstepAudio = GetComponent<PlayerFootstepAudio>();

            if (playerController == null)
            {
                Debug.LogError(
                    "PlayerAnimationEventReceiver가 PlayerController를 찾지 못했습니다.",
                    this);
                enabled = false;
            }
        }


        public void PlayAttackSoundAnimationEvent(int attackNumber)
        {
            playerController?.PlayAttackSound(attackNumber);
        }

        public void StartAttackHitAnimationEvent(int attackNumber)
        {
            playerController?.StartAttackHit(attackNumber);
        }

        public void EndAttackHitAnimationEvent()
        {
            playerController?.NotifyAttackHitEnded();
        }

        public void EndAttackAnimationEvent()
        {
            playerController?.NotifyAttackAnimationEnded();
        }

        public void BeginRollInvulnerabilityAnimationEvent()
        {
            playerController?.BeginRollInvulnerability();
        }

        public void EndRollInvulnerabilityAnimationEvent()
        {
            playerController?.EndRollInvulnerability();
        }

        public void PlayRollSoundAnimationEvent()
        {
            FindFootstepAudio();
            footstepAudio?.PlayRollSound();
        }

        public void PlayWalkFootstepAnimationEvent()
        {
            FindFootstepAudio();
            footstepAudio?.PlayWalkFootstep();
        }

        public void PlayRunFootstepAnimationEvent()
        {
            FindFootstepAudio();
            footstepAudio?.PlayRunFootstep();
        }

        public void PlayDeathKneeImpactAnimationEvent()
        {
            playerController?.PlayDeathKneeImpact();
        }

        public void PlayDeathBodyImpactAnimationEvent()
        {
            playerController?.PlayDeathBodyImpact();
        }

        private void FindFootstepAudio()
        {
            if (footstepAudio == null)
            {
                footstepAudio = GetComponent<PlayerFootstepAudio>();
            }
        }

        private void OnDisable()
        {
            playerController?.EndRollInvulnerability();
            playerController?.EndAttackHit();
        }
    }
}
