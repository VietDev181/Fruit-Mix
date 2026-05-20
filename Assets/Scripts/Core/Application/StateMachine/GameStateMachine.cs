using System.Collections.Generic;

public class GameStateMachine
{
    private IGameState currentState;
    private Dictionary<GameStateType, IGameState> states;
    public GameStateType CurrentStateType { get; private set; }
    public GameStateMachine(Dictionary<GameStateType, IGameState> states)
    {
        this.states = states;
    }

    public void ChangeState(GameStateType stateType)
    {
        if (!states.ContainsKey(stateType))
        {
            return;
        }
        currentState?.Exit();
        currentState = states[stateType];
        CurrentStateType = stateType;
        currentState.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }
}
