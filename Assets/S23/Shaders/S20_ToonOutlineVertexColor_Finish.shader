Shader "CG/S20_ToonOutlineVertexColor_Finish"
{
    Properties
    {
        _BaseColor ("Vertex Color Tint", Color) = (1, 1, 1, 1)
        _Ambient ("Ambient", Range(0, 1)) = 0.2
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess ("Shininess", Range(1, 256)) = 32
        _ToonSteps ("Toon Steps", Range(1, 8)) = 3
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // Keep the material buffer identical in both passes for the SRP Batcher.
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _SpecularColor;
            float4 _OutlineColor;
            float _Ambient;
            float _Shininess;
            float _ToonSteps;
            float _OutlineWidth;
        CBUFFER_END
        ENDHLSL

        // Combine toon lighting and mesh vertex colors in the same surface draw.
        Pass
        {
            Name "ToonVertexColor"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;
                float3 V = normalize(GetCameraPositionWS() - input.positionWS);
                float3 R = reflect(-L, N);

                float steps = max(1.0, _ToonSteps);
                float diffuse = floor(max(0, dot(N, L)) * steps) / steps;
                float specular = pow(max(0, dot(R, V)), _Shininess);
                float3 surfaceColor = input.color.rgb * _BaseColor.rgb;
                float3 color = surfaceColor * (_Ambient + diffuse * mainLight.color)
                             + _SpecularColor.rgb * specular * mainLight.color;
                return float4(color, 1);
            }
            ENDHLSL
        }

        // Same second-pass tag and inverted hull as S20_ToonOutline_Finish.
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 vertOutline(Attributes input) : SV_POSITION
            {
                float3 pushed = input.positionOS + input.normalOS * _OutlineWidth;
                return TransformObjectToHClip(pushed);
            }

            float4 fragOutline() : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
