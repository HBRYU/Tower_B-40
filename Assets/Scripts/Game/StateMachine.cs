using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Designed by GPT-4.
// Used for holding current state of various AIs and accessing them through IState.

public interface IState
{
    void Enter();
    void Update();
    void Exit();
}

public class StateMachine {
    private IState currentState;

    public void ChangeState(IState newState) {
        if (currentState != null)
            currentState.Exit();

        currentState = newState;
        // Debug.Log("Updated State");
        currentState.Enter();
    }

    public void ChangeStateIfNot(IState newState)
    {
        if (currentState != newState)
        {
            // Debug.Log("Updated State");
            currentState.Exit();
            currentState = newState;
            currentState.Enter();
        }
    }

    public bool CompareType(IState other)
    {
        return currentState.GetType() == other.GetType();
    }

    public void Update() {
        if (currentState != null)
            currentState.Update();
    }
}
