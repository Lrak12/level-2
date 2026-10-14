// Draws animated menu shading, light shafts, smoke, and sparks as one custom UI mesh.
// Study guide and script map: CODE_GUIDE.md in the project root.

using UnityEngine;
using UnityEngine.UI;

/// <summary>One UI mesh for smoke, sun shafts and windblown sparks. Never blocks input.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MenuAtmosphere : MaskableGraphic
{
    private float elapsed;
    // Initialize the base UI graphic and disable raycasts so these decorative effects do not intercept clicks.
    protected override void Awake() { base.Awake(); raycastTarget = false; }
    // Advance the effect using unscaled time so it can animate independently of gameplay pause.
    private void Update() { EvaluateAt(elapsed + Time.unscaledDeltaTime); }
    // Set the effect's animation time and mark its vertices dirty, asking Unity to rebuild the UI mesh.
    public void EvaluateAt(float seconds) { elapsed = seconds; SetVerticesDirty(); }

    // Unity calls this when rebuilding the graphic. Generate shaded strips, light shafts, smoke puffs, and sparks.
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        // Soft side shading leaves the warrior visible and supports the fixed menu.
        for (int i = 0; i < 16; i++)
        {
            float x0=i/16f, x1=(i+1)/16f;
            Shade(mesh,rect,x0,x1,Darkness(x0),Darkness(x1));
        }
        for(int i=0;i<3;i++)
        {
            float sway=Mathf.Sin(elapsed*.09f+i)*.035f;
            Vector2 origin=At(rect,new Vector2(.055f+i*.05f,1.14f));
            Vector2 end=At(rect,new Vector2(.39f+i*.12f+sway,-.12f));
            Wisp(mesh,origin,end,rect.width*.035f,new Color(1f,.77f,.36f,.025f));
        }
        for(int i=0;i<7;i++)
        {
            float x=Mathf.Repeat(i*.177f+elapsed*(.004f+i*.0004f),1.4f)-.2f;
            float y=.10f+i*.075f+Mathf.Sin(elapsed*.10f+i*1.3f)*.023f;
            Puff(mesh,At(rect,new Vector2(x,y)),new Vector2(rect.width*.21f,rect.height*.045f),
                new Color(.81f,.72f,.56f,.034f+.013f*Mathf.Sin(elapsed*.17f+i)));
        }
        for(int i=0;i<34;i++)
        {
            float seed=Hash(i+1);
            float progress=Mathf.Repeat(elapsed*(.026f+seed*.023f)+Hash(i+71),1f);
            float x=Mathf.Repeat(Hash(i+17)+progress*.20f+Mathf.Sin(elapsed*.3f+i)*.018f,1f);
            float y=progress*1.18f-.09f;
            float fade=Mathf.Sin(progress*Mathf.PI);
            float radius=rect.height*(.0008f+seed*.0013f);
            var point=At(rect,new Vector2(x,y));
            Puff(mesh,point,new Vector2(radius*5,radius*5),new Color(1f,.38f,.035f,fade*.14f));
            Puff(mesh,point,new Vector2(radius,radius*1.7f),new Color(1f,.78f,.36f,fade*(.4f+seed*.4f)));
        }
    }

    // Turn an integer seed into a repeatable value between zero and one to vary sparks without storing random state.
    private static float Hash(int n) { return Mathf.Repeat(Mathf.Sin(n*127.1f)*43758.5453f,1f); }
    // Convert proportional coordinates into local coordinates inside the UI rectangle.
    private static Vector2 At(Rect r,Vector2 uv) { return new Vector2(r.xMin+uv.x*r.width,r.yMin+uv.y*r.height); }
    // Return shading opacity at a horizontal position, making the menu side darker for readability.
    private static float Darkness(float x) { return .12f + .58f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,.84f,x)); }
    // Add one rectangular shading strip with different opacity at its left and right edges.
    private static void Shade(VertexHelper mesh,Rect r,float x0,float x1,float a0,float a1)
    {
        int start=mesh.currentVertCount;
        mesh.AddVert(At(r,new Vector2(x0,0)),new Color(.025f,.021f,.018f,a0),Vector2.zero);
        mesh.AddVert(At(r,new Vector2(x0,1)),new Color(.025f,.021f,.018f,a0),Vector2.zero);
        mesh.AddVert(At(r,new Vector2(x1,1)),new Color(.025f,.021f,.018f,a1),Vector2.zero);
        mesh.AddVert(At(r,new Vector2(x1,0)),new Color(.025f,.021f,.018f,a1),Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
    }
    // Build a soft elliptical puff from a colored center and transparent edge vertices, joined by triangles.
    private static void Puff(VertexHelper mesh,Vector2 center,Vector2 radius,Color tint)
    {
        const int segments=16;int start=mesh.currentVertCount;
        mesh.AddVert(center,tint,Vector2.zero);Color edge=tint;edge.a=0;
        for(int i=0;i<segments;i++)
        {
            float angle=i*Mathf.PI*2/segments;
            mesh.AddVert(center+new Vector2(Mathf.Cos(angle)*radius.x,Mathf.Sin(angle)*radius.y),edge,Vector2.zero);
        }
        for(int i=0;i<segments;i++)mesh.AddTriangle(start,start+1+i,start+1+(i+1)%segments);
    }
    // Build a tapered, transparent-edged light shaft between the supplied start and end points.
    private static void Wisp(VertexHelper mesh,Vector2 start,Vector2 end,float width,Color tint)
    {
        Vector2 perpendicular=new Vector2(-(end-start).y,(end-start).x).normalized*width;
        int i=mesh.currentVertCount;Color transparent=tint;transparent.a=0;
        mesh.AddVert(start,transparent,Vector2.zero);mesh.AddVert(end-perpendicular,transparent,Vector2.zero);
        mesh.AddVert(end,tint,Vector2.zero);mesh.AddVert(end+perpendicular,transparent,Vector2.zero);
        mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
    }
}
