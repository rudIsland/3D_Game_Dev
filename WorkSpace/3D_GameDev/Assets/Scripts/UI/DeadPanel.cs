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
        //투명도가 완전히 1이 됐을때 가능하게
        if (GetComponent<Image>().color.a >= 1)
        {
            Debug.Log("Dead판넬 클릭중!!!");

            if (Player.Instance != null)
                Destroy(Player.Instance.gameObject);

            if (UIManager.Instance != null)
                Destroy(UIManager.Instance.gameObject);

            if (StageManager.Instance != null)
                Destroy(StageManager.Instance.gameObject);

            SceneManager.LoadScene("0_Start", LoadSceneMode.Single);

            GameManager.Instance.GameReset(); //게임 재시작
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
