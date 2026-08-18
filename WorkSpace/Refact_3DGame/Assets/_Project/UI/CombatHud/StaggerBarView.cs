using UnityEngine;
using UnityEngine.UI;

namespace rudIsland.RPG3D.UI
{
    // 계산된 적 경직 값만 받아 화면 상단 경직 바를 그린다.
    public sealed class StaggerBarView : MonoBehaviour
    {
        [SerializeField] private GameObject barRoot;
        [SerializeField] private Image staggerFill;
        [SerializeField] private Text staggerText;

        private int displayedCurrentStagger = int.MinValue;
        private int displayedMaxStagger = int.MinValue;

        public void Show(float currentStagger, float maxStagger)
        {
            if (barRoot == null)
            {
                return;
            }

            UpdateStagger(currentStagger, maxStagger);
            barRoot.SetActive(true);
        }

        public void UpdateStagger(float currentStagger, float maxStagger)
        {
            if (staggerFill != null)
            {
                staggerFill.fillAmount =
                    maxStagger > 0f
                        ? Mathf.Clamp01(currentStagger / maxStagger)
                        : 0f;
            }

            UpdateStaggerText(currentStagger, maxStagger);
        }

        public void Hide()
        {
            if (barRoot != null)
            {
                barRoot.SetActive(false);
            }
        }

        private void UpdateStaggerText(float currentStagger, float maxStagger)
        {
            if (staggerText == null)
            {
                return;
            }

            int currentValue = Mathf.CeilToInt(currentStagger);
            int maxValue = Mathf.CeilToInt(maxStagger);
            if (displayedCurrentStagger == currentValue &&
                displayedMaxStagger == maxValue)
            {
                return;
            }

            displayedCurrentStagger = currentValue;
            displayedMaxStagger = maxValue;
            staggerText.text = string.Format(
                "STAGGER {0} / {1}",
                currentValue,
                maxValue);
        }

#if UNITY_EDITOR
        public void ConnectForEditor(
            GameObject root,
            Image fill,
            Text valueText)
        {
            barRoot = root;
            staggerFill = fill;
            staggerText = valueText;
        }
#endif
    }
}
