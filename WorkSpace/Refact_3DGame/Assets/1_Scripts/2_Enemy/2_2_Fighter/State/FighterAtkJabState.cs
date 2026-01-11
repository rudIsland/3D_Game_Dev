using UnityEngine;
public class FighterAtkJabState : FighterAtkBaseState
{
    public override int AttackIndex => 0;

    public FighterAtkJabState(EnemyStateMachine stateMachine, Fighter fighter, AttackData attackData) : base(stateMachine, fighter, attackData)
    {
    }

}
