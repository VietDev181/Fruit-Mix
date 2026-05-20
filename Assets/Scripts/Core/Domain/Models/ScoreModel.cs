public class ScoreModel 
{
    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }

    public ScoreModel(int highScore)
    {
        HighScore = highScore;
    }

    public void AddScore(int value)
    {
        CurrentScore += value;
        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
        }
    }

    public void Reset()
    {
        CurrentScore = 0;
    }
}
