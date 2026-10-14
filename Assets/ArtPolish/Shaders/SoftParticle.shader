Shader "Island/Soft Particle"
{
    Properties { _BaseColor("Tint", Color) = (1,1,1,1) _DstBlend("Destination blend", Float) = 1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha [_DstBlend]
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; half fog:TEXCOORD1; };
            V vert(A a) { V o; o.positionCS=TransformObjectToHClip(a.positionOS.xyz); o.uv=a.uv; o.color=a.color*_BaseColor; o.fog=ComputeFogFactor(o.positionCS.z); return o; }
            half4 frag(V i):SV_Target {
                float r=length(i.uv*2-1);
                half alpha=pow(saturate(1-r*r),3)*i.color.a;
                return half4(MixFogColor(i.color.rgb,half3(0,0,0),i.fog),alpha);
            }
            ENDHLSL
        }
    }
}
