
using UnityEngine;
using UnityEngine.UI;

public class LevelExpComponent : MonoBehaviour
{
    [SerializeField] private PlayerStatUIComponent playerStatUIComp;
    [SerializeField] private Slider expSlider;

    public void SetPlayerStatUI(PlayerStatUIComponent ui)
    {
        playerStatUIComp = ui;
    }

    public void UpdateEXPSlider()
    {
        expSlider.maxValue = playerStatUIComp.playerStatComp.playerStats.level.MaxExp;
        expSlider.value = playerStatUIComp.playerStatComp.playerStats.level.currentExp;

        if (playerStatUIComp.playerStatComp.playerStats.level.IsLevelUp && playerStatUIComp.playerStatComp.playerStats.level.TryLevelUp())
        {
            expSlider.maxValue = playerStatUIComp.playerStatComp.playerStats.level.MaxExp;
            expSlider.value = playerStatUIComp.playerStatComp.playerStats.level.currentExp;
            playerStatUIComp.UpdateLevelText();
            gameObject.SetActive(true);
        }
    }
}