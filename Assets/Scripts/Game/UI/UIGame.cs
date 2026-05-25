using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIGame : MonoBehaviour
{
    private IAudioService _audio;
    private ISceneService _sceneService;

    public Button homeButton;
    public Button pauseButton;
    public Button settingButton;
    public Button resumeButton;
    public Button resumeSettingButton;
    public Button replayButton;
    public Button replayGameOverButton;

    [Header("UI Texts")]
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private TextMeshProUGUI gameOverHighScoreText;
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("UI Panel")]
    public GameObject pausePanel;
    public GameObject setttingPanel;
    public GameObject gameOverPanel;

    public event Action OnPauseRequested;
    public event Action OnResumeRequested;

    public void Initialize(IAudioService audio, ISceneService sceneService)
    {
        _audio = audio;
        _sceneService = sceneService;

        homeButton.onClick.AddListener(OnHomeGame);
        pauseButton.onClick.AddListener(OnPauseGame);
        settingButton.onClick.AddListener(OnSettingGame);
        resumeButton.onClick.AddListener(OnResumeGame);
        resumeSettingButton.onClick.AddListener(OnResumeGame);
        replayButton.onClick.AddListener(OnReplayGame);
        replayGameOverButton.onClick.AddListener(OnReplayGame);

        pausePanel.SetActive(false);
        setttingPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }

    private void OnHomeGame()
    {
        _audio.PlayClickSFX();
        _sceneService.LoadSelectScene();
    }

    private void OnPauseGame()
    {
        _audio.PlayClickSFX();
        pausePanel.SetActive(true);
        OnPauseRequested?.Invoke();
    }

    private void OnSettingGame()
    {
        _audio.PlayClickSFX();
        setttingPanel.SetActive(true);
        OnPauseRequested?.Invoke();
    }

    private void OnResumeGame()
    {
        _audio.PlayClickSFX();
        pausePanel.SetActive(false);
        setttingPanel.SetActive(false);
        OnResumeRequested?.Invoke();
    }

    private void OnReplayGame()
    {
        _audio.PlayClickSFX();
        _audio.PlayGameBGM();
        _sceneService.ReloadCurrentScene();
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    public void ShowGameOver(int currentScore, int highScore)
    {
        _audio.PlayGameOverBGM();
        gameOverPanel.SetActive(true);
        OnPauseRequested?.Invoke();

        gameOverScoreText.text = "Score: " + currentScore;
        gameOverHighScoreText.text = "Best Score\n" + highScore;
    }
}
