using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PlayerActionAnimatorSetup
{
    public const string ControllerPath = "Assets/Stage2_Imported/Player/TribalWarrior.controller";
    public const string Controls = "WASD MOVE   SHIFT SPRINT   SPACE JUMP   Q DASH   LMB LIGHT   RMB HEAVY   ESC PAUSE";

    [MenuItem("Tools/Island Game/Install Jump and Heavy Attack")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
        var current=EditorSceneManager.GetActiveScene();
        if(current.isDirty)throw new InvalidOperationException("Save current scene edits first.");
        string original=current.path;
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Configure(controller);
        foreach(string name in new[]{"Stage2_Battlefield","Stage3_AshenCitadel"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
            var player=Object.FindFirstObjectByType<TribalPlayerController>();
            Undo.RecordObject(player.GetComponent<Animator>(),"Add jump and heavy animation states");
            player.GetComponent<Animator>().runtimeAnimatorController=controller;
            foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(text.name!="ControlsHint")continue;
                Undo.RecordObject(text,"Update player controls hint");text.text=Controls;
                text.enableAutoSizing=true;text.fontSizeMin=10;text.fontSizeMax=18;
                text.enableWordWrapping=false;
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        Debug.Log("Jump and heavy attack installed in both gameplay levels.");
    }

    // Also called by the original player setup so rebuilding the controller keeps
    // these states and the input contract instead of silently reverting controls.
    public static void Configure(AnimatorController controller)
    {
        if(controller==null)throw new ArgumentNullException(nameof(controller));
        Undo.RecordObject(controller,"Configure player action animation");
        AddParameter(controller,"HeavyAttack",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Jump",AnimatorControllerParameterType.Trigger);
        if(!controller.parameters.Any(p=>p.name=="Grounded"))
            controller.AddParameter(new AnimatorControllerParameter{name="Grounded",type=AnimatorControllerParameterType.Bool,defaultBool=true});
        var machine=controller.layers[0].stateMachine;
        var idle=machine.states.First(s=>s.state.name=="Idle").state;
        var heavyClip=LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx");
        var lightClip=LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx");
        // Reserve the committed overhead swing for the heavy action.
        var third=machine.states.First(s=>s.state.name=="Attack3").state;
        third.motion=lightClip;third.speed=1.25f;
        var heavy=State(machine,"HeavyAttack");heavy.motion=heavyClip;heavy.speed=1.15f;
        var jump=State(machine,"Jump");jump.motion=CreateJumpClip((AnimationClip)idle.motion);jump.speed=1f;
        foreach(var transition in machine.anyStateTransitions.Where(t=>t.destinationState==heavy || t.destinationState==jump).ToArray())
            machine.RemoveAnyStateTransition(transition);
        Trigger(machine,heavy,"HeavyAttack",.075f);
        Trigger(machine,jump,"Jump",.08f);
        foreach(var state in new[]{heavy,jump})foreach(var transition in state.transitions.ToArray())state.RemoveTransition(transition);
        var recover=heavy.AddTransition(idle);recover.hasExitTime=true;recover.exitTime=.94f;recover.duration=.10f;
        var land=jump.AddTransition(idle);land.hasExitTime=false;land.duration=.12f;land.AddCondition(AnimatorConditionMode.If,0,"Grounded");
        foreach(var name in new[]{"Idle","Walk","Run"})
        {
            var state=machine.states.First(s=>s.state.name==name).state;
            foreach(var transition in state.transitions.Where(t=>t.destinationState==jump).ToArray())state.RemoveTransition(transition);
            var fall=state.AddTransition(jump);fall.hasExitTime=false;fall.duration=.10f;
            fall.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");
        }
        EditorUtility.SetDirty(controller);
    }

    private static AnimationClip LoadClip(string path)
    {return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));}
    private static void AddParameter(AnimatorController controller,string name,AnimatorControllerParameterType type)
    {if(!controller.parameters.Any(p=>p.name==name))controller.AddParameter(name,type);}
    private static AnimatorState State(AnimatorStateMachine machine,string name)
    {return machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??machine.AddState(name);}
    private static void Trigger(AnimatorStateMachine machine,AnimatorState state,string name,float blend)
    {
        var transition=machine.AddAnyStateTransition(state);transition.hasExitTime=false;
        transition.duration=blend;transition.canTransitionToSelf=false;transition.AddCondition(AnimatorConditionMode.If,0,name);
    }

    private static AnimationClip CreateJumpClip(AnimationClip idle)
    {
        const string path="Assets/Stage2_Imported/Player/TribalJump.anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(clip!=null)return clip;
        clip=Object.Instantiate(idle);clip.name="TribalJump";clip.ClearCurves();
        AssetDatabase.CreateAsset(clip,path);
        // Keep the warrior's weapon-ready hands and neutral body orientation. Key
        // humanoid muscle channels so this pose retargets through the existing avatar.
        foreach(var binding in AnimationUtility.GetCurveBindings(idle))
        {
            if(binding.type!=typeof(Animator) || binding.path!="")continue;
            float value=AnimationUtility.GetEditorCurve(idle,binding).Evaluate(.15f);
            AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,.8f,value));
        }
        PoseCurve(clip,"Left Upper Leg Front-Back",.38f,.10f);
        PoseCurve(clip,"Right Upper Leg Front-Back",.22f,.05f);
        PoseCurve(clip,"Left Lower Leg Stretch",-.58f,-.12f);
        PoseCurve(clip,"Right Lower Leg Stretch",-.42f,-.10f);
        PoseCurve(clip,"Left Foot Up-Down",-.12f,0f);
        PoseCurve(clip,"Right Foot Up-Down",-.12f,0f);
        PoseCurve(clip,"Spine Front-Back",.10f,.03f);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;
        settings.startTime=0;settings.stopTime=.8f;AnimationUtility.SetAnimationClipSettings(clip,settings);
        EditorUtility.SetDirty(clip);return clip;
    }
    private static void PoseCurve(AnimationClip clip,string muscle,float tucked,float extended)
    {
        var binding=EditorCurveBinding.FloatCurve("",typeof(Animator),muscle);
        float initial=AnimationUtility.GetEditorCurve(clip,binding)?.Evaluate(0)??0;
        var curve=new AnimationCurve(new Keyframe(0,initial),new Keyframe(.18f,tucked),new Keyframe(.38f,tucked),new Keyframe(.68f,extended),new Keyframe(.8f,extended));
        for(int i=0;i<curve.length;i++)curve.SmoothTangents(i,0);
        AnimationUtility.SetEditorCurve(clip,binding,curve);
    }
}
