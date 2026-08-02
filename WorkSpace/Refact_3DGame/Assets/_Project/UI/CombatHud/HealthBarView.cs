using rudIsland.RPG3D.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace rudIsland.RPG3D.UI
{
    // Receives prepared health data and only draws one health bar.
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField] private GameObject barRoot; // 씬 또는 시스템 참조
        [SerializeField] private Image healthFill; // Inspector 설정 값
        [SerializeField] private Text healthText; // Inspector 설정 값
        [SerializeField] private Text targetNameText; // 대상 참조

        public bool IsVisible => // 기능 사용 여부
            barRoot != null && barRoot.activeSelf;

        public void Show(string targetName, UnitHealth health)
        {
            if (barRoot == null || health == null)
            {
                return;
            }

            if (targetNameText != null)
            {
                targetNameText.text = targetName;
            }

            UpdateHealth(health);
            barRoot.SetActive(true);
        }

        public void UpdateHealth(UnitHealth health)
        {
            if (health == null)
            {
                return;
            }

            if (healthFill != null)
            {
                healthFill.fillAmount =
                    Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);
            }

            if (healthText != null)
            {
                healthText.text = string.Format(
                    "{0:0} / {1:0}",
                    health.CurrentHealth,
                    health.MaxHealth);
            }
        }

        public void Hide()
        {
            if (barRoot != null)
            {
                barRoot.SetActive(false);
            }
        }

#if UNITY_EDITOR
        public void ConnectForEditor(
            GameObject root,
            Image fill,
            Text valueText,
            Text nameText)
        {
            barRoot = root;
            healthFill = fill;
            healthText = valueText;
            targetNameText = nameText;
        }
#endif
    }
}
