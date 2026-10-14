using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class ReviewPlayerControls
{
    private const string Pending="ReviewPlayerControls.Pending";
    private static TribalPlayerController player;
    private static IslandEnemyAI target,behind;
    private static IslandGameSession session;
    private static Animator animator;
    private static Vector3 start;
    private static int phase;
    private static float phaseTime,maxHeight,health,stamina;
    private static bool sawJump,sawHeavy;
    private static string sceneName,report;
    static ReviewPlayerControls()
    {EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=StateChanged;}

    [MenuItem("Tools/Island Game/Review Jump and Heavy Attack")]
    public static void Begin()
    {
        if(EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Save and exit Play mode first");
        Directory.CreateDirectory("VisualReview/Controls");
        if(File.Exists("VisualReview/Controls/Failure.txt"))File.Delete("VisualReview/Controls/Failure.txt");
        SessionState.SetString("ReviewPlayerControls.Original",EditorSceneManager.GetActiveScene().path);
        SessionState.SetInt("ReviewPlayerControls.Index",0);SessionState.SetBool("ReviewPlayerControls.Failed",false);
        SessionState.SetBool("SetupTribalPlayerLevel2.MouseOrbitV10",true);
        StartScene(0);
    }
    private static void StartScene(int index)
    {
        string name=index==0?"Stage2_Battlefield":"Stage3_AshenCitadel";
        EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
        SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    private static void StateChanged(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Pending,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            phase=0;phaseTime=Time.time;player=null;report="";sawJump=sawHeavy=false;
            Application.runInBackground=true;
            var view=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");EditorWindow.GetWindow(view).Focus();
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            if(SessionState.GetBool("ReviewPlayerControls.Failed",false)) {SessionState.SetBool(Pending,false);return;}
            int index=SessionState.GetInt("ReviewPlayerControls.Index",0)+1;SessionState.SetInt("ReviewPlayerControls.Index",index);
            if(index<2)EditorApplication.delayCall+=()=>StartScene(index);
            else
            {
                SessionState.SetBool(Pending,false);
                string original=SessionState.GetString("ReviewPlayerControls.Original","");
                if(!string.IsNullOrEmpty(original))EditorApplication.delayCall+=()=>EditorSceneManager.OpenScene(original);
                Debug.Log("PLAYER_CONTROLS_PASS: both levels validated");
            }
        }
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Pending,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling)return;
        try
        {
            float elapsed=Time.time-phaseTime;
            if(phase==0 && elapsed>.3f)
            {
                sceneName=EditorSceneManager.GetActiveScene().name;
                player=Object.FindFirstObjectByType<TribalPlayerController>();animator=player.GetComponent<Animator>();
                session=IslandGameSession.Instance;session.enabled=false;
                foreach(var enemy in Object.FindObjectsByType<IslandEnemyAI>(FindObjectsSortMode.None))
                {
                    enemy.enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;
                    if(target==null)target=enemy;else if(behind==null)behind=enemy;
                }
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Temporary action review floor";
                floor.transform.position=new Vector3(100,20,50);floor.transform.localScale=new Vector3(18,.4f,18);
                Physics.SyncTransforms();player.RespawnAt(new Vector3(100,21,50));player.transform.rotation=Quaternion.identity;
                start=player.transform.position;
                target.transform.position=start+Vector3.forward*2;target.ConfigureCombat(0,100,1000);
                behind.transform.position=start-Vector3.forward*3;behind.ConfigureCombat(0,100,1000);
                var extra=target.gameObject.AddComponent<BoxCollider>();extra.center=Vector3.up*.5f;extra.size=new Vector3(.5f,1,.5f);
                Physics.SyncTransforms();Next(1);
            }
            else if(phase==1 && elapsed>.25f)
            {
                Require(player.IsGrounded,"Player grounded before jump");start=player.transform.position;
                Require(player.TryJump(),"Space action accepts grounded jump");
                Require(!player.TryJump(),"Double jump rejected");Require(!player.TryHeavyAttack(),"Airborne heavy rejected");
                maxHeight=0;Next(2);
            }
            else if(phase==2)
            {
                maxHeight=Mathf.Max(maxHeight,player.transform.position.y-start.y);
                sawJump |= animator.GetCurrentAnimatorStateInfo(0).IsName("Jump");
                if(elapsed>.24f && elapsed<.34f)Capture("Jump",1.7f);
                if(elapsed>1.0f)
                {
                    Require(maxHeight>.85f && maxHeight<1.4f,"Jump height: "+maxHeight.ToString("F2")+"m");
                    Require(sawJump && player.IsGrounded,"Jump animation and landing complete");
                    Require(Mathf.Abs(player.transform.position.y-start.y)<.10f,"Landed back on the floor");
                    health=target.Health;stamina=player.Stamina;
                    Require(player.TryHeavyAttack(),"Heavy begins on the ground");
                    Require(Mathf.Abs(player.Stamina-(stamina-25))<.01f,"Heavy costs 25 stamina");
                    Require(!player.TryHeavyAttack() && !player.TryLightAttack() && !player.TryJump(),"Recovery cannot be interrupted by action spam");Next(3);
                }
            }
            else if(phase==3)
            {
                sawHeavy |= animator.GetCurrentAnimatorStateInfo(0).IsName("HeavyAttack");
                if(elapsed<.40f)Require(target.Health==health,"No early damage during heavy wind-up",false);
                if(elapsed>.22f && elapsed<.31f)Capture("HeavyWindup",1.05f);
                if(elapsed>.52f && elapsed<.61f)Capture("HeavyStrike",1.05f);
                if(elapsed>1.55f)
                {
                    Require(sawHeavy && !player.IsAttacking,"Heavy animation and recovery complete");
                    Require(Mathf.Abs(health-target.Health-70)<.01f,"Heavy hits once for 70 damage despite two colliders");
                    Require(behind.Health==1000,"Enemy behind player was not hit");
                    health=target.Health;Require(player.TryLightAttack(),"Light attack still available");Next(4);
                }
            }
            else if(phase==4 && elapsed>1.05f)
            {
                Require(Mathf.Abs(health-target.Health-35)<.01f,"Light attack deals 35 damage");
                session.TogglePause();
                Require(!player.TryJump()&&!player.TryHeavyAttack()&&!player.TryLightAttack(),"Paused gameplay blocks all actions");
                session.TogglePause();
                var serialized=new SerializedObject(player);serialized.FindProperty("heavyStaminaCost").floatValue=player.Stamina;serialized.ApplyModifiedPropertiesWithoutUndo();
                Require(player.TryHeavyAttack(),"Spend remaining stamina on an attack");
                serialized.Update();serialized.FindProperty("heavyStaminaCost").floatValue=25;serialized.ApplyModifiedPropertiesWithoutUndo();
                Next(6);
            }
            else if(phase==6 && elapsed>1.45f)
            {
                Require(player.Stamina<.01f,"Stamina exhausted after the recovery");
                Require(!player.TryHeavyAttack(),"Heavy rejected without stamina");
                Require(player.TryJump(),"Jump remains free when stamina is empty");
                player.TakeDamage(1000);Require(player.IsDead&&!player.TryJump()&&!player.TryHeavyAttack(),"Death blocks actions");
                player.RespawnAt(start);Next(5);
            }
            else if(phase==5 && elapsed>.25f)
            {
                Require(!player.IsDead&&player.IsGrounded&&!player.IsAttacking,"Respawn resets actions and landing");
                Require(player.TryHeavyAttack(),"Heavy works after respawn");
                File.WriteAllText("VisualReview/Controls/"+sceneName+".txt",report);
                phase=7;
                EditorApplication.isPlaying=false;
            }
            if(elapsed>12)throw new Exception("Review phase timed out: "+phase);
        }
        catch(Exception e)
        {
            SessionState.SetBool("ReviewPlayerControls.Failed",true);
            File.WriteAllText("VisualReview/Controls/Failure.txt",(sceneName??"startup")+" phase "+phase+"\n"+report+e);
            Debug.LogException(e);Time.timeScale=1;EditorApplication.isPlaying=false;
        }
    }
    private static void Next(int next){phase=next;phaseTime=Time.time;}
    private static void Require(bool condition,string message,bool record=true)
    {if(!condition)throw new Exception(message);if(record)report+="PASS: "+message+"\n";}
    private static void Capture(string label,float size)
    {
        string path="VisualReview/Controls/"+sceneName+"_"+label+".png";
        if(File.Exists(path))return;
        var go=new GameObject("Action Review Camera",typeof(Camera));var camera=go.GetComponent<Camera>();
        var targetTexture=RenderTexture.GetTemporary(1100,900,24);var old=RenderTexture.active;
        var pixels=new Texture2D(1100,900,TextureFormat.RGB24,false);
        var transforms=player.GetComponentsInChildren<Transform>();var layers=new int[transforms.Length];
        try
        {
            for(int i=0;i<transforms.Length;i++){layers[i]=transforms[i].gameObject.layer;transforms[i].gameObject.layer=31;}
            camera.cullingMask=1<<31;camera.orthographic=true;camera.orthographicSize=size;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.14f,.16f);
            camera.transform.position=start+new Vector3(1.7f,1.5f,2.5f);camera.transform.LookAt(start+Vector3.up*(label=="Jump"?1.1f:.55f));
            camera.targetTexture=targetTexture;camera.Render();RenderTexture.active=targetTexture;
            pixels.ReadPixels(new Rect(0,0,1100,900),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
        }
        finally
        {
            for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];
            camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(targetTexture);Object.DestroyImmediate(pixels);Object.DestroyImmediate(go);
        }
    }
}
