using System;
using System.Collections.Generic;
using UnityEngine;

public class AIStateMachine<TContext> where TContext : MonoBehaviour
{
    readonly Dictionary<Type, IAIState<TContext>> states = new();
    IAIState<TContext> currentState;
    TContext context;

    public Type CurrentStateType => currentState?.GetType();
    public event Action<Type> OnStateChanged;

    public void Initialize(TContext owner, params IAIState<TContext>[] registeredStates)
    {
        context = owner;

        foreach (var state in registeredStates)
            states[state.GetType()] = state;
    }

    public void Start<TState>() where TState : IAIState<TContext>
    {
        ChangeState(typeof(TState));
    }

    public void ChangeState(Type stateType)
    {
        if (currentState != null)
            currentState.Exit(context);

        if (!states.TryGetValue(stateType, out currentState))
        {
            Debug.LogError($"AI state not registered: {stateType.Name}");
            return;
        }

        currentState.Enter(context);
        OnStateChanged?.Invoke(stateType);
    }

    public void Tick(float deltaTime)
    {
        currentState?.Tick(context, deltaTime);
    }
}
