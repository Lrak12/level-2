using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ReviewSwordGrips
{
    public static void Capture()
    {
        if(EditorApplication.isPlaying) throw new Exception("Review outside Play mode.");
        Directory.CreateDirectory("VisualReview/PlatformSword");
        var source=Object.FindFirstObjectByType<TribalPlayerController>();
        var copy=Object.Instantiate(source.gameObject);copy.name="Sword Pose Review";
        var cameraObject=new GameObject("Sword Review Camera",typeof(Camera));
        var camera=cameraObject.GetComponent<Camera>();
        var target=RenderTexture.GetTemporary(1200,1000,24);
        var oldActive=RenderTexture.active;
        var pixels=new Texture2D(1200,1000,TextureFormat.RGB24,false);
        var bakedMesh=new Mesh();
        try
        {
            copy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            foreach(var t in copy.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=31;
            foreach(var c in copy.GetComponentsInChildren<Camera>(true))c.enabled=false;
            var animator=copy.GetComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();animator.Update(0);
            copy.GetComponent<PlayerSwordEquipper>().Equip();
            var skin=copy.GetComponentInChildren<SkinnedMeshRenderer>();
            skin.updateWhenOffscreen=true;
            var baked=new GameObject("Baked pose",typeof(MeshFilter),typeof(MeshRenderer));
            baked.layer=31;baked.transform.SetParent(skin.transform,false);
            baked.GetComponent<MeshFilter>().sharedMesh=bakedMesh;
            baked.GetComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
            skin.enabled=false;
            var right=animator.GetBoneTransform(HumanBodyBones.RightHand).Find("PlayerSword_Right");
            var left=animator.GetBoneTransform(HumanBodyBones.LeftHand).Find("PlayerSword_Left");
            var rightPose=new Pose(right.localPosition,right.localRotation);var leftPose=new Pose(left.localPosition,left.localRotation);
            camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.11f,.13f,.15f);
            camera.orthographic=true;camera.orthographicSize=.95f;camera.nearClipPlane=.05f;camera.farClipPlane=10;
            camera.transform.position=new Vector3(1.5f,1.1f,2.4f);camera.transform.LookAt(new Vector3(0,.8f,0));camera.targetTexture=target;
            var report=new StringBuilder();
            for(int attack=1;attack<=3;attack++)foreach(float phase in new[]{.15f,.35f,.65f,.9f})
            {
                animator.Play("Base Layer.Attack"+attack,0,phase);animator.Update(0);
                skin.BakeMesh(bakedMesh);
                if(!animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Attack"+attack))throw new Exception("Attack state was not sampled");
                foreach(var sword in new[]{right,left})
                {
                    var expected=sword==right?rightPose:leftPose;
                    if(Vector3.Distance(sword.localPosition,expected.position)>.0001f || Quaternion.Angle(sword.localRotation,expected.rotation)>.02f)
                        throw new Exception("Grip changed in attack pose");
                }
                camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1200,1000),0,0);pixels.Apply();
                File.WriteAllBytes("VisualReview/PlatformSword/Attack"+attack+"_"+Mathf.RoundToInt(phase*100)+".png",pixels.EncodeToPNG());
                report.AppendLine("Attack "+attack+" at "+phase+": PASS; both hilts rigidly seated in hand sockets.");
            }
            File.WriteAllText("VisualReview/PlatformSword/SwordValidation.txt",report.ToString());
        }
        finally
        {
            RenderTexture.active=oldActive;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);
            Object.DestroyImmediate(pixels);Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(copy);
            Object.DestroyImmediate(bakedMesh);
        }
    }
}
