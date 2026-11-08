Shader "Mid/Mid_GroundMist"
{
    Properties
    {
        _BaseColor("Mist color", Color) = (.19,.3,.32,.12)
        _Speed("Drift", Float) = .025
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Tags {"LightMode"="SRPDefaultUnlit"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _Speed;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;half fog:TEXCOORD2;};
            float hash(float2 p) {return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p)
            {
                float2 i=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            Varyings vert(Attributes v)
            {
                Varyings o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);
                o.uv=v.uv;o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                float2 p=i.world.xz*.32+float2(_Time.y*_Speed,_Time.y*_Speed*.4);
                float n=noise(p)*.65+noise(p*2.8)*.35;
                float2 edge=smoothstep(0,.22,i.uv)*smoothstep(0,.22,1-i.uv);
                // 카메라가 평면에 가까울 때 불투명한 선처럼 보이는 현상을 완화.
                float fade=saturate(distance(_WorldSpaceCameraPos,i.world)*.35);
                return half4(MixFog(_BaseColor.rgb,i.fog),_BaseColor.a*smoothstep(.2,.75,n)*edge.x*edge.y*fade);
            }
            ENDHLSL
        }
    }
}
