Shader "Mid/Mid_ScreenStatic"
{
    Properties
    {
        [PerRendererData] _MainTex("UI texture",2D)="white"{}
        _Intensity("Burst intensity",Range(0,1))=0
        _Seed("Seed",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Overlay" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True"}
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Intensity,_Seed;
            CBUFFER_END
            struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            V vert(A i) {V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
            float hash(float2 p) {return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            half4 frag(V i):SV_Target
            {
                float tick=floor(_Time.y*36)+_Seed;
                float grain=hash(floor(i.uv*_ScreenParams.xy*.75)+tick);
                float stripe=step(.93,hash(float2(floor(i.uv.y*100),tick)));
                float scan=.5+.5*sin(i.uv.y*_ScreenParams.y*3.14159);
                float tear=step(.982,hash(float2(floor(i.uv.y*23),tick)));
                float alpha=saturate(_Intensity*(.025+.16*grain+.10*stripe+.10*tear+.025*scan));
                float3 ink=lerp(float3(.025,.045,.05),float3(.72,.80,.78),step(.71,grain));
                ink+=float3(.06,0,.025)*stripe;
                return half4(ink,alpha);
            }
            ENDHLSL
        }
    }
}
