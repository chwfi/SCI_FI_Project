using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
    public State CurrentState { get; private set; }
    public State PrevState { get; private set; }

    public Dictionary<PlayerStateType, State> StateDictionary { get; private set; }

    public StateMachine()
    {
        StateDictionary = new Dictionary<PlayerStateType, State>();
    }

    public void Init(PlayerStateType stateType)
    {
        CurrentState = StateDictionary[stateType];
        CurrentState.EnterState();
    }

    public void ChangeState(PlayerStateType stateType)
    {
        PrevState = CurrentState;
        CurrentState.ExitState();
        CurrentState = StateDictionary[stateType];
        CurrentState.EnterState();
    }

    public void AddState(PlayerStateType stateType, State playerState)
    {
        StateDictionary.Add(stateType, playerState);
    }
}