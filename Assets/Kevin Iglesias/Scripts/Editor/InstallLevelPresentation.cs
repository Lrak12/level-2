using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>Reproducible presentation pass. Never rebuilds terrain, actors, or gameplay.</summary>
public static class InstallLevelPresentation
{
    private const string Root = "Assets/ArtPolish";
    private static Transform decor;
    private static Terrain terrain;
    private static Material glow, mist, bronze, stone, basalt, rune;
    private static bool dark;

    [MenuItem("Tools/Island Game/Polish Both Levels")]
    public static void InstallBoth()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the open scene before applying presentation.");
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Meshes");
        Directory.CreateDirectory(Root + "/Profiles");
        AssetDatabase.Refresh();
        ImportAudio();
        Apply(false);
        Apply(true);
        AssetDatabase.SaveAssets();
        Debug.Log("Presentation installed in both gameplay scenes: music, SFX, atmosphere and gameplay feedback.");
    }

    private static void ImportAudio()
    {
        foreach (string path in Directory.GetFiles(Root + "/Audio", "*.wav"))
        {
            var importer = AssetImporter.GetAtPath(path.Replace('\\', '/')) as AudioImporter;
            if (importer == null) continue;
            bool longClip = path.Contains("Watch") || path.Contains("Procession") || path.Contains("Loop") || path.Contains("Surf") || path.Contains("Rumble");
            var settings = importer.defaultSampleSettings;
            settings.loadType = longClip ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = longClip ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            settings.quality = .85f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = !path.Contains("Watch") && !path.Contains("Procession") && !path.Contains("Surf") && !path.Contains("Rumble");
            importer.SaveAndReimport();
        }
    }

    private static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/" + name + ".wav");

    private static GameObject Go(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Level presentation");
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static Material Mat(string name, string shader, Color color, float emission = 0)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, path); }
        Undo.RecordObject(m, "Configure presentation material");
        m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .22f);
        if (emission > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * emission); }
        EditorUtility.SetDirty(m);
        return m;
    }

    private static void Apply(bool citadel)
    {
        dark = citadel;
        var scene = EditorSceneManager.OpenScene(citadel ? "Assets/Scenes/Stage3_AshenCitadel.unity" : "Assets/Scenes/Stage2_Battlefield.unity");
        Undo.SetCurrentGroupName("Polish " + scene.name);
        var previous = GameObject.Find("Level Presentation");
        if (previous != null) Undo.DestroyObjectImmediate(previous);
        var root = Go("Level Presentation"); decor = root.transform;
        terrain = Object.FindFirstObjectByType<Terrain>();
        foreach (var relic in Object.FindObjectsByType<Level3Relic>(FindObjectsSortMode.None))
            RemoveOwnedChildren(relic.transform, "Relic halo", "Relic orbit sparks");
        foreach (var pickup in Object.FindObjectsByType<Level3HealingPickup>(FindObjectsSortMode.None))
            RemoveOwnedChildren(pickup.transform, "Healing halo", "Healing wisps");
        glow = Mat("SoftGlow", "Island/Soft Particle", Color.white);
        mist = Mat("SoftMist", "Island/Soft Particle", Color.white); mist.SetFloat("_DstBlend", 10);
        bronze = Mat("WeatheredBronze", "Universal Render Pipeline/Lit", new Color(.48f,.3f,.12f)); bronze.SetFloat("_Metallic", .45f);
        stone = Mat("CoolAshStone", "Universal Render Pipeline/Lit", new Color(.38f,.42f,.47f));
        basalt = Mat("BlueObsidian", "Universal Render Pipeline/Lit", new Color(.16f,.2f,.26f));
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Free Island Collection/Environment/Terrain/Textures/Cliff_2/Cliff_2_Normal.png");
        foreach (var material in new[] { stone, basalt })
        {
            material.SetTexture("_BumpMap", normal); material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", .45f); material.SetTextureScale("_BaseMap", new Vector2(2,2));
        }
        rune = Mat("EmberInlay", "Universal Render Pipeline/Lit", new Color(1f,.4f,.065f), 2f);
        var player = Object.FindFirstObjectByType<TribalPlayerController>();
        var cycle = Object.FindFirstObjectByType<IslandRainCycle>();
        var sounds = new AudioClip[Enum.GetValues(typeof(LevelSound)).Length];
        foreach (LevelSound id in Enum.GetValues(typeof(LevelSound))) sounds[(int)id] = Clip(id.ToString());
        Undo.AddComponent<LevelPresentation>(root).Configure(dark, player, cycle,
            Clip(dark ? "Ashen_Procession" : "Coastal_Watch"), Clip(dark ? "Citadel_Rumble" : "Coastal_Surf"),
            Clip(dark ? "Citadel_Rumble" : "Rain_Loop"), sounds, glow);
        if (player.GetComponent<ImpactFlash>() == null) Undo.AddComponent<ImpactFlash>(player.gameObject);
        foreach (var enemy in Object.FindObjectsByType<IslandEnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (enemy.GetComponent<ImpactFlash>() == null) Undo.AddComponent<ImpactFlash>(enemy.gameObject);
        Lighting();
        Surface();
        Checkpoints();
        var dust = Particles(dark ? "Drifting embers" : "Airborne pollen", player.transform.position + Vector3.up*3,
            dark ? new Color(2f,.53f,.12f,.65f) : new Color(1f,.88f,.56f,.38f),
            dark ? 8 : 5, dark ? .045f : .035f, 4.5f, .22f, 11f, glow);
        var shape = dust.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(23,6,23);
        dust.gameObject.AddComponent<AtmosphereMotion>().Configure(AtmosphereMotion.Motion.Follow, player.transform);
        if (dark) Citadel(); else Coast();
        if (cycle != null && dark)
        {
            var ash = cycle.GetComponent<ParticleSystem>();
            Undo.RecordObject(ash, "Polish ash fall");
            var main = ash.main; main.startSize3D = false; main.startSize = new ParticleSystem.MinMaxCurve(.025f,.07f);
            main.startLifetime = 5f; main.maxParticles = 350;
            main.startColor = new Color(.5f,.52f,.55f,.35f);
            var em = ash.emission; em.rateOverTime = 55f;
            ash.GetComponent<ParticleSystemRenderer>().sharedMaterial = mist;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static T Effect<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet<T>(out var existing)) return existing;
        var effect = profile.Add<T>(true); AssetDatabase.AddObjectToAsset(effect, profile); return effect;
    }

    private static void Lighting()
    {
        var sun = GameObject.Find("Directional Light").GetComponent<Light>();
        Undo.RecordObject(sun, "Polish key light"); Undo.RecordObject(sun.transform, "Polish sun direction");
        sun.color = dark ? new Color(1f,.86f,.71f) : new Color(1f,.96f,.85f);
        sun.intensity = dark ? 1.65f : 1.8f;
        sun.transform.rotation = Quaternion.Euler(dark ? 34 : 48, dark ? -35 : -28, 0);
        sun.shadows = LightShadows.Soft; sun.shadowStrength = dark ? .8f : .74f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = dark ? new Color(.4f,.49f,.63f) : new Color(.58f,.72f,.86f);
        RenderSettings.ambientEquatorColor = dark ? new Color(.28f,.32f,.4f) : new Color(.46f,.56f,.58f);
        RenderSettings.ambientGroundColor = dark ? new Color(.17f,.12f,.1f) : new Color(.3f,.26f,.18f);
        RenderSettings.reflectionIntensity = dark ? .38f : .55f;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = dark ? .006f : .0028f;
        RenderSettings.fogColor = dark ? new Color(.25f,.29f,.36f) : new Color(.63f,.78f,.82f);
        if (dark)
        {
            var sky = Mat("CitadelAshSky", "Island/Ashen Sky", Color.white);
            RenderSettings.skybox = sky;
        }
        var camera = Camera.main;
        // Three orphaned legacy components also exist in BeforeQualityPass.
        // Their original serialized state is retained in ArtPolish_Backup.
        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(camera.gameObject) > 0)
        {
            Undo.RegisterCompleteObjectUndo(camera.gameObject, "Remove broken camera script references");
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(camera.gameObject);
        }
        Undo.RecordObject(camera, "Polish camera"); camera.allowHDR = true;
        var cameraData = camera.GetUniversalAdditionalCameraData(); Undo.RecordObject(cameraData, "Enable post processing");
        cameraData.renderPostProcessing = true; cameraData.volumeLayerMask = 1;
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = AntialiasingQuality.High;
        var volume = Object.FindFirstObjectByType<Volume>(); Undo.RecordObject(volume, "Set visual grade");
        string path = Root + "/Profiles/" + (dark ? "Citadel" : "Coast") + ".asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile,path); }
        volume.sharedProfile = profile; volume.isGlobal = true; volume.weight = 1; volume.enabled = true; volume.gameObject.layer = 0;
        Effect<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);
        var color = Effect<ColorAdjustments>(profile); color.postExposure.Override(dark ? .35f : .25f);
        color.contrast.Override(dark ? 8 : 7); color.saturation.Override(dark ? -4 : 10); color.colorFilter.Override(Color.white);
        var bloom = Effect<Bloom>(profile); bloom.threshold.Override(1.05f); bloom.intensity.Override(dark ? .42f : .18f); bloom.scatter.Override(.6f);
        var vignette = Effect<Vignette>(profile); vignette.intensity.Override(dark ? .17f : .11f); vignette.smoothness.Override(.45f);
        foreach (var effect in profile.components) EditorUtility.SetDirty(effect);
        EditorUtility.SetDirty(profile);
        DynamicGI.UpdateEnvironment();
    }

    private static void Surface()
    {
        string name = dark ? "LivingLava" : "CoastalWater";
        var mat = Mat(name,"Island/Living Surface",Color.white);
        mat.SetFloat("_Lava",dark ? 1 : 0); mat.SetFloat("_Scale",dark ? 1.05f : .6f);
        mat.SetColor("_DeepColor",dark ? new Color(.038f,.015f,.015f) : new Color(.012f,.16f,.23f));
        mat.SetColor("_LightColor",dark ? new Color(.16f,.055f,.025f) : new Color(.05f,.46f,.51f));
        mat.SetColor("_GlowColor",dark ? new Color(1f,.22f,.025f) : new Color(.6f,.9f,.9f));
        foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if (renderer.name == "Ocean" || renderer.name == "LavaSea" || renderer.name.StartsWith("LavaPool_"))
            { Undo.RecordObject(renderer,"Animate water and lava"); renderer.sharedMaterial = mat; }
    }

    private static float Ground(float x,float z) => terrain.transform.position.y + terrain.SampleHeight(new Vector3(x,0,z));

    private static void RemoveOwnedChildren(Transform owner, params string[] names)
    {
        foreach (string name in names)
        {
            Transform child;
            while ((child = owner.Find(name)) != null) Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    private static ParticleSystem Particles(string name, Vector3 position, Color color, float rate, float size, float life, float speed, float radius, Material material)
    {
        var go = Go(name,decor); go.transform.position = position; go.transform.rotation = Quaternion.Euler(-90,0,0);
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.playOnAwake = true; main.duration = 5;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life*.65f,life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed*.55f,speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size*.6f,size*1.3f);
        main.startColor = color; main.maxParticles = Mathf.Clamp(Mathf.CeilToInt(rate*life*1.4f),25,350);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission; emission.rateOverTime = rate;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.radius = radius; shape.angle = 9;
        var col = ps.colorOverLifetime; col.enabled = true;
        var gradient = new Gradient(); gradient.SetKeys(new[] {new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[] {new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(.5f,.6f),new GradientAlphaKey(0,1)}); col.color = gradient;
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,.15f));
        var noise = ps.noise; noise.enabled = true; noise.strength = .18f; noise.frequency = .5f; noise.scrollSpeed = .25f;
        var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        return ps;
    }

    private static void Fire(Vector3 position, float size)
    {
        Particles("Flame tongues",position,new Color(2.8f,.7f,.075f,.85f),18,.52f*size,.7f,1.6f*size,.18f*size,glow);
        Particles("Fire sparks",position,new Color(2f,.66f,.1f,.75f),4,.038f,1.8f,1.4f,.17f,glow);
        Particles("Rising smoke",position+Vector3.up*.65f,new Color(.24f,.26f,.29f,.2f),3,.8f*size,3f,.8f,.3f,mist);
        var go = Go("Firelight",decor); go.transform.position = position+Vector3.up*.35f;
        var light = go.AddComponent<Light>(); light.type = LightType.Point; light.range = 8; light.intensity = dark ? 4.2f : 2.2f;
        light.color = new Color(1,.48f,.15f); light.shadows = LightShadows.None;
        go.AddComponent<AtmosphereMotion>().Configure(AtmosphereMotion.Motion.Flicker,null,.23f);
        var audio = go.AddComponent<AudioSource>(); audio.clip = Clip("Fire_Loop"); audio.loop = true;
        audio.playOnAwake = true; audio.volume = .34f; audio.spatialBlend = 1; audio.minDistance = 2; audio.maxDistance = 13;
        audio.rolloffMode = AudioRolloffMode.Linear; audio.dopplerLevel = 0;
    }

    private static LineRenderer Circle(string name, Vector3 position, float radius, Color color, Transform parent = null)
    {
        var go = Go(name,parent != null ? parent : decor); go.transform.position = position;
        var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = glow; line.useWorldSpace = false;
        line.loop = true; line.positionCount = 64; line.widthMultiplier = .055f;
        line.startColor = line.endColor = color; line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
        for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}
        return line;
    }

    private static void Checkpoints()
    {
        foreach (var checkpoint in Object.FindObjectsByType<IslandCheckpoint>(FindObjectsSortMode.None))
        {
            var marker = checkpoint.transform.Find("Beacon");
            var m = Mat(dark ? "AmberBeacon" : "TealBeacon","Universal Render Pipeline/Lit",dark ? new Color(1,.55f,.13f) : new Color(.12f,.75f,.86f),2);
            if(marker!=null)
            {
                Undo.RecordObject(marker.GetComponent<Renderer>(),"Beacon glow"); marker.GetComponent<Renderer>().sharedMaterial=m;
                if (marker.GetComponent<AtmosphereMotion>()==null) Undo.AddComponent<AtmosphereMotion>(marker.gameObject).Configure(AtmosphereMotion.Motion.Float);
            }
            var color = dark ? new Color(1.8f,.67f,.18f,.65f) : new Color(.17f,.95f,1.3f,.55f);
            Circle("Checkpoint halo",checkpoint.transform.position+Vector3.up*.16f,.7f,color);
            Particles("Checkpoint motes",checkpoint.transform.position+Vector3.up*.18f,color,4,.065f,1.7f,.6f,.4f,glow);
        }
    }

    // Environment details are below; all meshes remain outside the collision / navigation setup.
    private static void Coast()
    {
        foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if(renderer.name.EndsWith("Fire_Coals")) Fire(renderer.bounds.center+Vector3.up*.15f,1);
        Grass();
    }

    private static void Citadel()
    {
        foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var m=renderer.sharedMaterial;
            if(m==null)continue;
            if(m.name=="ObsidianBasalt"||m.name=="AshStone"||m.name=="AncientBronze")
            {Undo.RecordObject(renderer,"Cool stone and bronze");renderer.sharedMaterial=m.name=="ObsidianBasalt"?basalt:m.name=="AshStone"?stone:bronze;}
            if(renderer.name=="BrazierFlame" || (renderer.name=="Flame" && renderer.transform.parent != null && renderer.transform.parent.name.StartsWith("Brazier_")))
            {
                Fire(renderer.bounds.center-Vector3.up*.25f,.85f);
                Undo.RecordObject(renderer,"Replace solid flame with particles");renderer.enabled=false;
                var lamp=renderer.GetComponent<Light>();if(lamp!=null){Undo.RecordObject(lamp,"Replace fire light");lamp.enabled=false;}
            }
            if(renderer.name.StartsWith("LavaPool_"))
            {
                var p=renderer.transform.position;
                Particles("Lava embers",p+Vector3.up*.15f,new Color(2,.55f,.1f,.75f),5,.055f,2.7f,.7f,renderer.bounds.extents.x*.72f,glow);
                Particles("Lava steam",p+Vector3.up*.2f,new Color(.34f,.37f,.4f,.12f),2,1.5f,4, .5f,renderer.bounds.extents.x*.65f,mist);
            }
        }
        foreach(var relic in Object.FindObjectsByType<Level3Relic>(FindObjectsSortMode.None))
        {
            var crystal=relic.transform.Find("Crystal");
            if(crystal!=null&&crystal.GetComponent<AtmosphereMotion>()==null)Undo.AddComponent<AtmosphereMotion>(crystal.gameObject).Configure(AtmosphereMotion.Motion.Float);
            var halo=Circle("Relic halo",relic.transform.position+Vector3.up*1.12f,1.05f,new Color(2,.75f,.12f,.75f),relic.transform);
            var ps=Particles("Relic orbit sparks",relic.transform.position+Vector3.up*1.2f,new Color(2,.85f,.2f,.8f),7,.07f,2.4f,.5f,.65f,glow);
            ps.transform.SetParent(relic.transform,true);
        }
        var heal=Object.FindFirstObjectByType<Level3HealingPickup>();
        if(heal!=null)
        {
            Circle("Healing halo",heal.transform.position+Vector3.up*.51f,.8f,new Color(.13f,1.6f,.7f,.7f),heal.transform);
            var ps=Particles("Healing wisps",heal.transform.position+Vector3.up*.6f,new Color(.25f,1.6f,.6f,.65f),6,.07f,2.2f,.55f,.55f,glow);
            ps.transform.SetParent(heal.transform,true);
        }
        for(int z=25;z<=96;z+=4)
        {
            for(int side=-1;side<=1;side+=2)
                Detail("Path bronze edging",new Vector3(60+side*3.75f,Ground(60,z)+.12f,z),new Vector3(.09f,.025f,2.9f),bronze);
            if(z%12==1)Detail("Path ember inset",new Vector3(60,Ground(60,z)+.13f,z),new Vector3(.36f,.025f,.36f),rune).transform.Rotate(0,45,0);
        }
        foreach(int z in new[]{34,66}) foreach(int side in new[]{-1,1})
        {
            float x=60+side*10;
            for(int k=0;k<3;k++)
                Detail("Column bronze collar",new Vector3(x,Ground(x,z)+.7f+k*2.15f,z),new Vector3(1.62f,.13f,1.62f),bronze,PrimitiveType.Cylinder);
            for(int k=0;k<3;k++)
                Detail("Ancient ember glyph",new Vector3(x,Ground(x,z)+2+k*.42f,z-.76f),new Vector3(.18f,.18f,.035f),rune).transform.Rotate(0,0,45);
        }
        // Recessed, layered backdrop breaks up the large flat temple wall.
        float y=Ground(60,96);
        Detail("Altar backdrop",new Vector3(60,y+4.1f,103.99f),new Vector3(9,5.9f,.14f),stone);
        Detail("Altar inner recess",new Vector3(60,y+4.1f,103.88f),new Vector3(7.8f,4.9f,.12f),basalt);
        var seal=Circle("Ember sun seal",new Vector3(60,y+4.4f,103.75f),1.55f,new Color(2,.55f,.1f,.8f));seal.transform.rotation=Quaternion.Euler(90,0,0);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;
            var gem=Detail("Seal point",new Vector3(60+Mathf.Cos(a)*2.1f,y+4.4f+Mathf.Sin(a)*2.1f,103.73f),new Vector3(.23f,.43f,.07f),rune);
            gem.transform.rotation=Quaternion.Euler(0,0,i*45);
        }
    }

    private static GameObject Detail(string name,Vector3 position,Vector3 scale,Material material,PrimitiveType type=PrimitiveType.Cube)
    {
        var go=GameObject.CreatePrimitive(type);Undo.RegisterCreatedObjectUndo(go,"Presentation detail");
        go.name=name;go.transform.SetParent(decor,true);go.transform.position=position;go.transform.localScale=scale;
        Undo.DestroyObjectImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;
        return go;
    }

    private static void Grass()
    {
        var random=new System.Random(3116);
        for(int batch=0;batch<3;batch++)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<240;i++)
            {
                float x=18+(float)random.NextDouble()*88,z=20+(float)random.NextDouble()*80;
                // Preserve the central combat route and village entrances.
                if(Mathf.Abs(x-60)<6||Mathf.PerlinNoise(x*.09f,z*.09f)<.47f)continue;
                float y=Ground(x,z);if(y<1.4f)continue;
                for(int tuft=0;tuft<7;tuft++)
                {
                float tx=x+((float)random.NextDouble()-.5f)*1.8f,tz=z+((float)random.NextDouble()-.5f)*1.8f;
                Vector3 center=new Vector3(tx,Ground(tx,tz)+.015f,tz);
                for(int leaf=0;leaf<7;leaf++)
                {
                    float a=(float)random.NextDouble()*Mathf.PI*2;
                    var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=new Vector3(-dir.z,0,dir.x);
                    float h=.16f+(float)random.NextDouble()*.27f,w=.017f+(float)random.NextDouble()*.018f;
                    int n=vertices.Count;
                    vertices.Add(center-side*w);vertices.Add(center+side*w);
                    vertices.Add(center+Vector3.up*h*.62f+dir*h*.24f-side*w*.5f);
                    vertices.Add(center+Vector3.up*h*.62f+dir*h*.24f+side*w*.5f);
                    vertices.Add(center+Vector3.up*h+dir*h*.65f);
                    triangles.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3,n+2,n+4,n+3});
                }
                }
            }
            var mesh=new Mesh{name="CoastalGrass_"+batch};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Root+"/Meshes/CoastalGrass_"+batch+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}
            var go=Go("Coastal understory "+batch,decor);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var material=Mat("Grass_"+batch,"Universal Render Pipeline/Lit",batch==0?new Color(.25f,.38f,.1f):batch==1?new Color(.38f,.45f,.15f):new Color(.42f,.4f,.19f));
            material.SetFloat("_Cull",0);material.SetFloat("_Smoothness",.04f);
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
    }
}
