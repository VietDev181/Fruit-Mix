public class PlayingState : IGameState
{
    private GameService gameService;

    public PlayingState(GameService service)
    {
        gameService = service;
    }

    public void Enter()
    {
        
    }

    public void Exit() { }
    public void Update() { }
}
