
using UnityEngine;

public class Stage3 : Stage 
{
    protected override void Start()
    {
        base.Start();
    }

    public override void StageClearCondition()
    {
        int currentLevel = Player.Instance.playerStateMachine.playerStat.level.currentLevel;
        if (currentLevel > clearLevel)
            Portal.SetActive(true);
    }

}
