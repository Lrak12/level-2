using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds the Level 2 gameplay HUD, checkpoints, and weather.</summary>
public static class SetupIslandGameFeatures
{
    private const string Stage2 = "Assets/Scenes/Stage2_Battlefield.unity";
    private const string Backup = "Assets/Scenes/Stage2_Battlefield_BeforeGameplayFeatures.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string MaterialFolder = "Assets/Stage2_Imported/";

    [MenuItem("Tools/Island Game/Tune Tropical Rain")]
    public static void TuneRain()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "TropicalRain.mat");
        if (material == null)
            throw new InvalidOperationException("Install the gameplay features before tuning the rain.");
        ConfigureRainMaterial(material);
        var scene = EditorSceneManager.OpenScene(Stage2, OpenSceneMode.Single);
        var system = GameObject.Find("TropicalRainCycle")?.GetComponent<ParticleSystem>();
        if (system == null)
            throw new InvalidOperationException("Rain system missing from Level 2.");
        SetRainParticleSize(system);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Island Game/Install Level 2 Gameplay Features")]
    public static void Install()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != Stage2)
            throw new InvalidOperationException("Open Stage2_Battlefield before installing gameplay features.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Backup) == null)
            EditorSceneManager.SaveScene(scene, Backup, true);

        BuildStage2();
        EditorSceneManager.SaveScene(scene);
        var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!buildScenes.Exists(entry => entry.path == Stage2))
            buildScenes.Add(new EditorBuildSettingsScene(Stage2, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Level 2 gameplay features installed: 4 enemies / 180 seconds.");
    }

    private static void BuildStage2()
    {
        var player = GameObject.Find("TribalPlayer")?.GetComponent<TribalPlayerController>();
        var hud = GameObject.Find("PlayerHUD");
        var terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
        if (player == null || hud == null || terrain == null)
            throw new InvalidOperationException("Stage 2 player, HUD, or terrain is missing.");

        if (GameObject.Find("IslandGameSession") != null)
            throw new InvalidOperationException("Gameplay features are already installed in Stage 2.");

        var checkpointMaterial = EnsureMaterial("CheckpointBlue.mat", new Color(0.06f, 0.35f, 0.46f, 1f), false);
        var rainMaterial = EnsureMaterial("TropicalRain.mat", new Color(0.55f, 0.78f, 1f, 0.65f), true);
        ConfigureRainMaterial(rainMaterial);
        var checkpoints = new[]
        {
            CreateCheckpoint(terrain, "Checkpoint_01", 0, 54f, 21f, checkpointMaterial),
            CreateCheckpoint(terrain, "Checkpoint_02", 1, 57f, 53f, checkpointMaterial),
            CreateCheckpoint(terrain, "Checkpoint_03", 2, 67f, 89f, checkpointMaterial)
        };

        var sessionObject = new GameObject("IslandGameSession");
        var session = sessionObject.AddComponent<IslandGameSession>();
        session.Configure(player, checkpoints, 4, 180f, "");

        var weatherObject = new GameObject("TropicalRainCycle");
        weatherObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var particles = weatherObject.AddComponent<ParticleSystem>();
        ConfigureRain(particles, rainMaterial);
        var weather = weatherObject.AddComponent<IslandRainCycle>();
        weather.Configure(player.transform, particles);

        BuildHud(hud, session, player, weather);
        EnsureEventSystem();
        var header = GameObject.Find("LevelHeader")?.GetComponent<TMP_Text>();
        if (header != null)
            header.text = "LEVEL 2  •  DEFEND THE ISLAND";
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static IslandCheckpoint CreateCheckpoint(Terrain terrain, string name, int index,
        float x, float z, Material material)
    {
        var root = new GameObject(name);
        float y = terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y;
        root.transform.position = new Vector3(x, y, z);
        var trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 2.3f;
        trigger.center = new Vector3(0, 1.1f, 0);
        var checkpoint = root.AddComponent<IslandCheckpoint>();

        var baseObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObject.name = "GlowingBase";
        baseObject.transform.SetParent(root.transform, false);
        baseObject.transform.localPosition = new Vector3(0, 0.06f, 0);
        baseObject.transform.localScale = new Vector3(0.72f, 0.05f, 0.72f);
        UnityEngine.Object.DestroyImmediate(baseObject.GetComponent<Collider>());
        var baseRenderer = baseObject.GetComponent<Renderer>();
        baseRenderer.sharedMaterial = material;

        var beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beacon.name = "Beacon";
        beacon.transform.SetParent(root.transform, false);
        beacon.transform.localPosition = new Vector3(0, 1.05f, 0);
        beacon.transform.localScale = Vector3.one * 0.17f;
        UnityEngine.Object.DestroyImmediate(beacon.GetComponent<Collider>());
        beacon.GetComponent<Renderer>().sharedMaterial = material;
        checkpoint.Configure(index, baseRenderer);
        return checkpoint;
    }

    private static void ConfigureRain(ParticleSystem rain, Material material)
    {
        var main = rain.main;
        main.loop = true;
        main.startLifetime = 1.3f;
        main.startSpeed = 15f;
        SetRainParticleSize(rain);
        main.maxParticles = 450;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(0.65f, 0.83f, 1f, 0.72f);
        var emission = rain.emission;
        emission.rateOverTime = 185f;
        var shape = rain.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(27f, 27f, 1f);
        rain.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private static void SetRainParticleSize(ParticleSystem rain)
    {
        var main = rain.main;
        main.startSize3D = true;
        main.startSizeX = 0.025f;
        main.startSizeY = 0.18f;
        main.startSizeZ = 0.025f;
    }

    private static void ConfigureRainMaterial(Material material)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        UnityEditor.BaseShaderGUI.SetupMaterialBlendMode(material);
        material.SetTexture("_BaseMap", EnsureRainTexture());
        material.color = new Color(0.72f, 0.86f, 1f, 0.55f);
        EditorUtility.SetDirty(material);
    }

    private static Texture2D EnsureRainTexture()
    {
        string assetPath = MaterialFolder + "Raindrop.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (existing != null)
            return existing;

        const int width = 16;
        const int height = 64;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            float vertical = Mathf.Sin(Mathf.PI * y / (height - 1));
            for (int x = 0; x < width; x++)
            {
                float horizontal = Mathf.Clamp01(1f - Mathf.Abs(x - 7.5f) / 6f);
                float alpha = vertical * horizontal * horizontal;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "Stage2_Imported/Raindrop.png"), texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static Material EnsureMaterial(string name, Color color, bool particle)
    {
        string path = MaterialFolder + name;
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;
        var shader = Shader.Find(particle ? "Universal Render Pipeline/Particles/Unlit" : "Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        material = new Material(shader) { name = name.Replace(".mat", "") };
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void BuildHud(GameObject hud, IslandGameSession session,
        TribalPlayerController player, IslandRainCycle weather)
    {
        var canvas = hud.GetComponent<Canvas>();
        if (canvas == null)
            throw new InvalidOperationException("PlayerHUD needs a Canvas.");
        if (hud.GetComponent<GraphicRaycaster>() == null)
            hud.AddComponent<GraphicRaycaster>();
        var scaler = hud.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = hud.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            throw new InvalidOperationException("Import TextMeshPro Essentials before building the HUD.");
        var white = Color.white;
        var muted = new Color(0.78f, 0.87f, 0.9f);
        var gold = new Color(1f, 0.82f, 0.39f);
        var score = Label(hud.transform, "ScoreText", font, 32, gold, TextAlignmentOptions.Right,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-33, -30), new Vector2(360, 44));
        var lives = Label(hud.transform, "LivesText", font, 27, white, TextAlignmentOptions.Right,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-33, -76), new Vector2(360, 40));
        var timer = Label(hud.transform, "TimeText", font, 27, white, TextAlignmentOptions.Right,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-33, -116), new Vector2(360, 40));
        var stamina = Label(hud.transform, "StaminaText", font, 25, muted, TextAlignmentOptions.Left,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -91), new Vector2(420, 40));
        var objective = Label(hud.transform, "ObjectiveText", font, 29, white, TextAlignmentOptions.Center,
            new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -40), new Vector2(440, 44));
        var checkpoint = Label(hud.transform, "CheckpointText", font, 23, muted, TextAlignmentOptions.Center,
            new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -83), new Vector2(440, 40));
        var weatherText = Label(hud.transform, "WeatherText", font, 22, muted, TextAlignmentOptions.Left,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -130), new Vector2(420, 38));
        Label(hud.transform, "ControlsHint", font, 21, muted, TextAlignmentOptions.Left,
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 24), new Vector2(780, 36)).text =
            PlayerActionAnimatorSetup.Controls;
        Label(hud.transform, "LevelHeader", font, 24, gold, TextAlignmentOptions.Center,
            new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -115), new Vector2(540, 36));

        var pauseButton = MakeButton(hud.transform, "PauseButton", "PAUSE", font,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -176), new Vector2(155, 48));

        var pausePanel = Panel(hud.transform, "PausePanel", new Vector2(540, 360));
        Label(pausePanel.transform, "PauseTitle", font, 44, gold, TextAlignmentOptions.Center,
            new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -35), new Vector2(440, 60)).text = "PAUSED";
        var resume = MakeButton(pausePanel.transform, "ResumeButton", "RESUME", font,
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 35), new Vector2(260, 54));
        var restart = MakeButton(pausePanel.transform, "RestartButton", "RESTART", font,
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -35), new Vector2(260, 54));
        var pauseExit = MakeButton(pausePanel.transform, "ExitButton", "EXIT", font,
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -105), new Vector2(260, 54));

        var resultPanel = Panel(hud.transform, "ResultPanel", new Vector2(560, 400));
        var result = Label(resultPanel.transform, "ResultTitle", font, 39, gold, TextAlignmentOptions.Center,
            new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -39), new Vector2(500, 62));
        var next = MakeButton(resultPanel.transform, "NextLevelButton", "NEXT LEVEL", font,
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 30), new Vector2(280, 56));
        var resultRestart = MakeButton(resultPanel.transform, "ResultRestartButton", "RESTART", font,
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -45), new Vector2(280, 56));
        var resultExit = MakeButton(resultPanel.transform, "ResultExitButton", "EXIT", font,
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -120), new Vector2(280, 56));

        var controller = hud.AddComponent<IslandGameHUD>();
        controller.Configure(session, player, weather, score, lives, timer, objective, stamina,
            checkpoint, weatherText, result, pausePanel, resultPanel, pauseButton, resume,
            restart, pauseExit, resultRestart, resultExit, next);
        pausePanel.SetActive(false);
        resultPanel.SetActive(false);
    }

    private static GameObject Panel(Transform parent, string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        go.GetComponent<UnityEngine.UI.Image>().color = new Color(.045f, .10f, .12f, .96f);
        return go;
    }

    private static TMP_Text Label(Transform parent, string name, TMP_FontAsset font,
        float size, Color color, TextAlignmentOptions alignment, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 dimensions)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private static Button MakeButton(Transform parent, string name, string caption, TMP_FontAsset font,
        Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 dimensions)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image), typeof(Button));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        go.GetComponent<UnityEngine.UI.Image>().color = new Color(.11f, .35f, .40f, 1f);
        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(.27f, .65f, .67f, 1f);
        colors.pressedColor = new Color(.07f, .24f, .26f, 1f);
        button.colors = colors;
        var text = Label(go.transform, "Label", font, 26, Color.white, TextAlignmentOptions.Center,
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, dimensions);
        text.text = caption;
        return button;
    }

    private static void EnsureEventSystem()
    {
        var events = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        if (events != null)
            return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }
}
