using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private UIGame uiGame;

    private IGameService gameService;
    private IAudioService audioService;

    public void Initialize(IGameService service, IAudioService audio)
    {
        gameService = service;
        audioService = audio;

        gameService.OnScoreChanged += uiGame.UpdateScore;
        gameService.OnGameOver += HandleGameOver;
    }

    private void Start()
    {
        audioService.PlayGameBGM();
    }

    private void Update()
    {
        if (gameService.StateMachine.CurrentStateType != GameStateType.Playing)
            return;
        gameService.StateMachine.Update();
    }

    private void HandleGameOver(int current, int high)
    {
        uiGame.ShowGameOver(current, high);
    }

    private void OnDestroy()
    {
        if (gameService != null)
        {
            gameService.OnScoreChanged -= uiGame.UpdateScore;
            gameService.OnGameOver -= HandleGameOver;
        }
    }
}
