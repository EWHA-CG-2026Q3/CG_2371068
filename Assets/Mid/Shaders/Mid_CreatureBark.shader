Shader "Mid/Mid_CreatureBark"
{
    Properties
    {
        _BaseMap("Wood grain",2D)="white"{}
        _BaseColor("Weathered bark",Color)=(.48,.43,.34,1)
        _Smoothness("Smoothness",Range(0,1))=.14
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        half _Smoothness;
        CBUFFER_END
        struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
        struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
        V vert(A i)
        {
            V o;VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);
            o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(i.normalOS);
            o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.fog=ComputeFogFactor(p.positionCS.z);return o;
        }
        ENDHLSL
        Pass
        {
            Name "BarkForward"
            Tags {"LightMode"="UniversalForwardOnly"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            half4 frag(V i):SV_Target
            {
                half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                half grain=dot(tex,half3(.299,.587,.114));
                half grooves=pow(abs(sin(i.uv.x*75+sin(i.uv.y*11)*.55)),8);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=_BaseColor.rgb*lerp(.65,1.3,grain)*lerp(.62,1,grooves);
                surface.smoothness=_Smoothness;surface.specular=half3(.1,.1,.1);
                surface.normalTS=half3(0,0,1);surface.occlusion=.9;surface.alpha=1;
                InputData data=(InputData)0;
                data.positionWS=i.positionWS;data.normalWS=NormalizeNormalPerPixel(i.normalWS);
                data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                data.shadowCoord=TransformWorldToShadowCoord(i.positionWS);data.bakedGI=SampleSH(data.normalWS);
                data.vertexLighting=VertexLighting(i.positionWS,data.normalWS);
                data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);data.shadowMask=half4(1,1,1,1);
                half4 color=UniversalFragmentPBR(data,surface);color.rgb=MixFog(color.rgb,i.fog);return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "BarkShadow" Tags {"LightMode"="ShadowCaster"}
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection,_LightPosition;
            float4 shadowVert(A i):SV_POSITION
            {
                float3 p=TransformObjectToWorld(i.positionOS.xyz),n=TransformObjectToWorldNormal(i.normalOS);
                float3 direction=_LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                direction=normalize(_LightPosition-p);
                #endif
                float4 clip=TransformWorldToHClip(ApplyShadowBias(p,n,direction));
                #if UNITY_REVERSED_Z
                clip.z=min(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
                #else
                clip.z=max(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
                #endif
                return clip;
            }
            half4 shadowFrag():SV_Target {return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "BarkDepth" Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment depthFrag
            half4 depthFrag(V i):SV_Target {return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "BarkNormals" Tags {"LightMode"="DepthNormalsOnly"}
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment normalsFrag
            half4 normalsFrag(V i):SV_Target {return half4(NormalizeNormalPerPixel(i.normalWS),0);}
            ENDHLSL
        }
    }
}
