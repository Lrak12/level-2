using System;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>A linear, automatically advancing epilogue after all final objectives are met.</summary>
public sealed class Level3EndingSequence : MonoBehaviour
{
    [Serializable]
    public sealed class StoryBeat
    {
        public string speaker;
        [TextArea(2, 5)] public string text;
        [Min(2f)] public float readingSeconds = 5f;
    }

    [Header("Story (plays in order, without choices)")]
    [SerializeField] private StoryBeat[] story;
    [SerializeField, Min(1f)] private float charactersPerSecond = 38f;
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
    public bool IsPlaying { get; private set; }
    public int CurrentBeat { get; private set; } = -1;
    public int BeatCount => story == null ? 0 : story.Length;

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

    private void Awake()
    {
        if (overlay != null) overlay.SetActive(false);
    }

    public void Begin(Action onComplete)
    {
        if (started) return;
        started = true;
        if (overlay == null || dialogue == null || fade == null || storyLabel == null || BeatCount == 0)
        {
            Debug.LogWarning("Ending presentation is incomplete; showing the victory screen.", this);
            onComplete?.Invoke();
            return;
        }
        completed = onComplete;
        IsPlaying = true;
        StartCoroutine(PlayStory());
    }

    private IEnumerator PlayStory()
    {
        hudWasEnabled = gameplayHud != null && gameplayHud.enabled;
        if (gameplayHud != null) gameplayHud.enabled = false;
        overlay.SetActive(true);
        dialogue.alpha = chapter.alpha = endCard.alpha = 0f;
        SetFade(0f);
        // Let the final impact, collection effects and boss death settle behind the fade.
        if (player != null)
        {
            player.StopAllCoroutines();
            var animator = player.GetComponent<Animator>();
            if (animator != null) animator.SetFloat("Speed", 0f);
        }
        Time.timeScale = 1f;
        yield return FadeTo(1f, 1.6f);
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
        while (titleTime < 4.5f)
        {
            titleTime += Time.unscaledDeltaTime;
            endCard.alpha = Mathf.Clamp01(titleTime / 1.1f);
            yield return null;
        }
        endCard.alpha = 0f;
        RestoreCamera();
        if (gameplayHud != null) gameplayHud.enabled = hudWasEnabled;
        completed?.Invoke();
        completed = null;
        yield return FadeTo(0f, .8f);
        overlay.SetActive(false);
        IsPlaying = false;
    }

    private void SetBeat(int index)
    {
        CurrentBeat = index;
        speakerLabel.text = story[index].speaker;
        storyLabel.text = story[index].text;
        storyLabel.maxVisibleCharacters = int.MaxValue;
        storyLabel.ForceMeshUpdate();
        storyLabel.maxVisibleCharacters = 0;
        pageLabel.text = (index + 1).ToString("00") + " / " + BeatCount.ToString("00");
        progress.rectTransform.anchorMax = new Vector2(0f, 1f);
    }

    private static bool AdvancePressed()
    {
        var keyboard = Keyboard.current;
        return (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
    }

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

    private void SetFade(float alpha) => fade.color = new Color(.018f, .024f, .045f, alpha);

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

    private void LateUpdate()
    {
        if (!cameraMoving || sceneCamera == null) return;
        cameraElapsed += Time.unscaledDeltaTime;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(cameraElapsed / cameraDuration));
        sceneCamera.transform.SetPositionAndRotation(Vector3.Lerp(cameraStart.position, cameraEnd.position, t),
            Quaternion.Slerp(cameraStart.rotation, cameraEnd.rotation, t));
    }

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

    private void OnDisable()
    {
        if (!IsPlaying) return;
        StopAllCoroutines();
        RestoreCamera();
        if (gameplayHud != null) gameplayHud.enabled = hudWasEnabled;
        if (overlay != null) overlay.SetActive(false);
        Time.timeScale = 0f;
        IsPlaying = false;
        completed?.Invoke();
        completed = null;
    }
}
