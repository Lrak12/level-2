// Displays session information and connects gameplay menu buttons to IslandGameSession.
// Study guide and script map: CODE_GUIDE.md in the project root.

using TMPro; // Imports TextMeshPro types such as TMP_Text for displaying text in the interface.
using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.
using UnityEngine.UI; // Imports Canvas-based UI types such as Button and Image.

/// <summary>Displays session progress and connects the Pause, Restart, Exit, and Next buttons.</summary>
public sealed class IslandGameHUD : MonoBehaviour // Defines the attachable gameplay HUD component; sealed prevents other classes from inheriting it.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    [SerializeField] private IslandGameSession session; // Saves the session that owns score, lives, time, objectives, and game-state actions; SerializeField exposes it in the Inspector.
    [SerializeField] private TribalPlayerController player; // Saves the player controller used to read stamina values.
    [SerializeField] private IslandRainCycle weather; // Saves the weather component used to read the current weather label.
    [SerializeField] private TMP_Text scoreText; // Saves the TextMeshPro label used to display the score.
    [SerializeField] private TMP_Text livesText; // Saves the TextMeshPro label used to display remaining lives.
    [SerializeField] private TMP_Text timeText; // Saves the label used to display the remaining level time.
    [SerializeField] private TMP_Text objectiveText; // Saves the label used to display the current objective description.
    [SerializeField] private TMP_Text staminaText; // Saves the label used to display current and maximum stamina.
    [SerializeField] private TMP_Text checkpointText; // Saves the label used to display checkpoint progress.
    [SerializeField] private TMP_Text weatherText; // Saves the label used to display the weather condition.
    [SerializeField] private TMP_Text resultText; // Saves the label used to display victory or a loss reason.
    [SerializeField] private GameObject pausePanel; // Saves the pause-menu GameObject so the entire panel can be shown or hidden.
    [SerializeField] private GameObject resultPanel; // Saves the results-panel GameObject so it can appear after the game finishes.
    [SerializeField] private Button pauseButton; // Saves the button that requests a pause-state change.
    [SerializeField] private Button resumeButton; // Saves the pause panel's Resume button.
    [SerializeField] private Button restartButton; // Saves the pause panel's Restart button.
    [SerializeField] private Button pauseExitButton; // Saves the pause panel's Exit button.
    [SerializeField] private Button resultRestartButton; // Saves the results panel's Restart button.
    [SerializeField] private Button resultExitButton; // Saves the results panel's Exit button.
    [SerializeField] private Button nextLevelButton; // Saves the button used to advance to the next level after a victory.

    // Store the gameplay references, labels, panels, and buttons that this HUD will update.
    public void Configure(IslandGameSession game, TribalPlayerController owner, IslandRainCycle rain, // Begins the setup method's parameters with the session, player, and weather components.
        TMP_Text score, TMP_Text lives, TMP_Text timer, TMP_Text objective, TMP_Text stamina, // Continues the parameters with the score, lives, timer, objective, and stamina text components.
        TMP_Text checkpoint, TMP_Text weatherLabel, TMP_Text result, // Adds the checkpoint, weather, and outcome text components to the setup parameters.
        GameObject pauseMenu, GameObject outcomeMenu, Button pause, Button resume, // Adds both panel GameObjects and the Pause and Resume buttons.
        Button restart, Button pauseExit, Button outcomeRestart, Button outcomeExit, Button next) // Finishes the parameter list with restart, exit, and next-level buttons; public lets setup tools call this method.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        session = game; // Stores the supplied game-session reference.
        player = owner; // Stores the supplied player-controller reference.
        weather = rain; // Stores the supplied weather-component reference.
        scoreText = score; // Connects the score field to its supplied text component.
        livesText = lives; // Connects the lives field to its supplied text component.
        timeText = timer; // Connects the countdown field to its supplied text component.
        objectiveText = objective; // Connects the objective field to its supplied text component.
        staminaText = stamina; // Connects the stamina field to its supplied text component.
        checkpointText = checkpoint; // Connects the checkpoint field to its supplied text component.
        weatherText = weatherLabel; // Connects the weather field to its supplied text component.
        resultText = result; // Connects the result field to its supplied text component.
        pausePanel = pauseMenu; // Stores the GameObject representing the pause menu.
        resultPanel = outcomeMenu; // Stores the GameObject representing the outcome menu.
        pauseButton = pause; // Stores the button that opens or toggles pause.
        resumeButton = resume; // Stores the button that resumes gameplay from pause.
        restartButton = restart; // Stores the pause menu's restart control.
        pauseExitButton = pauseExit; // Stores the pause menu's exit control.
        resultRestartButton = outcomeRestart; // Stores the outcome menu's restart control.
        resultExitButton = outcomeExit; // Stores the outcome menu's exit control.
        nextLevelButton = next; // Stores the next-level control.
    } // Ends the block opened by the matching opening brace.

    // Find the session if needed and connect each button's click event to its matching session action.
    private void Awake() // Unity calls this during initialization to connect the supplied buttons to their actions.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (session == null) // Checks whether the session was left unassigned.
            session = FindFirstObjectByType<IslandGameSession>(); // Finds the first active IslandGameSession in the loaded scenes as a fallback.
        if (pauseButton != null) pauseButton.onClick.AddListener(session.TogglePause); // If Pause exists, registers TogglePause as a click callback; registration does not call it immediately.
        if (resumeButton != null) resumeButton.onClick.AddListener(session.TogglePause); // If Resume exists, registers the same pause toggle so clicking it can resume a paused session.
        if (restartButton != null) restartButton.onClick.AddListener(session.Restart); // If the pause-menu Restart button exists, connects its click to the session's Restart method.
        if (pauseExitButton != null) pauseExitButton.onClick.AddListener(session.Exit); // If the pause-menu Exit button exists, connects its click to the session's Exit method.
        if (resultRestartButton != null) resultRestartButton.onClick.AddListener(session.Restart); // If the results Restart button exists, connects its click to the same Restart method.
        if (resultExitButton != null) resultExitButton.onClick.AddListener(session.Exit); // If the results Exit button exists, connects its click to the same Exit method.
        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(session.LoadNextLevel); // If Next exists, connects its click to the session's next-level loading method.
    } // Ends the block opened by the matching opening brace.

    // After normal frame updates, read game values into the labels and show the appropriate pause/result panels.
    // Show Next only for a completed victory with a configured next level.
    private void LateUpdate() // Unity calls this after normal Update methods, letting the HUD display the latest game values.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        if (session == null || player == null) // Checks whether either the session or the player reference is missing.
            return; // Skips this frame's display update when essential data sources are unavailable.
        scoreText.text = "SCORE  " + session.Score.ToString("0000"); // Shows the score with at least four digits, adding leading zeroes such as 0025.
        livesText.text = "LIVES  " + session.Lives; // Shows the number of remaining lives after the LIVES label.
        timeText.text = "TIME  " + Mathf.CeilToInt(session.TimeLeft) + "s"; // Shows remaining time rounded up to a whole second and appends the seconds suffix.
        objectiveText.text = session.ObjectiveText; // Copies the session's current objective description into the objective label.
        staminaText.text = "STAMINA  " + Mathf.CeilToInt(player.Stamina) + " / " + Mathf.CeilToInt(player.MaxStamina); // Shows current and maximum stamina, each rounded up to an integer.
        checkpointText.text = "CHECKPOINT  " + session.ActiveCheckpoint + " / 3"; // Shows the active checkpoint number followed by the fixed total of three checkpoints.
        weatherText.text = "WEATHER  " + (weather != null ? weather.ConditionLabel : "CLEAR"); // Shows the weather component's label, or CLEAR if no weather component exists.
        pausePanel.SetActive(session.IsPaused); // Shows the pause panel exactly when the session reports a paused state.
        resultPanel.SetActive(session.IsFinished && !session.IsEnding && !session.IsInDialogue); // Shows results only after completion and while neither the ending nor dialogue is running.
        if (session.IsFinished) // Only chooses a victory/loss message once the session has finished.
            resultText.text = session.Won ? "VICTORY!  SCORE " + session.Score : // For a win, displays VICTORY and the score; the colon continues the choice for a loss on the next line.
                session.Lives <= 0 ? "OUT OF LIVES" : "TIME UP"; // For a loss, displays OUT OF LIVES if none remain; otherwise displays TIME UP.
        if (nextLevelButton != null) // Checks that a next-level button exists before accessing its GameObject.
            nextLevelButton.gameObject.SetActive(session.IsFinished && session.Won && session.HasNextLevel); // Shows Next only when the session has finished with a win and another level is configured.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
