using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class IntroMergeValidation
{
    public const string IntroScene = "Assets/Intro/Scenes/Intro.unity";
    public const string Level2Scene = "Assets/Scenes/Stage2_Battlefield.unity";
    public const string Level3Scene = "Assets/Scenes/Stage3_AshenCitadel.unity";
    private const string Pending = "IntroMergeValidation.Pending";
    private const string Report = "VisualReview/IntroMerge/Validation.txt";
    private static int phase;
    private static double phaseStart;
    private static bool failed;

    static IntroMergeValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayState;
        Application.logMessageReceived += OnLog;
    }

    [MenuItem("Tools/Intro/Configure and Validate Merge")]
    public static void ConfigureAndValidate()
    {
        Directory.CreateDirectory("VisualReview/IntroMerge");
        File.WriteAllText(Report, "Intro merge validation\n");
        SessionState.SetBool("SetupTribalPlayerLevel2.MouseOrbitV10", true);
        var remaining = EditorBuildSettings.scenes.Where(s => s.path != IntroScene).ToList();
        Require(remaining.Any(s => s.path == Level2Scene && s.enabled), "Level 2 remains enabled");
        Require(remaining.Any(s => s.path == Level3Scene && s.enabled), "Level 3 remains enabled");
        remaining.Insert(0, new EditorBuildSettingsScene(IntroScene, true));
        EditorBuildSettings.scenes = remaining.ToArray();
        foreach (var scenePath in new[] { IntroScene, Level2Scene, Level3Scene })
        {
            var scene = EditorSceneManager.OpenScene(scenePath);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                        throw new Exception("Missing script: " + scenePath + " / " + transform.name);
            Pass("No missing scripts in " + scene.name);
            if (scenePath == IntroScene)
            {
                var menu = Object.FindFirstObjectByType<MainMenu>();
                Require(menu != null && menu.FirstGameplayScene == Level2Scene, "New Game targets level 2");
                Require(Object.FindFirstObjectByType<EventSystem>() != null, "Menu event system present");
                foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    Require(button.onClick.GetPersistentEventCount() > 0, "Button wired: " + button.name);
                    for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                        Require(button.onClick.GetPersistentTarget(i) != null, "Button callback resolves: " + button.name);
                }
                foreach (var label in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    Require(label.font != null && label.font.material != null, "Font resolves: " + label.name);
                var motion = Object.FindFirstObjectByType<MenuBackdropMotion>();
                Require(motion != null && motion.GetComponent<Image>().sprite != null, "Animated background artwork resolves");
                foreach (var script in new MonoBehaviour[] { menu, Object.FindFirstObjectByType<UIAudio>(), Object.FindFirstObjectByType<OptionsMenu>(FindObjectsInactive.Include) })
                {
                    Require(script != null, "Menu script available");
                    var iterator = new SerializedObject(script).GetIterator();
                    while (iterator.NextVisible(true))
                        if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                            Require(iterator.objectReferenceValue != null, script.GetType().Name + "." + iterator.name + " resolves");
                }
            }
            else
            {
                Require(Object.FindFirstObjectByType<TribalPlayerController>() != null, "Player resolves in " + scene.name);
                var session = Object.FindFirstObjectByType<IslandGameSession>();
                Require(session != null, "Game session resolves in " + scene.name);
                if (scenePath == Level2Scene)
                    Require(new SerializedObject(session).FindProperty("nextSceneName").stringValue == "Stage3_AshenCitadel", "Level 2 still leads to level 3");
            }
        }
        EditorSceneManager.OpenScene(IntroScene);
        AssetDatabase.SaveAssets();
        Pass("Build order: Intro -> Stage2_Battlefield -> Stage3_AshenCitadel");
        Debug.Log("INTRO_MERGE_EDIT_PASS");
    }

    public static void BeginPlayTest()
    {
        SessionState.SetBool("SetupTribalPlayerLevel2.MouseOrbitV10", true);
        EditorSceneManager.OpenScene(IntroScene);
        SessionState.SetBool(Pending, true);
        phaseStart = EditorApplication.timeSinceStartup;
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            phase = 0;
            phaseStart = EditorApplication.timeSinceStartup;
            Application.runInBackground = true;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Pending, false);
            EditorSceneManager.OpenScene(IntroScene);
            Debug.Log(failed ? "INTRO_MERGE_PLAY_FAIL" : "INTRO_MERGE_PLAY_PASS");
            if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
        }
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (!SessionState.GetBool(Pending, false) || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
        failed = true;
        File.AppendAllText(Report, "RUNTIME ERROR: " + message + "\n" + stack + "\n");
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            double elapsed = EditorApplication.timeSinceStartup - phaseStart;
            if (elapsed > 90) throw new Exception("Play validation timed out at phase " + phase);
            if (elapsed < 1.5) return;
            if (phase == 0)
            {
                Require(SceneManager.GetActiveScene().path == IntroScene, "Play starts in Intro");
                var menu = Object.FindFirstObjectByType<MainMenu>();
                var data = new SerializedObject(menu);
                var main = (GameObject)data.FindProperty("mainMenuPanel").objectReferenceValue;
                var options = (GameObject)data.FindProperty("optionsPanel").objectReferenceValue;
                var help = (GameObject)data.FindProperty("howToPlayPanel").objectReferenceValue;
                menu.OpenOptions(); Require(options.activeInHierarchy && !main.activeSelf, "Options opens");
                menu.CloseOptions(); Require(main.activeInHierarchy && !options.activeSelf, "Options closes");
                menu.OpenHowToPlay(); Require(help.activeInHierarchy && !main.activeSelf, "How to Play opens");
                menu.CloseHowToPlay(); Require(main.activeInHierarchy && !help.activeSelf, "How to Play closes");
                Require(Time.timeScale == 1 && Cursor.visible, "Menu restores time and cursor");
                var motion = Object.FindFirstObjectByType<MenuBackdropMotion>();
                motion.EvaluateAt(0); var initial = motion.transform.localPosition;
                motion.EvaluateAt(12); Require(Vector3.Distance(initial, motion.transform.localPosition) > 1, "Backdrop animates");
                var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == "New Game");
                button.onClick.Invoke();
                Next(1);
            }
            else if (phase == 1)
            {
                ValidateGameplay(Level2Scene);
                Require(Object.FindFirstObjectByType<MainMenu>() == null, "Intro unloads after New Game");
                Require(Object.FindFirstObjectByType<UIAudio>() == null, "Menu audio unloads after New Game");
                IslandGameSession.Instance.TogglePause();
                Require(Time.timeScale == 0, "Level 2 pauses");
                IslandGameSession.Instance.Restart();
                Next(2);
            }
            else if (phase == 2)
            {
                ValidateGameplay(Level2Scene);
                Pass("Restart returns to level 2 after build indices changed");
                SceneManager.LoadScene(Level3Scene);
                Next(3);
            }
            else if (phase == 3)
            {
                ValidateGameplay(Level3Scene);
                SceneManager.LoadScene(IntroScene);
                Next(4);
            }
            else if (phase == 4)
            {
                Require(Object.FindFirstObjectByType<MainMenu>() != null, "Intro can be reloaded");
                Require(Time.timeScale == 1 && Cursor.visible && Cursor.lockState == CursorLockMode.None, "Reloaded intro restores cursor and time");
                Pass("Runtime smoke test completed");
                phase = 5;
                EditorApplication.isPlaying = false;
            }
        }
        catch (Exception exception)
        {
            failed = true;
            File.AppendAllText(Report, "FAIL: " + exception + "\n");
            EditorApplication.isPlaying = false;
        }
    }

    private static void ValidateGameplay(string path)
    {
        Require(SceneManager.GetActiveScene().path == path, "Loaded " + path);
        Require(IslandGameSession.Instance != null, "Session initialized");
        Require(Object.FindFirstObjectByType<TribalPlayerController>() != null && Camera.main != null, "Player and camera initialized");
        Require(Time.timeScale == 1, "Gameplay time is running");
    }
    private static void Next(int next) { phase = next; phaseStart = EditorApplication.timeSinceStartup; }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); Pass(message); }
    private static void Pass(string message) { File.AppendAllText(Report, "PASS: " + message + "\n"); }
}