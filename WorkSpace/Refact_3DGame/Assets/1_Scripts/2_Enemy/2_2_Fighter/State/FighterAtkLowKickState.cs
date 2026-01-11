
public class FighterAtkLowKickState : FighterAtkBaseState
{
    public override int AttackIndex => 1;

    public FighterAtkLowKickState(EnemyStateMachine stateMachine, Fighter fighter, AttackData attackData) : base(stateMachine, fighter, attackData)
    {
    }

}
