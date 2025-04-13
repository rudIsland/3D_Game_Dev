using TMPro;
using UnityEngine;

public class PlayerStatUIComponent : MonoBehaviour
{
    public TextMeshProUGUI[] statText = new TextMeshProUGUI[3];
    public TextMeshProUGUI levelText;

    private string STR_TXT = "STR: ";
    private string DEF_TXT = "DEF: ";
    private string HP_TXT = "HP: ";
    private string LEVEL_TXT = "Level: ";

    [Header("외부에서 가져올 오브젝트")]
    public PlayerStatComponent playerStatComp;

    public void Init(PlayerStatComponent comp)
    {
        playerStatComp = comp;
        UpdateAll();
    }

    public void UpdateStatText(int index)
    {
        if (playerStatComp.playerStats == null) return;

        switch (index)
        {
            case 0:
                statText[0].text = STR_TXT + playerStatComp.playerStats.ATK;
                break;
            case 1:
                statText[1].text = DEF_TXT + playerStatComp.playerStats.DEF;
                break;
            case 2:
                statText[2].text = HP_TXT + playerStatComp.playerStats.maxHP;
                break;
        }
    }

    public void UpdateLevelText()
    {
        if (playerStatComp.playerStats != null)
        {
            levelText.text = LEVEL_TXT + playerStatComp.playerStats.level.currentLevel.ToString();
        }
    }

    public void UpdateAll()
    {
        UpdateStatText(0);
        UpdateStatText(1);
        UpdateStatText(2);
        UpdateLevelText();
    }
}
