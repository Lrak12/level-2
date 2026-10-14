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
    public bool Won => won;
    public bool BlocksGameplay => paused || finished || handlingDeath;
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

    public void Configure(TribalPlayerController owner, IslandCheckpoint[] locations,
        int required, float duration, string nextLevel)
    {
        player = owner;
        checkpoints = locations;
        enemiesRequired = required;
        challengeSeconds = duration;
        nextSceneName = nextLevel;
    }

    public void ConfigureRelicObjective(int requiredRelics, int requiredEnemies, float duration)
    {
        relicsRequired = Mathf.Max(0, requiredRelics);
        enemiesRequired = Mathf.Max(1, requiredEnemies);
        challengeSeconds = Mathf.Max(10f, duration);
    }

    public void SetNextScene(string sceneName) => nextSceneName = sceneName;

    public void ConfigureFinalBoss(IslandEnemyAI boss) => finalBoss = boss;

    public void ConfigureEnding(Level3EndingSequence ending) => endingSequence = ending;

    private void Awake()
    {
        Instance = this;
        lives = startingLives;
        timeLeft = challengeSeconds;
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !finished)
            TogglePause();
        if (paused || finished)
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

    public void EnemyDefeated(IslandEnemyAI enemy)
    {
        if (finished || enemy == null)
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
        }
        TryFinishObjectives();
    }

    public void CollectRelic()
    {
        if (finished || relicsRequired <= 0 || relicsCollected >= relicsRequired)
            return;
        relicsCollected++;
        score += 150;
        TryFinishObjectives();
    }

    private void TryFinishObjectives()
    {
        if (finished)
            return;
        if (enemiesDefeated < enemiesRequired || relicsCollected < relicsRequired ||
            (finalBoss != null && !bossDefeated))
            return;
        score += Mathf.CeilToInt(timeLeft) * 2;
        Finish(true);
    }

    public void ReachCheckpoint(int index)
    {
        if (finished || index <= activeCheckpoint || checkpoints == null || index >= checkpoints.Length)
            return;
        activeCheckpoint = index;
        score += 25;
        if (LevelPresentation.Instance != null)
            LevelPresentation.Instance.Reward(LevelSound.Checkpoint, checkpoints[index].transform.position, new Color(.2f, 1f, .7f, .8f));
    }

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

    public void TogglePause()
    {
        if (finished)
            return;
        paused = !paused;
        if (LevelPresentation.Instance != null)
            LevelPresentation.Instance.Play(LevelSound.UI_Click, Vector3.zero, .5f, true);
        Time.timeScale = paused ? 0f : 1f;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextLevel()
    {
        if (!won || !HasNextLevel)
            return;
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

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
