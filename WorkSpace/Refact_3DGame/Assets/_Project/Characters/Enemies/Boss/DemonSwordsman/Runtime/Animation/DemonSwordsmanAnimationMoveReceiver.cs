using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    // 공격 애니메이션의 수평 이동량만 보스 루트에 전달한다.
    public sealed class DemonSwordsmanAnimationMoveReceiver : MonoBehaviour
    {
        private Animator bossAnimator;
        private DemonSwordsmanController bossController;

        private void Awake()
        {
            bossAnimator = GetComponent<Animator>();
            bossController = GetComponentInParent<DemonSwordsmanController>();

            if (bossController == null)
            {
                Debug.LogError(
                    "DemonSwordsmanAnimationMoveReceiver가 보스 루트 Controller를 찾지 못했습니다.",
                    this);
                enabled = false;
            }
        }

        private void OnAnimatorMove()
        {
            if (bossController == null)
            {
                return;
            }

            bossController.ApplyAttackAnimationMove(
                bossAnimator.deltaPosition);
        }
    }
}
