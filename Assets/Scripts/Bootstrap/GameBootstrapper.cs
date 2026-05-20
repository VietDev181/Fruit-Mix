using UnityEngine;
using System.Collections.Generic;

public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private AudioService audioServiceMono;
    [SerializeField] private SceneService sceneServiceMono;
    [SerializeField] private GameController gameController;
    [SerializeField] private UIGame uiGame;
    [SerializeField] private UISetting uiSetting;
    [SerializeField] private BrewingManager brewingManager; // optional: brewing gameplay

    private void Awake()
    {
        // Infrastructure
        IAudioService audio = audioServiceMono;
        ISceneService sceneService = sceneServiceMono;
        ISaveSystem saveSystem = new SaveSystem();

        // Core
        ScoreModel scoreModel = new ScoreModel(saveSystem.LoadHighScore());
        ScoreUseCase scoreUseCase = new ScoreUseCase(scoreModel);
        GameService gameService = new GameService(scoreUseCase, saveSystem);

        // State machine
        var states = new Dictionary<GameStateType, IGameState>
        {
            { GameStateType.Playing, new PlayingState(gameService) },
            { GameStateType.GameOver, new GameOverState() }
        };

        GameStateMachine stateMachine = new GameStateMachine(states);
        gameService.SetStateMachine(stateMachine);

        // Inject
        gameController.Initialize(gameService, audio);
        uiGame.Initialize(audio, sceneService);
        uiSetting.Initialize(audio);

        // Optional: let the brewing gameplay award score on a finished drink.
        if (brewingManager != null)
            brewingManager.BindGameService(gameService);

        // Pause/Resume
        uiGame.OnPauseRequested += () => Time.timeScale = 0f;
        uiGame.OnResumeRequested += () => Time.timeScale = 1f;

        // Start game
        stateMachine.ChangeState(GameStateType.Playing);
    }
}
