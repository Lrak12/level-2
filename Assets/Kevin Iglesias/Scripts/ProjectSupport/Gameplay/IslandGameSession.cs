// Coordinates level rules: objectives, score, lives, checkpoints, pause, victory/defeat, and scene transitions.
// Study guide and script map: CODE_GUIDE.md in the project root.

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Owns the score, finite lives, timed objective, checkpoints, and menu actions.</summary>
public sealed class IslandGameSession : MonoBehaviour
{
    [SerializeField] private TribalPlayerController player;
    [SerializeField] private IslandCheckpoint[] checkpoints;
    [SerializeField, Min(1)] private int startingLives = 3;
    [SerializeField, Min(1)] private int enemiesRequired = 4;
    [SerializeField, Min(10f)] private float challengeSeconds = 180f;
    [SerializeField, Min(0)] private int relicsRequired;
    [SerializeField] private IslandEnemyAI finalBoss;
    [SerializeField] private string nextSceneName;
    [SerializeField] private Level3EndingSequence endingSequence;
    [SerializeField] private Level3EndingSequence bossIntroduction;
    [SerializeField] private Level3EndingSequence levelTransition;

    private int score;
    private int lives;
    private int enemiesDefeated;
    private int relicsCollected;
    private int activeCheckpoint;
    private bool bossSpawned;
    private bool bossDefeated;
    private float timeLeft;
    private bool paused;
    private bool finished;
    private bool won;
    private bool handlingDeath;
    private bool bossIntroPending;
    private bool loadingNextLevel;

    public static IslandGameSession Instance { get; private set; }
    public int Score => score;
    public int Lives => lives;
    public int EnemiesDefeated => enemiesDefeated;
    public int EnemiesRequired => enemiesRequired;
    public int RelicsRequired => relicsRequired;
    public int RelicsCollected => relicsCollected;
    public int ActiveCheckpoint => activeCheckpoint + 1;
    public float TimeLeft => timeLeft;
    public bool IsPaused => paused;
    public bool IsFinished => finished;
    public bool IsEnding { get; private set; }
    public bool IsInDialogue { get; private set; }
    public bool Won => won;
    public bool BlocksGameplay => paused || finished || handlingDeath || IsInDialogue;
    public bool HasNextLevel => !string.IsNullOrEmpty(nextSceneName);
    public IslandEnemyAI FinalBoss => finalBoss;
    public bool BossActive => bossSpawned && !bossDefeated;
    public bool BossDefeated => bossDefeated;
    public string ObjectiveText => relicsRequired > 0
        ? finalBoss == null
            ? "RELICS " + relicsCollected + " / " + relicsRequired + "   ENEMIES " + enemiesDefeated + " / " + enemiesRequired
            : !bossSpawned
                ? "RELICS " + relicsCollected + " / " + relicsRequired + "   GUARDS " + enemiesDefeated + " / " + enemiesRequired
                : bossDefeated
                    ? "BOSS DEFEATED   RELICS " + relicsCollected + " / " + relicsRequired
                    : "NALLEGAM   RELICS " + relicsCollected + " / " + relicsRequired
        : "ENEMIES  " + enemiesDefeated + " / " + enemiesRequired;

    // Assign the player, checkpoint list, enemy target, time limit, and next scene when the level is set up.
    public void Configure(TribalPlayerController owner, IslandCheckpoint[] locations,
        int required, float duration, string nextLevel)
    {
        player = owner;
        checkpoints = locations;
        enemiesRequired = required;
        challengeSeconds = duration;
        nextSceneName = nextLevel;
    }

    // Set the relic/enemy targets and time limit, clamping values to the minimum allowed by this game.
    public void ConfigureRelicObjective(int requiredRelics, int requiredEnemies, float duration)
    {
        relicsRequired = Mathf.Max(0, requiredRelics);
        enemiesRequired = Mathf.Max(1, requiredEnemies);
        challengeSeconds = Mathf.Max(10f, duration);
    }

    // Set the scene name or path that LoadNextLevel will use after victory.
    public void SetNextScene(string sceneName) => nextSceneName = sceneName;

    // Store the boss whose activation and defeat are tracked as part of the level objectives.
    public void ConfigureFinalBoss(IslandEnemyAI boss) => finalBoss = boss;

    // Assign the story sequence to play after a victory in a level with a final boss.
    public void ConfigureEnding(Level3EndingSequence ending) => endingSequence = ending;

    // Assign the dialogue sequence that introduces the boss after enough guards have been defeated.
    public void ConfigureBossIntroduction(Level3EndingSequence sequence) => bossIntroduction = sequence;
    // Assign the story sequence to play before loading the next level.
    public void ConfigureLevelTransition(Level3EndingSequence sequence) => levelTransition = sequence;

    // Register this scene's session in Instance, initialize lives/time, and restore normal game speed.
    private void Awake()
    {
        Instance = this;
        lives = startingLives;
        timeLeft = challengeSeconds;
        Time.timeScale = 1f;
    }

    // When this session is destroyed, clear its shared reference and restore timeScale for the next scene.
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        Time.timeScale = 1f;
    }

    // Each frame, handle Escape, pending boss dialogue, the countdown, and player death.
    // Pause, finished-state, dialogue, and death handling prevent the relevant parts of this loop from advancing.
    private void Update()
    {
        if (IsInDialogue)
            return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !finished)
            TogglePause();
        if (paused || finished)
            return;

        if (TryBeginBossIntroduction())
            return;

        if (!handlingDeath)
        {
            timeLeft = Mathf.Max(0f, timeLeft - Time.deltaTime);
            if (timeLeft <= 0f)
                Finish(false);
            else if (player != null && player.IsDead)
                StartCoroutine(HandleDeath());
        }
    }

    // Called by an enemy on death. Award points, count normal enemies or record boss defeat, and recheck victory.
    // Reaching the guard target activates the configured boss and requests its introduction.
    public void EnemyDefeated(IslandEnemyAI enemy)
    {
        if (finished || IsInDialogue || enemy == null)
            return;
        if (enemy == finalBoss)
        {
            bossDefeated = true;
            score += 750;
            TryFinishObjectives();
            return;
        }

        enemiesDefeated++;
        score += 100;
        if (finalBoss != null && !bossSpawned && enemiesDefeated >= enemiesRequired)
        {
            bossSpawned = true;
            finalBoss.gameObject.SetActive(true);
            if (LevelPresentation.Instance != null)
                LevelPresentation.Instance.Reward(LevelSound.Boss_Warning, finalBoss.transform.position, new Color(2f, .4f, .05f, .8f));
            if (player != null)
                finalBoss.SetTarget(player.transform);
            bossIntroPending = bossIntroduction != null;
            TryBeginBossIntroduction();
        }
        TryFinishObjectives();
    }

    // Start a pending boss introduction only when gameplay is in a suitable state; return whether it started.
    // The completion callback releases the dialogue block when the story finishes.
    private bool TryBeginBossIntroduction()
    {
        if (!bossIntroPending || IsInDialogue || handlingDeath || paused || finished ||
            (player != null && player.IsDead))
            return false;
        bossIntroPending = false;
        IsInDialogue = true;
        bossIntroduction.Begin(() => IsInDialogue = false);
        return true;
    }

    // Called by a relic pickup. Add one relic and its score reward if still needed, then check the objectives.
    public void CollectRelic()
    {
        if (finished || relicsRequired <= 0 || relicsCollected >= relicsRequired)
            return;
        relicsCollected++;
        score += 150;
        TryFinishObjectives();
    }

    // Check enemy, relic, and optional boss requirements. If all are met, add the time bonus and finish with victory.
    private void TryFinishObjectives()
    {
        if (finished || IsInDialogue)
            return;
        if (enemiesDefeated < enemiesRequired || relicsCollected < relicsRequired ||
            (finalBoss != null && !bossDefeated))
            return;
        score += Mathf.CeilToInt(timeLeft) * 2;
        Finish(true);
    }

    // Called by a checkpoint trigger. Accept only a later valid checkpoint and award points once for that advance.
    // The saved array index determines the position used on the next respawn.
    public void ReachCheckpoint(int index)
    {
        if (finished || index <= activeCheckpoint || checkpoints == null || index >= checkpoints.Length)
            return;
        activeCheckpoint = index;
        score += 25;
        if (LevelPresentation.Instance != null)
            LevelPresentation.Instance.Reward(LevelSound.Checkpoint, checkpoints[index].transform.position, new Color(.2f, 1f, .7f, .8f));
    }

    // Coroutine that spends one life. End the run at zero lives; otherwise wait briefly and respawn at the checkpoint.
    // handlingDeath prevents Update from spending another life for the same death.
    private IEnumerator HandleDeath()
    {
        handlingDeath = true;
        lives--;
        if (lives <= 0)
        {
            Finish(false);
            yield break;
        }

        yield return new WaitForSeconds(1.6f);
        if (!finished && player != null && checkpoints != null && checkpoints.Length > activeCheckpoint)
            player.RespawnAt(checkpoints[activeCheckpoint].transform.position + Vector3.up * 0.25f);
        handlingDeath = false;
    }

    // Record victory or defeat once, freeze game time, unlock the cursor, and play the result sound.
    // A boss-level victory can start the ending story before the HUD shows the result panel.
    private void Finish(bool victory)
    {
        if (finished)
            return;
        finished = true;
        won = victory;
        if (LevelPresentation.Instance != null)
            LevelPresentation.Instance.Play(victory ? LevelSound.Victory : LevelSound.Defeat, Vector3.zero, 1f, true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (victory && finalBoss != null && endingSequence != null)
        {
            IsEnding = true;
            endingSequence.Begin(() => IsEnding = false);
        }
    }

    // Called by Escape or Pause/Resume buttons. Toggle game time and cursor access unless finished or in dialogue.
    public void TogglePause()
    {
        if (finished || IsInDialogue)
            return;
        paused = !paused;
        if (LevelPresentation.Instance != null)
            LevelPresentation.Instance.Play(LevelSound.UI_Click, Vector3.zero, .5f, true);
        Time.timeScale = paused ? 0f : 1f;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }

    // Called by Restart. Restore game speed and reload the current scene to reset the level.
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Called by Next after victory. Prevent duplicate loads and play transition dialogue if configured.
    public void LoadNextLevel()
    {
        if (!won || !HasNextLevel || loadingNextLevel || IsInDialogue)
            return;
        loadingNextLevel = true;
        if (levelTransition != null)
        {
            IsInDialogue = true;
            levelTransition.Begin(EnterNextLevel);
        }
        else
            EnterNextLevel();
    }

    // Load the configured next scene after clearing dialogue state and restoring normal game speed.
    private void EnterNextLevel()
    {
        IsInDialogue = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

    // Called by Exit. Stop Play mode in the Unity Editor or quit the built game.
    public void Exit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
