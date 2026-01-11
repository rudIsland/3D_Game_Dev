
public class FighterAtkHookState : FighterAtkBaseState
{
    public override int AttackIndex => 2;

    public FighterAtkHookState(EnemyStateMachine stateMachine, Fighter fighter, AttackData attackData) : base(stateMachine, fighter, attackData)
    {
    }
}
