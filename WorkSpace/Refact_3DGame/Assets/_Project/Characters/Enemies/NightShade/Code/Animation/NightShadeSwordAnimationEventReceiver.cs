using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 공격 애니메이션 이벤트를 Animator 속도 변경으로 전달한다.
    [DisallowMultipleComponent]
    public sealed class NightShadeSwordAnimationEventReceiver : MonoBehaviour
    {
        [SerializeField]
        private NightShadeSwordAnimationController animationController;

        private void Awake()
        {
            FindAnimationController();
        }

        public void SetAttackSpeed(float speed)
        {
            animationController?.SetAttackPlaybackSpeed(speed);
        }

        public void ResetAttackSpeed()
        {
            animationController?.ResetAttackPlaybackSpeed();
        }

        private void FindAnimationController()
        {
            if (animationController == null)
            {
                animationController =
                    GetComponentInParent<
                        NightShadeSwordAnimationController>(true);
            }
        }

#if UNITY_EDITOR
        public void ConnectForEditor(
            NightShadeSwordAnimationController controller)
        {
            animationController = controller;
        }

        private void OnValidate()
        {
            FindAnimationController();
        }
#endif
    }
}
