using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Adds grounded palm groves to the Stage 2 coastline without blocking combat routes.</summary>
public static class PopulateCoastalField
{
    private const string PalmFolder = "Assets/Yughues Free Palm Trees/Prefabs/";
    private const string ScenePath = "Assets/Scenes/Stage2_Battlefield.unity";

    private struct Grove
    {
        public readonly string Name;
        public readonly Rect Area;
        public readonly int Count;

        public Grove(string name, float x, float z, float width, float depth, int count)
        {
            Name = name;
            Area = new Rect(x, z, width, depth);
            Count = count;
        }
    }

    [MenuItem("Tools/Stage 2/Populate Coastal Field")]
    public static void Populate()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException("Open Stage2_Battlefield before populating its field.");

        var world = GameObject.Find("CompactIslandWorld");
        var terrain = world != null ? world.GetComponentInChildren<Terrain>() : null;
        if (world == null || terrain == null)
            throw new InvalidOperationException("The coastal island and its terrain must exist.");

        if (world.transform.Find("CoastalPalmGroves") != null)
        {
            Debug.Log("Coastal palm groves already exist; skipping duplicate placement.");
            return;
        }

        var prefabs = new[]
        {
            LoadPalm("PalmTreeSingleStraightTwoSided.prefab"),
            LoadPalm("PalmTreeSingleBendedTwoSided.prefab"),
            LoadPalm("PalmTreeDualBendedTwoSided.prefab"),
            LoadPalm("PalmTreeTrioTwoSided.prefab")
        };

        const string backup = "Assets/Scenes/Stage2_Battlefield_BeforePalmGroves.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(backup) == null)
            EditorSceneManager.SaveScene(scene, backup, true);

        var group = new GameObject("CoastalPalmGroves");
        Undo.RegisterCreatedObjectUndo(group, "Add coastal palm groves");
        group.transform.SetParent(world.transform, false);

        var occupied = new List<Vector2>();
        foreach (Transform child in world.transform)
            if (child.name.StartsWith("IslandPalm_", StringComparison.Ordinal))
                occupied.Add(new Vector2(child.position.x, child.position.z));

        var reserved = new List<Vector2>();
        foreach (Transform child in world.transform)
            if (child.name.Contains("Hut_") || child.name.StartsWith("IslandEnemy_", StringComparison.Ordinal) || child.name == "TribalPlayer")
                reserved.Add(new Vector2(child.position.x, child.position.z));

        var groves = new[]
        {
            new Grove("Western mountain foot", -66, 22, 69, 67, 9),
            new Grove("Southwest dunes", -20, 12, 66, 44, 8),
            new Grove("Southern shoreline", 18, 8, 94, 28, 10),
            new Grove("Eastern shoreline", 91, 27, 29, 76, 8),
            new Grove("Northern ridge", -10, 88, 106, 29, 7)
        };

        var rng = new System.Random(20260929);
        int total = 0;
        foreach (var grove in groves)
        {
            int placed = 0;
            for (int attempt = 0; attempt < 900 && placed < grove.Count; attempt++)
            {
                float x = grove.Area.xMin + (float)rng.NextDouble() * grove.Area.width;
                float z = grove.Area.yMin + (float)rng.NextDouble() * grove.Area.height;
                var point = new Vector2(x, z);
                float nx = (x - terrain.transform.position.x) / terrain.terrainData.size.x;
                float nz = (z - terrain.transform.position.z) / terrain.terrainData.size.z;
                if (nx < 0 || nx > 1 || nz < 0 || nz > 1)
                    continue;

                float ground = terrain.terrainData.GetInterpolatedHeight(nx, nz) + terrain.transform.position.y;
                float steepness = terrain.terrainData.GetSteepness(nx, nz);
                if (ground < 1.4f || ground > 12f || steepness > 21f)
                    continue;
                if (TooClose(point, occupied, 7.5f) || TooClose(point, reserved, 10f) || OnRoute(point))
                    continue;

                // Most groves use singles; occasional doubles and trios break up the silhouette.
                int variant = total % 13 == 5 ? 2 : total % 17 == 11 ? 3 : total % 3 == 0 ? 1 : 0;
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[variant], scene);
                Undo.RegisterCreatedObjectUndo(tree, "Add coastal palm");
                tree.name = string.Format("CoastalPalm_{0:00}", total + 1);
                tree.transform.SetParent(group.transform, false);
                tree.transform.SetPositionAndRotation(new Vector3(x, 0, z), Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0));
                float scale = (0.43f + (float)rng.NextDouble() * 0.13f) * (variant >= 2 ? 0.88f : 1f);
                tree.transform.localScale = Vector3.one * scale;

                float lowest = float.PositiveInfinity;
                foreach (var renderer in tree.GetComponentsInChildren<Renderer>())
                    lowest = Mathf.Min(lowest, renderer.bounds.min.y);
                if (float.IsInfinity(lowest))
                    throw new InvalidOperationException("Palm prefab has no renderer: " + prefabs[variant].name);
                tree.transform.position += Vector3.up * (ground - lowest - 0.03f);

                occupied.Add(point);
                placed++;
                total++;
            }
            Debug.Log(string.Format("{0}: added {1}/{2} palms", grove.Name, placed, grove.Count));
        }

        var navSurface = terrain.GetComponent<NavMeshSurface>();
        if (navSurface != null)
            navSurface.BuildNavMesh();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Coastal field populated with " + total + " additional palms.");
    }

    private static GameObject LoadPalm(string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PalmFolder + name);
        if (prefab == null)
            throw new InvalidOperationException("Missing palm prefab: " + name);
        return prefab;
    }

    private static bool TooClose(Vector2 point, List<Vector2> positions, float distance)
    {
        float squared = distance * distance;
        foreach (var position in positions)
            if ((point - position).sqrMagnitude < squared)
                return true;
        return false;
    }

    private static bool OnRoute(Vector2 point)
    {
        return NearSegment(point, new Vector2(60, 17), new Vector2(59, 60), 6f)
            || NearSegment(point, new Vector2(59, 60), new Vector2(62, 101), 5f)
            || NearSegment(point, new Vector2(59, 57), new Vector2(27, 76), 5f)
            || NearSegment(point, new Vector2(59, 57), new Vector2(98, 82), 5f);
    }

    private static bool NearSegment(Vector2 point, Vector2 a, Vector2 b, float width)
    {
        Vector2 delta = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude);
        return (point - (a + delta * t)).sqrMagnitude < width * width;
    }
}
