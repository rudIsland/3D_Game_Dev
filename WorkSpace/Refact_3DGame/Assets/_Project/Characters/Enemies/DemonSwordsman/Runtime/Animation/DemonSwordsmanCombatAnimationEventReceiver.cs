using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    [DisallowMultipleComponent]
    // DemonSwordsman Animation Event를 현재 보스 상태에 안전하게 전달한다.
    public sealed class DemonSwordsmanCombatAnimationEventReceiver :
        MonoBehaviour
    {
        private DemonSwordsmanController bossController; // 씬 또는 시스템 참조

        private void Awake()
        {
            bossController =
                GetComponentInParent<DemonSwordsmanController>();

            if (bossController == null)
            {
                Debug.LogError(
                    "DemonSwordsmanCombatAnimationEventReceiver가 보스 Controller를 찾지 못했습니다.",
                    this);
                enabled = false;
            }
        }

        public void OpenBranchWindow()
        {
            bossController?.OpenBranchWindow();
        }

        public void SwapWeapon()
        {
            bossController?.SwapWeapon();
        }

        public void FinishAction()
        {
            bossController?.FinishAction();
        }

    }
}
