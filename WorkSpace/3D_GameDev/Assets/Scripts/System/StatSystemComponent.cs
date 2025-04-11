using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatSystemComponent : MonoBehaviour
{
    public CharacterStats playerStat;

    void Awake()
    {
        playerStat = GameObject.Find("Player").GetComponent<CharacterStatsComponent>().stats;
        //gameObject.SetActive(false);
    }

    public void UpAttack()
    {
        if (playerStat != null)
        {
            playerStat.attack += 1;
        }
    }

    public void DownAttack()
    {
        if(playerStat != null)
        {
            playerStat.attack -= 1;
        }
    }

    public void UpDex()
    {
        if (playerStat != null)
        {
            playerStat.dex += 1;
        }
    }

    public void DownDex()
    {
        if (playerStat != null)
        {
            playerStat.dex -= 1;
        }
    }


    public void UpHP()
    {
        if (playerStat != null)
        {
            playerStat.maxHP += 1;
        }
    }

    public void DownHP()
    {
        if (playerStat != null)
        {
            playerStat.maxHP -= 1;
        }
    }

    public void CloseLevelPanel()
    {
        gameObject.SetActive(false);
    }
}
