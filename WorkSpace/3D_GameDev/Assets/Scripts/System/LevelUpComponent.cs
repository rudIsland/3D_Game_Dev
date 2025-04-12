using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelUpComponent : MonoBehaviour
{
    public CharacterStats playerStat;
    public PlayerStatUIComponent playerStatUIComp;

    void Awake()
    {
        playerStat = GameObject.Find("Player").GetComponent<PlayerStatComponent>().stats;
        gameObject.SetActive(false);
    }

    public void UpAttack()
    {
        if (playerStat != null)
        {
            playerStatUIComp.playerStatUI.STR += 1;
            playerStat.attack += 2;
            playerStatUIComp.UpdateStatText(0);
        }
    }

    public void DownAttack()
    {
        if(playerStat != null)
        {
            playerStatUIComp.playerStatUI.STR -= 1;
            playerStat.attack -= 2;
            playerStatUIComp.UpdateStatText(0);
        }
    }

    public void UpDef()
    {
        if (playerStat != null)
        {
            playerStatUIComp.playerStatUI.DEF += 1;
            playerStat.def += 1;
            playerStatUIComp.UpdateStatText(1);
        }
    }

    public void DownDef()
    {
        if (playerStat != null)
        {
            playerStatUIComp.playerStatUI.DEF -= 1;
            playerStat.def -= 1;
            playerStatUIComp.UpdateStatText(1);
        }
    }


    public void UpHP()
    {
        if (playerStat != null)
        {
            playerStatUIComp.playerStatUI.HP += 1;
            playerStat.maxHP += 20;
            playerStatUIComp.UpdateStatText(2);
        }
    }

    public void DownHP()
    {
        if (playerStat != null)
        {
            playerStatUIComp.playerStatUI.HP -= 1;
            playerStat.maxHP -= 20;
            playerStatUIComp.UpdateStatText(2);
        }
    }

    public void CloseLevelPanel()
    {
        gameObject.SetActive(false);
    }
}
