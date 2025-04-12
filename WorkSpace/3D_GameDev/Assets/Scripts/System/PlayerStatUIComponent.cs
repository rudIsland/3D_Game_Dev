using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerStatUIComponent : MonoBehaviour
{
    public TextMeshProUGUI[] statText = new TextMeshProUGUI[3];
    public PlayerStatUI playerStatUI;

    private string STR_TXT = "STR: ";
    private string DEX_TXT = "DEX: ";
    private string HP_TXT = "HP: ";

    private void Awake()
    {
        playerStatUI = new PlayerStatUI();
    }

    private void Start()
    {
        UpdateStatText(0);
        UpdateStatText(1);
        UpdateStatText(2);
    }

    public void UpdateStatText(int Index)
    {
        switch(Index)
        {
            case 0:
                statText[0].text = STR_TXT + playerStatUI.STR.ToString();
                break;
            case 1:
                statText[1].text = DEX_TXT + playerStatUI.DEF.ToString();
                break;
            case 2:
                statText[2].text = HP_TXT + playerStatUI.HP.ToString();
                break;

        }
    }
}
