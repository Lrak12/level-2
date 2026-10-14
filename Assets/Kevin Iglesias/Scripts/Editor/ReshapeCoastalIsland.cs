using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReshapeCoastalIsland
{
    private const string ScenePath = "Assets/Scenes/Stage2_Battlefield.unity";
    private const string OriginalTerrainPath = "Assets/Stage2_Imported/CompactIslandTerrain.asset";
    private const string CoastalTerrainPath = "Assets/Stage2_Imported/CoastalIslandTerrain.asset";
    private const string CoastalSandPath = "Assets/Stage2_Imported/CoastalIslandSand.terrainlayer";
    private const string CoastalOceanPath = "Assets/Stage2_Imported/CoastalIsland_Ocean.mat";

    private static readonly Vector3 OriginalTerrainPosition = new Vector3(0f, -1.5f, 0f);
    private static readonly Vector3 CoastalTerrainPosition = new Vector3(-95f, -1.5f, -10f);
    private static readonly Vector3 CoastalTerrainSize = new Vector3(230f, 52f, 140f);

    [MenuItem("Tools/Level 2/Reshape Coastal Island")]
    public static void Run()
    {
        if (SceneManager.GetActiveScene().path != ScenePath || EditorApplication.isPlaying)
        {
            Debug.LogError("Open Stage2_Battlefield in Edit mode before reshaping its island.");
            return;
        }

        Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
        TerrainData original = AssetDatabase.LoadAssetAtPath<TerrainData>(OriginalTerrainPath);
        if (terrain == null || original == null)
        {
            Debug.LogError("The battlefield terrain or its original TerrainData is missing.");
            return;
        }

        Dictionary<Transform, float> groundOffsets = CaptureGroundOffsets(terrain);
        float[,] originalHeights = original.GetHeights(0, 0, original.heightmapResolution, original.heightmapResolution);
        TerrainData coastal = AssetDatabase.LoadAssetAtPath<TerrainData>(CoastalTerrainPath);
        if (coastal == null)
        {
            coastal = UnityEngine.Object.Instantiate(original);
            coastal.name = "CoastalIslandTerrain";
            AssetDatabase.CreateAsset(coastal, CoastalTerrainPath);
        }

        coastal.size = CoastalTerrainSize;
        SetCoastalSandLayer(coastal, original);
        int heightResolution = coastal.heightmapResolution;
        float[,] heights = new float[heightResolution, heightResolution];
        for (int z = 0; z < heightResolution; z++)
        {
            float worldZ = CoastalTerrainPosition.z + z * CoastalTerrainSize.z / (heightResolution - 1);
            for (int x = 0; x < heightResolution; x++)
            {
                float worldX = CoastalTerrainPosition.x + x * CoastalTerrainSize.x / (heightResolution - 1);
                float worldHeight = HeightAt(worldX, worldZ, original, originalHeights);
                heights[z, x] = Mathf.Clamp01((worldHeight - CoastalTerrainPosition.y) / CoastalTerrainSize.y);
            }
        }
        coastal.SetHeights(0, 0, heights);

        int alphaResolution = coastal.alphamapResolution;
        int layerCount = coastal.terrainLayers.Length;
        float[,,] splats = new float[alphaResolution, alphaResolution, layerCount];
        for (int z = 0; z < alphaResolution; z++)
        {
            float worldZ = CoastalTerrainPosition.z + z * CoastalTerrainSize.z / (alphaResolution - 1);
            int hz = Mathf.RoundToInt((float)z * (heightResolution - 1) / (alphaResolution - 1));
            for (int x = 0; x < alphaResolution; x++)
            {
                float worldX = CoastalTerrainPosition.x + x * CoastalTerrainSize.x / (alphaResolution - 1);
                int hx = Mathf.RoundToInt((float)x * (heightResolution - 1) / (alphaResolution - 1));
                float worldHeight = CoastalTerrainPosition.y + heights[hz, hx] * CoastalTerrainSize.y;
                float dx = (heights[hz, Mathf.Min(hx + 1, heightResolution - 1)]
                    - heights[hz, Mathf.Max(hx - 1, 0)]) * CoastalTerrainSize.y
                    / (2f * CoastalTerrainSize.x / (heightResolution - 1));
                float dz = (heights[Mathf.Min(hz + 1, heightResolution - 1), hx]
                    - heights[Mathf.Max(hz - 1, 0), hx]) * CoastalTerrainSize.y
                    / (2f * CoastalTerrainSize.z / (heightResolution - 1));
                float slope = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;

                float rock = Mathf.Max(Smooth(10f, 17f, worldHeight), Smooth(24f, 47f, slope));
                float sand = Mathf.Lerp(1f, 0.42f, Smooth(2f, 6f, worldHeight)) * (1f - rock);
                float grass = (1f - rock) - sand;
                float path = PathWeight(worldX, worldZ) * (1f - rock) * Smooth(2.5f, 4.5f, worldHeight);
                sand *= 1f - path;
                grass *= 1f - path;

                splats[z, x, 0] = sand;
                splats[z, x, 3] = path;
                splats[z, x, 4] = grass * 0.7f;
                splats[z, x, 5] = grass * 0.3f;
                splats[z, x, 6] = rock * 0.65f;
                splats[z, x, 7] = rock * 0.35f;
            }
        }
        coastal.SetAlphamaps(0, 0, splats);

        Undo.RecordObject(terrain, "Reshape coastal island");
        terrain.terrainData = coastal;
        Undo.RecordObject(terrain.transform, "Move coastal island terrain");
        terrain.transform.position = CoastalTerrainPosition;
        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        if (collider != null)
        {
            Undo.RecordObject(collider, "Reshape coastal island collider");
            collider.terrainData = coastal;
        }

        foreach (KeyValuePair<Transform, float> entry in groundOffsets)
        {
            if (entry.Key == null)
                continue;
            Undo.RecordObject(entry.Key, "Place object on coastal island");
            Vector3 position = entry.Key.position;
            position.y = CoastalTerrainPosition.y + terrain.SampleHeight(position) + entry.Value;
            entry.Key.position = position;
        }

        SetCoastalOceanColor();
        SetCoastalDaylight();
        NavMeshSurface navMesh = terrain.GetComponent<NavMeshSurface>();
        if (navMesh != null)
            navMesh.BuildNavMesh();

        EditorUtility.SetDirty(coastal);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Coastal island reshaped. The original terrain asset and pre-reshape scene backup remain available.");
    }

    private static float HeightAt(float x, float z, TerrainData original, float[,] originalHeights)
    {
        float angle = Mathf.Atan2(z - 60f, x - 20f);
        float shoreline = 1f + 0.035f * Mathf.Sin(5f * angle) + 0.02f * Mathf.Cos(9f * angle);
        float nx = (x - 20f) / 110f;
        float nz = (z - 60f) / 60f;
        float distance = Mathf.Sqrt(nx * nx + nz * nz) / shoreline;
        float land = Smooth(0f, 1f, (1.04f - distance) / 0.23f);
        float baseHeight = Mathf.Lerp(-1.5f, 5.5f, land);
        baseHeight += land * 0.28f * (Mathf.PerlinNoise(x * 0.075f + 10f, z * 0.075f + 10f) - 0.5f);

        float westPeak = Gaussian(x, z, -52f, 62f, 22f, 24f);
        float northPeak = Gaussian(x, z, -18f, 101f, 19f, 20f);
        float crags = 3.5f * (Mathf.PerlinNoise(x * 0.19f + 42f, z * 0.19f + 42f) - 0.5f);
        float mountains = (westPeak * (31f + crags) + northPeak * (35f + crags))
            * Smooth(0f, 1f, (1.02f - distance) / 0.2f);
        float generated = baseHeight + mountains;

        float oldX = (x - OriginalTerrainPosition.x) / original.size.x;
        float oldZ = (z - OriginalTerrainPosition.z) / original.size.z;
        if (oldX < 0f || oldX > 1f || oldZ < 0f || oldZ > 1f)
            return generated;

        float originalHeight = OriginalTerrainPosition.y
            + Bilinear(originalHeights, oldX, oldZ) * original.size.y;
        float preserve = Smooth(10f, 35f, x)
            * Smooth(4f, 20f, z) * (1f - Smooth(108f, 120f, z))
            * Smooth(1.5f, 4.5f, originalHeight);
        return Mathf.Lerp(generated, originalHeight, preserve);
    }

    private static float PathWeight(float x, float z)
    {
        float main = Mathf.Min(
            SegmentDistance(x, z, 60f, 18f, 58f, 40f),
            Mathf.Min(SegmentDistance(x, z, 58f, 40f, 62f, 62f),
                SegmentDistance(x, z, 62f, 62f, 60f, 102f)));
        float east = Mathf.Min(SegmentDistance(x, z, 61f, 60f, 89f, 72f),
            SegmentDistance(x, z, 89f, 72f, 96f, 81f));
        float west = SegmentDistance(x, z, 59f, 57f, 31f, 75f);
        return Mathf.Max(1f - Smooth(2.3f, 5.2f, main),
            Mathf.Max(0.8f * (1f - Smooth(1.8f, 4.3f, east)),
                0.8f * (1f - Smooth(1.8f, 4.3f, west))));
    }

    private static Dictionary<Transform, float> CaptureGroundOffsets(Terrain terrain)
    {
        var offsets = new Dictionary<Transform, float>();
        Transform world = GameObject.Find("CompactIslandWorld")?.transform;
        if (world != null)
        {
            foreach (Transform child in world)
            {
                if (child == terrain.transform || child.name == "Ocean" || child.name == "TerrainVariation_Level2")
                    continue;
                if (child.name == "VillageDetails")
                {
                    foreach (Transform detail in child)
                        Capture(detail, terrain, offsets);
                }
                else
                    Capture(child, terrain, offsets);
            }
        }

        foreach (string name in new[]
        {
            "PlayerSpawn_PLACEHOLDER", "CombatArea_01", "EnemySpawn_01_PLACEHOLDER",
            "EnemySpawn_02_PLACEHOLDER", "ChokePoint", "CombatArea_02",
            "EnemySpawn_03_PLACEHOLDER", "EnemySpawn_04_PLACEHOLDER",
            "ExitToStage3_PLACEHOLDER", "VillageSquare", "Stage2_Battlefield_Layout"
        })
        {
            GameObject marker = GameObject.Find(name);
            if (marker != null)
                Capture(marker.transform, terrain, offsets);
        }
        return offsets;
    }

    private static void Capture(Transform target, Terrain terrain, Dictionary<Transform, float> offsets)
    {
        float ground = terrain.transform.position.y + terrain.SampleHeight(target.position);
        offsets[target] = target.position.y - ground;
    }

    private static void SetCoastalOceanColor()
    {
        Renderer ocean = GameObject.Find("Ocean")?.GetComponent<Renderer>();
        if (ocean == null || ocean.sharedMaterial == null)
            return;

        Material material = AssetDatabase.LoadAssetAtPath<Material>(CoastalOceanPath);
        if (material == null)
        {
            material = new Material(ocean.sharedMaterial) { name = "CoastalIsland_Ocean" };
            AssetDatabase.CreateAsset(material, CoastalOceanPath);
        }
        material.SetColor("_BaseColor", new Color(0.25f, 0.53f, 0.57f, 1f));
        material.SetFloat("_Smoothness", 0.78f);
        EditorUtility.SetDirty(material);
        Undo.RecordObject(ocean, "Tint coastal ocean");
        ocean.sharedMaterial = material;
    }

    private static void SetCoastalSandLayer(TerrainData coastal, TerrainData original)
    {
        TerrainLayer sand = AssetDatabase.LoadAssetAtPath<TerrainLayer>(CoastalSandPath);
        if (sand == null)
        {
            sand = UnityEngine.Object.Instantiate(original.terrainLayers[0]);
            sand.name = "CoastalIslandSand";
            AssetDatabase.CreateAsset(sand, CoastalSandPath);
        }
        sand.diffuseRemapMax = new Vector4(1.35f, 1.35f, 1.28f, 1f);
        EditorUtility.SetDirty(sand);
        TerrainLayer[] layers = coastal.terrainLayers;
        layers[0] = sand;
        coastal.terrainLayers = layers;
    }

    private static void SetCoastalDaylight()
    {
        Light light = GameObject.Find("Directional Light")?.GetComponent<Light>();
        if (light != null)
        {
            Undo.RecordObject(light, "Set coastal daylight");
            Undo.RecordObject(light.transform, "Set coastal daylight direction");
            light.color = new Color(0.95f, 0.98f, 1f, 1f);
            light.intensity = 1.45f;
            light.transform.rotation = Quaternion.Euler(62f, 126f, 0f);
        }
        RenderSettings.fogColor = new Color(0.62f, 0.75f, 0.76f, 1f);
        RenderSettings.ambientIntensity = 1.2f;
    }

    private static float Gaussian(float x, float z, float cx, float cz, float sx, float sz)
    {
        float dx = (x - cx) / sx;
        float dz = (z - cz) / sz;
        return Mathf.Exp(-0.5f * (dx * dx + dz * dz));
    }

    private static float Bilinear(float[,] heights, float x, float z)
    {
        int last = heights.GetLength(0) - 1;
        float fx = x * last;
        float fz = z * last;
        int x0 = Mathf.FloorToInt(fx);
        int z0 = Mathf.FloorToInt(fz);
        int x1 = Mathf.Min(x0 + 1, last);
        int z1 = Mathf.Min(z0 + 1, last);
        return Mathf.Lerp(
            Mathf.Lerp(heights[z0, x0], heights[z0, x1], fx - x0),
            Mathf.Lerp(heights[z1, x0], heights[z1, x1], fx - x0), fz - z0);
    }

    private static float SegmentDistance(float x, float z, float ax, float az, float bx, float bz)
    {
        Vector2 a = new Vector2(ax, az);
        Vector2 b = new Vector2(bx, bz);
        Vector2 p = new Vector2(x, z);
        Vector2 segment = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, segment) / segment.sqrMagnitude);
        return Vector2.Distance(p, a + segment * t);
    }

    private static float Smooth(float min, float max, float value)
    {
        float t = Mathf.Clamp01((value - min) / (max - min));
        return t * t * (3f - 2f * t);
    }
}
