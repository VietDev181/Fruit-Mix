using System;

public class GameService : IGameService
{
    public GameStateMachine StateMachine { get; private set; }
    private readonly ScoreUseCase scoreUseCase;
    private readonly ISaveSystem saveSystem;

    public event Action<int> OnScoreChanged;
    public event Action<int, int> OnGameOver;

    public GameService(ScoreUseCase scoreUseCase, ISaveSystem saveSystem)
    {
        this.scoreUseCase = scoreUseCase;
        this.saveSystem = saveSystem;
        this.scoreUseCase.OnScoreChanged += (score) => OnScoreChanged?.Invoke(score);
    }

    public void SetStateMachine(GameStateMachine machine)
    {
        StateMachine = machine;
    }

    public void AddScore(int value)
    {
        scoreUseCase.AddScore(value);
    }

    public void ResetGame()
    {
        scoreUseCase.Reset();
    }

    public void GameOver()
    {
        int current = scoreUseCase.GetCurrentScore();
        int high = scoreUseCase.GetHighScore();
        saveSystem.SaveHighScore(high);
        OnGameOver?.Invoke(current, high);
        StateMachine.ChangeState(GameStateType.GameOver);
    }
}
