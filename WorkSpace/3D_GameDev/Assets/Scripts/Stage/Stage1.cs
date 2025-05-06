
using UnityEngine;

public class Stage1 : Stage
{
    private GameObject Wall;

    protected override void Start()
    {
        base.Start();
    }
    public override void StageClearCondition()
    {
        int currentLevel = Player.Instance.playerStateMachine.playerStat.level.currentLevel;
        if (currentLevel > clearLevel)
        {
            Wall.SetActive(false);
            Portal.SetActive(true);
        }
    }

}
