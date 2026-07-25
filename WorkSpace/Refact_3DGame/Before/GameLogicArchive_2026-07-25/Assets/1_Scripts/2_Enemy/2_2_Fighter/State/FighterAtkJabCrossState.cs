
public class FighterAtkJabCrossState : FighterAtkBaseState
{
    public override int AttackIndex => 4;

    public FighterAtkJabCrossState(EnemyStateMachine stateMachine, Fighter fighter, AttackData attackData) : base(stateMachine, fighter, attackData)
    {
    }
}
