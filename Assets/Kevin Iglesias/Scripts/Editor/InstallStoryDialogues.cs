using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Adds introductions using the final level's existing story-panel design.</summary>
public static class InstallStoryDialogues
{
    private const string Level2 = "Assets/Scenes/Stage2_Battlefield.unity";
    private const string Level3 = "Assets/Scenes/Stage3_AshenCitadel.unity";
    private const string BossPortrait = "Assets/Stage3/Ending/NallegamPortrait.png";

    [MenuItem("Tools/Ending/Install Story Introductions")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave Play mode first.");
        var finalScene = SceneManager.GetSceneByPath(Level3);
        if (!finalScene.IsValid() || !finalScene.isLoaded)
            throw new InvalidOperationException("Open the Ashen Citadel scene first.");
        var source = Components<Level3EndingSequence>(finalScene).Single(x => x.name == "AshenCitadelEnding");
        var finalSession = Components<IslandGameSession>(finalScene).Single();
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(BossPortrait) == null)
            InstallLevel3Ending.CapturePortrait(finalSession.FinalBoss.gameObject, BossPortrait);
        var bossPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>(BossPortrait);

        var introduction = Components<Level3EndingSequence>(finalScene).FirstOrDefault(x => x.name == "NallegamIntroduction");
        if (introduction == null)
        {
            introduction = Clone(source, finalScene, "NallegamIntroduction", "LEVEL 3  /  BEFORE THE BATTLE", "A THRONE OF ASH", false, new[]
            {
                Beat("CHRONICLE", "The last guard falls. Above the silent courtyard, Nallegam steps forward to defend his throne."),
                Beat("NALLEGAM", "So, Upalupal. You have made it past my guards. Do you think that makes you ready to face me?", bossPortrait),
                Beat("UPALUPAL", "I came to free our people, Nallegam. Your hold over this island ends here."),
                Beat("NALLEGAM", "You will not leave this citadel. Let these walls witness your defeat.", bossPortrait),
                Beat("UPALUPAL", "They will witness our freedom.")
            });
        }
        Undo.RecordObject(finalSession, "Connect Nallegam introduction");
        finalSession.ConfigureBossIntroduction(introduction);
        EditorUtility.SetDirty(finalSession);
        EditorSceneManager.MarkSceneDirty(finalScene);
        EditorSceneManager.SaveScene(finalScene);

        var fieldScene = SceneManager.GetSceneByPath(Level2);
        bool openedField = !fieldScene.IsValid() || !fieldScene.isLoaded;
        if (openedField) fieldScene = EditorSceneManager.OpenScene(Level2, OpenSceneMode.Additive);
        var fieldSession = Components<IslandGameSession>(fieldScene).Single();
        var transition = Components<Level3EndingSequence>(fieldScene).FirstOrDefault(x => x.name == "JourneyToCitadel");
        if (transition == null)
        {
            transition = Clone(source, fieldScene, "JourneyToCitadel", "THE JOURNEY CONTINUES", "TOWARD THE CITADEL", true, new[]
            {
                Beat("CHRONICLE", "The last defender falls, and the road across the island lies open. Beyond it waits the Ashen Citadel."),
                Beat("UPALUPAL", "Nallegam is still there. As long as he holds the ember relics, our people will never be free."),
                Beat("CHRONICLE", "Three relics remain within the citadel. Their guardians stand between Upalupal and Nallegam's throne."),
                Beat("UPALUPAL", "Then that is where I am going. I will bring them home.")
            });
            var player = Components<TribalPlayerController>(fieldScene).Single();
            var camera = Components<Camera>(fieldScene).First(x => x.CompareTag("MainCamera"));
            var first = transition.transform.Find("OpeningShot");
            var last = transition.transform.Find("ClosingShot");
            first.position = camera.transform.position + new Vector3(3f, 1.4f, -2f);
            last.position = first.position + new Vector3(-1f, .5f, 2f);
            Vector3 lookAt = player.transform.position + new Vector3(0, 2f, 14f);
            first.LookAt(lookAt); last.LookAt(lookAt);
        }
        Undo.RecordObject(fieldSession, "Connect Level 3 introduction");
        fieldSession.ConfigureLevelTransition(transition);
        EditorUtility.SetDirty(fieldSession);
        EditorSceneManager.MarkSceneDirty(fieldScene);
        EditorSceneManager.SaveScene(fieldScene);
        if (openedField) EditorSceneManager.CloseScene(fieldScene, true);
        SceneManager.SetActiveScene(finalScene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = introduction.gameObject;
        Debug.Log("Installed Nallegam's introduction and the journey to Level 3.");
    }

    private static T[] Components<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true)).ToArray();

    private static Level3EndingSequence.StoryBeat Beat(string speaker, string text, Texture2D portrait = null) =>
        new Level3EndingSequence.StoryBeat { speaker = speaker, text = text, readingSeconds = 4.5f, portrait = portrait };

    private static Level3EndingSequence Clone(Level3EndingSequence source, Scene scene, string name,
        string eyebrow, string title, bool loadNextScene, Level3EndingSequence.StoryBeat[] beats)
    {
        var root = UnityEngine.Object.Instantiate(source.gameObject);
        root.name = name;
        SceneManager.MoveGameObjectToScene(root, scene);
        Undo.RegisterCreatedObjectUndo(root, "Add story introduction");
        var sequence = root.GetComponent<Level3EndingSequence>();
        var settings = new SerializedObject(sequence);
        settings.FindProperty("showEndCard").boolValue = false;
        settings.FindProperty("settleBeforeStory").boolValue = false;
        settings.FindProperty("keepBlackOnComplete").boolValue = loadNextScene;
        settings.FindProperty("player").objectReferenceValue = Components<TribalPlayerController>(scene).Single();
        settings.FindProperty("gameplayHud").objectReferenceValue = Components<Canvas>(scene).Single(x => x.name == "PlayerHUD");
        settings.FindProperty("sceneCamera").objectReferenceValue = Components<Camera>(scene).First(x => x.CompareTag("MainCamera"));
        var story = settings.FindProperty("story");
        story.arraySize = beats.Length;
        for (int i = 0; i < beats.Length; i++)
        {
            var beat = story.GetArrayElementAtIndex(i);
            beat.FindPropertyRelative("speaker").stringValue = beats[i].speaker;
            beat.FindPropertyRelative("text").stringValue = beats[i].text;
            beat.FindPropertyRelative("readingSeconds").floatValue = beats[i].readingSeconds;
            beat.FindPropertyRelative("portrait").objectReferenceValue = beats[i].portrait;
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
        var chapter = root.transform.Find("EndingCanvas/Chapter");
        chapter.Find("Eyebrow").GetComponent<TMP_Text>().text = eyebrow;
        chapter.Find("Title").GetComponent<TMP_Text>().text = title;
        root.transform.Find("EndingCanvas").gameObject.SetActive(false);
        return sequence;
    }
}
