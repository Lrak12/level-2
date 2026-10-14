using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>Adds the final boss phase to the existing Ashen Citadel scene.</summary>
public static class AddLevel3FinalBoss
{
    private const string Folder = "Assets/Stage3";

    [MenuItem("Tools/Add Level 3 Final Boss")]
    public static void InstallMenu() => AddToOpenScene(true);

    public static void AddToOpenScene(bool save)
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Leave Play mode before adding the boss.");

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Stage3_AshenCitadel.unity")
            throw new InvalidOperationException("Open Stage3_AshenCitadel before adding the boss.");

        Transform world = GameObject.Find("AshenCitadelWorld").transform;
        if (world.Find("AshenWarlord") != null)
            return;

        GameObject player = world.Find("TribalPlayer").gameObject;
        GameObject captain = world.Find("CitadelGuardian").gameObject;
        Terrain terrain = world.Find("Terrain").GetComponent<Terrain>();
        float x = 60f, z = 97f;
        float ground = terrain.transform.position.y + terrain.SampleHeight(new Vector3(x, 0f, z));

        var boss = UnityEngine.Object.Instantiate(captain, world);
        boss.name = "AshenWarlord";
        boss.transform.position = new Vector3(x, ground + 0.25f, z);
        boss.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        boss.transform.localScale = Vector3.one * 1.65f;
        var ai = boss.GetComponent<IslandEnemyAI>();
        ai.ConfigureBossCombat(24f, 2.7f, 350f);
        ai.SetTarget(player.transform);

        Material armor = MakeMaterial("WarlordArmor", new Color(0.38f, 0.075f, 0.055f));
        Material shield = MakeMaterial("WarlordShield", new Color(0.62f, 0.26f, 0.07f));
        Material blade = MakeMaterial("WarlordBlade", new Color(1f, 0.32f, 0.04f), true);
        Material warningMaterial = MakeWarningMaterial();
        foreach (Renderer renderer in boss.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.name.Contains("Shield"))
                renderer.sharedMaterial = shield;
            else if (renderer.name.Contains("Sword"))
                renderer.sharedMaterial = blade;
            else if (renderer is SkinnedMeshRenderer)
                renderer.sharedMaterial = armor;
        }

        var warning = new GameObject("GroundSlamWarning");
        warning.transform.SetParent(boss.transform, false);
        warning.transform.localPosition = Vector3.up * 0.15f;
        warning.transform.localScale = Vector3.one / boss.transform.localScale.x;
        var ring = warning.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 64;
        ring.widthMultiplier = 0.14f;
        ring.sharedMaterial = warningMaterial;
        ring.shadowCastingMode = ShadowCastingMode.Off;
        ring.receiveShadows = false;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / ring.positionCount;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * 4.3f, 0f, Mathf.Sin(angle) * 4.3f));
        }
        ring.enabled = false;

        var glow = new GameObject("WarlordGlow");
        glow.transform.SetParent(boss.transform, false);
        glow.transform.localPosition = Vector3.up * 2f;
        var light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.32f, 0.15f);
        light.intensity = 1.6f;
        light.range = 6f;

        var special = boss.AddComponent<Level3FinalBoss>();
        special.Configure(player.transform, ring);
        special.ConfigureAttack(18f, 8f);
        var session = GameObject.Find("IslandGameSession").GetComponent<IslandGameSession>();
        session.ConfigureFinalBoss(ai);
        session.ConfigureRelicObjective(3, 5, 360f);
        BuildBossHud(session);
        boss.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        if (save)
        {
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        Debug.Log("Level 3 final boss installed. Defeat five guards to summon Nallegam.");
    }

    private static Material MakeMaterial(string name, Color color, bool emissive = false)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = name;
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", emissive ? 0.55f : 0.18f);
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.6f);
        }
        AssetDatabase.CreateAsset(material, Folder + "/" + name + ".mat");
        return material;
    }

    private static Material MakeWarningMaterial()
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.name = "WarlordGroundSlamWarning";
        material.SetColor("_BaseColor", new Color(1f, 0.48f, 0.07f));
        AssetDatabase.CreateAsset(material, Folder + "/WarlordGroundSlamWarning.mat");
        return material;
    }

    private static void BuildBossHud(IslandGameSession session)
    {
        Transform hud = GameObject.Find("PlayerHUD").transform;
        TMP_Text reference = hud.Find("LevelHeader").GetComponent<TMP_Text>();
        var panel = new GameObject("BossPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(hud, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 78f);
        panelRect.sizeDelta = new Vector2(640f, 82f);
        var panelImage = panel.GetComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0.05f, 0.025f, 0.025f, 0.8f);
        panelImage.raycastTarget = false;

        var title = new GameObject("BossTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        title.transform.SetParent(panel.transform, false);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -7f);
        titleRect.sizeDelta = new Vector2(610f, 32f);
        var titleText = title.GetComponent<TextMeshProUGUI>();
        titleText.font = reference.font;
        titleText.fontSize = 22f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.72f, 0.5f);
        titleText.raycastTarget = false;

        var background = new GameObject("BossHealthBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        background.transform.SetParent(panel.transform, false);
        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 0f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0f);
        backgroundRect.pivot = new Vector2(0.5f, 0f);
        backgroundRect.anchoredPosition = new Vector2(0f, 14f);
        backgroundRect.sizeDelta = new Vector2(606f, 24f);
        var backgroundImage = background.GetComponent<UnityEngine.UI.Image>();
        backgroundImage.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        backgroundImage.raycastTarget = false;

        var fill = new GameObject("BossHealthFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        fill.transform.SetParent(background.transform, false);
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        var fillImage = fill.GetComponent<UnityEngine.UI.Image>();
        fillImage.color = new Color(0.92f, 0.2f, 0.07f);
        fillImage.type = UnityEngine.UI.Image.Type.Filled;
        fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        fillImage.fillAmount = 1f;
        fillImage.raycastTarget = false;

        hud.gameObject.AddComponent<Level3BossHUD>().Configure(session, panel, fillImage, titleText);
        panel.SetActive(false);
    }
}
