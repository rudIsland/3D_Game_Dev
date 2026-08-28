using UnityEngine;

namespace Characters.Enemies.NightShade
{
    // 공격 Animation Event를 NightShade 전투 경계로 전달한다.
    [DisallowMultipleComponent]
    public sealed class NightShadeSwordAnimationEventReceiver : MonoBehaviour
    {
        [SerializeField]
        private NightShadeSwordAnimationController animationController;
        [SerializeField]
        private NightShadeSwordController swordController;

        private void Awake()
        {
            FindAnimationController();
        }

        public void SetAttackSpeed(float speed)
        {
            if (swordController == null || swordController.IsAttackStateActive)
            {
                animationController?.SetAttackPlaybackSpeed(speed);
            }
        }

        public void ResetAttackSpeed()
        {
            animationController?.ResetAttackPlaybackSpeed();
        }

        public void StopAttackTurnAnimationEvent()
        {
            swordController?.StopAttackTurnAnimationEvent();
        }

        public void PlayAttackSoundAnimationEvent(int hitIndex)
        {
            swordController?.PlayAttackSoundAnimationEvent(hitIndex);
        }

        public void OpenAttackHitAnimationEvent(int hitIndex)
        {
            swordController?.OpenAttackHitAnimationEvent(hitIndex);
        }

        public void CloseAttackHitAnimationEvent()
        {
            swordController?.CloseAttackHitAnimationEvent();
        }

        private void FindAnimationController()
        {
            if (animationController == null)
            {
                animationController =
                    GetComponentInParent<
                        NightShadeSwordAnimationController>(true);
            }

            if (swordController == null)
            {
                swordController = GetComponentInParent<
                    NightShadeSwordController>(true);
            }
        }

#if UNITY_EDITOR
        public void ConnectForEditor(NightShadeSwordAnimationController controller, NightShadeSwordController owner)
        {
            animationController = controller;
            swordController = owner;
        }

        private void OnValidate()
        {
            FindAnimationController();
        }
#endif
    }
}
