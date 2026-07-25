
public class FighterAtkFlyKickState : FighterAtkBaseState
{
    public override int AttackIndex => 5;
    public FighterAtkFlyKickState(EnemyStateMachine stateMachine, Fighter fighter, AttackData attackData) : base(stateMachine, fighter, attackData)
    {

    }

}
