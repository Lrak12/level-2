using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Builds the authored ending UI in the final level; story text remains editable in the Inspector.</summary>
public static class InstallLevel3Ending
{
    private const string ScenePath = "Assets/Scenes/Stage3_AshenCitadel.unity";
    private const string PortraitPath = "Assets/Stage3/Ending/WarriorPortrait.png";
    private static readonly Color Gold = new Color(.91f, .75f, .46f);
    private static readonly Color Ink = new Color(.075f, .10f, .18f);
    private static readonly Color Paper = new Color(.96f, .94f, .87f);

    [MenuItem("Tools/Ending/Install Ashen Citadel Ending")]
    public static void Install()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("Open the Ashen Citadel scene in Edit mode first.");
        if (UnityEngine.Object.FindFirstObjectByType<Level3EndingSequence>() != null)
            throw new InvalidOperationException("The ending already exists. Edit its story on AshenCitadelEnding.");

        var session = UnityEngine.Object.FindFirstObjectByType<IslandGameSession>();
        var player = UnityEngine.Object.FindFirstObjectByType<TribalPlayerController>();
        var hudObject = GameObject.Find("PlayerHUD");
        if (session == null || session.FinalBoss == null || player == null || hudObject == null)
            throw new InvalidOperationException("The final boss, player and HUD must be present.");
        var font = hudObject.GetComponentsInChildren<TMP_Text>(true).First().font;
        if (font == null) throw new InvalidOperationException("The HUD font is missing.");

        Directory.CreateDirectory("Assets/Stage3/Ending");
        CreatePortrait(player);
        var portrait = AssetDatabase.LoadAssetAtPath<Texture2D>(PortraitPath);

        var root = new GameObject("AshenCitadelEnding");
        Undo.RegisterCreatedObjectUndo(root, "Add story ending");
        var ending = root.AddComponent<Level3EndingSequence>();
        var first = new GameObject("OpeningShot").transform;
        var last = new GameObject("ClosingShot").transform;
        first.SetParent(root.transform); last.SetParent(root.transform);
        Vector3 throne = session.FinalBoss.transform.position;
        first.position = throne + new Vector3(12f, 8.5f, -24f);
        last.position = throne + new Vector3(8f, 9.5f, -21f);
        first.LookAt(throne + new Vector3(0f, 2f, -2f));
        last.LookAt(throne + new Vector3(0f, 3f, -2f));

        var overlay = new GameObject("EndingCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        overlay.transform.SetParent(root.transform, false);
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = overlay.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        var wash = Image(overlay.transform, "CinematicShade", new Color(.015f, .025f, .07f, .18f), Vector2.zero, Vector2.one);
        wash.raycastTarget = true;
        Image(overlay.transform, "TopLetterbox", new Color(.018f, .024f, .045f, .96f), new Vector2(0, .925f), Vector2.one);
        Image(overlay.transform, "BottomLetterbox", new Color(.018f, .024f, .045f, .96f), Vector2.zero, new Vector2(1, .035f));

        var chapter = Group(overlay.transform, "Chapter", new Vector2(.05f, .78f), new Vector2(.7f, .9f));
        Label(chapter.transform, "Eyebrow", font, "EPILOGUE  /  ASHEN CITADEL", 23, Gold, new Vector2(0, .7f), Vector2.one);
        var title = Label(chapter.transform, "Title", font, "AFTER THE ASH", 54, Paper, Vector2.zero, new Vector2(1, .7f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 4;

        var panel = Group(overlay.transform, "StoryPanel", new Vector2(.05f, .07f), new Vector2(.95f, .355f));
        Image(panel.transform, "OuterBorder", Gold, Vector2.zero, Vector2.one);
        var bodyPanel = Image(panel.transform, "NavyPanel", Ink, Vector2.zero, Vector2.one);
        Inset(bodyPanel.rectTransform, 3);
        Image(bodyPanel.transform, "TopAccent", new Color(.25f, .31f, .44f), new Vector2(0, .975f), Vector2.one);

        var portraitFrame = Image(bodyPanel.transform, "PortraitFrame", Gold, new Vector2(.014f, .075f), new Vector2(.165f, .925f));
        var portraitBack = Image(portraitFrame.transform, "PortraitBackground", new Color(.12f, .16f, .23f), Vector2.zero, Vector2.one);
        Inset(portraitBack.rectTransform, 2);
        var portraitObject = new GameObject("WarriorPortrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.RawImage));
        var portraitArea = Group(portraitBack.transform, "PortraitArea", new Vector2(0, .15f), Vector2.one);
        portraitObject.transform.SetParent(portraitArea.transform, false);
        var raw = portraitObject.GetComponent<UnityEngine.UI.RawImage>();
        Rect(raw.rectTransform, Vector2.zero, Vector2.one);
        raw.texture = portrait; raw.raycastTarget = false;
        var aspect = portraitObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
        aspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
        aspect.aspectRatio = 384f / 448f;
        var tag = Label(portraitBack.transform, "PortraitTag", font, "UPALUPAL", 17, Gold, Vector2.zero, new Vector2(1, .13f));
        tag.alignment = TextAlignmentOptions.Center;

        var speaker = Label(bodyPanel.transform, "Speaker", font, "CHRONICLE", 27, Gold, new Vector2(.195f, .745f), new Vector2(.85f, .92f));
        speaker.fontStyle = FontStyles.Bold; speaker.characterSpacing = 3;
        var text = Label(bodyPanel.transform, "Story", font, "", 34, Paper, new Vector2(.195f, .23f), new Vector2(.96f, .72f));
        text.enableAutoSizing = true; text.fontSizeMin = 24; text.fontSizeMax = 34;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.lineSpacing = 10;
        var page = Label(bodyPanel.transform, "Page", font, "01 / 05", 22, Gold, new Vector2(.88f, .745f), new Vector2(.96f, .92f));
        page.alignment = TextAlignmentOptions.Right;
        var hint = Label(bodyPanel.transform, "PlaybackHint", font, "AUTO STORY     /     SPACE OR CLICK TO CONTINUE", 17,
            new Color(.62f, .70f, .80f), new Vector2(.195f, .07f), new Vector2(.96f, .17f));
        hint.characterSpacing = 1;
        var track = Image(bodyPanel.transform, "ReadingTrack", new Color(.18f, .23f, .33f), new Vector2(.195f, .045f), new Vector2(.96f, .053f));
        var progress = Image(track.transform, "ReadingProgress", Gold, Vector2.zero, Vector2.one);

        var fade = Image(overlay.transform, "Fade", new Color(.018f, .024f, .045f, 0f), Vector2.zero, Vector2.one);
        fade.raycastTarget = true;
        var end = Group(overlay.transform, "FinalCard", new Vector2(.1f, .25f), new Vector2(.9f, .75f));
        var endEyebrow = Label(end.transform, "Eyebrow", font, "THE THREE RELICS RESTORED", 23, Gold, new Vector2(0, .63f), new Vector2(1, .75f));
        endEyebrow.alignment = TextAlignmentOptions.Center; endEyebrow.characterSpacing = 3;
        var endTitle = Label(end.transform, "Title", font, "THE ISLAND ENDURES", 66, Paper, new Vector2(0, .37f), new Vector2(1, .62f));
        endTitle.alignment = TextAlignmentOptions.Center; endTitle.fontStyle = FontStyles.Bold;
        Image(end.transform, "Rule", Gold, new Vector2(.38f, .31f), new Vector2(.62f, .314f));
        var closing = Label(end.transform, "Closing", font, "Your journey is complete.", 27, new Color(.65f, .71f, .81f), new Vector2(0, .12f), new Vector2(1, .28f));
        closing.alignment = TextAlignmentOptions.Center;

        ending.Configure(overlay, panel, chapter, end, fade, speaker, text, page, progress,
            hudObject.GetComponent<Canvas>(), Camera.main, first, last, player, new[]
            {
                Beat("CHRONICLE", "Nallegam falls. For the first time in an age, the citadel answers with silence."),
                Beat("CHRONICLE", "With all three ember relics recovered, the power he held over the island is finally broken."),
                Beat("UPALUPAL", "These relics belong to our people. No throne will hold them again."),
                Beat("CHRONICLE", "Beyond the citadel, the villages will kindle their fires again. What was lost can be rebuilt."),
                Beat("UPALUPAL", "The battle is over. It is time to go home.")
            });
        Undo.RecordObject(session, "Connect final ending");
        session.ConfigureEnding(ending);
        EditorUtility.SetDirty(session);
        panel.alpha = chapter.alpha = end.alpha = 0;
        overlay.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root;
        Debug.Log("Installed linear Ashen Citadel ending: five automatic story beats after boss defeat and relic collection.");
    }

    private static Level3EndingSequence.StoryBeat Beat(string speaker, string text) =>
        new Level3EndingSequence.StoryBeat { speaker = speaker, text = text, readingSeconds = 4.5f };

    private static void CreatePortrait(TribalPlayerController player)
        => CapturePortrait(player.gameObject, PortraitPath);

    public static void CapturePortrait(GameObject subject, string path)
    {
        var preview = new PreviewRenderUtility();
        Texture2D image = null;
        try
        {
            var model = UnityEngine.Object.Instantiate(subject);
            model.name = "Ending portrait preview";
            preview.AddSingleGO(model);
            // Disable behaviours instead of removing required components from the preview clone.
            foreach (var behaviour in model.GetComponentsInChildren<Behaviour>(true))
                if (!(behaviour is Animator)) behaviour.enabled = false;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            model.SetActive(true);
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (var canvas in model.GetComponentsInChildren<Canvas>(true)) canvas.gameObject.SetActive(false);
            var animator = model.GetComponent<Animator>();
            animator.Rebind(); animator.Update(0f);
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            float scale = Mathf.Max(.01f, model.transform.lossyScale.y);
            Vector3 focus = head != null ? head.position + Vector3.down * .12f * scale : Vector3.up * 1.5f * scale;
            preview.BeginStaticPreview(new UnityEngine.Rect(0, 0, 384, 448));
            preview.camera.transform.position = focus + new Vector3(.10f, .02f, 1.1f) * scale;
            preview.camera.transform.LookAt(focus);
            preview.camera.fieldOfView = 32f;
            preview.camera.nearClipPlane = .01f;
            preview.camera.farClipPlane = 20f;
            preview.camera.clearFlags = CameraClearFlags.Color;
            preview.camera.backgroundColor = new Color(.12f, .16f, .23f);
            preview.ambientColor = new Color(.6f, .62f, .68f);
            preview.lights[0].intensity = 1.6f;
            preview.lights[0].transform.rotation = Quaternion.Euler(30f, 210f, 0f);
            preview.lights[1].intensity = 1.1f;
            preview.lights[1].transform.rotation = Quaternion.Euler(340f, 120f, 0f);
            preview.Render(true);
            image = preview.EndStaticPreview();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            preview.Cleanup();
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    private static CanvasGroup Group(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(parent, false);
        Rect(go.GetComponent<RectTransform>(), min, max);
        return go.GetComponent<CanvasGroup>();
    }

    private static UnityEngine.UI.Image Image(Transform parent, string name, Color color, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<UnityEngine.UI.Image>();
        Rect(image.rectTransform, min, max); image.color = color; image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Label(Transform parent, string name, TMP_FontAsset font, string value,
        float size, Color color, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        Rect(text.rectTransform, min, max);
        text.font = font; text.text = value; text.fontSize = size; text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
        return text;
    }

    private static void Rect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Inset(RectTransform rect, float value)
    {
        rect.offsetMin = Vector2.one * value;
        rect.offsetMax = Vector2.one * -value;
    }
}
