
using UnityEngine;

public class Stage1 : Stage
{
    public GameObject Wall;

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    public override void StageClearCondition()
    {
        int currentLevel = Player.Instance.playerStateMachine.playerStat.level.currentLevel;
        if (currentLevel >= clearLevel)
        {
            Wall.SetActive(false);
            Portal.SetActive(true);
        }
    }

}
