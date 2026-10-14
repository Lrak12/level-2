using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Displays session progress and connects the Pause, Restart, Exit, and Next buttons.</summary>
public sealed class IslandGameHUD : MonoBehaviour
{
    [SerializeField] private IslandGameSession session;
    [SerializeField] private TribalPlayerController player;
    [SerializeField] private IslandRainCycle weather;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text checkpointText;
    [SerializeField] private TMP_Text weatherText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button pauseExitButton;
    [SerializeField] private Button resultRestartButton;
    [SerializeField] private Button resultExitButton;
    [SerializeField] private Button nextLevelButton;

    public void Configure(IslandGameSession game, TribalPlayerController owner, IslandRainCycle rain,
        TMP_Text score, TMP_Text lives, TMP_Text timer, TMP_Text objective, TMP_Text stamina,
        TMP_Text checkpoint, TMP_Text weatherLabel, TMP_Text result,
        GameObject pauseMenu, GameObject outcomeMenu, Button pause, Button resume,
        Button restart, Button pauseExit, Button outcomeRestart, Button outcomeExit, Button next)
    {
        session = game;
        player = owner;
        weather = rain;
        scoreText = score;
        livesText = lives;
        timeText = timer;
        objectiveText = objective;
        staminaText = stamina;
        checkpointText = checkpoint;
        weatherText = weatherLabel;
        resultText = result;
        pausePanel = pauseMenu;
        resultPanel = outcomeMenu;
        pauseButton = pause;
        resumeButton = resume;
        restartButton = restart;
        pauseExitButton = pauseExit;
        resultRestartButton = outcomeRestart;
        resultExitButton = outcomeExit;
        nextLevelButton = next;
    }

    private void Awake()
    {
        if (session == null)
            session = FindFirstObjectByType<IslandGameSession>();
        if (pauseButton != null) pauseButton.onClick.AddListener(session.TogglePause);
        if (resumeButton != null) resumeButton.onClick.AddListener(session.TogglePause);
        if (restartButton != null) restartButton.onClick.AddListener(session.Restart);
        if (pauseExitButton != null) pauseExitButton.onClick.AddListener(session.Exit);
        if (resultRestartButton != null) resultRestartButton.onClick.AddListener(session.Restart);
        if (resultExitButton != null) resultExitButton.onClick.AddListener(session.Exit);
        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(session.LoadNextLevel);
    }

    private void LateUpdate()
    {
        if (session == null || player == null)
            return;
        scoreText.text = "SCORE  " + session.Score.ToString("0000");
        livesText.text = "LIVES  " + session.Lives;
        timeText.text = "TIME  " + Mathf.CeilToInt(session.TimeLeft) + "s";
        objectiveText.text = session.ObjectiveText;
        staminaText.text = "STAMINA  " + Mathf.CeilToInt(player.Stamina) + " / " + Mathf.CeilToInt(player.MaxStamina);
        checkpointText.text = "CHECKPOINT  " + session.ActiveCheckpoint + " / 3";
        weatherText.text = "WEATHER  " + (weather != null ? weather.ConditionLabel : "CLEAR");
        pausePanel.SetActive(session.IsPaused);
        resultPanel.SetActive(session.IsFinished && !session.IsEnding);
        if (session.IsFinished)
            resultText.text = session.Won ? "VICTORY!  SCORE " + session.Score :
                session.Lives <= 0 ? "OUT OF LIVES" : "TIME UP";
        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(session.IsFinished && session.Won && session.HasNextLevel);
    }
}
