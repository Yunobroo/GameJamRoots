Shader "Roots/IllustratedSalvage"
{
    Properties
    {
        _BaseMap("Panel Detail", 2D) = "white" {}
        _BaseColor("Source Color", Color) = (1,1,1,1)
        _PaintColor("Paint", Color) = (0.72,0.62,0.43,1)
        _EmissionMap("Emission", 2D) = "white" {}
        _EmissionColor("Emission", Color) = (0,0,0,1)
        _Cutoff("Cutoff", Float) = 0.5
        [HideInInspector] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _BaseColor, _PaintColor, _EmissionColor;
            float _Cutoff;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; float3 n:TEXCOORD1; float2 uv:TEXCOORD2; float fog:TEXCOORD3; };
            V vert(A v)
            {
                V o; VertexPositionInputs p=GetVertexPositionInputs(v.p.xyz);
                o.p=p.positionCS; o.world=p.positionWS; o.n=TransformObjectToWorldNormal(v.n);
                o.uv=TRANSFORM_TEX(v.uv,_BaseMap); o.fog=ComputeFogFactor(o.p.z); return o;
            }
            half4 frag(V i):SV_Target
            {
                half3 source=SAMPLE_TEXTURE2D_BIAS(_BaseMap,sampler_BaseMap,i.uv,1.25).rgb * _BaseColor.rgb;
                half value=sqrt(saturate(dot(source,half3(0.2126,0.7152,0.0722))));
                half3 ink=half3(0.022,0.036,0.038);
                // Broad paint areas and consistent dark seams replace glossy texture highlights.
                half fill=smoothstep(0.16,0.32,value);
                half tone=0.67 + 0.21*smoothstep(0.34,0.39,value) + 0.12*smoothstep(0.65,0.69,value);
                half3 paint=_PaintColor.rgb*tone;
                half accent=smoothstep(0.07,0.22,source.r-source.g) * (1-smoothstep(0.24,0.40,value));
                paint=lerp(paint,half3(0.65,0.22,0.055),accent*0.8);
                half3 albedo=lerp(ink,paint,fill);
                half3 n=normalize(i.n);
                half light=dot(n,normalize(half3(-0.35,0.8,0.45)))*0.5+0.5;
                half shade=0.68+0.32*smoothstep(0.43,0.55,light);
                half3 shadeColor=lerp(half3(0.62,0.77,0.78),half3(1,0.98,0.9),shade);
                // World-space hatch marks stay attached to the surface when the camera moves.
                float hatchCoordinate=(i.world.x+i.world.y*1.25+i.world.z*0.6)*19;
                float hatchWidth=max(fwidth(hatchCoordinate),0.08);
                half hatch=1-smoothstep(0.08,0.08+hatchWidth,abs(frac(hatchCoordinate)-0.5));
                half grain=sin(i.world.x*47+i.world.z*13)*sin(i.world.y*61+i.world.x*7)*0.018;
                half edge=pow(1-saturate(abs(dot(n,normalize(GetWorldSpaceViewDir(i.world))))),7);
                half3 color=albedo*shadeColor*shade*(1-hatch*(1-shade)*0.32+grain);
                color=lerp(color,ink,edge*0.40);
                half3 emission=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,i.uv).rgb*_EmissionColor.rgb;
                return half4(MixFog(color+min(emission,half3(0.25,0.18,0.08)),i.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
