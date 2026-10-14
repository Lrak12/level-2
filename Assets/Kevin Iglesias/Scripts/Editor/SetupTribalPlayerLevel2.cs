using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SetupTribalPlayerLevel2
{
    private const string ScenePath = "Assets/Scenes/Stage2_Battlefield.unity";
    private const string ModelPath = "Assets/Stage2_Imported/Player/TribalWarrior.fbx";
    private const string ControllerPath = "Assets/Stage2_Imported/Player/TribalWarrior.controller";
    private const string SessionKey = "SetupTribalPlayerLevel2.MouseOrbitV10";

    static SetupTribalPlayerLevel2()
    {
        EditorApplication.delayCall += TrySetup;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += TrySetup;
        };
    }

    [MenuItem("Tools/Level 2/Setup Tribal Player")]
    private static void SetupFromMenu()
    {
        SessionState.SetBool(SessionKey, false);
        TrySetup();
    }

    public static void RunBatch()
    {
        try
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

            SessionState.SetBool(SessionKey, false);
            TrySetup();
            GameObject player = GameObject.Find("TribalPlayer");
            GameObject cameraObject = GameObject.Find("CinemachinePlayerCamera");
            Camera mainCamera = Camera.main;
            System.Type brainType = FindType(
                "Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine",
                "Cinemachine.CinemachineBrain, Cinemachine");
            System.Type cameraType = FindType(
                "Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine",
                "Cinemachine.CinemachineVirtualCamera, Cinemachine");

            bool playerReady = player != null &&
                               player.GetComponent<Animator>() != null &&
                               player.GetComponent<CharacterController>() != null &&
                               player.GetComponent<TribalPlayerController>() != null;
            bool cameraReady = mainCamera != null && brainType != null && cameraType != null &&
                               mainCamera.GetComponent(brainType) != null && cameraObject != null &&
                               cameraObject.GetComponent(cameraType) != null;
            IslandEnemyAI[] enemies = Object.FindObjectsByType<IslandEnemyAI>(FindObjectsSortMode.None);
            bool enemiesReady = enemies.Length == 4 && enemies.All(enemy =>
                enemy.GetComponent<CapsuleCollider>() != null &&
                enemy.GetComponent<EnemyHealthBar>() != null);
            bool terrainReady = GameObject.Find("TerrainVariation_Level2") != null;
            bool success = playerReady && cameraReady && enemiesReady && terrainReady;
            Debug.Log(success ? "Batch tribal player setup succeeded." : "Batch tribal player setup did not create all required objects.");
            EditorApplication.Exit(success ? 0 : 1);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void TrySetup()
    {
        if (SessionState.GetBool(SessionKey, false) || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogWarning("Open Stage2_Battlefield before running the tribal player setup.");
            return;
        }

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        }
        if (model == null)
            return;

        ConfigureModelImporter();
        OptimizePlayerTextures();
        model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        AnimatorController controller = EnsureAnimatorController();
        if (controller == null)
        {
            EditorApplication.delayCall += TrySetup;
            return;
        }
        ShapeTerrainOnce();
        GameObject player = EnsurePlayer(model, controller);
        ConfigureEnemies(player.transform);
        bool cameraReady = ConfigureCamera(player.transform);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        SessionState.SetBool(SessionKey, cameraReady);
        Debug.Log(cameraReady
            ? "Level 2 tribal player and Cinemachine camera setup complete."
            : "Level 2 tribal player setup complete; waiting for Cinemachine installation.");
    }

    private static void ConfigureModelImporter()
    {
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            return;

        bool changed = importer.animationType != ModelImporterAnimationType.Human ||
                       importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;
        if (!changed)
            return;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.SaveAndReimport();
    }

    private static void OptimizePlayerTextures()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Stage2_Imported/Player/TribalWarrior.fbm" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            bool changed = false;
            if (importer.maxTextureSize > 1024)
            {
                importer.maxTextureSize = 1024;
                changed = true;
            }
            if (path.Contains("Normal_Bake") && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                changed = true;
            }
            if (changed)
                importer.SaveAndReimport();
        }
    }

    private static AnimatorController EnsureAnimatorController()
    {
        AnimationClip idleClip = LoadAssetClip(
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@CombatIdle1H01.fbx",
            "HumanM@CombatIdle1H01");
        AnimationClip walkClip = LoadClip("Walking.fbx", "Walk");
        AnimationClip runClip = LoadAssetClip(
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx",
            "HumanM@Run01_Forward");
        AnimationClip attack1Clip = LoadAssetClip(
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx",
            "HumanM@Attack1H01_R");
        AnimationClip attack2Clip = LoadAssetClip(
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_L.fbx",
            "HumanM@Attack1H01_L");
        AnimationClip attack3Clip = LoadAssetClip(
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx",
            "HumanM@Attack2H01");
        AnimationClip deathClip = LoadAssetClip(
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx",
            "HumanM@Death01");
        if (idleClip == null || walkClip == null || runClip == null || attack1Clip == null ||
            attack2Clip == null || attack3Clip == null || deathClip == null)
            return null;

        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (existing != null)
        {
            // Keep the controller GUID shared by both level scenes.
            PlayerActionAnimatorSetup.Configure(existing);
            AssetDatabase.SaveAssets();
            return existing;
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack1", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack2", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack3", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = AddState(machine, "Idle", idleClip);
        AnimatorState walk = AddState(machine, "Walk", walkClip);
        AnimatorState run = AddState(machine, "Run", runClip);
        AnimatorState attack1 = AddState(machine, "Attack1", attack1Clip);
        AnimatorState attack2 = AddState(machine, "Attack2", attack2Clip);
        AnimatorState attack3 = AddState(machine, "Attack3", attack3Clip);
        attack1.speed = 1.25f;
        attack2.speed = 1.25f;
        attack3.speed = 1.2f;
        AnimatorState death = AddState(machine, "Death", deathClip);
        machine.defaultState = idle;

        AddFloatTransition(idle, walk, AnimatorConditionMode.Greater, 0.1f, "Speed");
        AddFloatTransition(walk, idle, AnimatorConditionMode.Less, 0.1f, "Speed");
        AddFloatTransition(walk, run, AnimatorConditionMode.Greater, 0.65f, "Speed");
        AddFloatTransition(run, walk, AnimatorConditionMode.Less, 0.65f, "Speed");
        AddTriggerTransition(machine, attack1, "Attack1", 0.04f);
        AddTriggerTransition(machine, attack2, "Attack2", 0.04f);
        AddTriggerTransition(machine, attack3, "Attack3", 0.04f);
        AddTriggerTransition(machine, death, "Die", 0.05f);

        AddAttackReturn(attack1, idle);
        AddAttackReturn(attack2, idle);
        AddAttackReturn(attack3, idle);

        PlayerActionAnimatorSetup.Configure(controller);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimationClip LoadClip(string fileName, string preferredName)
    {
        string path = "Assets/Stage2_Imported/Enemies/Animations/" + fileName;
        return LoadAssetClip(path, preferredName);
    }

    private static AnimationClip LoadAssetClip(string path, string preferredName)
    {
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToArray();
        return clips.FirstOrDefault(c => c.name == preferredName) ?? clips.FirstOrDefault();
    }

    private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion)
    {
        AnimatorState state = machine.AddState(name);
        state.motion = motion;
        return state;
    }

    private static void AddFloatTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float value, string parameter)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.12f;
        transition.AddCondition(mode, value, parameter);
    }

    private static void AddTriggerTransition(AnimatorStateMachine machine, AnimatorState state, string parameter, float duration)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(state);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    private static void AddAttackReturn(AnimatorState attack, AnimatorState idle)
    {
        AnimatorStateTransition transition = attack.AddTransition(idle);
        transition.hasExitTime = true;
        transition.exitTime = 0.88f;
        transition.duration = 0.1f;
    }

    private static GameObject EnsurePlayer(GameObject model, RuntimeAnimatorController controller)
    {
        GameObject player = GameObject.Find("TribalPlayer");
        if (player == null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(model);
            player.name = "TribalPlayer";
            Transform world = GameObject.Find("CompactIslandWorld")?.transform;
            if (world != null)
                player.transform.SetParent(world, true);
        }

        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        GameObject spawn = GameObject.Find("PlayerSpawn_PLACEHOLDER");
        Vector3 position = spawn != null ? spawn.transform.position : new Vector3(60f, 0f, 21f);
        position.y = terrain.transform.position.y + terrain.SampleHeight(position) + 0.05f;
        player.transform.position = position;
        player.transform.rotation = Quaternion.identity;
        player.tag = "Player";

        Animator animator = player.GetComponent<Animator>();
        if (animator == null)
            animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        CharacterController character = player.GetComponent<CharacterController>();
        if (character == null)
            character = player.AddComponent<CharacterController>();
        character.radius = 0.38f;
        character.height = 1.85f;
        character.center = new Vector3(0f, 0.93f, 0f);
        character.stepOffset = 0.35f;
        character.slopeLimit = 48f;

        if (player.GetComponent<TribalPlayerController>() == null)
            player.AddComponent<TribalPlayerController>();
        if (player.GetComponent<HumanoidSwayStabilizer>() == null)
            player.AddComponent<HumanoidSwayStabilizer>();
        PlayerSwordEquipper swordEquipper = player.GetComponent<PlayerSwordEquipper>();
        if (swordEquipper == null)
            swordEquipper = player.AddComponent<PlayerSwordEquipper>();
        swordEquipper.Equip();
        return player;
    }

    private static void ConfigureEnemies(Transform player)
    {
        foreach (IslandEnemyAI enemy in Object.FindObjectsByType<IslandEnemyAI>(FindObjectsSortMode.None))
        {
            enemy.SetTarget(player);
            CapsuleCollider collider = enemy.GetComponent<CapsuleCollider>();
            if (collider == null)
                collider = enemy.gameObject.AddComponent<CapsuleCollider>();
            collider.radius = 0.42f;
            collider.height = 1.8f;
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.isTrigger = false;
            if (enemy.GetComponent<HumanoidSwayStabilizer>() == null)
                enemy.gameObject.AddComponent<HumanoidSwayStabilizer>();
            EnemyHealthBar healthBar = enemy.GetComponent<EnemyHealthBar>();
            if (healthBar == null)
                healthBar = enemy.gameObject.AddComponent<EnemyHealthBar>();
            healthBar.Initialize(enemy);
            enemy.SnapToGround();
        }
    }

    private static bool ConfigureCamera(Transform player)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return false;

        System.Type brainType = FindType(
            "Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine",
            "Cinemachine.CinemachineBrain, Cinemachine");
        System.Type cameraType = FindType(
            "Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine",
            "Cinemachine.CinemachineVirtualCamera, Cinemachine");
        if (brainType == null || cameraType == null)
            return false;

        if (camera.GetComponent(brainType) == null)
            camera.gameObject.AddComponent(brainType);

        GameObject cameraObject = GameObject.Find("CinemachinePlayerCamera");
        if (cameraObject == null)
            return false;
        Component virtualCamera = cameraObject.GetComponent(cameraType);
        if (virtualCamera == null)
            return false;
        SetTransformProperty(virtualCamera, "Follow", player);
        SetTransformProperty(virtualCamera, "LookAt", player);

        CinemachinePlayerFollow binder = cameraObject.GetComponent<CinemachinePlayerFollow>();
        if (binder == null)
            binder = cameraObject.AddComponent<CinemachinePlayerFollow>();
        binder.Configure(player);
        if (cameraObject.GetComponent<CinemachineMouseOrbit>() == null)
            cameraObject.AddComponent<CinemachineMouseOrbit>();

        return true;
    }

    private static System.Type FindType(params string[] names)
    {
        foreach (string name in names)
        {
            System.Type type = System.Type.GetType(name);
            if (type != null)
                return type;
        }
        return null;
    }

    private static void SetTransformProperty(Component component, string propertyName, Transform value)
    {
        PropertyInfo property = component.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property != null && property.CanWrite)
            property.SetValue(component, value);
    }

    private static void SetMember(Component component, string memberName, object value)
    {
        PropertyInfo property = component.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null && property.CanWrite)
        {
            property.SetValue(component, value);
            return;
        }
        FieldInfo field = component.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(component, value);
    }

    private static void AddCinemachine2PipelineComponent(Component virtualCamera, string componentTypeName, System.Action<Component> configure)
    {
        System.Type componentType = FindType(componentTypeName);
        if (componentType == null)
            return;
        MethodInfo addMethod = virtualCamera.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(method => method.Name == "AddCinemachineComponent" && method.IsGenericMethodDefinition);
        if (addMethod == null)
            return;
        Component component = addMethod.MakeGenericMethod(componentType).Invoke(virtualCamera, null) as Component;
        if (component != null)
            configure(component);
    }

    private static void ShapeTerrainOnce()
    {
        if (GameObject.Find("TerrainVariation_Level2") != null)
            return;

        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        if (terrain == null)
            return;

        TerrainData data = terrain.terrainData;
        Transform world = GameObject.Find("CompactIslandWorld")?.transform;
        var offsets = new Dictionary<Transform, float>();
        if (world != null)
        {
            foreach (Transform child in world)
            {
                if (child.name.Contains("Hut") || child.name.StartsWith("IslandPalm_") || child.name.StartsWith("IslandEnemy_"))
                {
                    float ground = terrain.transform.position.y + terrain.SampleHeight(child.position);
                    offsets[child] = child.position.y - ground;
                }
            }
        }

        int resolution = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, resolution, resolution);
        Vector3[] centers =
        {
            new Vector3(20f, 1.15f, 37f), new Vector3(37f, 0.85f, 57f),
            new Vector3(82f, 1.05f, 49f), new Vector3(104f, 1.1f, 62f),
            new Vector3(27f, 0.85f, 94f), new Vector3(96f, 0.75f, 93f)
        };

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float worldX = x * data.size.x / (resolution - 1);
                float worldZ = z * data.size.z / (resolution - 1);
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(Mathf.Min(worldX, data.size.x - worldX), Mathf.Min(worldZ, data.size.z - worldZ)) / 14f));
                float lift = 0.18f * Mathf.Sin(worldX * 0.17f) * Mathf.Cos(worldZ * 0.14f) * edge;
                foreach (Vector3 center in centers)
                {
                    float dx = (worldX - center.x) / 8f;
                    float dz = (worldZ - center.z) / 7f;
                    lift += center.y * Mathf.Exp(-0.5f * (dx * dx + dz * dz));
                }
                heights[z, x] = Mathf.Clamp01(heights[z, x] + lift / data.size.y);
            }
        }
        data.SetHeights(0, 0, heights);
        EditorUtility.SetDirty(data);

        foreach (KeyValuePair<Transform, float> pair in offsets)
        {
            Vector3 position = pair.Key.position;
            position.y = terrain.transform.position.y + terrain.SampleHeight(position) + pair.Value;
            pair.Key.position = position;
        }

        GameObject marker = new GameObject("TerrainVariation_Level2");
        if (world != null)
            marker.transform.SetParent(world, false);
        marker.hideFlags = HideFlags.HideInHierarchy;

        NavMeshSurface surface = terrain.GetComponent<NavMeshSurface>();
        if (surface != null)
            surface.BuildNavMesh();
    }
}
