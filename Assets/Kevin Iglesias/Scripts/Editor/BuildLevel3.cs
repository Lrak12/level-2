using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Builds the Ashen Citadel from the shared Level 2 gameplay setup.</summary>
public static class BuildLevel3
{
    private const string Scene2 = "Assets/Scenes/Stage2_Battlefield.unity";
    private const string Scene3 = "Assets/Scenes/Stage3_AshenCitadel.unity";
    private const string Folder = "Assets/Stage3";
    private static Transform world;
    private static Terrain terrain;
    private static Material basalt;
    private static Material trim;
    private static Material darkStone;
    private static Material lava;
    private static Material relicMaterial;
    private static Material sentinelArmor;
    private static Material guardianArmor;
    private static Material sentinelShield;
    private static Material guardianShield;
    private static Material emberSword;

    [MenuItem("Tools/Build Level 3 Ashen Citadel")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Leave Play mode before building Level 3.");
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Scene3) != null)
            throw new InvalidOperationException("Level 3 already exists. Keep the edited scene and do not rebuild over it.");

        EnsureFolder();
        var source = EditorSceneManager.OpenScene(Scene2, OpenSceneMode.Single);
        EditorSceneManager.SaveScene(source, Scene3, true);
        var scene = EditorSceneManager.OpenScene(Scene3, OpenSceneMode.Single);

        world = GameObject.Find("CompactIslandWorld").transform;
        terrain = world.Find("Terrain").GetComponent<Terrain>();
        GameObject player = world.Find("TribalPlayer").gameObject;
        var enemies = new List<IslandEnemyAI>();
        foreach (Transform child in world)
        {
            IslandEnemyAI enemy = child.GetComponent<IslandEnemyAI>();
            if (enemy != null)
                enemies.Add(enemy);
        }

        foreach (Transform child in new List<Transform>(Children(world)))
        {
            if (child.gameObject == terrain.gameObject || child.gameObject == player ||
                child.name == "Ocean" || child.GetComponent<IslandEnemyAI>() != null)
                continue;
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        foreach (string oldName in new[] { "Stage2_Battlefield_Layout", "PlayerSpawn_PLACEHOLDER",
                     "CombatArea_01", "CombatArea_02", "EnemySpawn_01_PLACEHOLDER",
                     "EnemySpawn_02_PLACEHOLDER", "EnemySpawn_03_PLACEHOLDER",
                     "EnemySpawn_04_PLACEHOLDER", "ChokePoint", "ExitToStage3_PLACEHOLDER", "VillageSquare" })
        {
            GameObject old = GameObject.Find(oldName);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old);
        }

        world.name = "AshenCitadelWorld";
        CreateMaterials();
        CreateTerrain();
        CreateArchitecture();
        CreateLava();
        CreateRelics();
        PlaceActors(player, enemies);
        ConfigureAtmosphere();
        ConfigureHudAndSession(player);
        AddLevel3FinalBoss.AddToOpenScene(false);
        AddLevel3HealingPickup.AddToOpenScene(false);

        var surface = terrain.GetComponent<NavMeshSurface>();
        surface.RemoveData();
        surface.BuildNavMesh();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var level2 = EditorSceneManager.OpenScene(Scene2, OpenSceneMode.Single);
        GameObject.Find("IslandGameSession").GetComponent<IslandGameSession>().SetNextScene("Stage3_AshenCitadel");
        EditorSceneManager.MarkSceneDirty(level2);
        EditorSceneManager.SaveScene(level2);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(Scene2, true),
            new EditorBuildSettingsScene(Scene3, true)
        };
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Scene3, OpenSceneMode.Single);
        Debug.Log("Built Level 3: Ashen Citadel, five guardians, three relics, lava hazards, three checkpoints.");
    }

    private static IEnumerable<Transform> Children(Transform parent)
    {
        foreach (Transform child in parent)
            yield return child;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets", "Stage3");
    }

    private static Material SaveMaterial(string name, Color color, bool emissive = false)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = name;
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", emissive ? 0.55f : 0.12f);
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.5f);
        }
        AssetDatabase.CreateAsset(material, Folder + "/" + name + ".mat");
        return material;
    }

    private static void CreateMaterials()
    {
        basalt = SaveMaterial("ObsidianBasalt", new Color(0.10f, 0.12f, 0.15f));
        darkStone = SaveMaterial("AshStone", new Color(0.25f, 0.24f, 0.23f));
        trim = SaveMaterial("AncientBronze", new Color(0.53f, 0.34f, 0.18f));
        lava = SaveMaterial("MoltenLava", new Color(0.95f, 0.22f, 0.035f), true);
        relicMaterial = SaveMaterial("EmberRelic", new Color(1f, 0.54f, 0.08f), true);
        sentinelArmor = SaveMaterial("SentinelArmor", new Color(0.17f, 0.15f, 0.16f));
        guardianArmor = SaveMaterial("GuardianArmor", new Color(0.36f, 0.12f, 0.09f));
        sentinelShield = SaveMaterial("SentinelShield", new Color(0.46f, 0.27f, 0.13f));
        guardianShield = SaveMaterial("GuardianShield", new Color(0.63f, 0.39f, 0.14f));
        emberSword = SaveMaterial("EmberSword", new Color(0.72f, 0.29f, 0.09f), true);
    }

    private static float Smooth(float a, float b, float value)
    {
        float t = Mathf.Clamp01((value - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    private static void CreateTerrain()
    {
        TerrainData data = UnityEngine.Object.Instantiate(terrain.terrainData);
        data.name = "AshenCitadelTerrain";
        AssetDatabase.CreateAsset(data, Folder + "/AshenCitadelTerrain.asset");
        terrain.terrainData = data;
        terrain.GetComponent<TerrainCollider>().terrainData = data;

        var colors = new[]
        {
            new Color(0.25f, 0.21f, 0.20f), new Color(0.18f, 0.18f, 0.19f),
            new Color(0.32f, 0.29f, 0.27f), new Color(0.22f, 0.17f, 0.16f),
            new Color(0.28f, 0.24f, 0.21f), new Color(0.16f, 0.17f, 0.19f),
            new Color(0.27f, 0.26f, 0.26f), new Color(0.39f, 0.31f, 0.25f)
        };
        var layers = new TerrainLayer[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            var texture = new Texture2D(128, 128, TextureFormat.RGB24, true);
            texture.name = "AshGroundTexture_" + i;
            texture.wrapMode = TextureWrapMode.Repeat;
            for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                float grain = Mathf.PerlinNoise((x + i * 43) * 0.15f, (y + i * 31) * 0.15f);
                float coarse = Mathf.PerlinNoise((x + i * 23) * 0.038f, (y + i * 19) * 0.038f);
                texture.SetPixel(x, y, colors[i] * (0.66f + grain * 0.34f + coarse * 0.22f));
            }
            texture.Apply(true);
            AssetDatabase.CreateAsset(texture, Folder + "/" + texture.name + ".asset");
            var layer = new TerrainLayer { name = "AshGround_" + i, diffuseTexture = texture,
                tileSize = new Vector2(11f, 11f), smoothness = 0.05f };
            AssetDatabase.CreateAsset(layer, Folder + "/" + layer.name + ".terrainlayer");
            layers[i] = layer;
        }
        data.terrainLayers = layers;

        int resolution = data.heightmapResolution;
        var heights = new float[resolution, resolution];
        Vector3 origin = terrain.transform.position;
        for (int z = 0; z < resolution; z++)
        for (int x = 0; x < resolution; x++)
        {
            float wx = origin.x + x * data.size.x / (resolution - 1);
            float wz = origin.z + z * data.size.z / (resolution - 1);
            float ridge = 17f * Smooth(26f, 65f, Mathf.Abs(wx - 60f));
            float edge = 5f * Smooth(104f, 135f, wz) + 3f * Smooth(5f, -10f, wz);
            float noise = (Mathf.PerlinNoise(wx * 0.045f, wz * 0.045f) - 0.5f) * 0.75f;
            float worldHeight = 5.8f + 1.3f * Smooth(25f, 95f, wz) + ridge + edge + noise;
            heights[z, x] = Mathf.Clamp01((worldHeight - origin.y) / data.size.y);
        }
        data.SetHeights(0, 0, heights);
        terrain.drawInstanced = true;
        terrain.heightmapPixelError = 4;
        EditorUtility.SetDirty(data);
    }

    private static float Ground(float x, float z) => terrain.transform.position.y + terrain.SampleHeight(new Vector3(x, 0f, z));

    private static GameObject Primitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material material, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.transform.SetParent(parent, true);
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private static void Column(string name, float x, float z, float height)
    {
        float y = Ground(x, z);
        var root = new GameObject(name);
        root.transform.SetParent(world);
        Primitive("Foot", PrimitiveType.Cube, new Vector3(x, y + 0.28f, z), new Vector3(2.5f, 0.55f, 2.5f), darkStone, root.transform);
        Primitive("Shaft", PrimitiveType.Cylinder, new Vector3(x, y + height * 0.5f, z), new Vector3(1.5f, height * 0.5f, 1.5f), basalt, root.transform);
        Primitive("Capital", PrimitiveType.Cube, new Vector3(x, y + height + 0.2f, z), new Vector3(2.8f, 0.5f, 2.8f), trim, root.transform);
    }

    private static void CreateArchitecture()
    {
        var architecture = new GameObject("CitadelArchitecture");
        architecture.transform.SetParent(world);
        Transform parent = architecture.transform;

        for (int z = 25; z <= 96; z += 4)
        {
            float y = Ground(60f, z);
            Primitive("StonePath_" + z, PrimitiveType.Cube, new Vector3(60f, y + 0.02f, z),
                new Vector3(8f, 0.16f, 3.4f), darkStone, parent);
        }

        foreach (int z in new[] { 34, 66 })
        {
            Column("GateColumn_Left_" + z, 50f, z, 5.5f);
            Column("GateColumn_Right_" + z, 70f, z, 5.5f);
            float y = Mathf.Max(Ground(50f, z), Ground(70f, z));
            Primitive("GateLintel_" + z, PrimitiveType.Cube, new Vector3(60f, y + 6.15f, z),
                new Vector3(23f, 1.2f, 2.4f), basalt, parent);
            Primitive("GateAccent_" + z, PrimitiveType.Cube, new Vector3(60f, y + 6.9f, z),
                new Vector3(23f, 0.24f, 2.6f), trim, parent);
        }

        foreach (int z in new[] { 29, 61, 88 })
        {
            Brazier(49f, z, parent);
            Brazier(71f, z, parent);
        }

        foreach (float x in new[] { 42f, 78f })
        foreach (float z in new[] { 53f, 78f, 94f })
            Column("RuinedColumn_" + x + "_" + z, x, z, (z > 90f ? 6f : 3.8f));

        float templeY = Ground(60f, 96f);
        Primitive("TempleDais", PrimitiveType.Cube, new Vector3(60f, templeY + 0.12f, 96f),
            new Vector3(31f, 0.38f, 21f), darkStone, parent);
        Primitive("TempleBackWall", PrimitiveType.Cube, new Vector3(60f, templeY + 3.8f, 105f),
            new Vector3(31f, 7.6f, 1.9f), basalt, parent);
        Primitive("TempleTopTrim", PrimitiveType.Cube, new Vector3(60f, templeY + 7.7f, 105f),
            new Vector3(33f, 0.5f, 2.2f), trim, parent);
        Primitive("Altar", PrimitiveType.Cube, new Vector3(60f, templeY + 1.1f, 101f),
            new Vector3(5f, 2.2f, 3f), basalt, parent);

        var random = new System.Random(3993);
        for (int i = 0; i < 38; i++)
        {
            float x = (i % 2 == 0 ? 27f : 93f) + (float)random.NextDouble() * 13f - 6.5f;
            float z = 22f + (float)random.NextDouble() * 81f;
            float y = Ground(x, z);
            var rock = Primitive("ObsidianSpire_" + i, PrimitiveType.Cube,
                new Vector3(x, y + 1.4f, z), new Vector3(1.5f, 3.1f + (float)random.NextDouble() * 3f, 1.8f), basalt, parent);
            rock.transform.rotation = Quaternion.Euler(12f + i % 3 * 8f, i * 27f, 9f);
        }
    }

    private static void Brazier(float x, float z, Transform parent)
    {
        float y = Ground(x, z);
        Primitive("BrazierStand", PrimitiveType.Cylinder, new Vector3(x, y + 0.7f, z),
            new Vector3(0.9f, 0.7f, 0.9f), trim, parent);
        var flame = Primitive("BrazierFlame", PrimitiveType.Sphere, new Vector3(x, y + 1.65f, z),
            new Vector3(0.55f, 0.85f, 0.55f), relicMaterial, parent);
        UnityEngine.Object.DestroyImmediate(flame.GetComponent<Collider>());
        var point = flame.AddComponent<Light>();
        point.type = LightType.Point;
        point.color = new Color(1f, 0.38f, 0.12f);
        point.intensity = 3.2f;
        point.range = 9f;
    }

    private static void CreateLava()
    {
        var hazards = new GameObject("LavaFields");
        hazards.transform.SetParent(world);
        var locations = new[]
        {
            new Vector3(40f, 0f, 43f), new Vector3(79f, 0f, 47f),
            new Vector3(38f, 0f, 71f), new Vector3(82f, 0f, 76f),
            new Vector3(50f, 0f, 99f), new Vector3(72f, 0f, 100f)
        };
        for (int i = 0; i < locations.Length; i++)
        {
            float x = locations[i].x, z = locations[i].z;
            float radius = i < 4 ? 6.2f : 3.8f;
            float y = Ground(x, z);
            var disc = Primitive("LavaPool_" + i, PrimitiveType.Cylinder,
                new Vector3(x, y + 0.06f, z), new Vector3(radius * 2f, 0.055f, radius * 2f), lava, hazards.transform);
            UnityEngine.Object.DestroyImmediate(disc.GetComponent<Collider>());
            var trigger = new GameObject("HeatHazard_" + i);
            trigger.transform.SetParent(hazards.transform);
            trigger.transform.position = new Vector3(x, y + 0.6f, z);
            var box = trigger.AddComponent<BoxCollider>();
            box.size = new Vector3(radius * 1.7f, 1.2f, radius * 1.7f);
            box.isTrigger = true;
            trigger.AddComponent<VolcanicHazard>().Configure(12f, 1.3f);
        }
    }

    private static void CreateRelics()
    {
        var relics = new GameObject("EmberRelics");
        relics.transform.SetParent(world);
        var positions = new[] { new Vector2(43f, 39f), new Vector2(78f, 60f), new Vector2(60f, 91f) };
        for (int i = 0; i < positions.Length; i++)
        {
            float x = positions[i].x, z = positions[i].y, y = Ground(x, z);
            var root = new GameObject("EmberRelic_" + (i + 1));
            root.transform.SetParent(relics.transform);
            root.transform.position = new Vector3(x, y, z);
            var trigger = root.AddComponent<SphereCollider>();
            trigger.radius = 2.1f;
            trigger.isTrigger = true;
            Primitive("Plinth", PrimitiveType.Cylinder, new Vector3(x, y + 0.5f, z),
                new Vector3(2.5f, 0.5f, 2.5f), darkStone, root.transform);
            var crystal = Primitive("Crystal", PrimitiveType.Cube, new Vector3(x, y + 2.0f, z),
                new Vector3(0.9f, 1.8f, 0.9f), relicMaterial, root.transform);
            crystal.transform.rotation = Quaternion.Euler(0f, 45f, 25f);
            UnityEngine.Object.DestroyImmediate(crystal.GetComponent<Collider>());
            root.AddComponent<Level3Relic>().Configure(crystal.GetComponent<Renderer>());
            var glow = new GameObject("EmberGlow");
            glow.transform.SetParent(root.transform);
            glow.transform.position = new Vector3(x, y + 2.3f, z);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.4f, 0.12f);
            light.intensity = 3.5f;
            light.range = 8f;
        }
    }

    private static void PlaceActors(GameObject player, List<IslandEnemyAI> enemies)
    {
        player.transform.position = new Vector3(60f, Ground(60f, 18f) + 0.3f, 18f);
        player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        var checkpoints = new[] { new Vector2(60f, 18f), new Vector2(60f, 52f), new Vector2(60f, 81f) };
        for (int i = 0; i < checkpoints.Length; i++)
        {
            var checkpoint = GameObject.Find("Checkpoint_0" + (i + 1));
            float x = checkpoints[i].x, z = checkpoints[i].y;
            checkpoint.transform.position = new Vector3(x, Ground(x, z) + 0.12f, z);
            checkpoint.GetComponent<IslandCheckpoint>().ConfigureActiveColor(new Color(0.95f, 0.38f, 0.12f));
            foreach (Renderer renderer in checkpoint.GetComponentsInChildren<Renderer>())
            {
                if (renderer.sharedMaterial != null)
                    renderer.sharedMaterial = trim;
                renderer.transform.localScale *= 0.65f;
            }
        }

        var positions = new[]
        {
            new Vector2(60f, 43f), new Vector2(45f, 56f), new Vector2(75f, 63f),
            new Vector2(55f, 79f), new Vector2(68f, 94f)
        };
        IslandEnemyAI guardian = UnityEngine.Object.Instantiate(enemies[0], world);
        enemies.Add(guardian);
        for (int i = 0; i < enemies.Count; i++)
        {
            float x = positions[i].x, z = positions[i].y;
            IslandEnemyAI enemy = enemies[i];
            enemy.name = i == 4 ? "CitadelGuardian" : "AshSentinel_0" + (i + 1);
            enemy.transform.position = new Vector3(x, Ground(x, z) + 0.18f, z);
            enemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            enemy.SetTarget(player.transform);
            enemy.ConfigureCombat(i == 4 ? 22f : 16f, i == 4 ? 2.5f : 3f, i == 4 ? 160f : 100f);
            foreach (Renderer renderer in enemy.GetComponentsInChildren<Renderer>())
            {
                if (renderer.name.Contains("Shield"))
                    renderer.sharedMaterial = i == 4 ? guardianShield : sentinelShield;
                else if (renderer.name.Contains("Sword"))
                    renderer.sharedMaterial = emberSword;
                else if (renderer is SkinnedMeshRenderer)
                    renderer.sharedMaterial = i == 4 ? guardianArmor : sentinelArmor;
            }
            if (i == 4)
                enemy.transform.localScale = Vector3.one * 1.25f;
        }
    }

    private static void ConfigureAtmosphere()
    {
        var light = GameObject.Find("Directional Light").GetComponent<Light>();
        light.transform.rotation = Quaternion.Euler(29f, -35f, 0f);
        light.color = new Color(1f, 0.60f, 0.37f);
        light.intensity = 1.45f;
        RenderSettings.ambientIntensity = 0.95f;
        RenderSettings.reflectionIntensity = 0.18f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.42f, 0.31f, 0.28f);
        RenderSettings.fogDensity = 0.0045f;

        var skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            var sky = new Material(skyShader) { name = "AshenDuskSky" };
            sky.SetColor("_SkyTint", new Color(0.43f, 0.28f, 0.26f));
            sky.SetColor("_GroundColor", new Color(0.18f, 0.14f, 0.14f));
            sky.SetFloat("_AtmosphereThickness", 1.8f);
            sky.SetFloat("_Exposure", 0.8f);
            AssetDatabase.CreateAsset(sky, Folder + "/AshenDuskSky.mat");
            RenderSettings.skybox = sky;
        }

        var ocean = world.Find("Ocean").GetComponent<Renderer>();
        ocean.sharedMaterial = lava;
        ocean.gameObject.name = "LavaSea";

        var volume = GameObject.Find("Level2GlobalVolume").GetComponent<Volume>();
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "Level3VisualGrade";
        AssetDatabase.CreateAsset(profile, Folder + "/Level3VisualGrade.asset");
        volume.sharedProfile = profile;
        volume.gameObject.name = "Level3GlobalVolume";
        var tone = AddGrade<Tonemapping>(profile);
        tone.mode.Override(TonemappingMode.Neutral);
        var color = AddGrade<ColorAdjustments>(profile);
        color.postExposure.Override(-0.16f);
        color.contrast.Override(15f);
        color.saturation.Override(-8f);
        color.colorFilter.Override(new Color(1f, 0.85f, 0.77f));
        var white = AddGrade<WhiteBalance>(profile);
        white.temperature.Override(18f);
        var bloom = AddGrade<Bloom>(profile);
        bloom.threshold.Override(1.2f);
        bloom.intensity.Override(0.32f);
        var vignette = AddGrade<Vignette>(profile);
        vignette.intensity.Override(0.16f);
        vignette.smoothness.Override(0.4f);
        EditorUtility.SetDirty(profile);

        var weather = GameObject.Find("TropicalRainCycle");
        weather.name = "VolcanicAshCycle";
        weather.GetComponent<IslandRainCycle>().ConfigureCycle(24f, 17f, "CALM", "ASH FALL");
        var particles = weather.GetComponent<ParticleSystem>();
        var main = particles.main;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.33f, 0.28f, 0.55f));
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
        main.gravityModifier = 0.1f;
    }

    private static T AddGrade<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T effect = profile.Add<T>(true);
        AssetDatabase.AddObjectToAsset(effect, profile);
        EditorUtility.SetDirty(effect);
        return effect;
    }

    private static void ConfigureHudAndSession(GameObject player)
    {
        var session = GameObject.Find("IslandGameSession").GetComponent<IslandGameSession>();
        session.ConfigureRelicObjective(3, 5, 240f);
        session.SetNextScene(string.Empty);
        var hud = GameObject.Find("PlayerHUD").transform;
        var header = hud.Find("LevelHeader").GetComponent<TMP_Text>();
        header.text = "LEVEL 3  •  THE ASHEN CITADEL";
        header.color = new Color(1f, 0.68f, 0.39f);
        var objective = hud.Find("ObjectiveText").GetComponent<TMP_Text>();
        objective.fontSize = 25f;
        objective.rectTransform.sizeDelta = new Vector2(570f, 44f);
        var controls = hud.Find("ControlsHint").GetComponent<TMP_Text>();
        controls.text = PlayerActionAnimatorSetup.Controls;
    }
}
