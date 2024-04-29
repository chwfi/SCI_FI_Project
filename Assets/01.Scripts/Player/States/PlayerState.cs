public class PlayerState : State
{
    protected PlayerController _player => _owner as PlayerController;

    public PlayerState(Entity owner, StateMachine stateMachine) : base(owner, stateMachine)
    {
    }
}
