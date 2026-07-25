
public class FighterAtkKickState : FighterAtkBaseState
{
    public override int AttackIndex => 3;

    public FighterAtkKickState(EnemyStateMachine stateMachine, Fighter fighter, AttackData attackData) : base(stateMachine, fighter, attackData)
    {
    }
}
