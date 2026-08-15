using UnityEngine;
using UnityEngine.UI;

namespace rudIsland.RPG3D.UI
{
    // 계산된 Stamina 값만 받아 플레이어 Stamina 바를 그린다.
    public sealed class StaminaBarView : MonoBehaviour
    {
        [SerializeField] private GameObject barRoot;
        [SerializeField] private Image staminaFill;

        public void Show(float currentStamina, float maxStamina)
        {
            if (barRoot == null)
            {
                return;
            }

            UpdateStamina(currentStamina, maxStamina);
            barRoot.SetActive(true);
        }

        public void UpdateStamina(
            float currentStamina,
            float maxStamina)
        {
            if (staminaFill == null)
            {
                return;
            }

            staminaFill.fillAmount =
                maxStamina > 0f
                    ? Mathf.Clamp01(currentStamina / maxStamina)
                    : 0f;
        }

        public void Hide()
        {
            if (barRoot != null)
            {
                barRoot.SetActive(false);
            }
        }

#if UNITY_EDITOR
        public void ConnectForEditor(GameObject root, Image fill)
        {
            barRoot = root;
            staminaFill = fill;
        }
#endif
    }
}
