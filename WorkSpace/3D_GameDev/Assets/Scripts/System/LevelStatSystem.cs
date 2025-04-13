using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelStatSystem : MonoBehaviour
{
    [SerializeField] private LevelUpComponent levelUpComp;
    [SerializeField] private LevelExpComponent expComp;
    [SerializeField] private PlayerStatUIComponent playerStatUIComp;

    private void Start()
    {
        levelUpComp = GetComponentInChildren<LevelUpComponent>(true);
        expComp = GetComponentInChildren<LevelExpComponent>(true);
        playerStatUIComp = GetComponentInChildren<PlayerStatUIComponent>(true);

        if (levelUpComp != null)
            levelUpComp.SetPlayerStatUI(playerStatUIComp);

        if (expComp != null)
        {
            expComp.SetPlayerStatUI(playerStatUIComp);
            expComp.UpdateEXPSlider();
        }
    }
}
