namespace Characters.Player.StateMachine
{
    internal interface IPlayerState
    {
        void Enter();
        void Update(float deltaTime, PlayerStateInput input);
        void Exit();
    }
}
