using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // �����̴��� ���� �ʿ�

public class SteminaComponent : MonoBehaviour
{
    [Header("���¹̳�")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;

    public float regenRate = 7.5f;      // �ʴ� ȸ����
    public float regenDelay = 2f;     // ������ �Һ� �� ȸ�� ������

    private float timeSinceLastUse = 0f;

    [Header("UI")]
    public Slider steminaSlider;

    private void Start()
    {
        steminaSlider = GameObject.Find("PlayerStemina").GetComponent<Slider>();

        if (steminaSlider != null)
        {
            steminaSlider.maxValue = maxStamina;
            steminaSlider.value = currentStamina;
        }
    }

    public bool CanUse(float amount)
    {
        return currentStamina >= amount;
    }

    public void Use(float amount)
    {
        currentStamina = Mathf.Max(currentStamina - amount, 0f);
        timeSinceLastUse = 0f;

        UpdateSlider();
    }

    private void Update()
    {
        timeSinceLastUse += Time.deltaTime;

        if (timeSinceLastUse >= regenDelay && currentStamina < maxStamina)
        {
            currentStamina += regenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);

            UpdateSlider();
        }
    }

    private void UpdateSlider()
    {
        if (steminaSlider != null)
        {
            steminaSlider.value = currentStamina;
        }
    }
}
