Shader "Island/Ashen Sky"
{
    Properties
    {
        _Zenith("Upper sky",Color)=(.085,.12,.19,1)
        _Horizon("Ash horizon",Color)=(.38,.29,.25,1)
        _Cloud("Cloud shadow",Color)=(.12,.15,.21,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Zenith,_Horizon,_Cloud;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0; };
            V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.direction=i.positionOS.xyz;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            half4 frag(V i):SV_Target
            {
                float3 d=normalize(i.direction);float h=saturate(d.y);
                half3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(h,.5));
                float2 p=d.xz/(max(d.y,.08)+.2)*2.2+_Time.y*float2(.0013,.0005);
                float n=noise(p)*.55+noise(p*2.03)*.27+noise(p*4.1)*.13;
                float cloud=smoothstep(.37,.72,n)*smoothstep(0,.24,h);
                color=lerp(color,_Cloud.rgb,cloud*.65);
                float halo=pow(saturate(dot(d,normalize(float3(-.4,.13,.8)))),24);
                color+=half3(.16,.065,.018)*halo;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
