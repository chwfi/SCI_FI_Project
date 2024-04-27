using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public StateMachine StateMachine;

    private void Awake()
    {
        StateMachine = new StateMachine();
    }
}
