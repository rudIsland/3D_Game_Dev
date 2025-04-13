using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelUpComponent : MonoBehaviour
{
    [SerializeField] private CharacterStats playerStat;
    private PlayerStatUIComponent playerStatUIComp;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void SetPlayerStatUI(PlayerStatUIComponent ui)
    {
        playerStatUIComp = ui;
    }

    public void SetPlayerStat(CharacterStats stats)
    {
        playerStat = stats;
    }

    public void UpAttack()
    {
        if (playerStat != null)
        {
            playerStat.ATK += 2;
            playerStatUIComp.UpdateStatText(0);
        }
    }

    public void DownAttack()
    {
        if(playerStat != null)
        {
            playerStat.ATK -= 2;
            playerStatUIComp.UpdateStatText(0);
        }
    }

    public void UpDef()
    {
        if (playerStat != null)
        {
            playerStat.DEF += 1;
            playerStatUIComp.UpdateStatText(1);
        }
    }

    public void DownDef()
    {
        if (playerStat != null)
        {
            playerStat.DEF -= 1;
            playerStatUIComp.UpdateStatText(1);
        }
    }


    public void UpHP()
    {
        if (playerStat != null)
        {
            playerStat.maxHP += 20;
            playerStatUIComp.UpdateStatText(2);
        }
    }

    public void DownHP()
    {
        if (playerStat != null)
        {
            playerStat.maxHP -= 20;
            playerStatUIComp.UpdateStatText(2);
        }
    }

    public void CloseLevelPanel()
    {
        gameObject.SetActive(false);
    }
}
