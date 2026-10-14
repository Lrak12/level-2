using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class ReviewLevelPresentation
{
    [MenuItem("Tools/Island Game/Validate Presentation and Save Previews")]
    public static void ReviewBoth()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene first.");
        var report = new StringBuilder("Presentation validation\n");
        Directory.CreateDirectory("VisualReview");
        foreach (string sceneName in new[] { "Stage2_Battlefield", "Stage3_AshenCitadel" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity");
            var manager = Object.FindObjectsByType<LevelPresentation>(FindObjectsSortMode.None);
            Require(manager.Length == 1, "One presentation manager");
            var data = new SerializedObject(manager[0]);
            foreach (string name in new[] { "music", "ambience", "rainLoop", "player", "weather", "particleMaterial" })
                Require(data.FindProperty(name).objectReferenceValue != null, name + " reference");
            var effects = data.FindProperty("effects");
            Require(effects.arraySize == Enum.GetValues(typeof(LevelSound)).Length, "Sound slot count");
            for (int i=0;i<effects.arraySize;i++) Require(effects.GetArrayElementAtIndex(i).objectReferenceValue != null, "SFX " + i);
            var camera = Camera.main;
            Require(camera != null && camera.GetUniversalAdditionalCameraData().renderPostProcessing, "Camera post processing");
            var volume = Object.FindFirstObjectByType<Volume>();
            Require(volume != null && volume.sharedProfile != null && volume.sharedProfile.components.Count == 4, "Saved volume overrides");
            Require(volume.sharedProfile.TryGet<Bloom>(out var bloom) && bloom.intensity.overrideState, "Active bloom override");
            int missing = 0;
            foreach(var root in scene.GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true))
                missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            Require(missing == 0, "No missing scripts");
            int fire = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Count(a=>a.clip != null && a.clip.name == "Fire_Loop");
            Require(fire == (sceneName.Contains("Stage3") ? 6 : 2), "Campfire/brazier sound sources");
            foreach(var shader in new[]{"Island/Living Surface","Island/Soft Particle","Island/Ashen Sky"})
                Require(Shader.Find(shader)!=null && !ShaderUtil.ShaderHasError(Shader.Find(shader)), "Compiled shader " + shader);
            int particleCount = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;
            report.AppendLine(sceneName+": PASS; music + ambience + "+effects.arraySize+" SFX; "+fire+" fires; "+particleCount+" ambient particle systems; 4 volume effects; no missing scripts.");
            foreach(var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                if(ps.name != "TropicalRainCycle" && ps.name != "VolcanicAshCycle") { ps.Simulate(2.8f,true,true); ps.Pause(); }
            Capture(camera,"VisualReview/"+sceneName+".png");
            // Additional view shows water / lava and environment detail outside the spawn framing.
            Vector3 pos = camera.transform.position; Quaternion rot = camera.transform.rotation;
            camera.transform.position = sceneName.Contains("Stage3") ? new Vector3(40,13,28) : new Vector3(111,14,47);
            camera.transform.LookAt(sceneName.Contains("Stage3") ? new Vector3(60,8,68) : new Vector3(73,5,57));
            Capture(camera,"VisualReview/"+sceneName+"_Environment.png");
            camera.transform.SetPositionAndRotation(pos,rot);
        }
        File.WriteAllText("VisualReview/Validation.txt",report.ToString());
        // Reload restores preview-only camera and particle state.
        EditorSceneManager.OpenScene("Assets/Scenes/Stage3_AshenCitadel.unity");
        Debug.Log(report.ToString());
    }

    private static void Require(bool condition,string label)
    { if(!condition)throw new InvalidOperationException("Presentation validation failed: "+label); }

    private static void Capture(Camera camera,string path)
    {
        var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);
        var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=previousTarget;RenderTexture.active=previousActive;
            Object.DestroyImmediate(texture);RenderTexture.ReleaseTemporary(rt);
        }
    }
}
