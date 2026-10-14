using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Places the single full-health pickup on the Level 3 boss approach.</summary>
public static class AddLevel3HealingPickup
{
    [MenuItem("Tools/Add Level 3 Healing Pickup")]
    public static void InstallMenu() => AddToOpenScene(true);

    public static void AddToOpenScene(bool save)
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Leave Play mode before placing the healing pickup.");

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Stage3_AshenCitadel.unity")
            throw new InvalidOperationException("Open Stage3_AshenCitadel before placing the pickup.");

        Transform world = GameObject.Find("AshenCitadelWorld").transform;
        if (world.Find("FullHealPickup") != null)
            return;

        Terrain terrain = world.Find("Terrain").GetComponent<Terrain>();
        float x = 53f, z = 84f;
        float y = terrain.transform.position.y + terrain.SampleHeight(new Vector3(x, 0f, z));
        Material orbMaterial = MakeMaterial("HealingOrb", new Color(0.12f, 0.85f, 0.31f));
        Material crossMaterial = MakeMaterial("HealingCross", new Color(0.86f, 1f, 0.88f));
        Material baseMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Stage3/AshStone.mat");

        var root = new GameObject("FullHealPickup");
        root.transform.SetParent(world);
        root.transform.position = new Vector3(x, y, z);
        var trigger = root.AddComponent<SphereCollider>();
        trigger.center = new Vector3(0f, 1.25f, 0f);
        trigger.radius = 1.45f;
        trigger.isTrigger = true;

        Primitive("HealingPlinth", PrimitiveType.Cylinder, root.transform,
            new Vector3(0f, 0.22f, 0f), new Vector3(1.7f, 0.22f, 1.7f), baseMaterial);
        var visual = new GameObject("FloatingHealIcon");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        Primitive("GreenOrb", PrimitiveType.Sphere, visual.transform,
            Vector3.zero, Vector3.one * 0.78f, orbMaterial);
        Primitive("CrossVertical", PrimitiveType.Cube, visual.transform,
            new Vector3(0f, 0f, -0.38f), new Vector3(0.16f, 0.58f, 0.09f), crossMaterial);
        Primitive("CrossHorizontal", PrimitiveType.Cube, visual.transform,
            new Vector3(0f, 0f, -0.39f), new Vector3(0.58f, 0.16f, 0.09f), crossMaterial);

        var glow = visual.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(0.24f, 1f, 0.42f);
        glow.intensity = 2.4f;
        glow.range = 6f;
        root.AddComponent<Level3HealingPickup>().Configure(visual.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        if (save)
        {
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        Debug.Log("Placed one full-health pickup beside the Level 3 final approach.");
    }

    private static Material MakeMaterial(string name, Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = name;
        material.SetColor("_BaseColor", color);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * 2.2f);
        AssetDatabase.CreateAsset(material, "Assets/Stage3/" + name + ".mat");
        return material;
    }

    private static GameObject Primitive(string name, PrimitiveType type, Transform parent,
        Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (name != "HealingPlinth")
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
}
