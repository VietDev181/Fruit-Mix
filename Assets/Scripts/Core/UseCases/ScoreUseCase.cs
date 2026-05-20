using System;

public class ScoreUseCase
{
    private ScoreModel model;
    public event Action<int> OnScoreChanged;

    public ScoreUseCase(ScoreModel model)
    {
        this.model = model;
    }

    public void AddScore(int value)
    {
        model.AddScore(value);
        OnScoreChanged?.Invoke(model.CurrentScore);
    }

    public void Reset()
    {
        model.Reset();
        OnScoreChanged?.Invoke(model.CurrentScore);
    }

    public int GetCurrentScore() => model.CurrentScore;
    public int GetHighScore() => model.HighScore;
}
