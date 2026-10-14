// Plays linear dialogue for the boss introduction, level transition, and ending, including camera movement and fades.
// Study guide and script map: CODE_GUIDE.md in the project root.

using System;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Shared linear story playback for the ending, boss introduction and level transition.</summary>
public sealed class Level3EndingSequence : MonoBehaviour
{
    [Serializable]
    public sealed class StoryBeat
    {
        public string speaker;
        [TextArea(2, 5)] public string text;
        [Min(2f)] public float readingSeconds = 5f;
        public Texture2D portrait;
    }

    [Header("Story (plays in order, without choices)")]
    [SerializeField] private StoryBeat[] story;
    [SerializeField, Min(1f)] private float charactersPerSecond = 38f;
    [SerializeField] private bool showEndCard = true;
    [SerializeField] private bool settleBeforeStory = true;
    [SerializeField] private bool keepBlackOnComplete;
    [Header("Presentation")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private CanvasGroup dialogue;
    [SerializeField] private CanvasGroup chapter;
    [SerializeField] private CanvasGroup endCard;
    [SerializeField] private UnityEngine.UI.Image fade;
    [SerializeField] private TMP_Text speakerLabel;
    [SerializeField] private TMP_Text storyLabel;
    [SerializeField] private TMP_Text pageLabel;
    [SerializeField] private UnityEngine.UI.Image progress;
    [SerializeField] private Canvas gameplayHud;
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private Transform cameraStart;
    [SerializeField] private Transform cameraEnd;
    [SerializeField] private TribalPlayerController player;

    private Action completed;
    private CinemachineBrain brain;
    private bool brainWasEnabled;
    private bool hudWasEnabled;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private float savedFieldOfView;
    private bool hasCameraState;
    private bool cameraMoving;
    private float cameraElapsed;
    private float cameraDuration;
    private bool started;
    private float savedTimeScale;
    private CursorLockMode savedCursorLock;
    private bool savedCursorVisible;
    private UnityEngine.UI.RawImage portraitImage;
    private TMP_Text portraitTag;
    private Texture defaultPortrait;
    public bool IsPlaying { get; private set; }
    public int CurrentBeat { get; private set; } = -1;
    public int BeatCount => story == null ? 0 : story.Length;

    // Connect the story's UI, camera shot markers, player, and ordered StoryBeat data during scene setup.
    public void Configure(GameObject root, CanvasGroup dialoguePanel, CanvasGroup chapterHeading,
        CanvasGroup finalCard, UnityEngine.UI.Image fadeImage, TMP_Text speaker,
        TMP_Text body, TMP_Text page, UnityEngine.UI.Image readingProgress, Canvas hud,
        Camera camera, Transform firstShot, Transform lastShot, TribalPlayerController hero,
        StoryBeat[] beats)
    {
        overlay = root; dialogue = dialoguePanel; chapter = chapterHeading; endCard = finalCard;
        fade = fadeImage; speakerLabel = speaker; storyLabel = body; pageLabel = page;
        progress = readingProgress; gameplayHud = hud; sceneCamera = camera;
        cameraStart = firstShot; cameraEnd = lastShot; player = hero; story = beats;
    }

    // Hide the story overlay when this component first loads.
    private void Awake()
    {
        if (overlay != null) overlay.SetActive(false);
    }

    // Start this sequence once, save game/cursor state, and remember the callback to run when it finishes.
    // If essential presentation data is missing, warn and invoke the callback immediately so the caller can continue.
    public void Begin(Action onComplete)
    {
        if (started) return;
        started = true;
        if (overlay == null || dialogue == null || fade == null || storyLabel == null || BeatCount == 0)
        {
            Debug.LogWarning("Story presentation is incomplete; continuing gameplay.", this);
            onComplete?.Invoke();
            return;
        }
        completed = onComplete;
        savedTimeScale = Time.timeScale;
        savedCursorLock = Cursor.lockState;
        savedCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        portraitImage = overlay.GetComponentInChildren<UnityEngine.UI.RawImage>(true);
        if (portraitImage != null) defaultPortrait = portraitImage.texture;
        foreach (var text in overlay.GetComponentsInChildren<TMP_Text>(true))
            if (text.name == "PortraitTag") portraitTag = text;
        IsPlaying = true;
        StartCoroutine(PlayStory());
    }

    // Coroutine that hides the HUD, fades into the shot, reveals each dialogue page, and optionally shows an end card.
    // Use unscaled time to progress while gameplay is frozen, then restore saved state and invoke completion.
    private IEnumerator PlayStory()
    {
        hudWasEnabled = gameplayHud != null && gameplayHud.enabled;
        if (gameplayHud != null) gameplayHud.enabled = false;
        overlay.SetActive(true);
        dialogue.alpha = chapter.alpha = endCard.alpha = 0f;
        SetFade(0f);
        // Let the final impact, collection effects and boss death settle behind the fade.
        if (settleBeforeStory && player != null)
        {
            player.StopAllCoroutines();
            var animator = player.GetComponent<Animator>();
            if (animator != null) animator.SetFloat("Speed", 0f);
        }
        // Introductions freeze immediately so attacks, hazards and the timer cannot advance.
        // Preserve in-flight player coroutines so combat state resumes normally afterwards.
        Time.timeScale = settleBeforeStory ? 1f : 0f;
        yield return FadeTo(1f, settleBeforeStory ? 1.6f : .6f);
        Time.timeScale = 0f;
        TakeCamera();
        SetBeat(0);
        chapter.alpha = dialogue.alpha = 1f;
        yield return FadeTo(0f, 1f);

        for (int i = 0; i < BeatCount; i++)
        {
            SetBeat(i);
            float typed = 0f;
            int count = storyLabel.textInfo.characterCount;
            // Consume the input that advanced the previous page before listening again.
            yield return null;
            while (storyLabel.maxVisibleCharacters < count)
            {
                typed += Time.unscaledDeltaTime * charactersPerSecond;
                storyLabel.maxVisibleCharacters = Mathf.Min(count, Mathf.FloorToInt(typed));
                if (AdvancePressed()) storyLabel.maxVisibleCharacters = count;
                yield return null;
            }
            float hold = Mathf.Max(story[i].readingSeconds, count * .035f);
            float elapsed = 0f;
            while (elapsed < hold)
            {
                elapsed += Time.unscaledDeltaTime;
                progress.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(elapsed / hold), 1f);
                if (elapsed > .25f && AdvancePressed()) break;
                yield return null;
            }
        }

        yield return FadeTo(1f, .9f);
        cameraMoving = false;
        dialogue.alpha = chapter.alpha = 0f;
        // The title card is above the black fade, so it can appear on a clean backdrop.
        float titleTime = 0f;
        while (showEndCard && titleTime < 4.5f)
        {
            titleTime += Time.unscaledDeltaTime;
            endCard.alpha = Mathf.Clamp01(titleTime / 1.1f);
            yield return null;
        }
        endCard.alpha = 0f;
        RestoreCamera();
        if (gameplayHud != null) gameplayHud.enabled = hudWasEnabled;
        if (!keepBlackOnComplete)
            yield return FadeTo(0f, .8f);
        IsPlaying = false;
        Time.timeScale = savedTimeScale;
        Cursor.lockState = savedCursorLock;
        Cursor.visible = savedCursorVisible;
        if (!keepBlackOnComplete) overlay.SetActive(false);
        // Clear the callback before it can unload the scene and invoke OnDisable.
        var onComplete = completed;
        completed = null;
        onComplete?.Invoke();
    }

    // Display the selected page's speaker, portrait, text, and page number; reset typing/progress indicators.
    private void SetBeat(int index)
    {
        CurrentBeat = index;
        speakerLabel.text = story[index].speaker;
        if (portraitImage != null)
            portraitImage.texture = story[index].portrait != null ? story[index].portrait : defaultPortrait;
        if (portraitTag != null)
            portraitTag.text = story[index].speaker == "CHRONICLE" ? "UPALUPAL" : story[index].speaker;
        storyLabel.text = story[index].text;
        storyLabel.maxVisibleCharacters = int.MaxValue;
        storyLabel.ForceMeshUpdate();
        storyLabel.maxVisibleCharacters = 0;
        pageLabel.text = (index + 1).ToString("00") + " / " + BeatCount.ToString("00");
        progress.rectTransform.anchorMax = new Vector2(0f, 1f);
    }

    // Return true for a fresh Space, Enter, left-click, or gamepad south-button press used to reveal/advance dialogue.
    private static bool AdvancePressed()
    {
        var keyboard = Keyboard.current;
        return (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
    }

    // Coroutine that smoothly changes overlay opacity to the target over the requested number of real-time seconds.
    private IEnumerator FadeTo(float target, float seconds)
    {
        float initial = fade.color.a;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFade(Mathf.Lerp(initial, target, Mathf.SmoothStep(0f, 1f, elapsed / seconds)));
            yield return null;
        }
        SetFade(target);
    }

    // Set the fade image's opacity while keeping its dark background color.
    private void SetFade(float alpha) => fade.color = new Color(.018f, .024f, .045f, alpha);

    // Save the gameplay camera, temporarily disable Cinemachine control, and start the scripted shot between two markers.
    private void TakeCamera()
    {
        if (sceneCamera == null || cameraStart == null || cameraEnd == null) return;
        savedCameraPosition = sceneCamera.transform.position;
        savedCameraRotation = sceneCamera.transform.rotation;
        savedFieldOfView = sceneCamera.fieldOfView;
        brain = sceneCamera.GetComponent<CinemachineBrain>();
        brainWasEnabled = brain != null && brain.enabled;
        if (brain != null) brain.enabled = false;
        hasCameraState = cameraMoving = true;
        sceneCamera.fieldOfView = 48f;
        cameraElapsed = 0f;
        cameraDuration = 1f;
        foreach (var beat in story)
            cameraDuration += beat.text.Length / charactersPerSecond + Mathf.Max(beat.readingSeconds, beat.text.Length * .035f);
        sceneCamera.transform.SetPositionAndRotation(cameraStart.position, cameraStart.rotation);
    }

    // After normal frame updates, move and rotate the story camera between its markers using unscaled time.
    private void LateUpdate()
    {
        if (!cameraMoving || sceneCamera == null) return;
        cameraElapsed += Time.unscaledDeltaTime;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(cameraElapsed / cameraDuration));
        sceneCamera.transform.SetPositionAndRotation(Vector3.Lerp(cameraStart.position, cameraEnd.position, t),
            Quaternion.Slerp(cameraStart.rotation, cameraEnd.rotation, t));
    }

    // Stop the scripted shot and restore the saved camera pose, field of view, and Cinemachine enabled state.
    private void RestoreCamera()
    {
        cameraMoving = false;
        if (!hasCameraState) return;
        if (sceneCamera != null)
        {
            sceneCamera.transform.SetPositionAndRotation(savedCameraPosition, savedCameraRotation);
            sceneCamera.fieldOfView = savedFieldOfView;
        }
        if (brain != null) brain.enabled = brainWasEnabled;
        hasCameraState = false;
    }

    // If interrupted, stop the story and restore the camera, HUD, time, and cursor.
    // Clear the completion callback so cancellation cannot accidentally start combat or load another scene.
    private void OnDisable()
    {
        if (!IsPlaying) return;
        StopAllCoroutines();
        RestoreCamera();
        if (gameplayHud != null) gameplayHud.enabled = hudWasEnabled;
        if (overlay != null) overlay.SetActive(false);
        Time.timeScale = savedTimeScale;
        Cursor.lockState = savedCursorLock;
        Cursor.visible = savedCursorVisible;
        IsPlaying = false;
        // Cancellation/unloading must never start a fight or load another scene.
        completed = null;
    }
}
