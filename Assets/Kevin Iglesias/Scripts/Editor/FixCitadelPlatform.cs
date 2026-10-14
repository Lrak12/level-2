using System;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class FixCitadelPlatform
{
    [MenuItem("Tools/Island Game/Fix Citadel Platform")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Stage3_AshenCitadel.unity") throw new InvalidOperationException("Open Ashen Citadel first.");
        var terrain = Terrain.activeTerrain;
        var platform = GameObject.Find("TempleDais");
        var bounds = platform.GetComponent<BoxCollider>().bounds;
        float top = bounds.max.y;
        float bottom = top;
        for (int x=0;x<=6;x++) for(int z=0;z<=6;z++)
        {
            Vector3 p = new Vector3(Mathf.Lerp(bounds.min.x,bounds.max.x,x/6f),0,Mathf.Lerp(bounds.min.z,bounds.max.z,z/6f));
            bottom = Mathf.Min(bottom, terrain.SampleHeight(p)+terrain.transform.position.y-.12f);
        }
        Undo.RecordObject(platform.transform,"Ground platform foundation");
        var pos=platform.transform.position;pos.y=(top+bottom)*.5f;platform.transform.position=pos;
        var scale=platform.transform.localScale;scale.y=top-bottom;platform.transform.localScale=scale;

        var ramp=GameObject.Find("CitadelApproachRamp");
        if(ramp==null)
        {
            ramp=new GameObject("CitadelApproachRamp",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
            Undo.RegisterCreatedObjectUndo(ramp,"Platform approach ramp");
            ramp.transform.SetParent(platform.transform.parent,true);
        }
        float front=82f,back=bounds.min.z+.45f;
        float start=terrain.SampleHeight(new Vector3(60,0,front))+terrain.transform.position.y+.08f;
        float rampBottom=Mathf.Min(bottom,start-.15f);
        var mesh=new Mesh {name="CitadelApproachRamp"};
        mesh.vertices=new[]{new Vector3(54.5f,start,front),new Vector3(65.5f,start,front),new Vector3(54.5f,top,back),new Vector3(65.5f,top,back),
            new Vector3(54.5f,rampBottom,front),new Vector3(65.5f,rampBottom,front),new Vector3(54.5f,rampBottom,back),new Vector3(65.5f,rampBottom,back)};
        mesh.triangles=new[]{0,2,1,1,2,3, 4,5,6,5,7,6, 0,1,4,1,5,4, 2,6,3,3,6,7, 0,4,2,2,4,6, 1,3,5,3,7,5};
        mesh.uv=new[]{new Vector2(0,0),new Vector2(3,0),new Vector2(0,1),new Vector2(3,1),Vector2.zero,Vector2.right,Vector2.up,Vector2.one};
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        string meshPath="Assets/Stage3/CitadelApproachRamp.asset";
        var oldMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(oldMesh==null)AssetDatabase.CreateAsset(mesh,meshPath);
        else{EditorUtility.CopySerialized(mesh,oldMesh);Object.DestroyImmediate(mesh);mesh=oldMesh;}
        ramp.GetComponent<MeshFilter>().sharedMesh=mesh;
        ramp.GetComponent<MeshCollider>().sharedMesh=mesh;
        ramp.GetComponent<MeshRenderer>().sharedMaterial=platform.GetComponent<Renderer>().sharedMaterial;

        var player=Object.FindFirstObjectByType<TribalPlayerController>();
        var modifier=player.GetComponent<NavMeshModifier>() ?? Undo.AddComponent<NavMeshModifier>(player.gameObject);
        Undo.RecordObject(modifier,"Exclude moving player from navigation bake");modifier.ignoreFromBuild=true;
        var ocean=GameObject.Find("LavaSea");
        if(ocean!=null)
        {
            var oceanModifier=ocean.GetComponent<NavMeshModifier>() ?? Undo.AddComponent<NavMeshModifier>(ocean);
            oceanModifier.ignoreFromBuild=true;
        }
        var surface=terrain.GetComponent<NavMeshSurface>();
        Undo.RecordObject(surface,"Bake platform and architecture into navigation");
        surface.collectObjects=CollectObjects.Volume;
        surface.center=terrain.transform.InverseTransformPoint(new Vector3(60,16,64));
        surface.size=new Vector3(132,60,136);
        surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
        surface.ignoreNavMeshAgent=true;surface.ignoreNavMeshObstacle=true;
        surface.overrideVoxelSize=true;surface.voxelSize=.08f;surface.buildHeightMesh=true;
        Physics.SyncTransforms();
        surface.BuildNavMesh();
        var data=surface.navMeshData;
        string navPath="Assets/Stage3/CitadelPlatformNavigation.asset";
        var oldData=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
        if(oldData==null)AssetDatabase.CreateAsset(data,navPath);
        else
        {
            surface.RemoveData();EditorUtility.CopySerialized(data,oldData);surface.navMeshData=oldData;surface.AddData();
        }
        foreach(var enemy in Object.FindObjectsByType<IslandEnemyAI>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            Undo.RecordObject(enemy.transform,"Ground actor on navigable surface");
            var agent=enemy.GetComponent<NavMeshAgent>();Undo.RecordObject(agent,"Correct scaled foot offset");
            var animator=enemy.GetComponent<Animator>();
            var foot=animator.GetBoneTransform(HumanBodyBones.LeftToes) ?? animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            float offset=enemy.transform.InverseTransformPoint(foot.position).y;
            agent.baseOffset=.025f/Mathf.Max(.001f,enemy.transform.lossyScale.y)-offset;
            Vector3 position=enemy.transform.position;
            if(NavMesh.SamplePosition(position,out var hit,4,agent.areaMask))
            {position.y=hit.position.y+agent.baseOffset*enemy.transform.lossyScale.y;enemy.transform.position=position;}
        }
        player.GetComponent<PlayerSwordEquipper>().Equip();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Verify();
    }

    public static void Verify()
    {
        float top=GameObject.Find("TempleDais").GetComponent<BoxCollider>().bounds.max.y;
        float maxError=0;
        foreach(float x in new[]{48f,60f,72f})foreach(float z in new[]{88f,94f,99f})
        {
            if(!NavMesh.SamplePosition(new Vector3(x,top,z),out var hit,.3f,NavMesh.AllAreas))throw new Exception("No platform navigation at "+x+","+z);
            maxError=Mathf.Max(maxError,Mathf.Abs(hit.position.y-top));
        }
        if(maxError>.08f)throw new Exception("Navigation sits below platform by "+maxError);
        NavMesh.SamplePosition(new Vector3(60,7,79),out var start,2,NavMesh.AllAreas);
        NavMesh.SamplePosition(new Vector3(60,top,97),out var end,2,NavMesh.AllAreas);
        var path=new NavMeshPath();
        bool connected=NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        if(!connected)throw new Exception("Platform approach is disconnected");
        Directory.CreateDirectory("VisualReview/PlatformSword");
        File.WriteAllText("VisualReview/PlatformSword/PlatformValidation.txt","PASS: 9 platform samples; maximum navmesh height error "+maxError.ToString("F4")+"m; complete path from courtyard across approach ramp to boss platform.\n");
        Debug.Log("Platform verified: navmesh height error="+maxError+"; approach route complete.");
    }
}
