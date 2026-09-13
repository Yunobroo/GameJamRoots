Shader "Roots/PaintedAdventure"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,1)
        _TextureStrength("Texture Detail", Range(0,1)) = 1.0
        _EmissionMap("Emission", 2D) = "white" {}
        _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _Cutoff("Cutoff", Float) = 0.5
        [HideInInspector] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "CartoonForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _BaseColor, _EmissionColor;
            float _TextureStrength, _Cutoff;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; float fog : TEXCOORD3; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half3 CartoonLight(Light light, half3 normal)
            {
                half n = saturate(dot(normal, light.direction));
                half band = smoothstep(-0.15h, 0.85h, n);
                return light.color * band * light.distanceAttenuation * lerp(0.55h, 1.0h, light.shadowAttenuation);
            }
            half4 frag(Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                half3 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                half3 albedo = lerp(half3(0.72h,0.72h,0.72h), tex, _TextureStrength) * _BaseColor.rgb;
                half gray = dot(albedo, half3(0.2126h,0.7152h,0.0722h));
                half warmMask = smoothstep(0.035h, 0.18h, albedo.r - albedo.b) * smoothstep(0.015h, 0.09h, albedo.g - albedo.b);
                half3 ivory = half3(0.84h,0.86h,0.90h) * (0.25h + 1.1h * sqrt(max(gray, 0.0h)));
                albedo = lerp(albedo, ivory, warmMask * 0.85h);
                half sky = saturate(n.y * 0.5h + 0.5h);
                half3 lighting = lerp(half3(0.30h,0.33h,0.46h), half3(0.66h,0.73h,0.87h), sky);
                lighting += CartoonLight(GetMainLight(TransformWorldToShadowCoord(i.positionWS)), n);
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    lighting += CartoonLight(GetAdditionalLight(lightIndex, i.positionWS), n);
                LIGHT_LOOP_END
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
                return half4(MixFog(albedo * min(lighting, 1.35h) + emission, i.fog), 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}

