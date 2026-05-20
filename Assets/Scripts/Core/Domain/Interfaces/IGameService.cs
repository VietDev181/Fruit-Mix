using System;

public interface IGameService
{
    GameStateMachine StateMachine { get;}

    event Action<int> OnScoreChanged;
    event Action<int, int> OnGameOver;

    void AddScore(int value);
    void ResetGame();
    void GameOver();
}
