using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyGUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI levelText;

    private Transform _mainCameraTransform;

    void Start()
    {
        if (Camera.main != null)
            _mainCameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (_mainCameraTransform == null) return;

        Vector3 targetDirection = _mainCameraTransform.position - transform.position;
        targetDirection.y = 0;

        if (targetDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(-targetDirection);
        }
    }

    // --- 데이터 연동 함수들 ---

    public void SetLevel(int level)
    {
        if (levelText != null) levelText.text = $"{level}";
    }

    public void UpdateHPUI(float current, float max)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = max;
            hpSlider.value = Mathf.Max(0, current); // 0 아래로 내려가지 않게 방지
        }

        if (hpText != null)
        {
            hpText.text = $"{Mathf.Max(0, current):F0} / {max:F0}";
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}