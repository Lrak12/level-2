Shader "Island/Living Surface"
{
    Properties
    {
        _DeepColor("Deep color",Color)=(.015,.14,.23,1)
        _LightColor("Light color",Color)=(.06,.48,.55,1)
        _GlowColor("Foam / molten glow",Color)=(.5,.85,.85,1)
        _Lava("Lava mode",Float)=0
        _Scale("Pattern scale",Float)=.6
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _DeepColor, _LightColor, _GlowColor;
            float _Lava, _Scale;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; half fog:TEXCOORD1; };
            float2 hash(float2 p) { return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453); }
            float cells(float2 p) {
                float2 id=floor(p), uv=frac(p); float a=8,b=8;
                [unroll] for(int y=-1;y<=1;y++) [unroll] for(int x=-1;x<=1;x++) {
                    float2 off=float2(x,y); float2 delta=off+hash(id+off)-uv; float d=dot(delta,delta);
                    if(d<a){b=a;a=d;}else if(d<b)b=d;
                }
                return sqrt(b)-sqrt(a);
            }
            V vert(A i) { V o; o.world=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); o.fog=ComputeFogFactor(o.positionCS.z); return o; }
            half4 frag(V i):SV_Target {
                float2 p=i.world.xz*_Scale; float t=_Time.y;
                half3 color;
                if(_Lava>.5) {
                    float2 flow=p+float2(sin(p.y*.55+t*.19),cos(p.x*.63-t*.15))*.18;
                    float edge=cells(flow);
                    float cracks=1-smoothstep(.025,.14,edge);
                    float ember=.7+.3*sin(p.x*2+p.y*1.6+t*.7);
                    color=lerp(_DeepColor.rgb,_LightColor.rgb,saturate(edge*1.5));
                    color+=_GlowColor.rgb*cracks*(2.1+ember);
                } else {
                    float wave=sin(p.x*1.3+t*.7+sin(p.y*.67-t*.5))*.5+.5;
                    float wave2=sin(p.y*1.7-t*.6+sin(p.x*.55))*.5+.5;
                    float bands=pow(saturate(wave*wave2),9);
                    float3 normal=normalize(float3(cos(p.x*1.3+t*.7)*.13,1,cos(p.y*1.7-t*.6)*.12));
                    float3 view=normalize(GetWorldSpaceViewDir(i.world));
                    float fresnel=pow(1-saturate(dot(normal,view)),4);
                    float spec=pow(saturate(dot(reflect(-normalize(float3(-.4,1,.3)),normal),view)),96);
                    color=lerp(_DeepColor.rgb,_LightColor.rgb,.25+wave*.18+fresnel*.5);
                    color+=_GlowColor.rgb*(bands*.32+spec*.65);
                }
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}
