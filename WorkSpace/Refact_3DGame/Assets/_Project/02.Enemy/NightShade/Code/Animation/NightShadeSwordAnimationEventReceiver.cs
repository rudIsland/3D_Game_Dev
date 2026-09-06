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

        public void SetAttackSpeed(AnimationEvent animationEvent)
        {
            if (CanReceive(animationEvent))
            {
                animationController.SetAttackPlaybackSpeed(animationEvent.floatParameter);
            }
        }

        public void ResetAttackSpeed(AnimationEvent animationEvent)
        {
            if (CanReceive(animationEvent))
                animationController.ResetAttackPlaybackSpeed();
        }

        public void StopAttackTurnAnimationEvent(AnimationEvent animationEvent)
        {
            if (CanReceive(animationEvent))
                swordController.StopAttackTurnAnimationEvent();
        }

        public void PlayAttackSoundAnimationEvent(AnimationEvent animationEvent)
        {
            if (CanReceive(animationEvent))
                swordController.PlayAttackSoundAnimationEvent(animationEvent.intParameter);
        }

        public void OpenAttackHitAnimationEvent(AnimationEvent animationEvent)
        {
            if (CanReceive(animationEvent))
                swordController.OpenAttackHitAnimationEvent(animationEvent.intParameter);
        }

        public void CloseAttackHitAnimationEvent(AnimationEvent animationEvent)
        {
            if (CanReceive(animationEvent))
                swordController.CloseAttackHitAnimationEvent();
        }

        private bool CanReceive(AnimationEvent animationEvent)
        {
            // 전환 중 남아 있는 이전 공격 클립의 이벤트는 새 공격에 적용하지 않는다.
            return swordController != null && swordController.IsAttackStateActive &&
                animationController != null && animationController.IsRequestedAnimationEvent(animationEvent);
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
