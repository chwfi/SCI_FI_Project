public class State
{
    protected Entity _owner;
    protected StateMachine _stateMachine;

    public State(Entity owner, StateMachine stateMachine)
    {
        _owner = owner;
        _stateMachine = stateMachine;
    }

    public virtual void EnterState()
    {
    }

    public virtual void UpdateState()
    { 
    }

    public virtual void ExitState()
    {
    }
}
