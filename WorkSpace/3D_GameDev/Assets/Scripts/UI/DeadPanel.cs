using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeadPanel : MonoBehaviour, IPointerClickHandler
{
    private void Start()
    {
        ActiveDeadPanel(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //������ ������ 1�� ������ �����ϰ�
        if (GetComponent<Image>().color.a >= 1)
        {
            Debug.Log("Dead�ǳ� Ŭ����!!!");

            if (Player.Instance != null)
                Destroy(Player.Instance.gameObject);

            if (UIManager.Instance != null)
                Destroy(UIManager.Instance.gameObject);

            if (StageManager.Instance != null)
                Destroy(StageManager.Instance.gameObject);

            SceneManager.LoadScene("0_Start", LoadSceneMode.Single);

            GameManager.Instance.GameReset(); //���� �����
        }

        ActiveDeadPanel(false);

    }

    public void ShowDeadPanel(float time)
    {
        Image image = GetComponent<Image>();
        Color color = image.color;
        color.a = time;
        image.color = color;
    }

    public void ActiveDeadPanel(bool onOff)
    {
        gameObject.SetActive(onOff);
    }


}
